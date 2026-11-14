using System;
using System.Threading.Tasks;
using UnityEngine;

// ขวาน (woodcut.lua): which trees in the forest behind the tambon canal have grown back, felling one
// for ท่อนไม้, and splitting logs into ฟืน at the chopping block. Both sell at the community shop.
[RequireComponent(typeof(NakamaConnection))]
public class WoodClient : MonoBehaviour
{
    [Serializable] public class Tree { public string id, kind; public int ready_in; }
    [Serializable] public class Kind { public string id, name, verb; public int energy, min, max; }

    [Serializable]
    public class State
    {
        public Tree[] trees;
        public Kind[] kinds;
        public bool has_axe;
        public int logs, firewood, log_price, firewood_price, split_energy, split_gives, split_max;
        public int got, split;
        public string name, kind;
    }

    [Serializable] private class ChopRequest { public string tree; }
    [Serializable] private class SplitRequest { public int count; }

    public static WoodClient Instance { get; private set; }
    public State Current { get; private set; }
    private float receivedAt;
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
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Wood] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => Call("wood_get");
    public Task<State> ChopAsync(string tree) => Call("wood_chop", JsonUtility.ToJson(new ChopRequest { tree = tree }));
    public Task<State> SplitAsync(int count) => Call("wood_split", JsonUtility.ToJson(new SplitRequest { count = count }));

    // Seconds until this tree has grown back (0 = standing); -1 while we don't know yet
    public float ReadyIn(string tree)
    {
        if (Current?.trees == null) return -1f;
        foreach (var t in Current.trees)
            if (t.id == tree) return Mathf.Max(0f, t.ready_in - (Time.time - receivedAt));
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
        if (m.Contains("no axe")) return "ต้องมีขวานก่อน — ซื้อขวานผ่าฟืนที่ร้านค้าชุมชน";
        if (m.Contains("not enough energy")) return "พลังงานไม่พอ — พักหรือกินข้าวก่อน";
        if (m.Contains("sleeping")) return "กำลังนอนอยู่";
        if (m.Contains("not grown")) return "ยังไม่ขึ้นใหม่ รอสักพัก";
        if (m.Contains("not enough logs")) return "ท่อนไม้ไม่พอ — ไปโค่นต้นไม้ก่อน";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }
}
