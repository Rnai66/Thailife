using System;
using System.Threading.Tasks;
using UnityEngine;

// Cow farm (ranch.lua): how many cows you own. One is free from the อบต., more come from the cow seller.
[RequireComponent(typeof(NakamaConnection))]
public class RanchClient : MonoBehaviour
{
    [Serializable]
    public class State
    {
        public int cows, max_cows, price;
        public bool claimed;
        public int feed, max_feed, feed_pack, feed_pack_price;
        public int milk, milk_price, grown, grown_days, grown_price, next_day_s;
        public int[] growth;
        public string done;
    }

    public static RanchClient Instance { get; private set; }
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
        catch (Exception e) { Debug.LogWarning($"[Ranch] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => Call("ranch_get");
    public Task<State> ClaimAsync() => Call("ranch_claim");
    public Task<State> BuyAsync() => Call("ranch_buy");
    public Task<State> FeedAsync(int packs) => Call("ranch_feed", $"{{\"packs\":{packs}}}");
    public Task<State> MilkAsync() => Call("ranch_milk");
    public Task<State> SellAsync() => Call("ranch_sell");

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        if (WalletClient.Instance != null && rpc != "ranch_get" && rpc != "ranch_claim") _ = WalletClient.Instance.RefreshAsync();
        if (rpc == "ranch_milk") LifeClient.Instance?.Refresh(); // milking costs a little energy
        return state;
    }
}
