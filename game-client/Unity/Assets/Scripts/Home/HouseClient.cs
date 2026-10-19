using System;
using System.Threading.Tasks;
using UnityEngine;

// Home decoration (แต่งบ้าน) from house.lua: which furniture you own and where it stands in
// your stilt house. The server checks ownership and the room's bounds.
[RequireComponent(typeof(NakamaConnection))]
public class HouseClient : MonoBehaviour
{
    [Serializable]
    public class Owned
    {
        public string item;
        public string name;
        public int owned;
        public int placed;
        public int Stored => owned - placed;
    }

    [Serializable]
    public class Piece
    {
        public int id;
        public string item;
        public float x;
        public float z;
        public int rot;
    }

    [Serializable]
    public class State
    {
        public Owned[] owned;
        public Piece[] placed;
        public int max_pieces;
        public int placed_id;
    }

    [Serializable] private class PlaceRequest { public string item; public float x, z; public int rot; }
    [Serializable] private class MoveRequest { public int id; public float x, z; public int rot; }
    [Serializable] private class IdRequest { public int id; }

    public static HouseClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;

    public event Action<State> Changed;

    private NakamaConnection connection;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    // The village scene's stilt house becomes a walk-in room
    private void Start() => HomeInterior.Setup();

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

    private void OnConnected(NakamaConnection conn) => Refresh();

    public void Refresh() => _ = RefreshQuietly();

    private async Task RefreshQuietly()
    {
        try { await CallAsync("house_get"); }
        catch (Exception e) { Debug.LogWarning($"[House] {e.Message}"); }
    }

    public Task<State> PlaceAsync(string item, float x, float z, int rot) =>
        CallAsync("house_place", JsonUtility.ToJson(new PlaceRequest { item = item, x = x, z = z, rot = rot }));

    public Task<State> MoveAsync(int id, float x, float z, int rot) =>
        CallAsync("house_move", JsonUtility.ToJson(new MoveRequest { id = id, x = x, z = z, rot = rot }));

    public Task<State> RemoveAsync(int id) =>
        CallAsync("house_remove", JsonUtility.ToJson(new IdRequest { id = id }));

    public string NameOf(string item)
    {
        if (Current?.owned != null)
            foreach (var o in Current.owned) if (o.item == item) return o.name;
        return item;
    }

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        state.owned ??= new Owned[0];
        state.placed ??= new Piece[0];
        Current = state;
        Changed?.Invoke(state);
        return state;
    }
}
