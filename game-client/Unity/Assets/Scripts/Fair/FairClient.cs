using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// The fair ticket system from fair.lua: today's gate pass (บัตรผ่านประตู), ride coupons (คูปอง),
// points (แต้ม) won at the skill games, and the prize counter (ซุ้มแลกรางวัล). Nothing is random:
// a game pays points for what the player hit, a prize costs a fixed number of points.
[RequireComponent(typeof(NakamaConnection))]
public class FairClient : MonoBehaviour
{
    [Serializable]
    public class Game
    {
        public string id;
        public string name;
        public int shots;
        public int[] points; // points[hits] = what a round pays
        public int best;
    }

    [Serializable]
    public class Ride
    {
        public string id;
        public string name;
        public int coupons;
        public int energy;
    }

    [Serializable]
    public class Prize
    {
        public string id;
        public string name;
        public int points;
        public string icon;
        public bool big;
        public int redeemed;
    }

    [Serializable]
    public class Pack
    {
        public string id;
        public int coupons;
        public int price;
    }

    [Serializable]
    public class State
    {
        public bool open;
        public string fair;
        public string venue;     // "big" | "small" | "village"
        public bool has_pass;
        public int pass_price;
        public int pass_coupons;
        public int coupons;
        public int points;
        public Game[] games;
        public Ride[] rides;
        public Prize[] prizes;
        public Pack[] packs;
        // fair_pass / fair_coupons
        public string bought;
        public int got;
        public int paid;
        // fair_ride
        public string ride_name;
        public int used;
        public int gained;
        // fair_start
        public string round;
        public string game;
        public int seed;
        public int shots;
        // fair_finish
        public string game_name;
        public int hits;
        public int earned;
        public bool record;
        // fair_redeem
        public string prize;
        public string icon;
        public bool big;
        public int spent;
    }

    [Serializable] private class PackRequest { public string pack; }
    [Serializable] private class RideRequest { public string ride; }
    [Serializable] private class GameRequest { public string game; }
    [Serializable] private class FinishRequest { public string round; public int hits; }
    [Serializable] private class PrizeRequest { public string prize; }

    public static FairClient Instance { get; private set; }

    public State Current { get; private set; }
    public event Action<State> Changed;

    private NakamaConnection connection;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Game FindGame(string id)
    {
        if (Current?.games != null) foreach (var g in Current.games) if (g.id == id) return g;
        return null;
    }

    public Ride FindRide(string id)
    {
        if (Current?.rides != null) foreach (var r in Current.rides) if (r.id == id) return r;
        return null;
    }

    public Task<State> RefreshAsync() => CallAsync("fair_get");
    public Task<State> BuyPassAsync() => Spend("fair_pass", "{}");
    public Task<State> BuyCouponsAsync(string pack) => Spend("fair_coupons", JsonUtility.ToJson(new PackRequest { pack = pack }));
    public Task<State> RideAsync(string ride) => CallAsync("fair_ride", JsonUtility.ToJson(new RideRequest { ride = ride }));
    public Task<State> StartAsync(string game) => CallAsync("fair_start", JsonUtility.ToJson(new GameRequest { game = game }));
    public Task<State> FinishAsync(string round, int hits) => CallAsync("fair_finish", JsonUtility.ToJson(new FinishRequest { round = round, hits = hits }));
    public Task<State> RedeemAsync(string prize) => CallAsync("fair_redeem", JsonUtility.ToJson(new PrizeRequest { prize = prize }));

    // Buying with baht: the wallet changes too
    private async Task<State> Spend(string rpc, string payload)
    {
        var state = await CallAsync(rpc, payload);
        if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return state;
    }

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        return state;
    }

    // Server errors in words the player understands
    public static string Explain(Exception e)
    {
        string m = e is ApiResponseException ? e.Message : "";
        return m.Contains("no pass") ? "ต้องซื้อบัตรผ่านประตูก่อน ที่ซุ้มขายบัตรหน้างาน"
            : m.Contains("coupons") ? "คูปองไม่พอ ซื้อเพิ่มได้ที่ซุ้มขายบัตร"
            : m.Contains("points") ? "แต้มยังไม่พอ"
            : m.Contains("baht") ? "เงินไม่พอ"
            : m.Contains("already have pass") ? "มีบัตรผ่านประตูงานนี้แล้ว"
            : m.Contains("already own") ? "มีของชิ้นนี้แล้ว"
            : m.Contains("big") ? "มีเฉพาะงานใหญ่ในเมือง"
            : m.Contains("no festival") ? "ยังไม่มีงานตอนนี้"
            : m.Contains("expired") ? "รอบนี้หมดเวลาแล้ว"
            : e is ApiResponseException ? "ทำรายการไม่ได้ตอนนี้" : "เชื่อมต่อเซิร์ฟเวอร์ไม่ได้";
    }
}
