using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// The farm lots south of the village (farmlot.lua): every player's own โคกหนองนา — paddies, fish pond,
// cow pen and chicken coop in the corners they chose, behind the fences they built. Each lot someone
// has shows their farm (copies of the zone and fence templates the Editor built, kept inactive); yours
// works (FarmPlot, FishPond, CowPen, the coop), the neighbours' are to look at. The หมู่ shown is the
// village's (HouseLots / HomeClient.MooIn). Refreshed when you arrive, after a change and every minute.
public class FarmLots : MonoBehaviour
{
    [Serializable] public class Fences { public string front = "none", back = "none", left = "none", right = "none"; }
    [Serializable] public class Mine { public int moo, lot; public string[] layout; public Fences fences; public string moo_name, address; }
    [Serializable] public class LotEntry { public int no; public bool vacant, mine; public string owner, owner_id; public string[] layout; public Fences fences; }
    [Serializable] public class Option { public string id, name; public int price; }
    [Serializable] public class View
    {
        public Mine mine;
        public int moo, lot_count, layout_fee;
        public string moo_name, done;
        public LotEntry[] lots;
        public Option[] fences_list, modules;
    }
    [Serializable] private class SetLayout { public string[] layout; }
    [Serializable] private class SetFence { public string side, fence; }
    [Serializable] private class GetRequest { public int moo; }
    [Serializable] private class MoveRequest { public int moo; }

    [SerializeField] private Transform templates;

    private readonly Dictionary<int, FarmLot> lots = new Dictionary<int, FarmLot>();
    private float nextRefresh;
    private bool busy;

    public static FarmLots Current { get; private set; }
    public static View Last { get; private set; }          // the latest answer (with your own lot)
    public static int MyMoo => Last?.mine != null ? Last.mine.moo : 0;
    public static bool Loaded { get; private set; }
    public static event Action<View> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Last = null; Loaded = false; Changed = null; }

    public void Configure(Transform templateRoot) => templates = templateRoot;

    private void Awake()
    {
        Current = this;
        Loaded = false;
        foreach (var lot in GetComponentsInChildren<FarmLot>(true)) lots[lot.Number] = lot;
        if (templates != null) templates.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Start()
    {
        foreach (var lot in lots.Values) lot.Clear(false);
    }

    private void Update()
    {
        if (Time.time < nextRefresh || busy) return;
        nextRefresh = Time.time + 60f;
        _ = Refresh();
    }

    public GameObject Template(string name)
    {
        if (templates == null || string.IsNullOrEmpty(name)) return null;
        var t = templates.Find(name);
        return t != null ? t.gameObject : null;
    }

    private static NakamaConnection Connection()
    {
        var c = FindAnyObjectByType<NakamaConnection>();
        return c != null && c.IsConnected ? c : null;
    }

    public static bool Online => Connection() != null;

    private int ShownMoo()
    {
        var houses = HouseLots.Current;
        return houses != null ? houses.Moo : (MyMoo > 0 ? MyMoo : 1);
    }

    public async Task Refresh()
    {
        var conn = Connection();
        if (conn == null) return;
        busy = true;
        try
        {
            // the first call (moo 0) gives you a lot and says which หมู่ it's in
            int moo = Loaded ? ShownMoo() : 0;
            var result = await conn.Client.RpcAsync(conn.Session, "farmlot_get", JsonUtility.ToJson(new GetRequest { moo = moo }));
            var view = JsonUtility.FromJson<View>(result.Payload);
            bool first = !Loaded;
            Accept(view);
            if (this == null) return;
            if (first && view.moo != ShownMoo())
            {
                result = await conn.Client.RpcAsync(conn.Session, "farmlot_get", JsonUtility.ToJson(new GetRequest { moo = ShownMoo() }));
                Accept(JsonUtility.FromJson<View>(result.Payload));
            }
        }
        catch (Exception e) { Debug.LogWarning($"[FarmLots] {e.Message}"); }
        finally { busy = false; }
    }

    private void Accept(View view)
    {
        if (!Loaded && view?.mine != null && HomeClient.LivesIn("village") && view.mine.moo != HomeClient.MyMoo)
            UiKit.FloatAbovePlayer($"แปลงเกษตรของคุณอยู่{view.mine.moo_name} · ย้ายมาหมู่บ้านของคุณได้ที่เมนูย้ายบ้าน", new Color(1f, 0.8f, 0.4f));
        Last = view;
        Loaded = true;
        if (this != null) Apply(view);
        Changed?.Invoke(view);
    }

    private void Apply(View view)
    {
        if (view?.lots == null || view.moo != ShownMoo()) return;
        foreach (var l in view.lots)
        {
            if (!lots.TryGetValue(l.no, out var lot)) continue;
            if (l.vacant) lot.Clear(true);
            else lot.Show(this, l.layout, l.fences, l.owner, l.mine);
        }
    }

    // Your own lot in the หมู่ on show (null when your farm is in another หมู่)
    public FarmLot MyLot()
    {
        if (Last?.mine == null || Last.mine.moo != ShownMoo()) return null;
        return lots.TryGetValue(Last.mine.lot, out var lot) ? lot : null;
    }

    public static Task<View> SetLayoutAsync(string[] layout) => Set(JsonUtility.ToJson(new SetLayout { layout = layout }));
    public static Task<View> SetFenceAsync(string side, string fence) => Set(JsonUtility.ToJson(new SetFence { side = side, fence = fence }));

    private static async Task<View> Set(string payload)
    {
        var conn = Connection();
        if (conn == null) throw new Exception("offline");
        var result = await conn.Client.RpcAsync(conn.Session, "farmlot_set", payload);
        var view = JsonUtility.FromJson<View>(result.Payload);
        if (Current != null) Current.Accept(view);
        else { Last = view; Changed?.Invoke(view); }
        return view;
    }

    // Your farm lot wherever you are (e.g. the move-house window): the first answer gives you one if needed
    public static async Task<View> FetchMineAsync()
    {
        var conn = Connection();
        if (conn == null) return Last;
        var result = await conn.Client.RpcAsync(conn.Session, "farmlot_get", JsonUtility.ToJson(new GetRequest { moo = 0 }));
        var view = JsonUtility.FromJson<View>(result.Payload);
        Last = view;
        Changed?.Invoke(view);
        return view;
    }

    // Move your farm (free): a village resident to their own หมู่; others to `moo`
    public static async Task<View> MoveAsync(int moo = 0)
    {
        var conn = Connection();
        if (conn == null) throw new Exception("offline");
        var result = await conn.Client.RpcAsync(conn.Session, "farmlot_move", JsonUtility.ToJson(new MoveRequest { moo = moo }));
        var view = JsonUtility.FromJson<View>(result.Payload);
        Last = view;
        if (Current != null) { Current.nextRefresh = 0f; foreach (var lot in Current.lots.Values) lot.Clear(false); }
        Changed?.Invoke(view);
        return view;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message ?? "";
        if (m.Contains("not enough baht")) return "เงินไม่พอ";
        if (m.Contains("same layout")) return "จัดโซนแบบนี้อยู่แล้ว";
        if (m.Contains("same fence")) return "ด้านนี้เป็นรั้วแบบนี้อยู่แล้ว";
        if (m.Contains("offline")) return "ต้องออนไลน์ก่อน";
        if (m.Contains("farms full")) return "หมู่นี้แปลงเกษตรเต็มแล้ว";
        if (m.Contains("same farm")) return "แปลงเกษตรอยู่หมู่นี้อยู่แล้ว";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }
}
