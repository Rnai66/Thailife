using System;
using System.Threading.Tasks;
using UnityEngine;

// ร้านกาแฟของฉัน (cafe.lua): the real shop for your coffee business — its name, style and the
// furniture you've arranged inside (CafeSite shows it, CafeUI changes it). HotelShopClient is the
// same for the lobby of your hotel (Kind "hotel", the hotelshop_* calls), ShophouseClient for the
// shophouse on your city land (Kind "shophouse", shophouse_*: yours while it stands, LandClient).
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

    public static CafeClient Instance { get; private set; }  // the café
    public static CafeClient Hotel { get; private set; }     // the hotel lobby
    public static CafeClient Shophouse { get; private set; } // the shophouse on your city land
    public static CafeClient Of(string kind) => kind == "hotel" ? Hotel : kind == "shophouse" ? Shophouse : Instance;

    public virtual string Kind => "cafe";
    private string Prefix => Kind == "hotel" ? "hotelshop" : Kind == "shophouse" ? "shophouse" : "cafe";
    private string BusinessType => Kind == "hotel" ? "hotel" : "coffee";
    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public event Action<State> Changed;

    private NakamaConnection connection;
    private BankClient bank; // listened to for opening / closing the business

    private void Awake()
    {
        if (Kind == "hotel") Hotel = this;
        else if (Kind == "shophouse") Shophouse = this;
        else Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnEnable()
    {
        connection.Connected += OnConnected;
        if (connection.IsConnected) OnConnected(connection);
        if (Kind == "shophouse") ListenToLand();
        else ListenToBank();
    }

    // The bank may come up after this one: keep trying until it's there (otherwise opening the
    // business at the bank would not open the shop until the next login)
    private void Update()
    {
        if (Kind == "shophouse") { if (land == null) ListenToLand(); return; }
        if (bank == null) ListenToBank();
    }

    // The shophouse is yours while it stands on your land: built or pulled down, look again
    private LandClient land;
    private string hadBuild;
    private void ListenToLand()
    {
        if (land != null || LandClient.Instance == null) return;
        land = LandClient.Instance;
        land.Changed += OnLand;
        if (land.Current != null) OnLand(land.Current);
    }

    private async void OnLand(LandClient.State s)
    {
        string build = s != null && s.own_city ? s.city_build ?? "" : "";
        if (build == hadBuild && Current != null) return;
        hadBuild = build;
        if (connection == null || !connection.IsConnected) return;
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Shophouse] {e.Message}"); }
    }

    private void ListenToBank()
    {
        if (bank != null || BankClient.Instance == null) return;
        bank = BankClient.Instance;
        bank.Changed += OnBank;
        if (bank.Current != null) OnBank(bank.Current);
    }

    private void OnDisable()
    {
        connection.Connected -= OnConnected;
        if (bank != null) bank.Changed -= OnBank;
        bank = null;
        if (land != null) land.Changed -= OnLand;
        land = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Hotel == this) Hotel = null;
        if (Shophouse == this) Shophouse = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Cafe] {e.Message}"); }
    }

    // Opened or closed the business at the bank: the shop opens or shuts
    private bool hadBusiness;
    private async void OnBank(BankClient.State s)
    {
        bool has = false;
        if (s?.businesses != null) foreach (var b in s.businesses) if (b.type == BusinessType) has = true;
        if (has == hadBusiness && Current != null && Current.open == has) return;
        hadBusiness = has;
        if (connection == null || !connection.IsConnected) return;
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Cafe] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => Call("get");
    public Task<State> PlaceAsync(string item, float x, float z, int r) => Call("place", new PlaceRequest { item = item, x = x, z = z, r = r });
    public Task<State> MoveAsync(int id, float x, float z, int r) => Call("move", new MoveRequest { id = id, x = x, z = z, r = r });
    public Task<State> RemoveAsync(int id) => Call("remove", new IdRequest { id = id });
    public Task<State> StyleAsync(string style, string name) => Call("style", new StyleRequest { style = style, name = name });

    private async Task<State> Call(string what, object request = null)
    {
        string payload = request == null ? "{}" : JsonUtility.ToJson(request);
        var result = await connection.Client.RpcAsync(connection.Session, Prefix + "_" + what, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        if (WalletClient.Instance != null && what != "get") _ = WalletClient.Instance.RefreshAsync();
        return state;
    }
}
