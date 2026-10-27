using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Fish cages in the Kok Nong Na pond (fishpond.lua): load the three cages and act on one
// (stock with fry, feed, harvest). The last state is kept for the pond's look and the window.
public static class PondClient
{
    [Serializable]
    public class Cage
    {
        public int id;
        public string stage;      // "empty" | "growing" | "grown"
        public string species, name;
        public float progress;
        public int ready_in_s, feeds, feeds_needed;
        public bool fed_today;
    }

    [Serializable]
    public class Species
    {
        public string id, name;
        public int price, per_kg, fed_kg, level;
        public float grow_s;
        public bool unlocked;
    }

    [Serializable]
    public class State
    {
        public Cage[] cages;
        public Species[] species;
        public int feed_price, level;
        public string done;
        public int earned, harvested_kg;
    }

    [Serializable]
    private class ActRequest
    {
        public int cage;
        public string action, species;
    }

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
    public static bool Online => Connection != null && Connection.IsConnected;

    public static Task<State> GetAsync() => Call("pond_get", "{}");

    public static Task<State> ActAsync(int cage, string action, string species = "") =>
        Call("pond_act", JsonUtility.ToJson(new ActRequest { cage = cage, action = action, species = species }));

    private static async Task<State> Call(string rpc, string payload)
    {
        var conn = Connection;
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        Current = s;
        ReceivedAt = Time.time;
        Changed?.Invoke(s);
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("offline") ? "ต้องออนไลน์ก่อน"
            : m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("energy") ? "พลังงานไม่พอ — พักก่อน"
            : m.Contains("level too low") ? "เลเวลเลี้ยงปลายังไม่ถึง"
            : m.Contains("already fed") ? "วันนี้ให้อาหารแล้ว พรุ่งนี้ค่อยให้ใหม่"
            : m.Contains("not grown") ? "ปลายังไม่โต"
            : m.Contains("not empty") ? "กระชังนี้มีปลาอยู่แล้ว"
            : m.Contains("nothing growing") ? "กระชังนี้ยังว่าง"
            : "ทำรายการไม่สำเร็จ ลองใหม่";
    }
}
