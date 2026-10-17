using System;
using System.Threading.Tasks;
using UnityEngine;

// Chicken raising (เลี้ยงไก่) and the beauty contest from chickens.lua.
// The server lays the eggs, hatches the chicks and rolls their traits; this class only displays.
[RequireComponent(typeof(NakamaConnection))]
public class CoopClient : MonoBehaviour
{
    [Serializable]
    public class Chicken
    {
        public int id;
        public string name;
        public string sex;      // hen / rooster
        public int beauty;      // 1..100
        public string color;    // Thai feather colour name
        public bool adult;
        public int grown_in_s;
        public int price;
    }

    [Serializable]
    public class ContestEntry
    {
        public string name;
        public int score;
        public bool mine;
    }

    [Serializable]
    public class State
    {
        public Chicken[] chickens;
        public int capacity = 8;
        public int eggs;
        public int fertile_eggs;
        public int[] hatching_s;
        public int fed_for_s;
        public int feed_bags;
        public int egg_price;
        public int fertile_egg_price;
        public bool fair_open;
        public int fair_ends_in_s;
        public int next_fair_in_s;
        public bool entered_this_fair;
        public int game_day;
        public int energy;
        public int max_energy = 100;
        public bool sleeping;
        // action results
        public string action;
        public int earned;
        public int sold;
        public string sold_name;
        public ContestEntry[] contest;
        public int place;
        public int prize;
        public string entry_name;
    }

    [Serializable]
    private class ActRequest
    {
        public string action;
        public int chicken;
    }

    private const float PollInterval = 15f;

    public static CoopClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public float Age => Time.time - receivedAt;   // seconds since Current arrived (for countdowns)

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
        try { await CallAsync("coop_get", "{}"); }
        catch (Exception e) { Debug.LogWarning($"[Coop] Could not load: {e.Message}"); }
    }

    private void Update()
    {
        if (Current == null || !connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + PollInterval;
        Refresh();
    }

    public async void Refresh()
    {
        try { await CallAsync("coop_get", "{}"); }
        catch (Exception) { /* next poll will retry */ }
    }

    // Throws ApiResponseException with the server's reason when refused
    public Task<State> ActAsync(string action, int chicken = 0) =>
        CallAsync("coop_act", JsonUtility.ToJson(new ActRequest { action = action, chicken = chicken }));

    public Task<State> EnterContestAsync() => CallAsync("contest_enter", "{}");

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

    // Feather colours for the 3D chickens and UI swatches
    public static Color FeatherColor(string colorName) => colorName switch
    {
        "ประดู่หางดำ" => new Color(0.45f, 0.18f, 0.1f),
        "เหลืองหางขาว" => new Color(0.95f, 0.8f, 0.3f),
        "เขียวหางดำ" => new Color(0.15f, 0.3f, 0.2f),
        "ทองแดงหางดำ" => new Color(0.75f, 0.4f, 0.15f),
        "ขาวล้วน" => new Color(0.95f, 0.95f, 0.92f),
        _ => new Color(0.6f, 0.5f, 0.4f),
    };

    public static string SexTh(Chicken c) => c.sex == "rooster" ? "ตัวผู้" : "ตัวเมีย";

    public static string Countdown(float seconds)
    {
        var t = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours} ชม. {t.Minutes} นาที" : $"{(int)t.TotalMinutes}:{t.Seconds:00}";
    }
}
