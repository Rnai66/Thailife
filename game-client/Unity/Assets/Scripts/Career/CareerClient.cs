using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;

// Boxer career on the client: loads stats from the server, batches training hits and reports them.
// The server (career.lua) owns XP and levels; this class only displays and forwards.
[RequireComponent(typeof(NakamaConnection))]
public class CareerClient : MonoBehaviour
{
    [Serializable]
    public class Stats
    {
        public int level = 1;
        public int xp;
        public int xp_to_next = 40;
        public int total_hits;
        public int accepted;
        public int energy;      // พลังงาน left after this training report (0 max_energy = not sent)
        public int max_energy;
        public bool tired;      // some hits didn't count: out of energy
    }

    // Energy per hit by training target (same numbers as career.lua; the server's are the real ones)
    public static float EnergyPerHit(string target) => target == "camp" ? 0.5f : target == "bag" ? 0.4f : 0.3f;

    [Serializable]
    private class TrainRequest
    {
        public string target;
        public int hits;
    }

    [SerializeField] private float flushInterval = 2f;

    public static CareerClient Instance { get; private set; }

    public Stats Current { get; private set; }
    public bool IsOnline => Current != null;
    public int Level => Current?.level ?? 1;

    // (previous, current) — previous is null on the first load
    public event Action<Stats, Stats> StatsChanged;

    private NakamaConnection connection;
    private readonly Dictionary<string, int> pendingHits = new Dictionary<string, int>();
    private float nextFlushTime;
    private bool flushing;

    public static string RankFor(int level) =>
        level >= 100 ? "ตำนานมวยไทย" :
        level >= 50 ? "ยอดแชมป์" :
        level >= 20 ? "แชมป์" :
        level >= 15 ? "ยอดมวย" :
        level >= 10 ? "นักมวยดาวรุ่ง" :
        level >= 6 ? "นักมวยค่าย" :
        level >= 3 ? "นักมวยหน้าใหม่" :
        "เด็กบ้านนา";

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

    // Re-read level/XP after something else on the server changed them (a fight, pad work)
    public void Reload()
    {
        if (connection != null && connection.IsConnected) OnConnected(connection);
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try
        {
            var result = await conn.Client.RpcAsync(conn.Session, "career_get", "{}");
            Apply(JsonUtility.FromJson<Stats>(result.Payload));
            Debug.Log($"[Career] Loaded: Lv {Current.level}, XP {Current.xp}/{Current.xp_to_next}, {Current.total_hits} hits");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Career] Could not load stats: {e.Message}");
        }
    }

    public void RecordHit(string targetId)
    {
        pendingHits.TryGetValue(targetId, out int n);
        pendingHits[targetId] = n + 1;
    }

    private void Update()
    {
        if (flushing || pendingHits.Count == 0 || !connection.IsConnected || Time.time < nextFlushTime) return;
        nextFlushTime = Time.time + flushInterval;
        Flush();
    }

    private async void Flush()
    {
        flushing = true;
        var batch = new Dictionary<string, int>(pendingHits);
        pendingHits.Clear();

        foreach (var entry in batch)
        {
            try
            {
                string payload = JsonUtility.ToJson(new TrainRequest { target = entry.Key, hits = entry.Value });
                var result = await connection.Client.RpcAsync(connection.Session, "career_train", payload);
                var stats = JsonUtility.FromJson<Stats>(result.Payload);
                if (stats.max_energy > 0) LifeClient.Instance?.Apply(stats.energy, stats.max_energy, false);
                if (stats.tired) Tired();
                Apply(stats);
            }
            catch (ApiResponseException e)
            {
                if (e.Message.Contains("energy")) Tired();
                if (e.Message.Contains("injured"))
                    UiKit.FloatAbovePlayer("บาดเจ็บ! ไปรักษาที่ รพ.สต. หรือโรงพยาบาล", new Color(1f, 0.45f, 0.4f));
                LifeClient.Instance?.Refresh();
                Debug.LogWarning($"[Career] Training '{entry.Key}' rejected: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] Training report failed: {e.Message}");
            }
        }

        flushing = false;
    }

    private static void Tired() =>
        UiKit.FloatAbovePlayer("เหนื่อยแล้ว! กินอะไรหน่อยหรือนอนพัก", new Color(1f, 0.55f, 0.35f));

    private void Apply(Stats stats)
    {
        var previous = Current;
        Current = stats;

        var combat = FindAnyObjectByType<PlayerCombat>();
        if (previous == null || previous.level != stats.level)
        {
            if (combat != null) combat.ApplyCareerLevel(stats.level);

            if (previous != null && combat != null)
            {
                FloatingText.Spawn(combat.transform.position + Vector3.up * 2f, $"LEVEL UP! Lv {stats.level}", new Color(1f, 0.85f, 0.2f));
                Debug.Log($"[Career] Level up → {stats.level} ({RankFor(stats.level)})");
            }
        }

        StatsChanged?.Invoke(previous, stats);
    }
}
