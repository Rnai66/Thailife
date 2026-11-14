using System;
using System.Threading.Tasks;
using UnityEngine;

// ร้านกาแฟของฉัน (cafe.lua): the real shop for your coffee business — its name, style and the
// furniture you've arranged inside (CafeSite shows it, CafeUI changes it).
[RequireComponent(typeof(NakamaConnection))]
public class CafeClient : MonoBehaviour
{
    [Serializable] public class Piece { public int id; public string item; public float x, z; public int r; }
    [Serializable] public class Item { public string id, name; public int price; }
    [Serializable] public class Style { public string id, name; }

    [Serializable]
    public class State
    {
        public bool open;
        public int staff, earned, cash, style_price, max_pieces, name_max;
        public float half_w = 4.5f, half_d = 3.5f;
        public string name, style, done;
        public Piece[] pieces;
        public Item[] items;
        public Style[] styles;

        public string NameOf(string item)
        {
            if (items != null) foreach (var i in items) if (i.id == item) return i.name;
            return item;
        }

        public string StyleName
        {
            get
            {
                if (styles != null) foreach (var s in styles) if (s.id == style) return s.name;
                return style;
            }
        }
    }

    [Serializable] private class PlaceRequest { public string item; public float x, z; public int r; }
    [Serializable] private class MoveRequest { public int id; public float x, z; public int r; }
    [Serializable] private class IdRequest { public int id; }
    [Serializable] private class StyleRequest { public string style, name; }

    public static CafeClient Instance { get; private set; }
    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public event Action<State> Changed;

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
        if (BankClient.Instance != null) BankClient.Instance.Changed += OnBank;
    }

    private void OnDisable()
    {
        connection.Connected -= OnConnected;
        if (BankClient.Instance != null) BankClient.Instance.Changed -= OnBank;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Cafe] {e.Message}"); }
    }

    // Opened or closed a coffee business at the bank: the shop opens or shuts
    private bool hadCoffee;
    private async void OnBank(BankClient.State s)
    {
        bool has = false;
        if (s?.businesses != null) foreach (var b in s.businesses) if (b.type == "coffee") has = true;
        if (has == hadCoffee && Current != null && Current.open == has) return;
        hadCoffee = has;
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Cafe] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => Call("cafe_get");
    public Task<State> PlaceAsync(string item, float x, float z, int r) => Call("cafe_place", new PlaceRequest { item = item, x = x, z = z, r = r });
    public Task<State> MoveAsync(int id, float x, float z, int r) => Call("cafe_move", new MoveRequest { id = id, x = x, z = z, r = r });
    public Task<State> RemoveAsync(int id) => Call("cafe_remove", new IdRequest { id = id });
    public Task<State> StyleAsync(string style, string name) => Call("cafe_style", new StyleRequest { style = style, name = name });

    private async Task<State> Call(string rpc, object request = null)
    {
        string payload = request == null ? "{}" : JsonUtility.ToJson(request);
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        if (WalletClient.Instance != null && rpc != "cafe_get") _ = WalletClient.Instance.RefreshAsync();
        return state;
    }
}
