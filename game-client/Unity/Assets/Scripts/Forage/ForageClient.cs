using System;
using System.Threading.Tasks;
using UnityEngine;

// หาของป่า (forage.lua): which spots in the forest behind the tambon canal have grown back, and
// picking one. What you pick sells at the community shop with your fish and rice.
[RequireComponent(typeof(NakamaConnection))]
public class ForageClient : MonoBehaviour
{
    [Serializable]
    public class Spot
    {
        public string id, kind;
        public int ready_in; // seconds until it has grown back (0 = ready)
    }

    [Serializable]
    public class Kind
    {
        public string id, name, verb;
        public int price;
    }

    [Serializable]
    public class State
    {
        public Spot[] spots;
        public Kind[] kinds;
        public int energy, got, value;
        public string name, kind;
    }

    [Serializable] private class PickRequest { public string spot; }

    public static ForageClient Instance { get; private set; }
    public State Current { get; private set; }
    private float receivedAt; // Time.time when Current arrived (ready_in counts down from there)
    private NakamaConnection connection;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnEnable()
    {
        connection.Connected += OnConnected;
        if (connection.IsConnected) OnConnected(connection);
    }

    private void OnDisable() => connection.Connected -= OnConnected;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try { await Call("forage_get"); }
        catch (Exception e) { Debug.LogWarning($"[Forage] {e.Message}"); }
    }

    public Task<State> PickAsync(string spot) => Call("forage_pick", JsonUtility.ToJson(new PickRequest { spot = spot }));

    // Seconds until this spot can be picked again (0 = now); -1 while we don't know yet
    public float ReadyIn(string spot)
    {
        if (Current?.spots == null) return -1f;
        foreach (var s in Current.spots)
            if (s.id == spot) return Mathf.Max(0f, s.ready_in - (Time.time - receivedAt));
        return -1f;
    }

    public Kind KindOf(string kind)
    {
        if (Current?.kinds == null) return null;
        foreach (var k in Current.kinds) if (k.id == kind) return k;
        return null;
    }

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        Current = s;
        receivedAt = Time.time;
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message ?? "";
        if (m.Contains("not enough energy")) return "พลังงานไม่พอ — พักหรือกินข้าวก่อน";
        if (m.Contains("sleeping")) return "กำลังนอนอยู่";
        if (m.Contains("not grown")) return "ยังไม่ขึ้นใหม่ รอสักพัก";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }
}
