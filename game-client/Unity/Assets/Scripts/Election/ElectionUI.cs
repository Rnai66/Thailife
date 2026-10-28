using System;
using UnityEngine;
using UnityEngine.UI;

// Election window (เลือกตั้ง อบต., election.lua; one election every 4 game years). At the อบต. office ("sao") the player registers as a
// candidate for ส.อบต. or นายก อบต. and, once elected, draws the daily salary; at the village hall
// ("hall") they campaign (หาเสียง) and, on voting days, cast a vote per position. Both show the live
// standings of each position and the countdown to the next phase.
public class ElectionUI : MonoBehaviour
{
    private const int MaxRows = 6;

    private static ElectionUI instance;

    private class PositionCard
    {
        public Text header, info;
        public RectTransform rows;
    }

    private GameObject backdrop;
    private Text titleText, subtitleText, phaseText, meText, hintText, toastText, campaignText;
    private readonly PositionCard[] cards = new PositionCard[2];
    private GameObject saoActions, hallActions;
    private Button registerMember, registerMayor, salaryButton, campaignButton;
    private Coroutine toast;
    private string mode = "sao";
    private bool busy, portrait;
    private float cardWidth, nextTick;

    public static void Show(string mode)
    {
        if (instance == null) instance = new GameObject("ElectionUI").AddComponent<ElectionUI>();
        instance.mode = mode;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "ElectionCanvas", UiKit.WindowSize(new Vector2(1400f, 940f), 1500f), 56, "icon:fair",
            out backdrop, out titleText, out subtitleText, Hide, out _);

        // Status: the phase and its countdown, then the player's own standing
        float w = portrait ? 690f : 1300f;
        var status = UiKit.Panel("Status", body, Color.white, new Vector2(0f, portrait ? 590f : 340f), new Vector2(w, portrait ? 130f : 90f), shadow: true);
        status.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        phaseText = UiKit.Label(status, portrait ? 28 : 26, "", new Vector2(0f, portrait ? 30f : 20f), new Vector2(w - 30f, 40f), UiKit.Ink);
        phaseText.fontStyle = FontStyle.Bold;
        meText = UiKit.Label(status, 22, "", new Vector2(0f, portrait ? -25f : -20f), new Vector2(w - 30f, portrait ? 60f : 36f), UiKit.Price);
        meText.lineSpacing = 1.1f;

        // One card per position: side by side in landscape, stacked when upright
        cardWidth = portrait ? 690f : 640f;
        for (int i = 0; i < cards.Length; i++)
            cards[i] = BuildCard(body, portrait ? new Vector2(0f, 290f - i * 440f) : new Vector2(i == 0 ? -330f : 330f, 60f));

        float actionsY = portrait ? -465f : -230f;
        saoActions = UiKit.Container(body, new Vector2(0f, actionsY)).gameObject;
        float bw = portrait ? 220f : 400f, step = portrait ? 235f : 420f;
        registerMember = UiKit.RoundButton(saoActions.transform, "", new Vector2(-step, 0f), new Vector2(bw, 100f), UiKit.Gold, UiKit.GoldInk, () => Register("member"), 21);
        registerMayor = UiKit.RoundButton(saoActions.transform, "", new Vector2(0f, 0f), new Vector2(bw, 100f), UiKit.Gold, UiKit.GoldInk, () => Register("mayor"), 21);
        salaryButton = UiKit.RoundButton(saoActions.transform, "", new Vector2(step, 0f), new Vector2(bw, 100f), UiKit.PayGreen, Color.white, Salary, 21);

        hallActions = UiKit.Container(body, new Vector2(0f, actionsY)).gameObject;
        campaignButton = UiKit.RoundButton(hallActions.transform, "", Vector2.zero, new Vector2(portrait ? 690f : 900f, 100f), UiKit.Gold, UiKit.GoldInk, Campaign, 26);
        campaignText = campaignButton.GetComponentInChildren<Text>();

