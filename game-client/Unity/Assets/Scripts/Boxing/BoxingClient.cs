using System;
using System.Threading.Tasks;
using UnityEngine;

// Fights and pad work from boxing.lua: which events are on, starting one, reporting the result.
[RequireComponent(typeof(NakamaConnection))]
public class BoxingClient : MonoBehaviour
{
    [Serializable]
    public class Event
    {
        public string id;
        public string name;
        public int level;
        public int energy;
        public bool member;
        public string when;
        public bool open;
        public bool fought;
        public bool unlocked;
        public int purse_win;
        public int purse_lose;
        public int xp_win;
        public bool pads;
    }

    [Serializable]
    public class Opponent
    {
        public string name;
        public int level;
        public int hp;
        public float power;
        public float speed;
        public int skin;
        public bool strong; // ตัวเก่ง: a much stronger boxer turned up
    }

    [Serializable]
    public class State
    {
        public int level;
        public int wins;
        public int losses;
        public int kos;
        public string titles;
        public Event[] events;
        // boxing_start
        public int session;
        public string event_name;
        public int seconds;
        public int rounds;
        public int round_s;
        public Opponent opponent;
        // boxing_finish
        public bool win;
        public bool ko;
        public int purse;
        public int xp;
        public int good;
        public string title;
        public bool leveled_up;
        public bool injured;
        public int new_level;
    }

    [Serializable] private class StartRequest { public string @event; }
    [Serializable] private class FinishRequest { public int session; public string result; public int landed, taken, damage, good; }

    public static BoxingClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;

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
        catch (Exception e) { Debug.LogWarning($"[Boxing] {e.Message}"); }
    }

    public Task<State> RefreshAsync() => CallAsync("boxing_get");

    public Task<State> StartAsync(string eventId) =>
        CallAsync("boxing_start", JsonUtility.ToJson(new StartRequest { @event = eventId }));

    public Task<State> FinishAsync(int session, string result, int landed, int taken, int damage, int good) =>
        CallAsync("boxing_finish", JsonUtility.ToJson(new FinishRequest { session = session, result = result, landed = landed, taken = taken, damage = damage, good = good }));

    public Event Find(string id)
    {
        if (Current?.events != null) foreach (var e in Current.events) if (e.id == id) return e;
        return null;
    }

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        return state;
    }
}
