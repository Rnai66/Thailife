using UnityEngine;

// พระบิณฑบาต: every morning (05:30-09:00 game time, temple.lua ALMS) a line of monks (MonkNpc) walks
// barefoot from the temple along the road with their alms bowls, in single file, eyes down, and back.
// 5 monks on an ordinary day, 10 on วันพระ, 20 on the holy days (เข้าพรรษา, ออกพรรษา, ตักบาตรเทโว,
// กฐิน, ผ้าป่า, …) — the number comes from the server (TempleClient alms). Walk up to a monk to
// ตักบาตร (AlmsUI): the whole line waits while he receives and blesses you.
public class MonkProcession : MonoBehaviour
{
    [SerializeField] private Vector3 from, to;   // world points along the road
    [SerializeField] private int monks = 3;      // (before the server says)

    private const float Gap = 1.7f, Reach = 2.4f;
    private const int DefaultFrom = 330, DefaultTo = 540;

    private MonkNpc[] line;
    private float lead;   // how far the first monk has come along the out-and-back path (metres)
    private float nextCheck;
    private int shownDay = -1;
    private PlayerController player;
    private MonkNpc offered;

    public static MonkProcession Current { get; private set; }

    public void Configure(Vector3 start, Vector3 end, int count)
    {
        from = start;
        to = end;
        monks = count;
    }

    private void OnEnable() => Current = this;
    private void OnDisable()
    {
        if (Current == this) Current = null;
        if (offered != null) ContextButton.Hide(this);
    }

    private static TempleClient.Alms Alms => TempleClient.Instance?.Current?.alms;
    private static int WindowFrom => Alms != null && Alms.to > 0 ? Alms.from : DefaultFrom;
    private static int WindowTo => Alms != null && Alms.to > 0 ? Alms.to : DefaultTo;
    private int Count => Alms != null && Alms.monks > 0 ? Alms.monks : Mathf.Max(monks, 5);
    private static bool Morning => GameClock.MinuteOfDay >= WindowFrom && GameClock.MinuteOfDay < WindowTo;

    // The line waits while someone is giving (the UI is open or a monk is receiving)
    public bool Waiting { get; set; }

    private float PathLength => Vector3.Distance(from, to) * 2f;

    // Where the head of the line should be by the clock
    private float LeadByClock(int count)
    {
        float u = Mathf.Clamp01((GameClock.MinuteOfDay - WindowFrom) / Mathf.Max(1f, WindowTo - WindowFrom));
        return u * (PathLength + (count - 1) * Gap);
    }

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 2f;
            if (Morning && (line == null || line.Length != Count || shownDay != GameClock.Day)) Spawn();
            else if (!Morning && line != null) Clear();
        }
        if (line == null) return;

        bool hold = Waiting;
        foreach (var m in line) if (m != null && m.Receiving) hold = true;
        // keep to the clock, catching up after waiting for someone
        float target = LeadByClock(line.Length);
        lead = Mathf.MoveTowards(lead, target, hold ? 0f : Time.deltaTime * 1.2f);

        float half = PathLength * 0.5f;
        Vector3 dir = (to - from).normalized;
        for (int i = 0; i < line.Length; i++)
        {
            var m = line[i];
            if (m == null) continue;
            float s = Mathf.Clamp(lead - i * Gap, 0f, PathLength);
            bool back = s > half;
            Vector3 p = back ? Vector3.Lerp(to, from, (s - half) / half) : Vector3.Lerp(from, to, s / half);
            p += Vector3.Cross(Vector3.up, dir) * (back ? -0.9f : 0.9f); // keep to one side of the road
            if (!m.Receiving)
            {
                m.transform.position = p;
                m.transform.rotation = Quaternion.LookRotation(back ? -dir : dir);
            }
        }
        OfferNearest();
    }

    // The monk nearest the player that you haven't given to yet today: offer ใส่บาตร
    private void OfferNearest()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        MonkNpc best = null;
        var temple = TempleClient.Instance;
        if (player != null && !Waiting && temple != null && temple.IsOnline)
        {
            float bestD = Reach * Reach;
            foreach (var m in line)
            {
                if (m == null) continue;
                float d = (m.transform.position - player.transform.position).sqrMagnitude;
                if (d < bestD && !temple.GaveAlms(m.Index)) { bestD = d; best = m; }
            }
        }
        if (best == offered) return;
        offered = best;
        if (best == null) { ContextButton.Hide(this); return; }
        var a = Alms;
        string why = a != null && !string.IsNullOrEmpty(a.day_name) ? $"{a.day_name} · พระออกบิณฑบาต {line.Length} รูป · " : "";
        int barami = a != null && a.barami > 0 ? a.barami : 8;
        var monk = best;
        ContextButton.Show(this, "ใส่บาตร", $"{why}ใส่บาตรด้วยอาหารในกระเป๋า · บารมี +{barami} ต่อพระ 1 รูป", () => AlmsUI.Open(this, monk));
    }

    // After giving: that monk can't be offered again today
    public void Given()
    {
        offered = null;
        ContextButton.Hide(this);
    }

    private void Spawn()
    {
        Clear();
        int count = Count;
        line = new MonkNpc[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Monk_พระ_{i + 1}");
            go.transform.SetParent(transform, false);
            var m = go.AddComponent<MonkNpc>();
            m.Init(i + 1, i * 7 + GameClock.Day);
            line[i] = m;
        }
        shownDay = GameClock.Day;
        if (TempleClient.Instance != null) TempleClient.Instance.Poke(); // how many walk today (and who you gave to)
        lead = LeadByClock(count);
        var a = Alms;
        if (a != null && !string.IsNullOrEmpty(a.day_name) && count > 5 && player != null
            && (player.transform.position - from).sqrMagnitude < 120f * 120f)
            UiKit.FloatAbovePlayer($"{a.day_name} · พระออกบิณฑบาต {count} รูป", new Color(1f, 0.82f, 0.35f));
    }

    private void Clear()
    {
        if (line != null) foreach (var m in line) if (m != null) Destroy(m.gameObject);
        line = null;
        offered = null;
        ContextButton.Hide(this);
    }
}
