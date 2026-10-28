using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Ring window, premium layout: your fight record on top, a card per event this ring offers
// (requirements, when it's on, purse, XP, energy) with a button to step in.
public class BoxingUI : MonoBehaviour
{
    private static BoxingUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private RectTransform list;
    private FightSpot spot;
    private Coroutine toast;
    private bool busy;

    // Result / message window
    private GameObject resultBackdrop;
    private Text resultTitle, resultSub, resultBody;

    public static void Show(FightSpot at)
    {
        Ensure();
        instance.spot = at;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.RefreshThenRender();
    }

    public static void ShowResult(BoxingClient.State r)
    {
        Ensure();
        bool pads = r.event_name != null && r.event_name.Contains("เป้า");
        instance.resultTitle.text = r.event_name ?? "ผลการชก";
        instance.resultSub.text = $"สถิติ: ชนะ {r.wins} · แพ้ {r.losses} · ชนะน็อค {r.kos}";
        string head = pads ? $"ซ้อมเป้าล่อ: เป๊ะ {r.good} ครั้ง" : r.win ? (r.ko ? "ชนะน็อค!" : "ชนะคะแนน!") : "แพ้";
        string body = $"{head}\nค่าตัว/รางวัล {r.purse:N0} บาท · XP +{r.xp}";
        if (!string.IsNullOrEmpty(r.title)) body += $"\nได้ตำแหน่ง \"{r.title}\"!";
        if (r.leveled_up) body += $"\nเลเวลอัป! นักมวย Lv {r.new_level}";
        if (r.injured) body += "\nบาดเจ็บ! ไปรักษาที่ รพ.สต. (ตำบล) หรือโรงพยาบาล (เมือง)";
        instance.resultBody.text = body;
        instance.resultBody.color = r.win ? UiKit.OkGreen : UiKit.Danger;
        instance.resultBackdrop.SetActive(true);
    }

    public static void ShowMessage(string title, string message)
    {
        Ensure();
        instance.resultTitle.text = title;
        instance.resultSub.text = "";
        instance.resultBody.text = message;
        instance.resultBody.color = UiKit.Ink;
        instance.resultBackdrop.SetActive(true);
    }

    private static void Ensure()
    {
        if (instance == null)
            instance = new GameObject("BoxingUI").AddComponent<BoxingUI>();
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "BoxingCanvas", UiKit.WindowSize(new Vector2(1400f, 900f), 1450f), 55, "icon:boxing",
            out backdrop, out titleText, out subtitleText, Close, out _);
        list = UiKit.Container(body, new Vector2(0f, UiKit.Portrait ? 520f : 255f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, UiKit.Portrait ? -560f : -340f), 760f);
        backdrop.SetActive(false);

        var rbody = UiKit.PremiumWindow(transform, "FightResult", UiKit.WindowSize(new Vector2(1000f, 640f), 700f), 60, "ผ",
            out resultBackdrop, out resultTitle, out resultSub, () => resultBackdrop.SetActive(false), out _);
        resultBody = UiKit.Label(rbody, 40, "", new Vector2(0f, 40f), new Vector2(UiKit.Portrait ? 680f : 900f, 300f), UiKit.Ink);
        resultBody.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(rbody, "ตกลง", new Vector2(0f, -170f), new Vector2(300f, 90f), UiKit.Gold, UiKit.GoldInk,
            () => resultBackdrop.SetActive(false), 34);
        UiKit.AutoFade(resultBackdrop, 6f);
        resultBackdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void RefreshThenRender()
    {
        var boxing = BoxingClient.Instance;
        if (boxing == null) return;
        try { await boxing.RefreshAsync(); } catch (Exception) { }
        if (backdrop.activeSelf) Render();
    }

    private void Render()
    {
        UiKit.Clear(list);
        titleText.text = spot != null ? spot.Title : "เวทีมวย";
        var boxing = BoxingClient.Instance;
        if (boxing == null || !boxing.IsOnline)
        {
            subtitleText.text = "ต้องออนไลน์ก่อน";
            return;
        }
        var s = boxing.Current;
        subtitleText.text = $"นักมวย Lv {s.level} · ชนะ {s.wins} แพ้ {s.losses} น็อค {s.kos}"
                            + (string.IsNullOrEmpty(s.titles) ? "" : $" · {s.titles}");
        int i = 0;
        if (spot != null)
            foreach (var id in spot.Events)
            {
                var e = boxing.Find(id);
                if (e != null) AddCard(e, i++);
            }
    }

