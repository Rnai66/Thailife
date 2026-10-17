using System;
using System.Threading.Tasks;
using UnityEngine;

// Rice farming (ทำนา) from farming.lua: the player's 6 plots in Kok Nong Na.
// The server owns the stages and growth timer; this class caches them for the plots to display.
[RequireComponent(typeof(NakamaConnection))]
public class FarmClient : MonoBehaviour
{
    [Serializable]
    public class Plot
    {
        public int id;
        public string stage;      // empty / plowed / growing / ripe
        public float progress;    // 0..1 while growing
        public bool watered;
        public int ripe_in_s;
        public string variety;
        public string variety_name;
        public float grow_s = 720f;
    }

    [Serializable]
    public class Variety
    {
        public string id;
        public string name;
        public int seeds;        // seed bags owned
        public float stored_kg;  // in the barn
        public int seed_price;
        public float grow_s;
        public int price_per_kg;
    }

    [Serializable]
    public class State
    {
        public Plot[] plots;
        public Variety[] varieties;
        public float barn_capacity = 100f;
        public float barn_total;
        public int energy;
        public int max_energy = 100;
        public bool sleeping;
        public string action;
        public int harvested_kg;
        public string harvested_name;
    }

    [Serializable]
    private class ActRequest
    {
        public int plot;
        public string action;
        public string variety;
    }

    private const float PollInterval = 15f;

    public static FarmClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;

    public event Action<State> Changed;

    private NakamaConnection connection;
    private float receivedAt;
    private float nextPoll;

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
        try { await CallAsync("farm_get", "{}"); }
        catch (Exception e) { Debug.LogWarning($"[Farm] Could not load: {e.Message}"); }
    }

    private void Update()
    {
        if (Current == null || !connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + PollInterval;
        _ = RefreshQuietly();
    }

    // Called after shop purchases/sales change seeds or the barn
    public void Refresh() => _ = RefreshQuietly();

    private async Task RefreshQuietly()
    {
        try { await CallAsync("farm_get", "{}"); }
        catch (Exception) { /* next poll will retry */ }
    }

    public Plot GetPlot(int id)
    {
        if (Current?.plots == null) return null;
        foreach (var plot in Current.plots)
            if (plot.id == id) return plot;
        return null;
    }

    // Growth estimate between server updates (the server decides when it is really ripe)
    public float ProgressOf(Plot plot)
    {
        if (plot == null) return 0f;
        if (plot.stage == "ripe") return 1f;
        if (plot.stage != "growing") return 0f;
        float grow = plot.grow_s > 0f ? plot.grow_s : 720f;
        return Mathf.Clamp01(plot.progress + (Time.time - receivedAt) / grow);
    }

    public float SecondsUntilRipe(Plot plot) =>
        plot == null ? 0f : Mathf.Max(0f, plot.ripe_in_s - (Time.time - receivedAt));

    // Throws ApiResponseException with the server's reason when refused
    public Task<State> ActAsync(int plot, string action, string variety = null) =>
        CallAsync("farm_act", JsonUtility.ToJson(new ActRequest { plot = plot, action = action, variety = variety ?? "" }));

    // Seed bag to plant next: the pricier hom mali first if we have it, else any variety with seeds
    public Variety PickSeed()
    {
        if (Current?.varieties == null) return null;
        Variety pick = null;
        foreach (var v in Current.varieties)
            if (v.seeds > 0 && (pick == null || v.price_per_kg > pick.price_per_kg)) pick = v;
        return pick;
    }

    public string SeedSummary()
    {
        if (Current?.varieties == null) return "";
        var parts = new System.Collections.Generic.List<string>();
        foreach (var v in Current.varieties) parts.Add($"{v.name} {v.seeds} ถุง");
        return string.Join(" · ", parts);
    }

    private async Task<State> CallAsync(string rpc, string payload)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        receivedAt = Time.time;
        nextPoll = Time.time + PollInterval;

        LifeClient.Instance?.Apply(state.energy, state.max_energy, state.sleeping);
        Changed?.Invoke(state);
        return state;
    }
}
