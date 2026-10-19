using System;
using System.Threading.Tasks;
using UnityEngine;

// City bank (bank.lua): savings, loans, land, cars, businesses. Also applies the best car's
// travel speed to the player.
[RequireComponent(typeof(NakamaConnection))]
public class BankClient : MonoBehaviour
{
    [Serializable] public class Asset { public string id, kind, name, perk; public int price, level; public bool owned; }
    [Serializable] public class BusinessType { public string id, name; public int setup, rent, wage, revenue; }
    [Serializable] public class Business { public int slot, staff, till, per_day; public string type, name, building; }
    [Serializable] public class Building { public string id, name; public bool taken; }

    [Serializable]
    public class State
    {
        public int level, cash, savings, loan, loan_limit, loan_level, business_level, business_cash, max_staff, till;
        public float savings_rate, loan_rate, speed = 1f;
        public Asset[] assets;
        public BusinessType[] business_types;
        public Business[] businesses;
        public Building[] buildings;
        public string done;
    }

    [Serializable] private class AmountRequest { public long amount; }
    [Serializable] private class AssetRequest { public string asset; }
    [Serializable] private class OpenRequest { public string business, building; }
    [Serializable] private class StaffRequest { public int slot, delta; }
    [Serializable] private class SlotRequest { public int slot; }

    public static BankClient Instance { get; private set; }
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
    }

    private void OnDisable() => connection.Connected -= OnConnected;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Bank] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => Call("bank_get");
    public Task<State> DepositAsync(long n) => Call("bank_deposit", new AmountRequest { amount = n });
    public Task<State> WithdrawAsync(long n) => Call("bank_withdraw", new AmountRequest { amount = n });
    public Task<State> BorrowAsync(long n) => Call("bank_borrow", new AmountRequest { amount = n });
    public Task<State> RepayAsync(long n) => Call("bank_repay", new AmountRequest { amount = n });
    public Task<State> BuyAsync(string asset) => Call("bank_buy", new AssetRequest { asset = asset });
    public Task<State> OpenAsync(string business, string building) => Call("bank_open", new OpenRequest { business = business, building = building });
    public Task<State> StaffAsync(int slot, int delta) => Call("bank_staff", new StaffRequest { slot = slot, delta = delta });
    public Task<State> CollectAsync() => Call("bank_collect");
    public Task<State> CloseAsync(int slot) => Call("bank_close", new SlotRequest { slot = slot });

    private async Task<State> Call(string rpc, object request = null)
    {
        string payload = request == null ? "{}" : JsonUtility.ToJson(request);
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.VehicleBoost = Mathf.Max(1f, state.speed); // your best car
        Changed?.Invoke(state);
        if (WalletClient.Instance != null && rpc != "bank_get") _ = WalletClient.Instance.RefreshAsync();
        return state;
    }
}
