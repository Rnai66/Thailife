using System;
using System.Threading.Tasks;
using UnityEngine;

// Talks to fishing.lua. The server decides when the fish bites and what is caught.
[RequireComponent(typeof(NakamaConnection))]
public class FishingClient : MonoBehaviour
{
    [Serializable]
    public class Inventory
    {
        public int total;
        public float biggest_kg;
        public string biggest_name;
    }

    [Serializable]
    private class CastResult
    {
        public int bite_in_ms;
    }

    [Serializable]
    public class ReelResult
    {
        public bool caught;
        public string reason;     // "early" / "late" when not caught
        public string fish_id;
        public string fish_name;
        public float kg;
        public Inventory inventory;
    }

    [Serializable]
    private class GetResult
    {
        public Inventory inventory;
    }

    public static FishingClient Instance { get; private set; }

    public Inventory Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected;

    public event Action<Inventory> InventoryChanged;

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
            var result = await conn.Client.RpcAsync(conn.Session, "fishing_get", "{}");
            SetInventory(JsonUtility.FromJson<GetResult>(result.Payload).inventory);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Fishing] Could not load inventory: {e.Message}");
        }
    }

    // Returns seconds until the bite, or a negative number if the cast failed.
    // water: "fresh" (canal, pond) or "sea" (the beach pier)
    public async Task<float> CastAsync(string water = "fresh")
    {
        var result = await connection.Client.RpcAsync(connection.Session, "fishing_cast", $"{{\"water\":\"{water}\"}}");
        return JsonUtility.FromJson<CastResult>(result.Payload).bite_in_ms / 1000f;
    }

    public async Task<ReelResult> ReelAsync()
    {
        var result = await connection.Client.RpcAsync(connection.Session, "fishing_reel", "{}");
        var reel = JsonUtility.FromJson<ReelResult>(result.Payload);
        SetInventory(reel.inventory);
        return reel;
    }

    // Also used by WalletClient after selling fish at the shop
    public void SetInventory(Inventory inventory)
    {
        if (inventory == null) return;
        Current = inventory;
        InventoryChanged?.Invoke(inventory);
    }
}
