using System;
using System.Threading.Tasks;
using UnityEngine;

// Traditional festivals (งานประเพณี) from festival.lua: which one is on, where (big = city,
// small = tambon), and playing the fair games. Polls so the fairground appears on time.
[RequireComponent(typeof(NakamaConnection))]
public class FestivalClient : MonoBehaviour
{
    [Serializable]
    public class CalendarEntry
    {
        public string name;
        public string date;
        public string where;
        public string kind;    // งานประเพณี | งานประจำเดือน | ระดับประเทศ
        public string scale;
        public bool open;
        public int in_s, ends_s;
    }

    [Serializable]
    public class State
    {
        public bool open;
        public string name;
        public string scale;   // "big" (city) | "small" (tambon) | "village" | "beach" | "both" (test mode)
        public string kind;    // "tradition" | "monthly" | "national"
        public bool temple;    // traditional festivals are also held at the village temple
        public string where;
        public int ends_in_s;
        public int next_in_s;
        public bool chance_games; // สอยดาว บิงโก ก.ไก่ ปาโป่ง are open (server CHANCE_GAMES switch)
        public CalendarEntry[] calendar;
        // festival_play
        public string game_name;
        public string prize;
        public bool won;
        public int paid;
        public int gained;
        public string[] card;   // มหาลัย ก.ไก่: your 4 letters
        public string[] draws;  // the letters called, in order, until your card was complete
        public string ticket;   // สอยดาว: the number on the paper you picked
        public string icon;     // prize picture (Resources/Branding/Icons)
        public bool big;        // สอยดาว: one of the big prizes
        public bool[] throws;   // ปาโป่ง: each dart pops a balloon or misses
        public int pops;
        public int[] bingo_card; // บิงโก: 25 numbers row by row, 0 = free middle
        public int[] calls;      // the balls called, in order
    }

    [Serializable] private class PlayRequest { public string game; }

    private const float PollInterval = 30f;

    public static FestivalClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOpen => Current != null && Current.open;
    public event Action<State> Changed;

    // Is an event on at this venue right now? "any" = the village temple ground: every traditional
    // festival plus the village's monthly events.
    public bool OpenFor(string venueScale) =>
        IsOpen && (Current.scale == "both" || Current.scale == venueScale
                   || (venueScale == "any" && (Current.temple || Current.scale == "village")));

    private NakamaConnection connection;
    private float nextPoll, refreshedAt;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!connection.IsConnected || Time.time < nextPoll) return;
        nextPoll = Time.time + PollInterval;
        _ = Refresh();
    }

    // Real seconds since the last answer (the calendar's countdowns are relative to it)
    public float SecondsSinceRefresh => Time.realtimeSinceStartup - refreshedAt;

    public Task RefreshAsync() => connection != null && connection.IsConnected ? Refresh() : Task.CompletedTask;

    private async Task Refresh()
    {
        try { await CallAsync("festival_get"); }
        catch (Exception e) { Debug.LogWarning($"[Festival] {e.Message}"); }
    }

    public Task<State> PlayAsync(string game) => CallAsync("festival_play", JsonUtility.ToJson(new PlayRequest { game = game }));

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        refreshedAt = Time.realtimeSinceStartup;
        Changed?.Invoke(state);
        return state;
    }
}
