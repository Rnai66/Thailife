using System;
using System.Threading.Tasks;
using UnityEngine;

// Your land (land.lua): which parcels you own at the bank, what is built on them, the crop field's
// four plots and the rent waiting. The scene's LandSite / FieldPlot pieces show it; LandUI acts on it.
[RequireComponent(typeof(NakamaConnection))]
public class LandClient : MonoBehaviour
{
    [Serializable]
    public class Plot
    {
        public int id;
        public string stage;      // empty | plowed | growing | ripe
        public float progress;
        public bool watered;
        public string crop, crop_name;
        public float ripe_in_s;
    }

    [Serializable]
    public class Crop
    {
        public string id, name;
        public int seed_price, price_per_kg, stored;
        public float grow_s;
    }

    [Serializable]
    public class Build
    {
        public string id, name;
        public int cost, rent;
    }

    [Serializable]
    public class State
    {
        public bool own_plain, own_rice, own_city;
        public string plain_build, city_build;
        public int plain_rent, city_rent, max_rent_days, pen_cows, store;
        public Plot[] plots;
        public Crop[] crops;
        public Build[] builds;
        public string done, action, harvested_name;
        public int harvested_kg;
    }

    [Serializable] private class FieldRequest { public int plot; public string action, crop; }

    private const float PollInterval = 60f;

    public static LandClient Instance { get; private set; }
    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public event Action<State> Changed;

    // The crop the field plots plant next (picked in LandUI)
    public string NextCrop { get; set; } = "corn";

    private NakamaConnection connection;
    private float receivedAt, nextPoll;

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
        catch (Exception e) { Debug.LogWarning($"[Land] {e.Message}"); }
    }

    private void Update()
    {
        if (Current == null || !connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + PollInterval;
        _ = Quietly();
    }

    private async Task Quietly()
    {
        try { await RefreshAsync(); } catch (Exception) { /* the next poll retries */ }
    }

    public Plot GetPlot(int id)
    {
        if (Current?.plots == null) return null;
        foreach (var p in Current.plots) if (p.id == id) return p;
        return null;
    }

    public Crop GetCrop(string id)
    {
        if (Current?.crops == null) return null;
        foreach (var c in Current.crops) if (c.id == id) return c;
        return null;
    }

    public float ProgressOf(Plot p)
    {
        if (p == null) return 0f;
        if (p.stage == "ripe") return 1f;
        if (p.stage != "growing") return 0f;
        var c = GetCrop(p.crop);
        float grow = c != null && c.grow_s > 0f ? c.grow_s : 3600f;
        return Mathf.Clamp01(p.progress + (Time.time - receivedAt) / grow);
    }

    public float SecondsUntilRipe(Plot p) => p == null ? 0f : Mathf.Max(0f, p.ripe_in_s - (Time.time - receivedAt));

    public Task<State> RefreshAsync() => Call("land_get");
    public Task<State> BuildAsync(string parcel, string build) => Call("land_build", $"{{\"parcel\":\"{parcel}\",\"build\":\"{build}\"}}");
    public Task<State> ClearAsync(string parcel) => Call("land_clear", $"{{\"parcel\":\"{parcel}\"}}");
    public Task<State> CollectAsync(string parcel) => Call("land_collect", $"{{\"parcel\":\"{parcel}\"}}");
    public Task<State> SellAsync(string crop) => Call("land_sell", $"{{\"crop\":\"{crop}\"}}");
    public Task<State> FieldAsync(int plot, string action, string crop = null) =>
        Call("land_field", JsonUtility.ToJson(new FieldRequest { plot = plot, action = action, crop = crop ?? "" }));

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        receivedAt = Time.time;
        nextPoll = Time.time + PollInterval;
        Changed?.Invoke(state);
        if (rpc != "land_get")
        {
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            if ((rpc == "land_build" || rpc == "land_clear") && RanchClient.Instance != null) _ = RanchClient.Instance.RefreshAsync(); // a pen on the land adds cow places
        }
        return state;
    }
}
