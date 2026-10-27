using System;
using UnityEngine;

// Daily quests (ภารกิจประจำวัน) and level quests (ภารกิจตามเลเวล, one chapter at levels 1, 10, 25,
// 50, 75, 100) from quests.lua. The server counts progress when the player fishes, farms, feeds
// chickens, plays music, trains...; this client polls it, tells the player when a quest is
// finished or a chapter opens, and claims rewards.
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
    public class Task
    {
        public string kind;
        public string title;
        public int need;
        public int have;
    }

    [Serializable]
    public class Chapter
    {
        public int level;
        public string title;
        public string story;
        public int reward;
        public Task[] tasks;
        public bool unlocked;
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
        public Chapter[] chapters;
        public int level;
    }

    [Serializable]
    private class ClaimRequest
    {
        public int quest;
        public bool bonus;
        public int chapter;
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
            if (LevelRewardReady) return true;
            return Current.all_done && !Current.bonus_claimed;
        }
    }

    // A level chapter is finished but its reward not claimed yet
    public bool LevelRewardReady
    {
        get
        {
            if (Current?.chapters != null) foreach (var c in Current.chapters) if (c.done && !c.claimed) return true;
            return false;
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

    private async System.Threading.Tasks.Task RefreshQuietly()
    {
        try { await CallAsync("quests_get"); }
        catch (Exception e) { Debug.LogWarning($"[Quests] {e.Message}"); }
    }

    public System.Threading.Tasks.Task<State> ClaimAsync(int questId) => CallAsync("quests_claim", JsonUtility.ToJson(new ClaimRequest { quest = questId }));
    public System.Threading.Tasks.Task<State> ClaimChapterAsync(int level) => CallAsync("quests_claim", JsonUtility.ToJson(new ClaimRequest { chapter = level }));
    public System.Threading.Tasks.Task<State> ClaimBonusAsync() => CallAsync("quests_claim", JsonUtility.ToJson(new ClaimRequest { bonus = true }));

    private async System.Threading.Tasks.Task<State> CallAsync(string rpc, string payload = "{}")
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

    // Pop a message over the player when a quest gets finished or a level chapter opens
    private static void Announce(State before, State after)
    {
        if (before?.chapters != null && after?.chapters != null)
            foreach (var c in after.chapters)
                foreach (var old in before.chapters)
                    if (old.level == c.level)
                    {
                        if (c.unlocked && !old.unlocked)
                            UiKit.FloatAbovePlayer($"ภารกิจเลเวล {c.level} เปิดแล้ว! {c.title}", new Color(1f, 0.85f, 0.3f));
                        else if (c.done && !old.done)
                            UiKit.FloatAbovePlayer($"ภารกิจเลเวล {c.level} สำเร็จ! ไปรับรางวัลได้เลย", new Color(1f, 0.85f, 0.3f));
                    }
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
