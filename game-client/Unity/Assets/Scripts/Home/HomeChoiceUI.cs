using System;
using UnityEngine;
using UnityEngine.UI;

// เลือกที่อยู่ / ย้ายบ้าน: first where to live — บ้านนอก (the village), ในเมือง (town) or กรุงเทพฯ —
// then which house there. Asked once when you start (free); later "ย้ายบ้าน" in the settings moves
// you for a fee. Choosing takes you straight to your new porch (HomeClient.GoHome).
public class HomeChoiceUI : MonoBehaviour
{
    private static HomeChoiceUI instance;

    private GameObject backdrop;
    private Text title, subtitle, toast;
    private RectTransform list;
    private Coroutine toastRun;
    private bool busy, first, portrait;
    private float bodyW, bodyH;

    private static readonly (string area, string icon)[] Icons = { ("village", "farm"), ("city", "city"), ("bangkok", "temple") };

    public static void Show(bool firstTime = false)
    {
        var home = HomeClient.Instance;
        if (home?.Current?.areas == null)
        {
            UiKit.FloatAbovePlayer("ต้องออนไลน์ก่อน", new Color(1f, 0.6f, 0.4f));
            return;
        }
        if (instance == null) instance = new GameObject("HomeChoiceUI").AddComponent<HomeChoiceUI>();
        instance.first = firstTime;
        instance.backdrop.SetActive(true);
        instance.ShowAreas();
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var size = UiKit.WindowSize(new Vector2(1320f, 840f), 1400f);
        var body = UiKit.PremiumWindow(transform, "HomeChoiceCanvas", size, 58, "icon:home", out backdrop, out title, out subtitle, Close, out _);
        bodyW = size.x - 40f;
        bodyH = size.y - 156f;
        list = UiKit.Container(body, Vector2.zero);
        toast = UiKit.ToastBar(body, new Vector2(0f, -bodyH * 0.5f + 40f), 900f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Close() => backdrop.SetActive(false);

    private HomeClient.State State => HomeClient.Instance?.Current;

    private void Heading(string head)
    {
        var s = State;
        title.text = first ? "เลือกที่อยู่" : "ย้ายบ้าน";
        subtitle.text = first ? $"{head} · เลือกครั้งแรกฟรี" : $"{head} · ค่าย้ายบ้าน {s.move_fee:N0} บาท · ตอนนี้อยู่{s.style_name}";
    }

    // Step 1: which part of the country
    private void ShowAreas()
    {
        UiKit.Clear(list);
        Heading("จะเริ่มชีวิตที่ไหนดี?");
        var areas = State.areas;
        for (int i = 0; i < areas.Length; i++)
        {
            var a = areas[i];
            string icon = "home";
            foreach (var (id, ic) in Icons) if (id == a.id) icon = ic;
            string houses = a.styles != null ? $"บ้าน {a.styles.Length} แบบ" : "";
            Card(i, areas.Length, icon, a.name, $"{a.desc}\n{houses}", a.id == State.area && State.chosen ? "ที่อยู่ตอนนี้" : null,
                "ดูบ้าน", () => ShowStyles(a));
        }
        if (!first && State.chosen) // opened from the HUD: the quick way back to your porch
            UiKit.RoundButton(list, "กลับบ้าน", new Vector2(portrait ? 0f : -bodyW * 0.5f + 180f, -bodyH * 0.5f + (portrait ? 110f : 50f)),
                new Vector2(300f, 64f), UiKit.Gold, UiKit.GoldInk, () => { Close(); HomeClient.Instance?.GoHome(); }, 24);
    }

    // Step 2: which house there
    private void ShowStyles(HomeClient.Area a)
    {
        UiKit.Clear(list);
        Heading($"เลือกบ้านใน{a.name}");
        for (int i = 0; i < a.styles.Length; i++)
        {
            var s = a.styles[i];
            bool current = State.chosen && State.style == s.id;
            Card(i, a.styles.Length, "home", s.name, s.desc, current ? "บ้านปัจจุบัน" : null,
                current ? "อยู่บ้านนี้แล้ว" : "อยู่บ้านนี้", current ? (Action)null : () => Choose(a, s));
        }
        UiKit.RoundButton(list, "← เลือกพื้นที่อื่น", new Vector2(portrait ? 0f : -bodyW * 0.5f + 180f, -bodyH * 0.5f + (portrait ? 110f : 50f)),
            new Vector2(300f, 64f), UiKit.Pill, UiKit.Ink, ShowAreas, 24);
    }

    // A card: side by side in landscape, one under another upright
    private void Card(int index, int count, string icon, string name, string desc, string badge, string button, Action onClick)
    {
        Vector2 size, at;
        if (portrait)
        {
            float h = Mathf.Min(270f, (bodyH - 200f) / count - 16f);
            size = new Vector2(bodyW - 40f, h);
            at = new Vector2(0f, bodyH * 0.5f - 30f - h * 0.5f - index * (h + 16f));
        }
        else
        {
            float w = Mathf.Min(400f, (bodyW - 40f) / count - 20f);
            size = new Vector2(w, bodyH - 170f);
            at = new Vector2((index - (count - 1) * 0.5f) * (w + 20f), 20f);
        }
        var card = UiKit.Panel("Card", list, Color.white, at, size, shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        if (portrait)
        {
            float x0 = -size.x * 0.5f;
            UiKit.IconTile(card, icon, new Vector2(x0 + 90f, 0f), new Vector2(130f, 130f));
            OneLineFit.Apply(UiKit.Cartoon(UiKit.Label(card, 32, name, new Vector2(x0 + 180f + (size.x - 420f) * 0.5f, size.y * 0.5f - 40f), new Vector2(size.x - 420f, 50f), UiKit.Ink, TextAnchor.MiddleLeft), 1f), 22);
            UiKit.Label(card, 26, desc, new Vector2(x0 + 180f + (size.x - 420f) * 0.5f, -14f), new Vector2(size.x - 420f, size.y - 90f), UiKit.Muted, TextAnchor.UpperLeft);
            if (badge != null) UiKit.PillLabel(card, badge, new Vector2(size.x * 0.5f - 115f, size.y * 0.5f - 34f), new Vector2(200f, 40f), UiKit.OkTint, UiKit.OkGreen, 20);
            var b = UiKit.RoundButton(card, button, new Vector2(size.x * 0.5f - 115f, -size.y * 0.5f + 50f), new Vector2(200f, 66f), UiKit.Gold, UiKit.GoldInk, () => onClick?.Invoke(), 24);
            b.interactable = onClick != null;
        }
        else
        {
            float top = size.y * 0.5f;
            UiKit.IconTile(card, icon, new Vector2(0f, top - 110f), new Vector2(160f, 160f));
            OneLineFit.Apply(UiKit.Cartoon(UiKit.Label(card, 32, name, new Vector2(0f, top - 230f), new Vector2(size.x - 30f, 56f), UiKit.Ink), 1f), 22);
            UiKit.Label(card, 26, desc, new Vector2(0f, top - 330f), new Vector2(size.x - 40f, 140f), UiKit.Muted, TextAnchor.UpperCenter);
            if (badge != null) UiKit.PillLabel(card, badge, new Vector2(0f, top - 24f), new Vector2(200f, 38f), UiKit.OkTint, UiKit.OkGreen, 20);
            var b = UiKit.RoundButton(card, button, new Vector2(0f, -top + 52f), new Vector2(size.x - 60f, 72f), UiKit.Gold, UiKit.GoldInk, () => onClick?.Invoke(), 26);
            b.interactable = onClick != null;
        }
    }

    private async void Choose(HomeClient.Area a, HomeClient.Style s)
    {
        var home = HomeClient.Instance;
        if (busy || home == null) return;
        busy = true;
        try
        {
            var r = await home.ChooseAsync(a.id, s.id); // takes you there
            if (r.paid > 0) UiKit.FloatAbovePlayer($"-{r.paid:N0} ฿", new Color(1f, 0.6f, 0.3f));
            UiKit.FloatAbovePlayer(r.done ?? "ได้บ้านแล้ว!", new Color(1f, 0.85f, 0.3f));
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Close();
        }
        catch (Exception e)
        {
            toastRun = UiKit.Toast(this, toast, HomeClient.Explain(e), UiKit.Danger, toastRun);
        }
        busy = false;
    }
}
