using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// The church and the mosque (faith.lua): what is on now (the Sunday service, the five daily prayers),
// what's next, how often you have prayed and given today. Prayer gives energy and some main XP;
// giving gives main XP up to a cap a day. Made next to LifeClient.
[RequireComponent(typeof(NakamaConnection))]
public class FaithClient : MonoBehaviour
{
    [Serializable]
    public class Place
    {
        public string id, name, now, next;
        public bool now_done;
        public int prayed, given, total;
    }

    [Serializable]
    public class State
    {
        public string day_name;
        public int candle_cost, seconds, give_cap;
        public int[] give_amounts;
        public Place[] places;
        // results
        public string token, done;
        public int gained_xp, gained_energy;
    }

    [Serializable] private class PrayRequest { public string place, action, token; public bool candle; }
    [Serializable] private class GiveRequest { public string place; public int amount; }

    public static FaithClient Instance { get; private set; }
    public State Current { get; private set; }

    private NakamaConnection connection;
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
        try { await RefreshAsync(); }
        catch (Exception e) { Debug.LogWarning($"[Faith] {e.Message}"); }
    }

    private void Update()
    {
        if (Current == null || !connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + 45f;
        _ = Quietly();
    }

    private async Task Quietly()
    {
        try { await RefreshAsync(); } catch (Exception) { }
    }

    public Place Of(string id)
    {
        if (Current?.places == null) return null;
        foreach (var p in Current.places) if (p.id == id) return p;
        return null;
    }

    public Task<State> RefreshAsync() => Call("faith_get");
    public Task<State> PrayStartAsync(string place, bool candle) => Call("faith_pray", JsonUtility.ToJson(new PrayRequest { place = place, action = "start", candle = candle }));
    public Task<State> PrayFinishAsync(string place, string token) => Call("faith_pray", JsonUtility.ToJson(new PrayRequest { place = place, action = "finish", token = token }));
    public Task<State> GiveAsync(string place, int amount) => Call("faith_give", JsonUtility.ToJson(new GiveRequest { place = place, amount = amount }));

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        nextPoll = Time.time + 45f;
        if (rpc != "faith_get")
        {
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            if (state.gained_xp > 0) CareerClient.Instance?.Reload();
        }
        return state;
    }

    public static string Explain(ApiResponseException e)
    {
        string m = e.Message;
        return m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("too soon") ? "ยังไม่ครบเวลา"
            : m.Contains("sleeping") ? "กำลังนอนอยู่"
            : "ทำรายการไม่สำเร็จ";
    }
}
