using System;
using UnityEngine;
using UnityEngine.UI;

// Classroom window (ห้องเรียน) at the tambon school: four subjects (คอมพิวเตอร์ กีฬา ช่าง ตรรกะ), each
// with its level and XP bar, the fee and energy, and a "เข้าเรียน" button. A class lasts 30 s (the server
// times it); the progress bar fills, then the class ends by itself and the XP is shown. Underneath, the
// skilled jobs each subject opens up, ticked when you qualify.
public class SchoolUI : MonoBehaviour
{
    private static SchoolUI instance;

    private class Card
    {
        public Text name, level, desc, cost;
        public RectTransform xpFill;
        public Button button;
        public Text buttonText;
    }

    private GameObject backdrop;
    private Text title, subtitle, toast, classText, jobsText;
    private RectTransform progressFill, progressBar;
    private readonly Card[] cards = new Card[4];
    private Education.State state;
    private Coroutine toastRun;
    private float classEndsAt;
    private bool busy, finishing;

    private static readonly string[] SubjectIds = { "computer", "sports", "craft", "logic" };

    public static void Show()
    {
        if (instance == null) instance = new GameObject("SchoolUI").AddComponent<SchoolUI>();
        instance.backdrop.SetActive(true);
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "SchoolCanvas", new Vector2(1520f, 900f), 55, "icon:school",
            out backdrop, out title, out subtitle, Hide, out _);
        title.text = "ห้องเรียน · โรงเรียนตำบล";
        subtitle.text = "เรียนเพื่อเพิ่มเลเวล และเปิดอาชีพใหม่";

        for (int i = 0; i < cards.Length; i++)
            cards[i] = BuildCard(body, SubjectIds[i], new Vector2(-540f + i * 360f, 95f));

        // Class in progress
        progressBar = UiKit.Panel("Progress", body, new Color(0.9f, 0.86f, 0.78f), new Vector2(0f, -178f), new Vector2(1100f, 34f));
        progressFill = UiKit.Rect("Fill", progressBar, UiKit.PayGreen);
        progressFill.anchorMin = new Vector2(0f, 0f);
        progressFill.anchorMax = new Vector2(0f, 1f);
        progressFill.pivot = new Vector2(0f, 0.5f);
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        classText = UiKit.Label(body, 26, "", new Vector2(0f, -140f), new Vector2(1200f, 40f), UiKit.Ink);
        classText.font = ThaiFont.Display;

        // Jobs the subjects open up
        var jobsCard = UiKit.Panel("Jobs", body, Color.white, new Vector2(0f, -290f), new Vector2(1420f, 140f), shadow: true);
        jobsCard.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(jobsCard, 24, "อาชีพที่ต้องใช้วิชา", new Vector2(0f, 48f), new Vector2(1380f, 34f), UiKit.Price).fontStyle = FontStyle.Bold;
        jobsText = UiKit.Label(jobsCard, 21, "", new Vector2(0f, -14f), new Vector2(1380f, 90f), UiKit.Ink);
        jobsText.lineSpacing = 1.15f;