    private void AddCard(BoxingClient.Event e, int index)
    {
        // Upright: a narrower, taller card with the button under the details on the right
        bool P = UiKit.Portrait;
        var card = UiKit.Panel("Event", list, Color.white, new Vector2(0f, -index * (P ? 245f : 175f)), P ? new Vector2(690f, 230f) : new Vector2(1300f, 160f), shadow: true);
        float tx = P ? -10f : -260f, tw = P ? 650f : 700f;
        card.gameObject.AddComponent<Outline>().effectColor = e.open && e.unlocked && !e.fought ? UiKit.Gold : UiKit.CardBorder;

        UiKit.Label(card, 32, e.name, new Vector2(tx, P ? 80f : 45f), new Vector2(tw, 46f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        string need = $"ต้อง Lv {e.level}+" + (e.member ? " · สมาชิกค่าย" : "") + $" · พลังงาน {e.energy}";
        UiKit.Label(card, 22, need, new Vector2(tx, P ? 42f : 5f), new Vector2(tw, 32f), UiKit.Muted, TextAnchor.MiddleLeft);
        UiKit.Label(card, 22, e.when, new Vector2(tx, P ? 8f : -30f), new Vector2(tw, 32f), UiKit.Muted, TextAnchor.MiddleLeft);
        string reward = e.pads ? "XP ตามจำนวนครั้งที่เป๊ะ" : e.purse_win > 0 ? $"ชนะ {e.purse_win:N0}฿ · แพ้ {e.purse_lose:N0}฿ · XP {e.xp_win}" : $"XP {e.xp_win}";
        UiKit.PillLabel(card, reward, P ? new Vector2(-120f, -65f) : new Vector2(-260f, -63f), new Vector2(P ? 420f : 700f, P ? 56f : 32f), UiKit.Pill, UiKit.Price, 20);

        string status = !e.unlocked ? (e.member ? "ต้อง Lv และสมาชิกค่าย" : $"ล็อค (Lv {e.level})")
            : !e.open ? "ยังไม่เปิด" : e.fought ? "ชกครั้งนี้ไปแล้ว" : null;
        if (status != null)
            UiKit.PillLabel(card, status, P ? new Vector2(220f, -65f) : new Vector2(440f, 0f), new Vector2(P ? 220f : 330f, 70f), UiKit.DangerTint, UiKit.Danger, P ? 22 : 26);
        else
        {
            var id = e.id;
            UiKit.RoundButton(card, e.pads ? "เริ่มซ้อม" : "ขึ้นชก", P ? new Vector2(220f, -65f) : new Vector2(440f, 0f), new Vector2(P ? 220f : 330f, P ? 80f : 90f), UiKit.Gold, UiKit.GoldInk,
                () => Begin(id), 34).interactable = !busy;
        }
    }

    private async void Begin(string id)
    {
        var boxing = BoxingClient.Instance;
        if (busy || boxing == null || spot == null || FightDirector.Busy) return;
        busy = true;
        try
        {
            var session = await boxing.StartAsync(id);
            Close();
            ContextButton.Hide(spot);
            FightDirector.Begin(spot, id, session);
            LifeClient.Instance?.Refresh();
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("injured") ? "บาดเจ็บอยู่ — ไปรักษาที่ รพ.สต. หรือโรงพยาบาลก่อน"
                : e.Message.Contains("energy") ? "พลังงานไม่พอ — กินอะไรหน่อยหรือนอนพัก"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่"
                : e.Message.Contains("level") ? "เลเวลยังไม่ถึง"
                : e.Message.Contains("membership") ? "ต้องสมัครสมาชิกค่ายก่อน"
                : e.Message.Contains("closed") ? "ยังไม่ถึงวันจัดงาน"
                : e.Message.Contains("already") ? "งานนี้ชกไปแล้ว รองานหน้า" : "เริ่มไม่สำเร็จ", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Boxing] {e.Message}");
        }
        busy = false;
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
