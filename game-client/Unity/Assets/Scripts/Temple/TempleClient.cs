using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Temple life and the home's protection (temple.lua): ordained or not, monk and หมอผี levels, บารมี,
// the amulets and yantra cloths on your home shrine, the temple dog and the กุมารทอง, and what came
// for your home in the night. A new night's story pops up over the player once.
[RequireComponent(typeof(NakamaConnection))]
public class TempleClient : MonoBehaviour
{
    // พุทธคุณ (amulet_core.lua): klaeo แคล้วคลาด, nangniao หนังเหนียว, pongkan ป้องกัน, bangta บังตา,
    // niyom มหานิยม, sanae เสน่ห์ — points, i.e. percent
    [Serializable]
    public class Power
    {
        public string id, name;
        public int value;
    }

    [Serializable]
    public class Item
    {
        public string id, kind, name;
        public int price, guard, luck, barami;
        public bool owned, worn;
        public Power[] powers;
    }

    [Serializable]
    public class Ghost
    {
        public string id, name;
        public int level, baht;
    }

    [Serializable]
    public class Night
    {
        public int seq, day;
        public string kind, text;
        public bool saved;
    }

    [Serializable]
    public class State
    {
        public bool ordained, exorcist, dog, kuman, night;
        public int monk_level, monk_xp, monk_to_next, exorcist_level, exorcist_xp, exorcist_to_next;
        public int barami, barami_xp, barami_to_next, guard, luck;
        public int ordain_level, ordain_cost, exorcist_monk_level, incense_cost, meditate_seconds, worn_max;
        public Power[] my_powers; // the worn amulets together
        public Item[] items;
        public Ghost[] ghosts;
        public Night[] log;
        public string[] banished;
        // results
        public string done, token, ghost, ghost_name;
        public int seconds, sigils, need, gained_barami, gained_monk, gained_energy, reward, xp;
        public bool won;
    }

    [Serializable] private class MeditateRequest { public string action, token; public bool incense; }
    [Serializable] private class GhostRequest { public string spot, ghost, token; public int hits; }
    [Serializable] private class ItemRequest { public string item; }
    [Serializable] private class DisrobeRequest { public bool exorcist; }
    [Serializable] private class WearRequest { public string item; public bool wear; }

    private const float PollInterval = 60f;
    private const string SeenKey = "temple.night.seen";

    public static TempleClient Instance { get; private set; }
    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public event Action<State> Changed;

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
        catch (Exception e) { Debug.LogWarning($"[Temple] {e.Message}"); }
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

    public bool Owns(string id)
    {
        if (Current?.items == null) return false;
        foreach (var i in Current.items) if (i.id == id) return i.owned;
        return false;
    }

    public bool Banished(string spot)
    {
        if (Current?.banished == null) return false;
        foreach (var s in Current.banished) if (s == spot) return true;
        return false;
    }

    // A power of the amulets worn right now (0 when none / offline)
    public int PowerOf(string id)
    {
        if (Current?.my_powers == null) return 0;
        foreach (var p in Current.my_powers) if (p.id == id) return p.value;
        return 0;
    }

    public static int Buddha(string id) => Instance != null ? Instance.PowerOf(id) : 0;

    public int WornCount
    {
        get
        {
            int n = 0;
            if (Current?.items != null) foreach (var i in Current.items) if (i.worn) n++;
            return n;
        }
    }

    public Ghost GhostInfo(string id)
    {
        if (Current?.ghosts == null) return null;
        foreach (var g in Current.ghosts) if (g.id == id) return g;
        return null;
    }