        toast = UiKit.ToastBar(body, new Vector2(0f, -178f), 1000f);
        backdrop.SetActive(false);
    }

    private Card BuildCard(RectTransform body, string id, Vector2 at)
    {
        var c = new Card();
        var card = UiKit.Panel("Subject", body, Color.white, at, new Vector2(340f, 430f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.IconTile(card, "subj_" + id, new Vector2(0f, 128f), new Vector2(140f, 140f));
        c.name = UiKit.Label(card, 34, "", new Vector2(0f, 30f), new Vector2(320f, 46f), UiKit.Ink);
        c.name.font = ThaiFont.Display;
        c.level = UiKit.PillLabel(card, "", new Vector2(0f, -14f), new Vector2(150f, 36f), UiKit.Pill, UiKit.Price, 22);
        var bar = UiKit.Panel("XpBar", card, new Color(0.9f, 0.86f, 0.78f), new Vector2(0f, -48f), new Vector2(280f, 14f));
        c.xpFill = UiKit.Rect("Fill", bar, UiKit.Gold);
        c.xpFill.anchorMin = new Vector2(0f, 0f);
        c.xpFill.anchorMax = new Vector2(0f, 1f);
        c.xpFill.pivot = new Vector2(0f, 0.5f);
        c.xpFill.offsetMin = c.xpFill.offsetMax = Vector2.zero;
        c.desc = UiKit.Label(card, 19, "", new Vector2(0f, -84f), new Vector2(310f, 50f), UiKit.Muted);
        c.cost = UiKit.Label(card, 19, "", new Vector2(0f, -124f), new Vector2(320f, 46f), UiKit.Ink);
        string subject = id;
        c.button = UiKit.RoundButton(card, "เข้าเรียน", new Vector2(0f, -180f), new Vector2(290f, 64f), UiKit.Gold, UiKit.GoldInk, () => StartClass(subject), 26);
        c.buttonText = c.button.GetComponentInChildren<Text>();
        return c;
    }

    private async void Refresh()
    {
        if (!Education.Online) { Say("ต้องออนไลน์ก่อน", UiKit.Danger); return; }
        try { Render(await Education.GetAsync()); }
        catch (Exception e) { Say(Education.Explain(e), UiKit.Danger); }
    }

    private void Render(Education.State s)
    {
        state = s;
        if (s == null || s.subjects == null) return;
        subtitle.text = $"เลเวลหลัก Lv {s.main_level} · เรียนเพื่อเพิ่มเลเวลและเปิดอาชีพใหม่";
        bool inClass = s.@class != null && s.@class.Active;
        bool working = s.job != null && s.job.Active;
        for (int i = 0; i < cards.Length && i < s.subjects.Length; i++)
        {
            var sub = s.subjects[i];
            var c = cards[i];
            c.name.text = sub.name;
            c.level.text = $"Lv {sub.level}";
            float k = sub.xp_to_next > 0 ? Mathf.Clamp01(sub.xp / (float)sub.xp_to_next) : 1f;
            c.xpFill.anchorMax = new Vector2(k, 1f);
            c.desc.text = sub.desc;
            c.cost.text = $"ค่าเรียน {sub.fee} ฿ · พลังงาน {sub.energy}\n+{sub.main_xp} XP เลเวลหลัก";
            bool here = inClass && s.@class.id == sub.id;
            c.buttonText.text = !sub.unlocked ? $"ต้องเลเวล {sub.need_level}" : here ? "กำลังเรียน..." : "เข้าเรียน";
            c.button.interactable = sub.unlocked && !inClass && !working && !busy;
        }

        var lines = new System.Text.StringBuilder();
        for (int i = 0; s.jobs != null && i < s.jobs.Length; i++)
        {
            var j = s.jobs[i];
            lines.Append($"{(j.unlocked ? "<color=#2E8B57>" : "<color=#88877F>")}{j.name} ({j.place}) {(j.unlocked ? "ทำได้แล้ว" : j.missing)}</color>");
            lines.Append(i % 2 == 0 ? "      " : "\n");
        }
        jobsText.supportRichText = true;
        jobsText.text = lines.ToString().TrimEnd();

        if (inClass)
        {
            classEndsAt = Time.unscaledTime + s.@class.remain;
            string name = "";
            foreach (var sub in s.subjects) if (sub.id == s.@class.id) name = sub.name;
            classText.text = $"กำลังเรียนวิชา{name}...";
        }
        else classText.text = working ? "กำลังทำงานอยู่ — เลิกงานก่อนแล้วค่อยมาเรียน" : "เลือกวิชาที่อยากเรียน";
        progressBar.gameObject.SetActive(inClass);
    }

    private void Update()
    {
        if (backdrop == null || !backdrop.activeSelf || state == null || state.@class == null || !state.@class.Active) return;
        float remain = Mathf.Max(0f, classEndsAt - Time.unscaledTime);
        progressFill.anchorMax = new Vector2(1f - remain / 30f, 1f);
        if (remain <= 0f && !finishing) FinishClass();
    }

    private async void StartClass(string subject)
    {
        if (busy) return;
        busy = true;
        try
        {
            Render(await Education.StartClassAsync(subject));
            LifeClient.Instance?.Refresh();
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            GameAudio.Play("bell", null, 0.6f);
        }
        catch (Exception e) { Say(Education.Explain(e), UiKit.Danger); }
        busy = false;
        if (state != null) Render(state);
    }

    private async void FinishClass()
    {
        finishing = true;
        try
        {
            var s = await Education.FinishClassAsync();
            var f = s.finished;
            Render(s);
            string msg = $"จบคาบ{f.name}! +{f.xp} XP วิชา · +{f.main_xp} XP เลเวลหลัก";
            if (f.subject_up) msg = $"วิชา{f.name} ขึ้น Lv {f.level}! " + msg;
            if (f.main_up) msg = $"เลเวลหลักขึ้น Lv {f.main_level}! " + msg;
            Say(msg, UiKit.PayGreen);
            if (f.main_up || f.subject_up) UiKit.FloatAbovePlayer(f.main_up ? $"เลเวลอัป! Lv {f.main_level}" : $"{f.name} Lv {f.level}", new Color(1f, 0.85f, 0.3f));
            GameAudio.Play("coins");
            Education.RefreshLevel();
        }
        catch (Exception e)
        {
            Say(Education.Explain(e), UiKit.Danger);
            Refresh();
        }
        finishing = false;
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
