using System;
using System.Threading.Tasks;
using UnityEngine;

// Money (เงินบาท) from economy.lua. The balance lives in the server-side Nakama wallet.
[RequireComponent(typeof(NakamaConnection))]
public class WalletClient : MonoBehaviour
{
    [Serializable]
    public class FishEntry
    {
        public string id;
        public string name;
        public int count;
        public int price;
    }

    [Serializable]
    public class Snapshot
    {
        public int baht;
        public FishEntry[] fish;
        public int total_value;
        public int sold;
        public int earned;
    }

    public static WalletClient Instance { get; private set; }

    public Snapshot Current { get; private set; }
    public int Baht => Current?.baht ?? 0;
    public bool IsOnline => connection != null && connection.IsConnected;

    public event Action<Snapshot> Changed;

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
        try
        {
            await RefreshAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Wallet] Could not load: {e.Message}");
        }
    }

    public async Task<Snapshot> RefreshAsync() => Apply(await CallAsync("economy_get"));

    public async Task<Snapshot> SellAllFishAsync()
    {
        var snapshot = Apply(await CallAsync("shop_sell_fish"));

        // The basket is empty now; keep the fishing HUD in sync
        var fishing = FishingClient.Instance;
        if (fishing != null && fishing.Current != null && snapshot.sold > 0)
            fishing.SetInventory(new FishingClient.Inventory
            {
                total = 0,
                biggest_kg = fishing.Current.biggest_kg,
                biggest_name = fishing.Current.biggest_name,
            });
        return snapshot;
    }

    private async Task<Snapshot> CallAsync(string rpc)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, "{}");
        return JsonUtility.FromJson<Snapshot>(result.Payload);
    }

    private Snapshot Apply(Snapshot snapshot)
    {
        Current = snapshot;
        Changed?.Invoke(snapshot);
        return snapshot;
    }
}
