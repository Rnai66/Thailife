using System;
using System.Threading.Tasks;
using UnityEngine;

// Energy (พลังงาน) and sleep from life.lua. Shows a smooth estimate between server updates;
// the server's number is the one that counts.
[RequireComponent(typeof(NakamaConnection))]
public class LifeClient : MonoBehaviour
{
    [Serializable]
    public class Food
    {
        public string id;
        public string name;
        public int count;   // in the bag
        public int energy;  // restored when eaten
    }

    [Serializable]
    public class State
    {
        public int energy;
        public int max_energy = 100;
        public bool sleeping;
        public Food[] foods;
        public string ate;
        public int gained;
    }

    [Serializable]
    private class EatRequest
    {
        public string food;
    }

    private const float PassiveRegenPerSec = 1f / 60f;
    private const float SleepRegenPerSec = 3f;
    private const float PollInterval = 20f;

    public static LifeClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public bool IsSleeping => Current != null && Current.sleeping;

    // Estimated energy right now (server value + regen since it arrived)
    public float Energy
    {
        get
        {
            if (Current == null) return 0f;
            float rate = Current.sleeping ? SleepRegenPerSec : PassiveRegenPerSec;
            return Mathf.Min(Current.max_energy, Current.energy + (Time.time - receivedAt) * rate);
        }
    }

    public event Action<State> Changed;

    private NakamaConnection connection;
    private float receivedAt;
    private float nextPoll;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();

        // Daily quests ride along with the life systems (no scene setup needed)
        if (GetComponent<QuestClient>() == null) gameObject.AddComponent<QuestClient>();

        // Every location scene gets a day/night sun without extra setup
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional && light.GetComponent<DayNightCycle>() == null)
                light.gameObject.AddComponent<DayNightCycle>();
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
        try { await CallAsync("life_get"); }
        catch (Exception e) { Debug.LogWarning($"[Life] Could not load: {e.Message}"); }
    }

    private void Update()
    {
        if (!connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + PollInterval;
        _ = RefreshQuietly();
    }

    private async Task RefreshQuietly()
    {
        try { await CallAsync("life_get"); }
        catch (Exception) { /* next poll will retry */ }
    }

    public Task<State> SleepAsync() => CallAsync("life_sleep");
    public Task<State> WakeAsync() => CallAsync("life_wake");
    public Task<State> EatAsync(string foodId) => CallAsync("life_eat", JsonUtility.ToJson(new EatRequest { food = foodId }));

    // Re-read the bag after buying food at a shop
    public void Refresh() => _ = RefreshQuietly();

    // Other systems (farm, fishing) report the energy the server returned with their result
    public void Apply(int energy, int maxEnergy, bool sleeping)
    {
        Set(new State { energy = energy, max_energy = maxEnergy, sleeping = sleeping, foods = Current?.foods });
    }

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Set(state);
        return state;
    }

    private void Set(State state)
    {
        Current = state;
        receivedAt = Time.time;
        nextPoll = Time.time + PollInterval;
        Changed?.Invoke(state);
    }
}
