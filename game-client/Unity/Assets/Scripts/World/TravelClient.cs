using System;
using System.Threading.Tasks;
using UnityEngine;

// Tickets to other provinces (travel.lua): fares from here by bus, train or plane, and buying one.
public static class TravelClient
{
    [Serializable] public class Trip { public string to; public int fare; public float hours; }
    [Serializable] public class Quote { public string mode, from; public Trip[] trips; }
    [Serializable] public class Ticket { public string to, mode, done; public int fare; public float hours; }

    [Serializable] private class QuoteRequest { public string from, mode; }
    [Serializable] private class BuyRequest { public string from, to, mode; }

    private static NakamaConnection Connection => UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();

    public static async Task<Quote> QuoteAsync(string from, string mode)
    {
        var q = JsonUtility.FromJson<Quote>(await Call("travel_quote", JsonUtility.ToJson(new QuoteRequest { from = from, mode = mode })));
        q.trips ??= new Trip[0];
        return q;
    }

    public static async Task<Ticket> BuyAsync(string from, string to, string mode)
    {
        var t = JsonUtility.FromJson<Ticket>(await Call("travel_buy", JsonUtility.ToJson(new BuyRequest { from = from, to = to, mode = mode })));
        if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return t;
    }

    // เรือข้ามฟาก: pay the flat fare; the fare paid comes back
    public static async Task<int> FerryAsync()
    {
        var r = JsonUtility.FromJson<FerryFare>(await Call("ferry_ride", "{}"));
        if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return r.fare;
    }

    [Serializable] private class FerryFare { public int fare; }

    private static async Task<string> Call(string rpc, string payload)
    {
        var conn = Connection;
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        return result.Payload;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("offline") ? "ต้องออนไลน์ก่อนซื้อตั๋ว"
            : m.Contains("not enough baht") ? "เงินไม่พอซื้อตั๋ว"
            : m.Contains("no such trip") ? "ไม่มีเที่ยวนี้"
            : "ซื้อตั๋วไม่สำเร็จ ลองใหม่";
    }

    // "9.5" hours → "9 ชม. 30 นาที"
    public static string Hours(float hours)
    {
        int h = Mathf.FloorToInt(hours);
        bool half = hours - h > 0.25f;
        return h == 0 ? "30 นาที" : half ? $"{h} ชม. 30 นาที" : $"{h} ชม.";
    }
}
