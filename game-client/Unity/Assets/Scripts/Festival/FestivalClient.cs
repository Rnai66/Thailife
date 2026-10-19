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
    }

    [Serializable]
    public class State
    {
        public bool open;
        public string name;
        public string scale;   // "big" | "small" | "both" (test mode)
        public string where;
        public int ends_in_s;
        public int next_in_s;
        public CalendarEntry[] calendar;
        // festival_play
        public string game_name;
        public string prize;
        public bool won;
        public int paid;
        public int gained;
        public string[] card;   // มหาลัย ก.ไก่: your 4 letters
        public string[] draws;  // the letters called, in order, until your card was complete
    }

    [Serializable] private class PlayRequest { public string game; }

    private const float PollInterval = 30f;

    public static FestivalClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOpen => Current != null && Current.open;
    public event Action<State> Changed;

    // Is the fair on at a venue of this size right now?
    public bool OpenFor(string venueScale) =>
        IsOpen && (venueScale == "any" || Current.scale == "both" || Current.scale == venueScale);

    private NakamaConnection connection;
    private float nextPoll;

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
        Changed?.Invoke(state);
        return state;
    }
}
