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
    }

    [Serializable]
    public class Kind
    {
        public string id, group, name;
        public int price;
        public float length;
    }

    [Serializable]
    public class State
    {
        public Piece[] pieces;
        public Kind[] catalog;
        public string[] colors;
        public int max, placed;
        public float radius;
        public string done;
    }

    [Serializable] private class PlaceRequest { public string kind; public float x, z, yaw; public int color; }
    [Serializable] private class EditRequest { public int id; public float x, z, yaw; public int color; }
    [Serializable] private class IdRequest { public int id; }

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

    public Task<State> RefreshAsync() => Call("build_get");
    public Task<State> PlaceAsync(string kind, float x, float z, float yaw, int color) =>
        Call("build_place", JsonUtility.ToJson(new PlaceRequest { kind = kind, x = x, z = z, yaw = yaw, color = color }));
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
        return state;
    }
}
