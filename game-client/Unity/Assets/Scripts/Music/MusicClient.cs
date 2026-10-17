using System;
using System.Threading.Tasks;
using UnityEngine;

// Musician career (นักดนตรี) from music.lua: skill level, instruments, venues, performances.
[RequireComponent(typeof(NakamaConnection))]
public class MusicClient : MonoBehaviour
{
    [Serializable]
    public class Instrument
    {
        public string id;
        public string name;
        public bool owned;
        public bool equipped;
        public bool folk;
        public float mult;
    }

    [Serializable]
    public class Venue
    {
        public string id;
        public string name;
        public int level;
        public int @base;
        public string bonus;   // folk / modern / any
        public bool unlocked;
        public bool open;
        public bool played_today;   // pubs: once per game day each
        public string when;
    }

    [Serializable]
    public class State
    {
        public int level = 1;
        public int xp;
        public int xp_to_next = 30;
        public int performances;
        public Instrument[] instruments;
        public Venue[] venues;
        public string instrument;
        public string instrument_name;
        // finish results
        public int tips;
        public int accuracy;
        public int gained_xp;
        public bool leveled_up;
        public int perfect;
        public int good;
        public int total_notes;
        public string venue_name;
    }

    [Serializable]
    public class Note
    {
        public int t;      // ms from the start of the performance
        public int lane;   // 1..3
    }

    [Serializable]
    public class Chart
    {
        public int session;
        public string venue;
        public string venue_name;
        public string song;
        public int bpm;
        public Note[] notes;
        public int duration_ms;
        public int energy;
    }

    [Serializable] private class VenueRequest { public string venue; }
    [Serializable] private class EquipRequest { public string instrument; }
    [Serializable] private class FinishRequest { public int session; public int perfect; public int good; }

    public static MusicClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;

    public event Action<State> Changed;

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
        catch (Exception e) { Debug.LogWarning($"[Music] Could not load: {e.Message}"); }
    }

    public Task<State> RefreshAsync() => CallStateAsync("music_get", "{}");

    public Task<State> EquipAsync(string instrument) =>
        CallStateAsync("music_equip", JsonUtility.ToJson(new EquipRequest { instrument = instrument }));

    // Throws ApiResponseException (locked venue, closed, not enough energy) when refused
    public async Task<Chart> StartAsync(string venue)
    {
        var result = await connection.Client.RpcAsync(connection.Session, "music_start", JsonUtility.ToJson(new VenueRequest { venue = venue }));
        var chart = JsonUtility.FromJson<Chart>(result.Payload);
        LifeClient.Instance?.Refresh();
        return chart;
    }

    public Task<State> FinishAsync(int session, int perfect, int good) =>
        CallStateAsync("music_finish", JsonUtility.ToJson(new FinishRequest { session = session, perfect = perfect, good = good }));

    public Venue GetVenue(string id)
    {
        if (Current?.venues == null) return null;
        foreach (var v in Current.venues) if (v.id == id) return v;
        return null;
    }

    private async Task<State> CallStateAsync(string rpc, string payload)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        Changed?.Invoke(state);
        return state;
    }
}