        hintText = UiKit.Label(body, 21, "", new Vector2(0f, portrait ? -560f : -310f), new Vector2(w, portrait ? 76f : 50f), UiKit.Muted);
        hintText.lineSpacing = 1.1f;
        toastText = UiKit.ToastBar(body, new Vector2(0f, portrait ? -630f : -360f), 1000f);
        backdrop.SetActive(false);
        ElectionClient.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        ElectionClient.Changed -= OnChanged;
        if (instance == this) instance = null;
    }

    private void OnChanged(ElectionClient.State s)
    {
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    private PositionCard BuildCard(RectTransform body, Vector2 at)
    {
        var c = new PositionCard();
        var card = UiKit.Panel("Position", body, Color.white, at, new Vector2(cardWidth, 430f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        c.header = UiKit.Label(card, 30, "", new Vector2(0f, 180f), new Vector2(cardWidth - 30f, 44f), UiKit.Ink);
        c.header.font = ThaiFont.Display;
        c.info = UiKit.Label(card, 20, "", new Vector2(0f, 140f), new Vector2(cardWidth - 30f, 32f), UiKit.Muted);
        c.rows = UiKit.Container(card, Vector2.zero);
        return c;
    }

    private async void Refresh()
    {
        if (!ElectionClient.Online) { Notify("ต้องออนไลน์ก่อน", UiKit.Danger); return; }
        try { await ElectionClient.GetAsync(); }
        catch (Exception e) { Notify(ElectionClient.Explain(e), UiKit.Danger); }
    }


    private static int Left(ElectionClient.State s) =>
        Mathf.CeilToInt(s.phase_ends_s - (Time.time - ElectionClient.ReceivedAt));

    private void RenderPhase(ElectionClient.State s)
    {
        phaseText.text = s.phase == "vote"
            ? $"วาระที่ {s.cycle} · วันลงคะแนน! · ปิดหีบในอีก {GameClock.Countdown(Left(s))}"
            : $"วาระที่ {s.cycle} ปีที่ {s.year}/{s.years} · รับสมัครและหาเสียง · เปิดหีบในอีก {GameClock.Countdown(Left(s))}";
    }

    private void Render()
    {
        var s = ElectionClient.Current;
        bool hall = mode == "hall";
        titleText.text = hall ? "เลือกตั้ง · ศาลากลางบ้าน" : "สมัครรับเลือกตั้ง · อบต.";
        saoActions.SetActive(!hall);
        hallActions.SetActive(hall);

        if (s?.positions == null)
        {
            subtitleText.text = "กำลังโหลด…";
            phaseText.text = meText.text = hintText.text = "";
            foreach (var c in cards) { c.header.text = c.info.text = ""; UiKit.Clear(c.rows); }
            registerMember.interactable = registerMayor.interactable = salaryButton.interactable = campaignButton.interactable = false;
            return;
        }

        subtitleText.text = hall
            ? "หาเสียงให้ตัวเอง แล้วมาลงคะแนนในวันเลือกตั้ง"
            : "ลงสมัคร ส.อบต. หรือ นายก อบต. · ชนะแล้วรับเงินเดือนทุกวัน";
        RenderPhase(s);

        string me = $"เลเวล {s.level} · นักการเมือง Lv {s.politics}";
        if (!string.IsNullOrEmpty(s.office)) me += $" · ดำรงตำแหน่ง {s.office_name}";
        var mine = Find(s, s.registered);
        me += mine != null ? $"{(portrait ? "\n" : " · ")}ลงสมัคร {mine.name} อยู่" : $"{(portrait ? "\n" : " · ")}ยังไม่ได้ลงสมัครครั้งนี้";
        meText.text = me;

        for (int i = 0; i < cards.Length && i < s.positions.Length; i++) RenderCard(cards[i], s.positions[i], s, hall);

        if (hall)
        {
            bool candidate = mine != null;
            campaignText.text = candidate
                ? $"หาเสียง {mine.campaign_price:N0} ฿ · พลังงาน {s.energy} · เหลือวันนี้ {s.campaigns_left} ครั้ง"
                : "ต้องลงสมัครที่ อบต. ก่อนจึงหาเสียงได้";
            campaignButton.interactable = candidate && s.campaigns_left > 0 && !busy;
            hintText.text = s.phase == "vote"
                ? "วันเลือกตั้ง! กด \"เลือก\" ข้างชื่อผู้สมัครได้ตำแหน่งละ 1 เสียง (+10 คะแนน)"
                : "เลือกตั้งทุก 4 ปี ลงคะแนนได้ 7 วันสุดท้ายของวาระ · หาเสียงได้วันละ 3 ครั้ง ยิ่งเลเวลสูงยิ่งได้เสียงมาก";
            return;
        }

        bool open = s.phase == "register" && string.IsNullOrEmpty(s.registered);
        SetRegister(registerMember, Find(s, "member"), s, open);
        SetRegister(registerMayor, Find(s, "mayor"), s, open);
        bool inOffice = !string.IsNullOrEmpty(s.office);
        var office = Find(s, s.office);
        salaryButton.GetComponentInChildren<Text>().text = !inOffice ? "รับเงินเดือน\n(ต้องชนะเลือกตั้ง)"
            : s.salary_today ? "วันนี้รับเงินเดือนแล้ว" : $"รับเงินเดือน\n{office?.salary ?? 0:N0} ฿";
        salaryButton.interactable = inOffice && !s.salary_today && !busy;
        hintText.text = LastResults(s);
    }

    private void SetRegister(Button b, ElectionClient.Position p, ElectionClient.State s, bool open)
    {
        if (p == null) { b.interactable = false; return; }
        var label = b.GetComponentInChildren<Text>();
        label.text = s.registered == p.id ? $"ลงสมัคร {p.@short}\nแล้ว ✓"
            : s.level < p.level ? $"{p.@short}\nต้องเลเวล {p.level}"
            : $"สมัคร {p.@short}\n{p.fee:N0} ฿";
        b.interactable = open && s.level >= p.level && !busy;
    }

    private static string LastResults(ElectionClient.State s)
    {
        if (s.last_results == null || s.last_results.Length == 0)
            return "ยังไม่มีผลเลือกตั้งครั้งก่อน · ค่าสมัครไม่คืน · ผู้ชนะดำรงตำแหน่ง 4 ปี รับเงินเดือนทุกวัน";
        var parts = new System.Collections.Generic.List<string>();
        foreach (var r in s.last_results)
        {
            var names = new System.Collections.Generic.List<string>();
            if (r.winners != null) foreach (var w in r.winners) names.Add(w.me ? $"{w.name} (คุณ)" : w.name);
            parts.Add($"{r.name}: {string.Join(", ", names)}");
        }
        return "ผลครั้งก่อน — " + string.Join(" · ", parts);
    }

    private static ElectionClient.Position Find(ElectionClient.State s, string id)
    {
        if (s?.positions != null && !string.IsNullOrEmpty(id))
            foreach (var p in s.positions) if (p.id == id) return p;
        return null;
    }

    private void RenderCard(PositionCard card, ElectionClient.Position p, ElectionClient.State s, bool hall)
    {
        card.header.text = p.name;
        card.info.text = $"{p.seats} ที่นั่ง · เงินเดือน {p.salary:N0} ฿/วัน · สมัคร Lv {p.level} ค่าสมัคร {p.fee:N0} ฿";
        UiKit.Clear(card.rows);
        bool canVote = hall && s.phase == "vote" && string.IsNullOrEmpty(p.my_vote);
        float rw = cardWidth - 30f;
        int n = p.candidates == null ? 0 : Mathf.Min(MaxRows, p.candidates.Length);
        for (int i = 0; i < n; i++)
        {
            var c = p.candidates[i];
            bool seat = i < p.seats;
            var row = UiKit.Panel("Row", card.rows, c.me ? UiKit.OkTint : seat ? new Color32(0xFF, 0xF3, 0xD0, 0xFF) : UiKit.Cream,
                new Vector2(0f, 90f - i * 50f), new Vector2(rw, 44f));
            if (seat) row.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
            float left = -rw * 0.5f;
            UiKit.Label(row, 22, $"{i + 1}", new Vector2(left + 24f, 0f), new Vector2(40f, 40f), seat ? UiKit.Price : UiKit.Muted).fontStyle = FontStyle.Bold;
            UiKit.Label(row, 23, c.me ? $"{c.name} (คุณ)" : c.npc ? c.name : $"{c.name} · ผู้เล่น",
                new Vector2(left + 60f + rw * 0.2f, 0f), new Vector2(rw * 0.4f, 40f), UiKit.Ink, TextAnchor.MiddleLeft);
            UiKit.Label(row, 22, $"{c.votes:N0} เสียง", new Vector2(rw * 0.5f - 230f, 0f), new Vector2(150f, 40f), UiKit.Price, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;

            string id = c.id, pos = p.id;
            if (p.my_vote == c.id)
                UiKit.PillLabel(row, "เลือกแล้ว ✓", new Vector2(rw * 0.5f - 72f, 0f), new Vector2(130f, 36f), UiKit.PayGreen, Color.white, 18);
            else if (canVote)
            {
                var b = UiKit.RoundButton(row, "เลือก", new Vector2(rw * 0.5f - 72f, 0f), new Vector2(130f, 38f), UiKit.Gold, UiKit.GoldInk, () => Vote(pos, id), 20);
                b.interactable = !busy;
            }
        }
        if (p.candidates != null && p.candidates.Length > MaxRows)
            UiKit.Label(card.rows, 18, $"และผู้สมัครอีก {p.candidates.Length - MaxRows} คน", new Vector2(0f, 90f - MaxRows * 50f + 10f), new Vector2(rw, 26f), UiKit.Muted);
    }

    private void Update()
    {
        if (backdrop == null || !backdrop.activeSelf || Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 1f;
        var s = ElectionClient.Current;
        if (s?.positions == null) return;
        if (Left(s) < -2 && !busy) { Refresh(); return; } // phase changed: reload the standings
        RenderPhase(s);
    }

    private void Register(string position) => Run(() => ElectionClient.RegisterAsync(position), "coins");
    private void Campaign() => Run(ElectionClient.CampaignAsync, "whoosh");
    private void Vote(string position, string candidate) => Run(() => ElectionClient.VoteAsync(position, candidate), "whoosh");
    private void Salary() => Run(ElectionClient.SalaryAsync, "coins");

    private async void Run(Func<System.Threading.Tasks.Task<ElectionClient.State>> call, string sound)
    {
        if (busy) return;
        busy = true;
        Render();
        try
        {
            var r = await call();
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play(sound, null, 0.6f);
            if (r.gained > 0) UiKit.FloatAbovePlayer($"+{r.gained} เสียง", new Color(1f, 0.85f, 0.3f));
            if (r.earned > 0) UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.3f));
            LifeClient.Instance?.Refresh();
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            CareerClient.Instance?.Reload();
        }
        catch (Exception e)
        {
            Notify(ElectionClient.Explain(e), UiKit.Danger);
            Refresh();
        }
        busy = false;
        Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
