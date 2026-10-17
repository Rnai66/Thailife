using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Daily quests window (ภารกิจประจำวัน), premium layout: summary + countdown to the next set,
// one card per quest with a progress bar and its reward, and the all-done bonus at the bottom.
// Opened from the "ภารกิจ" button on the HUD.
public class QuestUI : MonoBehaviour
{
    private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
    {
        { "fish", "ปลา" }, { "sell_produce", "ขาย" }, { "plant", "ปลูก" }, { "harvest", "เกี่ยว" },
        { "feed", "ไก่" }, { "sell_eggs", "ไข่" }, { "music", "ดนตรี" }, { "train", "มวย" },
        { "eat", "กิน" }, { "shop", "ซื้อ" },
    };

    private static QuestUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, summaryText, resetText, toastText;
    private RectTransform list, summaryFill;
    private Button bonusButton;
    private Text bonusNote;
    private Coroutine toast;
    private bool busy;
    private float nextTick;

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
        var body = UiKit.PremiumWindow(transform, "QuestCanvas", new Vector2(1300f, 920f), 55, "ภ",
            out backdrop, out titleText, out subtitleText, Close, out _);
        titleText.text = "ภารกิจประจำวัน";
        subtitleText.text = "ทำให้ครบ รับเงินรางวัล · ภารกิจใหม่ทุกวันในเกม";

        var summary = UiKit.Panel("Summary", body, Color.white, new Vector2(0f, 300f), new Vector2(1180f, 100f), shadow: true);
        summary.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        summaryText = UiKit.Label(summary, 30, "", new Vector2(-400f, 0f), new Vector2(340f, 50f), UiKit.Ink, TextAnchor.MiddleLeft);
        summaryText.fontStyle = FontStyle.Bold;
        var track = UiKit.Panel("Track", summary, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(40f, 0f), new Vector2(480f, 24f));
        summaryFill = UiKit.Panel("Fill", track, UiKit.Gold, Vector2.zero, new Vector2(0f, 24f));
        summaryFill.anchorMin = summaryFill.anchorMax = new Vector2(0f, 0.5f);
        summaryFill.pivot = new Vector2(0f, 0.5f);
        resetText = UiKit.Label(summary, 22, "", new Vector2(450f, 0f), new Vector2(260f, 40f), UiKit.Muted, TextAnchor.MiddleRight);

        list = UiKit.Container(body, new Vector2(0f, 165f));

        var bonus = UiKit.Panel("Bonus", body, new Color32(0xFF, 0xF6, 0xDF, 0xFF), new Vector2(0f, -255f), new Vector2(1180f, 110f), shadow: true);
        bonus.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(bonus, 28, "โบนัสทำครบทุกภารกิจ", new Vector2(-330f, 18f), new Vector2(480f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        bonusNote = UiKit.Label(bonus, 22, "", new Vector2(-330f, -22f), new Vector2(480f, 36f), UiKit.Muted, TextAnchor.MiddleLeft);
        bonusButton = UiKit.RoundButton(bonus, "", new Vector2(410f, 0f), new Vector2(300f, 72f), UiKit.Gold, UiKit.GoldInk, ClaimBonus, 28);

        toastText = UiKit.ToastBar(body, new Vector2(0f, -345f), 760f);
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
        summaryFill.sizeDelta = new Vector2(total > 0 ? 480f * done / total : 0f, 24f);

        if (s.quests != null)
            for (int i = 0; i < s.quests.Length; i++)
                AddQuestCard(s.quests[i], i);

        bonusNote.text = s.bonus_claimed ? "รับโบนัสวันนี้แล้ว" : s.all_done ? "ทำครบแล้ว! กดรับได้เลย" : "ทำภารกิจด้านบนให้ครบทั้งหมด";
        bonusButton.GetComponentInChildren<Text>().text = s.bonus_claimed ? "รับแล้ว" : $"รับ +{s.bonus} ฿";
        bonusButton.interactable = !busy && s.all_done && !s.bonus_claimed;
    }

    private void AddQuestCard(QuestClient.Quest q, int index)
    {
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
