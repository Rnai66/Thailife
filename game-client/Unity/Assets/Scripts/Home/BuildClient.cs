using System;
using System.Threading.Tasks;
using UnityEngine;

// งานก่อสร้าง (build.lua): the fences, gates and other pieces you've built round your home, and the
// catalogue of what can be built. BuiltPieces shows them in the world; BuildUI places and edits them.
[RequireComponent(typeof(NakamaConnection))]
public class BuildClient : MonoBehaviour
{
    [Serializable]
    public class Piece
    {
        public int id;
        public string kind;
        public float x, z, yaw;
        public int color;
        public string site; // empty: round your home; "farm", "plain", "city": another site you hold
    }

    [Serializable]
    public class Kind
    {
        public string id, group, name;
        public int price;
        public float length;
    }

    // Your house lot, relative to the house (build.lua bounds); all zero when you have no lot
    [Serializable]
    public class Area { public float x0, x1, z0, z1; }

    // The other sites you hold (build.lua `sites`), each relative to its middle; null or empty if not yours
    [Serializable]
    public class Sites { public Area farm, plain, city; }

    public static readonly string[] OtherSites = { "farm", "plain", "city" };
    public static string SiteOf(Piece p) => string.IsNullOrEmpty(p.site) ? "home" : p.site;

    [Serializable]
    public class State
    {
        public Area bounds;
        public int outside; // pieces from before house lots that stick out of yours (only you see them)
        public Piece[] pieces;
        public Kind[] catalog;
        public string[] colors;
        public int max, placed;
        public float radius;
        public string done;
        public Sites sites;
        public string anchor; // "village_stilt": the pieces stand round the village's own stilt house (BuiltPieces.AnchorFor)
    }

    [Serializable] private class PlaceRequest { public string kind, site; public float x, z, yaw; public int color; }
    [Serializable] private class EditRequest { public int id; public float x, z, yaw; public int color; }
    [Serializable] private class IdRequest { public int id; }
    [Serializable] private class LotsRequest { public string area; public int moo; }
    [Serializable] public class LotPieces { public int no; public string user_id, anchor; public Piece[] pieces; }
    [Serializable] private class LotsReply { public LotPieces[] lots; }

    public static BuildClient Instance { get; private set; }
    public State Current { get; private set; }
    public int Version { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public event Action<State> Changed;

    private NakamaConnection connection;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
        if (GetComponent<BuiltPieces>() == null) gameObject.AddComponent<BuiltPieces>();
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
        catch (Exception e) { Debug.LogWarning($"[Build] {e.Message}"); }
    }

    public Kind GetKind(string id)
    {
        if (Current?.catalog == null) return null;
        foreach (var k in Current.catalog) if (k.id == id) return k;
        return null;
    }

    public Piece GetPiece(int id)
    {
        if (Current?.pieces == null) return null;
        foreach (var p in Current.pieces) if (p.id == id) return p;
        return null;
    }

    // Whether a spot (relative to the house) is somewhere you may build: your lot, else RADIUS
    public bool Inside(float x, float z)
    {
        var s = Current;
        if (s == null) return false;
        var b = s.bounds;
        if (OnLot) return x >= b.x0 && x <= b.x1 && z >= b.z0 && z <= b.z1;
        return Mathf.Abs(x) <= s.radius && Mathf.Abs(z) <= s.radius;
    }

    public bool OnLot => Current?.bounds != null && Current.bounds.x1 > Current.bounds.x0;

    // Bounds of another site you hold (null if it isn't yours)
    public Area SiteBounds(string site)
    {
        var s = Current?.sites;
        var b = s == null ? null : site == "farm" ? s.farm : site == "plain" ? s.plain : site == "city" ? s.city : null;
        return b != null && b.x1 > b.x0 ? b : null;
    }

    public bool Inside(string site, float x, float z)
    {
        if (site == "home") return Inside(x, z);
        var b = SiteBounds(site);
        return b != null && x >= b.x0 && x <= b.x1 && z >= b.z0 && z <= b.z1;
    }

    private static bool toldOutside;

    public Task<State> RefreshAsync() => Call("build_get");

    // What the people on one หมู่'s house lots built (inside their lots), to draw round their houses
    public async Task<LotPieces[]> LotsAsync(string area, int moo)
    {
        var result = await connection.Client.RpcAsync(connection.Session, "build_lots", JsonUtility.ToJson(new LotsRequest { area = area, moo = moo }));
        return JsonUtility.FromJson<LotsReply>(result.Payload)?.lots ?? new LotPieces[0];
    }
    public Task<State> PlaceAsync(string kind, float x, float z, float yaw, int color, string site = "home") =>
        Call("build_place", JsonUtility.ToJson(new PlaceRequest { kind = kind, site = site, x = x, z = z, yaw = yaw, color = color }));
    public Task<State> EditAsync(Piece p) =>
        Call("build_edit", JsonUtility.ToJson(new EditRequest { id = p.id, x = p.x, z = p.z, yaw = p.yaw, color = p.color }));
    public Task<State> RemoveAsync(int id) => Call("build_remove", JsonUtility.ToJson(new IdRequest { id = id }));

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        if (state.pieces == null) state.pieces = new Piece[0];
        Current = state;
        Version++;
        Changed?.Invoke(state);
        if (state.outside > 0 && !toldOutside)
        {
            toldOutside = true;
            UiKit.FloatAbovePlayer($"ของที่สร้างไว้ {state.outside} ชิ้นอยู่นอกแปลงบ้าน · เพื่อนบ้านจะไม่เห็น ย้ายเข้ามาในแปลงได้ที่เมนูก่อสร้าง", new Color(1f, 0.8f, 0.4f));
        }
        return state;
    }
}
