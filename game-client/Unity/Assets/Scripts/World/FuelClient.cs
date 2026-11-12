using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// น้ำมัน (fuel.lua): what's in each of your vehicles' tanks, filling up at a petrol station, the
// fuel burnt driving round, and driving yourself to another place. Keeps the last known litres per
// vehicle so the gauge can run down between reports.
public static class FuelClient
{
    [Serializable] public class Fuel { public string name; public float price; }
    [Serializable] public class Fuels { public Fuel g95, diesel; }
    [Serializable] public class Car { public string id, name, fuel; public float litres, tank, kmpl; }
    [Serializable] public class State { public Fuels fuels; public Car[] cars; }
    [Serializable] public class Bought { public string car; public float litres, added, tank; public int paid; }
    [Serializable] public class Used { public string car; public float litres; }
    [Serializable] public class Trip { public string to; public float km, litres, hours; }
    [Serializable] public class Quote { public string car; public float litres, tank; public Trip[] trips; }
    [Serializable] public class Drive { public string to; public float km, litres, left, hours; public int seconds; }

    [Serializable] private class BuyRequest { public string car; public int baht; public bool full; }
    [Serializable] private class UseRequest { public string car; public float metres; }
    [Serializable] private class QuoteRequest { public string from, car; }
    [Serializable] private class GoRequest { public string from, to, car; }

    public const float WorldScale = 1.6f; // fuel.lua WORLD_SCALE: road metres per metre of the map

    public static State Last { get; private set; }
    private static readonly Dictionary<string, Car> cars = new Dictionary<string, Car>();

    public static event Action Changed;

    // Litres in a vehicle's tank as last known (or -1 not known yet); tank 0 = needs no fuel
    public static float Litres(string id) => cars.TryGetValue(id ?? "", out var c) ? c.litres : -1f;
    public static Car Info(string id) => cars.TryGetValue(id ?? "", out var c) ? c : null;

    public static float Price(string fuel) =>
        Last?.fuels == null ? 0f : fuel == "diesel" ? Last.fuels.diesel?.price ?? 0f : Last.fuels.g95?.price ?? 0f;

    public static string FuelName(string fuel) =>
        Last?.fuels == null ? "" : fuel == "diesel" ? Last.fuels.diesel?.name : Last.fuels.g95?.name;

    // Driving round: burn it down locally (the server is told now and then by Use)
    public static void Burn(string id, float metres)
    {
        if (!cars.TryGetValue(id ?? "", out var c) || c.kmpl <= 0f) return;
        c.litres = Mathf.Max(0f, c.litres - metres * WorldScale / 1000f / c.kmpl);
    }

    public static async Task<State> GetAsync()
    {
        var s = JsonUtility.FromJson<State>(await Call("fuel_get", "{}"));
        s.cars ??= new Car[0];
        Last = s;
        cars.Clear();
        foreach (var c in s.cars) cars[c.id] = c;
        Changed?.Invoke();
        return s;
    }

    public static async Task<Bought> BuyAsync(string car, int baht, bool full)
    {
        var b = JsonUtility.FromJson<Bought>(await Call("fuel_buy", JsonUtility.ToJson(new BuyRequest { car = car, baht = baht, full = full })));
        if (cars.TryGetValue(car, out var c)) c.litres = b.litres;
        if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        Changed?.Invoke();
        return b;
    }

    public static async Task UseAsync(string car, float metres)
    {
        try
        {
            var u = JsonUtility.FromJson<Used>(await Call("fuel_use", JsonUtility.ToJson(new UseRequest { car = car, metres = metres })));
            if (cars.TryGetValue(car, out var c)) c.litres = Mathf.Min(c.litres, u.litres);
            Changed?.Invoke();
        }
        catch (Exception e) { Debug.LogWarning("[Fuel] use: " + e.Message); }
    }

    public static async Task<Quote> QuoteAsync(string from, string car)
    {
        var q = JsonUtility.FromJson<Quote>(await Call("drive_quote", JsonUtility.ToJson(new QuoteRequest { from = from, car = car })));
        q.trips ??= new Trip[0];
        if (cars.TryGetValue(car, out var c)) c.litres = q.litres;
        return q;
    }

    public static async Task<Drive> GoAsync(string from, string to, string car)
    {
        var d = JsonUtility.FromJson<Drive>(await Call("drive_go", JsonUtility.ToJson(new GoRequest { from = from, to = to, car = car })));
        if (cars.TryGetValue(car, out var c)) c.litres = d.left;
        Changed?.Invoke();
        return d;
    }

    private static async Task<string> Call(string rpc, string payload)
    {
        var conn = UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        return result.Payload;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("offline") ? "ต้องออนไลน์ก่อน"
            : m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("not enough fuel") ? "น้ำมันไม่พอไปถึง เติมก่อนนะ"
            : m.Contains("tank full") ? "ถังเต็มแล้ว"
            : m.Contains("no tank") ? "จักรยานไม่ต้องเติมน้ำมัน"
            : m.Contains("not your car") ? "ยังไม่มีรถคันนี้"
            : "ไม่สำเร็จ ลองใหม่";
    }

    // "1.5" hours → "1 ชม. 30 นาที"
    public static string Hours(float hours)
    {
        int total = Mathf.RoundToInt(hours * 60f);
        int h = total / 60, m = total % 60;
        return h == 0 ? $"{m} นาที" : m == 0 ? $"{h} ชม." : $"{h} ชม. {m} นาที";
    }
}
