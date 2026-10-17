using System;
using System.Threading.Tasks;
using UnityEngine;

// Daily quests (ภารกิจประจำวัน) from quests.lua. The server counts progress when the player
// fishes, farms, feeds chickens, plays music, trains...; this client polls it, tells the player
// when a quest is finished, and claims rewards.
[RequireComponent(typeof(NakamaConnection))]
public class QuestClient : MonoBehaviour
{
    [Serializable]
    public class Quest
    {
        public int id;
        public string kind;
        public string title;
        public string hint;
        public int need;
        public int have;
        public int reward;
        public bool done;
        public bool claimed;
    }

    [Serializable]
    public class State
    {
        public int day;
        public Quest[] quests;
        public int bonus;
        public bool all_done;
        public bool bonus_claimed;
        public int resets_in_s;
        public int earned;
    }

    [Serializable]
    private class ClaimRequest
    {
        public int quest;
        public bool bonus;
    }

    private const float PollInterval = 10f;

    public static QuestClient Instance { get; private set; }

    public State Current { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected && Current != null;
    public float Age => Time.time - receivedAt; // seconds since Current arrived

    public int DoneCount
    {
        get
        {
            int n = 0;
            if (Current?.quests != null) foreach (var q in Current.quests) if (q.done) n++;
            return n;
        }
    }

    // Something finished but not claimed yet
    public bool HasReward
    {
        get
        {
            if (Current?.quests == null) return false;
            foreach (var q in Current.quests) if (q.done && !q.claimed) return true;
            return Current.all_done && !Current.bonus_claimed;
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

    private void OnConnected(NakamaConnection conn) => Refresh();

    private void Update()
    {
        if (!connection.IsConnected || Time.time < nextPoll) return;
        Refresh();
    }

    public void Refresh()
    {
        nextPoll = Time.time + PollInterval;
        _ = RefreshQuietly();
    }

    private async Task RefreshQuietly()
    {
        try { await CallAsync("quests_get"); }
        catch (Exception e) { Debug.LogWarning($"[Quests] {e.Message}"); }
    }

    public Task<State> ClaimAsync(int questId) => CallAsync("quests_claim", JsonUtility.ToJson(new ClaimRequest { quest = questId }));
    public Task<State> ClaimBonusAsync() => CallAsync("quests_claim", JsonUtility.ToJson(new ClaimRequest { bonus = true }));

    private async Task<State> CallAsync(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Announce(Current, state);
        Current = state;
        receivedAt = Time.time;
        Changed?.Invoke(state);
        if (state.earned > 0 && WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return state;
    }

    // Pop a message over the player when a quest gets finished
    private static void Announce(State before, State after)
    {
        if (before?.quests == null || after?.quests == null || before.day != after.day) return;
        foreach (var q in after.quests)
        {
            if (!q.done) continue;
            foreach (var old in before.quests)
                if (old.id == q.id && !old.done)
                    UiKit.FloatAbovePlayer($"ภารกิจสำเร็จ! {q.title}", new Color(1f, 0.85f, 0.3f));
        }
    }
}
