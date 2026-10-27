using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Quests window, two tabs. ภารกิจประจำวัน: summary + countdown to the next set, one card per quest
// with a progress bar and its reward, and the all-done bonus at the bottom. ภารกิจตามเลเวล: the
// chapters at levels 1, 10, 25, 50, 75, 100 down the left, the picked chapter's tasks and big
// reward on the right. Opened from the "ภารกิจ" button on the HUD.
public class QuestUI : MonoBehaviour
{
    private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
    {
        { "fish", "ปลา" }, { "sell_produce", "ขาย" }, { "plant", "ปลูก" }, { "harvest", "เกี่ยว" },
        { "feed", "ไก่" }, { "sell_eggs", "ไข่" }, { "music", "ดนตรี" }, { "train", "มวย" },
        { "eat", "กิน" }, { "shop", "ซื้อ" }, { "study", "เรียน" },
    };

    private static QuestUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, summaryText, resetText, toastText;
    private RectTransform list, summaryFill;
    private Button bonusButton;
    private Text bonusNote;
    private Coroutine toast;
    private bool busy, portrait;
    private float SummaryWidth => portrait ? 620f : 480f;
    private float nextTick;
    private RectTransform dailyView, levelView, chapterList, chapterDetail;
    private Image dailyTab, levelTab;
    private bool showLevels;
    private int pickedChapter = -1; // level of the chapter shown; -1 = pick the one to work on

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("QuestUI").AddComponent<QuestUI>();
        instance.backdrop.SetActive(true);
        QuestClient.Instance?.Refresh();
        instance.Render();
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "QuestCanvas", UiKit.WindowSize(new Vector2(1300f, 920f), 1400f), 55, "ภ",
            out backdrop, out titleText, out subtitleText, Close, out var header);
        // The two tabs sit in the header; upright the header is too narrow, so they go across the top of the body
        Transform tabRow = portrait ? body : header;
        dailyTab = UiKit.RoundButton(tabRow, "ประจำวัน", portrait ? new Vector2(-170f, 582f) : new Vector2(170f, 0f), new Vector2(portrait ? 320f : 200f, 64f), UiKit.Gold, UiKit.GoldInk,
            () => { showLevels = false; Render(); }, 26).GetComponent<Image>();
        levelTab = UiKit.RoundButton(tabRow, "ตามเลเวล", portrait ? new Vector2(170f, 582f) : new Vector2(385f, 0f), new Vector2(portrait ? 320f : 200f, 64f), UiKit.Pill, UiKit.GoldInk,
            () => { showLevels = true; Render(); }, 26).GetComponent<Image>();

        // The daily quests and the level chapters each live in their own container, one shown at a time
        var window = body;
        dailyView = UiKit.Container(window, Vector2.zero);
        levelView = UiKit.Container(window, Vector2.zero);
        chapterList = UiKit.Container(levelView, Vector2.zero);
        chapterDetail = UiKit.Container(levelView, Vector2.zero);
        body = dailyView;

        var summary = UiKit.Panel("Summary", body, Color.white, new Vector2(0f, portrait ? 490f : 300f), new Vector2(portrait ? 690f : 1180f, portrait ? 110f : 100f), shadow: true);
        summary.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        summaryText = UiKit.Label(summary, 30, "", portrait ? new Vector2(-170f, 22f) : new Vector2(-400f, 0f), new Vector2(portrait ? 320f : 340f, 50f), UiKit.Ink, TextAnchor.MiddleLeft);
        summaryText.fontStyle = FontStyle.Bold;
        var track = UiKit.Panel("Track", summary, new Color32(0xF1, 0xEF, 0xE8, 0xFF), portrait ? new Vector2(0f, -26f) : new Vector2(40f, 0f), new Vector2(SummaryWidth, 24f));
        summaryFill = UiKit.Panel("Fill", track, UiKit.Gold, Vector2.zero, new Vector2(0f, 24f));
        summaryFill.anchorMin = summaryFill.anchorMax = new Vector2(0f, 0.5f);
        summaryFill.pivot = new Vector2(0f, 0.5f);
        resetText = UiKit.Label(summary, 22, "", portrait ? new Vector2(170f, 22f) : new Vector2(450f, 0f), new Vector2(portrait ? 300f : 260f, 40f), UiKit.Muted, TextAnchor.MiddleRight);

        list = UiKit.Container(body, new Vector2(0f, portrait ? 350f : 165f));

        var bonus = UiKit.Panel("Bonus", body, new Color32(0xFF, 0xF6, 0xDF, 0xFF), new Vector2(0f, portrait ? -260f : -255f), new Vector2(portrait ? 690f : 1180f, portrait ? 170f : 110f), shadow: true);
        bonus.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(bonus, 28, "โบนัสทำครบทุกภารกิจ", portrait ? new Vector2(0f, 52f) : new Vector2(-330f, 18f), new Vector2(portrait ? 640f : 480f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        bonusNote = UiKit.Label(bonus, 22, "", portrait ? new Vector2(0f, 14f) : new Vector2(-330f, -22f), new Vector2(portrait ? 640f : 480f, 36f), UiKit.Muted, TextAnchor.MiddleLeft);
        bonusButton = UiKit.RoundButton(bonus, "", portrait ? new Vector2(0f, -45f) : new Vector2(410f, 0f), new Vector2(portrait ? 400f : 300f, portrait ? 64f : 72f), UiKit.Gold, UiKit.GoldInk, ClaimBonus, 28);

        toastText = UiKit.ToastBar(window, new Vector2(0f, portrait ? -540f : -345f), 760f);
        backdrop.SetActive(false);
        if (QuestClient.Instance != null) QuestClient.Instance.Changed += _ => { if (backdrop.activeSelf) Render(); };
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (!backdrop.activeSelf || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        var quests = QuestClient.Instance;
        if (quests == null || !quests.IsOnline) return;
        float left = quests.Current.resets_in_s - quests.Age;
        resetText.text = left > 0 ? $"ภารกิจใหม่ในอีก {Countdown(left)}" : "กำลังโหลดภารกิจใหม่...";
        if (left <= -2f && Time.time % 5f < 1f) quests.Refresh(); // a new game day started
    }

    private static string Countdown(float seconds)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{s / 60}:{s % 60:00}";
    }

    private void Render()
    {
        dailyView.gameObject.SetActive(!showLevels);
        levelView.gameObject.SetActive(showLevels);
        dailyTab.color = showLevels ? UiKit.Pill : UiKit.Gold;
        levelTab.color = showLevels ? UiKit.Gold : UiKit.Pill;
        titleText.text = showLevels ? "ภารกิจตามเลเวล" : "ภารกิจประจำวัน";
        subtitleText.text = showLevels ? "เปิดที่เลเวล 1 · 10 · 25 · 50 · 75 · 100 · รางวัลก้อนใหญ่" : "ทำให้ครบ รับเงินรางวัล · ภารกิจใหม่ทุกวันในเกม";
        if (showLevels) RenderLevels();
        else RenderDaily();
    }

    private void RenderDaily()
    {
        UiKit.Clear(list);
        var quests = QuestClient.Instance;
        if (quests == null || !quests.IsOnline)
        {
            summaryText.text = "ออฟไลน์";
            resetText.text = "";
            summaryFill.sizeDelta = new Vector2(0f, 24f);
            bonusNote.text = "";
            bonusButton.interactable = false;
            UiKit.Label(list, 28, "ต้องออนไลน์ก่อนถึงจะดูภารกิจได้", new Vector2(0f, -120f), new Vector2(900f, 60f), UiKit.Muted);
            return;
        }

        var s = quests.Current;
        int total = s.quests?.Length ?? 0;
        int done = quests.DoneCount;
        summaryText.text = $"ทำแล้ว {done}/{total}";
        summaryFill.sizeDelta = new Vector2(total > 0 ? SummaryWidth * done / total : 0f, 24f);

        if (s.quests != null)
            for (int i = 0; i < s.quests.Length; i++)
                AddQuestCard(s.quests[i], i);

        bonusNote.text = s.bonus_claimed ? "รับโบนัสวันนี้แล้ว" : s.all_done ? "ทำครบแล้ว! กดรับได้เลย" : "ทำภารกิจด้านบนให้ครบทั้งหมด";
        bonusButton.GetComponentInChildren<Text>().text = s.bonus_claimed ? "รับแล้ว" : $"รับ +{s.bonus} ฿";
        bonusButton.interactable = !busy && s.all_done && !s.bonus_claimed;
    }

    private void AddQuestCard(QuestClient.Quest q, int index)
    {
        if (portrait) { AddQuestRow(q, index); return; }
        var card = UiKit.Panel("Quest", list, q.claimed ? (Color)new Color32(0xF3, 0xFA, 0xEC, 0xFF) : Color.white,
            new Vector2(0f, -index * 140f), new Vector2(1180f, 125f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = q.done ? UiKit.PayGreen : UiKit.CardBorder;

        var tile = UiKit.Panel("Icon", card, q.done ? UiKit.OkTint : UiKit.Pill, new Vector2(-520f, 0f), new Vector2(96f, 96f));
        UiKit.Label(tile, 30, Icons.TryGetValue(q.kind, out var icon) ? icon : "?", Vector2.zero, new Vector2(88f, 88f),
            q.done ? UiKit.OkGreen : UiKit.Price).fontStyle = FontStyle.Bold;

        UiKit.Label(card, 28, q.title, new Vector2(-195f, 30f), new Vector2(540f, 42f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 20, $"ที่: {q.hint}", new Vector2(-195f, -4f), new Vector2(540f, 30f), UiKit.Muted, TextAnchor.MiddleLeft);

        var track = UiKit.Panel("Track", card, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(-245f, -38f), new Vector2(440f, 16f));
        var fill = UiKit.Panel("Fill", track, q.done ? UiKit.PayGreen : UiKit.Gold, Vector2.zero,
            new Vector2(440f * Mathf.Clamp01((float)q.have / Mathf.Max(1, q.need)), 16f));
        fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        UiKit.Label(card, 22, $"{q.have}/{q.need}", new Vector2(30f, -38f), new Vector2(90f, 30f), UiKit.Muted, TextAnchor.MiddleLeft);

        UiKit.PillLabel(card, $"+{q.reward} ฿", new Vector2(190f, 0f), new Vector2(130f, 48f), UiKit.Pill, UiKit.Price, 26);

        if (q.claimed)
            UiKit.PillLabel(card, "รับแล้ว", new Vector2(440f, 0f), new Vector2(240f, 64f), UiKit.OkTint, UiKit.OkGreen, 26);
        else
        {
            int id = q.id;
            var b = UiKit.RoundButton(card, q.done ? "รับรางวัล" : "ยังไม่ครบ", new Vector2(440f, 0f), new Vector2(240f, 70f),
                q.done ? UiKit.Gold : UiKit.Pill, q.done ? UiKit.GoldInk : UiKit.Muted, () => Claim(id), 28);
            b.interactable = q.done && !busy;
        }
    }

    // Upright quest card: icon, title, where, progress on the left; reward and button stacked on the right
    private void AddQuestRow(QuestClient.Quest q, int index)
    {
        var card = UiKit.Panel("Quest", list, q.claimed ? (Color)new Color32(0xF3, 0xFA, 0xEC, 0xFF) : Color.white,
            new Vector2(0f, -index * 140f), new Vector2(690f, 128f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = q.done ? UiKit.PayGreen : UiKit.CardBorder;

        var tile = UiKit.Panel("Icon", card, q.done ? UiKit.OkTint : UiKit.Pill, new Vector2(-290f, 0f), new Vector2(90f, 90f));
        UiKit.Label(tile, 28, Icons.TryGetValue(q.kind, out var icon) ? icon : "?", Vector2.zero, new Vector2(84f, 84f),
            q.done ? UiKit.OkGreen : UiKit.Price).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 28, q.title, new Vector2(-55f, 34f), new Vector2(360f, 42f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 20, $"ที่: {q.hint}", new Vector2(-55f, 0f), new Vector2(360f, 30f), UiKit.Muted, TextAnchor.MiddleLeft);
        var track = UiKit.Panel("Track", card, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(-95f, -38f), new Vector2(280f, 16f));
        var fill = UiKit.Panel("Fill", track, q.done ? UiKit.PayGreen : UiKit.Gold, Vector2.zero,
            new Vector2(280f * Mathf.Clamp01((float)q.have / Mathf.Max(1, q.need)), 16f));
        fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        UiKit.Label(card, 22, $"{q.have}/{q.need}", new Vector2(100f, -38f), new Vector2(90f, 30f), UiKit.Muted, TextAnchor.MiddleLeft);

        UiKit.PillLabel(card, $"+{q.reward} ฿", new Vector2(250f, 32f), new Vector2(150f, 40f), UiKit.Pill, UiKit.Price, 24);
        if (q.claimed)
            UiKit.PillLabel(card, "รับแล้ว", new Vector2(250f, -24f), new Vector2(160f, 54f), UiKit.OkTint, UiKit.OkGreen, 24);
        else
        {
            int id = q.id;
            var b = UiKit.RoundButton(card, q.done ? "รับรางวัล" : "ยังไม่ครบ", new Vector2(250f, -24f), new Vector2(160f, 58f),
                q.done ? UiKit.Gold : UiKit.Pill, q.done ? UiKit.GoldInk : UiKit.Muted, () => Claim(id), 24);
            b.interactable = q.done && !busy;
        }
    }

    // ── Level chapters ──────────────────────────────────────────────────────────────────────────
    private void RenderLevels()
    {
        UiKit.Clear(chapterList);
        UiKit.Clear(chapterDetail);
        var quests = QuestClient.Instance;
        var chapters = quests != null && quests.IsOnline ? quests.Current.chapters : null;
        if (chapters == null || chapters.Length == 0)
        {
            UiKit.Label(chapterDetail, 28, "ต้องออนไลน์ก่อนถึงจะดูภารกิจได้", Vector2.zero, new Vector2(900f, 60f), UiKit.Muted);
            return;
        }

        // Show the chapter to work on: the first open one not yet claimed, else the last open one
        QuestClient.Chapter picked = null;
        foreach (var c in chapters) if (c.level == pickedChapter) picked = c;
        if (picked == null)
        {
            foreach (var c in chapters) if (c.unlocked && !c.claimed) { picked = c; break; }
            if (picked == null) foreach (var c in chapters) if (c.unlocked) picked = c;
            if (picked == null) picked = chapters[0];
        }

        for (int i = 0; i < chapters.Length; i++)
            AddChapterChip(chapters[i], portrait ? new Vector2((i % 3 - 1) * 230f, 515f - (i / 3) * 108f) : new Vector2(-470f, 300f - i * 118f), chapters[i] == picked);
        ChapterDetail(picked, quests.Current.level);
    }

    private void AddChapterChip(QuestClient.Chapter c, Vector2 position, bool selected)
    {
        Color fill = c.claimed ? UiKit.OkTint : c.done ? (Color)new Color32(0xFF, 0xF1, 0xC9, 0xFF)
            : c.unlocked ? Color.white : (Color)new Color32(0xE6, 0xE1, 0xD6, 0xFF);
        int level = c.level;
        float w = portrait ? 222f : 290f; // upright: three chips per row
        var chip = UiKit.RoundButton(chapterList, "", position, new Vector2(w, portrait ? 100f : 104f), fill, UiKit.Ink,
            () => { pickedChapter = level; Render(); }, 24);
        var edge = chip.gameObject.AddComponent<Outline>();
        edge.effectColor = selected ? UiKit.Gold : UiKit.CardBorder;
        if (selected) edge.effectDistance = new Vector2(4f, -4f);
        UiKit.Label(chip.transform, 34, $"Lv {c.level}", new Vector2(portrait ? -45f : -70f, 16f), new Vector2(portrait ? 110f : 130f, 46f),
            c.unlocked ? UiKit.Price : UiKit.Muted, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(chip.transform, 20, c.title, new Vector2(0f, -24f), new Vector2(w - 20f, 32f), c.unlocked ? UiKit.Ink : UiKit.Muted);
        string status = c.claimed ? "รับแล้ว" : c.done ? "รับได้!" : c.unlocked ? Progress(c) : "ล็อก";
        UiKit.PillLabel(chip.transform, status, new Vector2(portrait ? 58f : 78f, 16f), new Vector2(portrait ? 90f : 110f, 36f),
            c.claimed ? UiKit.OkTint : c.done ? UiKit.Gold : UiKit.Pill, c.claimed ? UiKit.OkGreen : c.done ? UiKit.GoldInk : UiKit.Muted, 20);
    }

    // "2/4" tasks finished
    private static string Progress(QuestClient.Chapter c)
    {
        int done = 0;
        foreach (var t in c.tasks) if (t.have >= t.need) done++;
        return $"{done}/{c.tasks.Length}";
    }

    private void ChapterDetail(QuestClient.Chapter c, int playerLevel)
    {
        // Upright the panel sits under the chips, narrower and a little taller
        float w = portrait ? 650f : 860f, top = portrait ? 50f : 0f, k = w / 860f;
        var panel = UiKit.Panel("Chapter", chapterDetail, Color.white, portrait ? new Vector2(0f, -60f) : new Vector2(150f, 0f), new Vector2(w + 40f, portrait ? 820f : 730f), shadow: true);
        panel.gameObject.AddComponent<Outline>().effectColor = c.done ? UiKit.PayGreen : UiKit.CardBorder;

        UiKit.Label(panel, 40, $"เลเวล {c.level} · {c.title}", new Vector2(0f, 310f + top), new Vector2(w, 56f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(panel, 24, c.story, new Vector2(0f, 262f + top), new Vector2(w, 36f), UiKit.Muted);
        if (!c.unlocked)
            UiKit.PillLabel(panel, $"ตอนนี้เลเวล {playerLevel} · ต้องถึงเลเวล {c.level} ก่อน", new Vector2(0f, 214f + top), new Vector2(560f, 44f), UiKit.Pill, UiKit.Danger, 22);

        for (int i = 0; i < c.tasks.Length; i++)
            TaskRow(panel, c.tasks[i], new Vector2(0f, 140f + top * 1.1f - i * 100f), c.unlocked, k);

        var reward = UiKit.Panel("Reward", panel, new Color32(0xFF, 0xF6, 0xDF, 0xFF), new Vector2(0f, portrait ? -320f : -290f), new Vector2(w, 110f));
        reward.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(reward, 26, "รางวัลเมื่อทำครบทุกข้อ", new Vector2(portrait ? -150f : -250f, 18f), new Vector2(340f, 40f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(reward, 34, $"{c.reward:N0} บาท", new Vector2(portrait ? -150f : -250f, -22f), new Vector2(340f, 44f), UiKit.Price, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        bool ready = c.done && !c.claimed;
        string label = c.claimed ? "รับแล้ว" : c.done ? $"รับ +{c.reward:N0} ฿" : c.unlocked ? "ยังทำไม่ครบ" : "ยังไม่เปิด";
        int level = c.level;
        var claim = UiKit.RoundButton(reward, label, new Vector2(portrait ? 185f : 250f, 0f), new Vector2(portrait ? 260f : 300f, 74f),
            ready ? UiKit.Gold : UiKit.Pill, ready ? UiKit.GoldInk : UiKit.Muted, () => ClaimChapter(level), 28);
        claim.interactable = ready && !busy;
    }

    // k < 1 narrows the row (upright)
    private static void TaskRow(RectTransform panel, QuestClient.Task t, Vector2 position, bool open, float k = 1f)
    {
        bool done = open && t.have >= t.need;
        var row = UiKit.Panel("Task", panel, done ? UiKit.OkTint : (Color)new Color32(0xFB, 0xF8, 0xF1, 0xFF), position, new Vector2(860f * k, 86f));
        var tile = UiKit.Panel("Icon", row, done ? UiKit.OkTint : UiKit.Pill, new Vector2(-375f * k + 10f * (1f - k), 0f), new Vector2(70f, 70f));
        UiKit.Label(tile, 24, Icons.TryGetValue(t.kind, out var icon) ? icon : "?", Vector2.zero, new Vector2(66f, 66f),
            done ? UiKit.OkGreen : UiKit.Price).fontStyle = FontStyle.Bold;
        UiKit.Label(row, 26, t.title, new Vector2(-120f * k + 20f * (1f - k), 16f), new Vector2(420f * k, 38f), open ? UiKit.Ink : UiKit.Muted, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;

        var track = UiKit.Panel("Track", row, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(-120f * k + 20f * (1f - k), -20f), new Vector2(420f * k, 14f));
        var fill = UiKit.Panel("Fill", track, done ? UiKit.PayGreen : UiKit.Gold, Vector2.zero,
            new Vector2(420f * k * Mathf.Clamp01((float)t.have / Mathf.Max(1, t.need)), 14f));
        fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        UiKit.Label(row, 24, done ? "สำเร็จ" : $"{t.have:N0}/{t.need:N0}", new Vector2(300f * k + 20f * (1f - k), 0f), new Vector2(200f * k, 40f),
            done ? UiKit.OkGreen : UiKit.Muted).fontStyle = FontStyle.Bold;
    }

    private void ClaimChapter(int level) => Run(() => QuestClient.Instance.ClaimChapterAsync(level));

    private void Claim(int id) => Run(() => QuestClient.Instance.ClaimAsync(id));
    private void ClaimBonus() => Run(() => QuestClient.Instance.ClaimBonusAsync());

    private async void Run(Func<System.Threading.Tasks.Task<QuestClient.State>> call)
    {
        if (busy || QuestClient.Instance == null) return;
        busy = true;
        try
        {
            var r = await call();
            Notify($"ได้รับ {r.earned:N0} บาท", UiKit.PayGreen);
            UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.2f));
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("already") ? "รับรางวัลนี้ไปแล้ว" : "ภารกิจยังไม่ครบ", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Quests] {e.Message}");
        }
        busy = false;
        Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