    public Task<State> RefreshAsync() => Call("temple_get");
    public Task<State> OrdainAsync() => Call("temple_ordain");
    public Task<State> DisrobeAsync(bool exorcist) => Call("temple_disrobe", JsonUtility.ToJson(new DisrobeRequest { exorcist = exorcist }));
    public Task<State> BuyAsync(string item) => Call("temple_buy", JsonUtility.ToJson(new ItemRequest { item = item }));
    public Task<State> WearAsync(string item, bool wear) => Call("temple_wear", JsonUtility.ToJson(new WearRequest { item = item, wear = wear }));
    public Task<State> MeditateStartAsync(bool incense) => Call("temple_meditate", JsonUtility.ToJson(new MeditateRequest { action = "start", incense = incense }));
    public Task<State> MeditateFinishAsync(string token) => Call("temple_meditate", JsonUtility.ToJson(new MeditateRequest { action = "finish", token = token }));
    public Task<State> GhostStartAsync(string spot, string ghost) => Call("ghost_start", JsonUtility.ToJson(new GhostRequest { spot = spot, ghost = ghost }));
    public Task<State> GhostFinishAsync(string token, int hits) => Call("ghost_finish", JsonUtility.ToJson(new GhostRequest { token = token, hits = hits }));

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        nextPoll = Time.time + PollInterval;
        Changed?.Invoke(state);
        TellNights(state);
        if (rpc != "temple_get")
        {
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            if (rpc == "temple_ordain" || rpc == "temple_disrobe")
            {
                if (ProfileClient.Instance != null) _ = ProfileClient.Instance.ReloadLookAsync();
                CareerClient.Instance?.Reload();
            }
        }
        return state;
    }

    // What came for the home since we last looked: say it once, over the player
    private void TellNights(State state)
    {
        if (state.log == null || state.log.Length == 0) return;
        int seen = PlayerPrefs.GetInt(SeenKey, -1);
        int newest = state.log[0].seq;
        if (seen < 0) { PlayerPrefs.SetInt(SeenKey, newest); return; } // first run: only what happens from now on
        if (newest <= seen) return;
        for (int i = state.log.Length - 1; i >= 0; i--)
        {
            var n = state.log[i];
            if (n.seq <= seen) continue;
            UiKit.FloatAbovePlayer(n.text, n.saved ? new Color(0.55f, 0.95f, 0.6f) : new Color(1f, 0.55f, 0.45f));
        }
        GameAudio.Play(state.log[0].kind == "ghost" ? "ghost" : "bell");
        PlayerPrefs.SetInt(SeenKey, newest);
    }

    // A Thai message for a refusal from temple.lua
    public static string Explain(ApiResponseException e)
    {
        string m = e.Message;
        return m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("not enough energy") ? "พลังงานไม่พอ — พักก่อน"
            : m.Contains("sleeping") ? "กำลังนอนอยู่"
            : m.Contains("monk level") ? "ต้องเป็นพระเลเวล 30 ก่อนจึงสึกออกมาเป็นหมอผีได้"
            : m.Contains("already ordained") ? "บวชอยู่แล้ว"
            : m.Contains("not ordained") ? "ยังไม่ได้บวช"
            : m.Contains("barami") ? "บารมียังไม่พอ — นั่งสมาธิในโบสถ์เพิ่มบารมีก่อน"
            : m.Contains("level") ? "เลเวลยังไม่ถึง"
            : m.Contains("already owned") ? "มีอยู่แล้ว"
            : m.Contains("neck full") ? "ห้อยพระได้ไม่เกิน 3 องค์ — ถอดองค์อื่นก่อน"
            : m.Contains("not owned") ? "ต้องบูชาก่อน"
            : m.Contains("not exorcist") ? "ต้องเป็นหมอผีก่อน (บวชจนพระเลเวล 30 แล้วสึกออกมา)"
            : m.Contains("daytime") ? "ผีออกเฉพาะตอนกลางคืน (19:00-05:00)"
            : m.Contains("banished") ? "ปราบผีตัวนี้ไปแล้วคืนนี้"
            : m.Contains("too soon") ? "ยังไม่ครบเวลา"
            : "ทำรายการไม่สำเร็จ";
    }
}
