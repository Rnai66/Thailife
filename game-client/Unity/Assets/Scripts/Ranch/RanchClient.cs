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

    private async Task<State> Call(string rpc)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, "{}");
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        if (WalletClient.Instance != null && rpc == "ranch_buy") _ = WalletClient.Instance.RefreshAsync();
        return state;
    }
}
