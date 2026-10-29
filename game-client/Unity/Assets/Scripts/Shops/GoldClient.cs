using System;
using System.Threading.Tasks;
using UnityEngine;

// Gold shop (ร้านทอง) and pawn shop (โรงรับจำนำ) — gold.lua. The gold price moves every game hour;
// buy low and sell high in ครึ่งสลึง. Pawn gold, spare instruments or bank assets for a loan at
// their market price and redeem the ticket with interest before it runs out.
public static class GoldClient
{
    [Serializable]
    public class Price
    {
        public int mid, sell_baht, buy_baht, unit_sell, unit_buy, change, next_s;
        public int[] history;
    }

    [Serializable]
    public class Ticket
    {
        public int id, loan, market, interest, redeem, days_left;
        public string name, kind;
    }

    [Serializable]
    public class Pawnable
    {
        public string kind, @ref, name;
        public int market, loan, units, unit_loan;
    }

    [Serializable]
    public class State
    {
        public Price price;
        public int units, cost, value, profit, baht, units_per_baht, max_units;
        public string unit_name;
        public Ticket[] tickets;
        public Pawnable[] pawnables;
        public float rate_month;
        public int term_days, max_tickets;
        public string done;
    }

    [Serializable] private class TradeRequest { public string side; public int units; }
    [Serializable] private class PawnRequest { public string action, kind, @ref; public int units, ticket; }

    public static State Current { get; private set; }
    public static float ReceivedAt { get; private set; }
    public static event Action<State> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }

    private static NakamaConnection Connection => UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();

    public static Task<State> GetAsync() => Call("gold_get", "{}");

    public static Task<State> TradeAsync(string side, int units) =>
        Call("gold_trade", JsonUtility.ToJson(new TradeRequest { side = side, units = units }));

    public static Task<State> PawnAsync(Pawnable item, int units) =>
        Call("pawn_act", JsonUtility.ToJson(new PawnRequest { action = "pawn", kind = item.kind, @ref = item.@ref, units = units }));

    public static Task<State> RedeemAsync(int ticket) =>
        Call("pawn_act", JsonUtility.ToJson(new PawnRequest { action = "redeem", ticket = ticket }));

    private static async Task<State> Call(string rpc, string payload)
    {
        var conn = Connection;
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        s.tickets ??= new Ticket[0];
        s.pawnables ??= new Pawnable[0];
        Current = s;
        ReceivedAt = Time.time;
        Changed?.Invoke(s);
        if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("offline") ? "ต้องออนไลน์ก่อน"
            : m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("not enough gold") ? "ทองไม่พอ"
            : m.Contains("too much gold") ? "เก็บทองได้ไม่เกิน 100 บาททอง"
            : m.Contains("too many tickets") ? "ตั๋วจำนำเต็มแล้ว — ไถ่ถอนก่อน"
            : m.Contains("not owned") ? "ไม่มีของชิ้นนี้แล้ว"
            : m.Contains("no ticket") ? "ไม่พบตั๋วจำนำ"
            : "ทำรายการไม่สำเร็จ ลองใหม่";
    }
}
