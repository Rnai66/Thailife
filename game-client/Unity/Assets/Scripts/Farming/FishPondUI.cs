using System;
using UnityEngine;
using UnityEngine.UI;

// Fish cage window (เลี้ยงปลาในกระชัง, fishpond.lua) at the Kok Nong Na pond: three cages side by side,
// each showing what's in it, a growth bar and the time left, how many days it's been fed, and one button
// (ปล่อยลูกปลา / ให้อาหาร / จับปลาขาย). Pick the fry to stock from the row underneath; better fish open up
// at เลี้ยงปลา Lv 10 and 25. Fed fish weigh more when harvested.
public class FishPondUI : MonoBehaviour
{
    private static FishPondUI instance;

    private class CageCard
    {
        public Text title, status, info, buttonText;
        public RectTransform fill;
        public Button button;
    }

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText, pickText;
    private readonly CageCard[] cages = new CageCard[3];
    private RectTransform speciesRow;
    private Coroutine toast;
    private string picked = "tilapia";
    private bool busy;
    private float nextTick;

    public static void Show()
    {
        if (instance == null) instance = new GameObject("FishPondUI").AddComponent<FishPondUI>();
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
        var body = UiKit.PremiumWindow(transform, "FishPondCanvas", new Vector2(1400f, 860f), 55, "icon:farm",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "เลี้ยงปลาในกระชัง · สระโคกหนองนา";
        subtitleText.text = "ปล่อยลูกปลา ให้อาหารวันละครั้ง พอโตแล้วจับขายให้แม่ค้าปลา";

        for (int i = 0; i < cages.Length; i++)
            cages[i] = BuildCage(body, i + 1, new Vector2((i - 1) * 430f, 140f));

        pickText = UiKit.Label(body, 24, "", new Vector2(0f, -120f), new Vector2(1300f, 36f), UiKit.Price);
        pickText.fontStyle = FontStyle.Bold;
        speciesRow = UiKit.Container(body, new Vector2(0f, -200f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, -300f), 1000f);
        backdrop.SetActive(false);
        PondClient.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        PondClient.Changed -= OnChanged;
        if (instance == this) instance = null;
    }

    private void OnChanged(PondClient.State s)
    {
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    private CageCard BuildCage(RectTransform body, int id, Vector2 at)
    {
        var c = new CageCard();
        var card = UiKit.Panel("Cage", body, Color.white, at, new Vector2(400f, 400f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        c.title = UiKit.Label(card, 30, $"กระชัง {id}", new Vector2(0f, 160f), new Vector2(380f, 44f), UiKit.Ink);
        c.title.font = ThaiFont.Display;
        c.status = UiKit.Label(card, 26, "", new Vector2(0f, 105f), new Vector2(380f, 40f), UiKit.Price);
        c.status.fontStyle = FontStyle.Bold;
        var track = UiKit.Panel("Track", card, new Color(0.9f, 0.86f, 0.78f), new Vector2(0f, 60f), new Vector2(340f, 18f));
        c.fill = UiKit.Rect("Fill", track, UiKit.PayGreen);
        c.fill.anchorMin = new Vector2(0f, 0f);
        c.fill.anchorMax = new Vector2(0f, 1f);
        c.fill.pivot = new Vector2(0f, 0.5f);
        c.fill.offsetMin = c.fill.offsetMax = Vector2.zero;
        c.info = UiKit.Label(card, 21, "", new Vector2(0f, -30f), new Vector2(370f, 120f), UiKit.Muted);
        c.info.lineSpacing = 1.15f;
        c.button = UiKit.RoundButton(card, "", new Vector2(0f, -145f), new Vector2(340f, 72f), UiKit.Gold, UiKit.GoldInk, () => Act(id), 24);
        c.buttonText = c.button.GetComponentInChildren<Text>();
        return c;
    }

    private async void Refresh()
    {
        if (!PondClient.Online) { Notify("ต้องออนไลน์ก่อน", UiKit.Danger); return; }
        try { await PondClient.GetAsync(); }
        catch (Exception e) { Notify(PondClient.Explain(e), UiKit.Danger); }
    }

    private static int Remaining(PondClient.Cage c) =>
        Mathf.Max(0, Mathf.CeilToInt(c.ready_in_s - (Time.time - PondClient.ReceivedAt)));

    private static string Clock(int s) => s >= 60 ? $"{s / 60} นาที {s % 60:00} วิ" : $"{s} วิ";

    private PondClient.Species Find(string id)
    {
        var s = PondClient.Current;
        if (s?.species != null) foreach (var sp in s.species) if (sp.id == id) return sp;
        return null;
    }

    private void Render()
    {
        var s = PondClient.Current;
        UiKit.Clear(speciesRow);
        if (s?.cages == null)
        {
            pickText.text = "กำลังโหลด…";
            foreach (var c in cages)
            {
                c.status.text = "-";
                c.info.text = "";
                c.fill.anchorMax = new Vector2(0f, 1f);
                c.buttonText.text = "-";
                c.button.interactable = false;
            }
            return;
        }

        subtitleText.text = $"เลี้ยงปลา Lv {s.level} · ให้อาหารวันละครั้ง ถุงละ {s.feed_price} บาท · ปลาที่ได้กินครบโตน้ำหนักดีกว่า";
        var pick = Find(picked);
        if (pick == null || !pick.unlocked) { picked = "tilapia"; pick = Find(picked); }
        pickText.text = pick != null ? $"ลูกปลาที่จะปล่อย: {pick.name} · {pick.price} บาท" : "เลือกลูกปลา";

        for (int i = 0; i < cages.Length && i < s.cages.Length; i++) RenderCage(cages[i], s.cages[i], pick);

        for (int i = 0; s.species != null && i < s.species.Length; i++)
        {
            var sp = s.species[i];
            bool on = sp.id == picked;
            string id = sp.id;
            string label = sp.unlocked
                ? $"{sp.name} · {sp.price} ฿\nโต {Mathf.RoundToInt(sp.grow_s / 60f)} นาที · กก.ละ {sp.per_kg} ฿"
                : $"{sp.name}\nต้องเลี้ยงปลา Lv {sp.level}";
            var b = UiKit.RoundButton(speciesRow, label, new Vector2((i - (s.species.Length - 1) * 0.5f) * 320f, 0f), new Vector2(300f, 96f),
                on ? UiKit.OkTint : UiKit.Pill, on ? UiKit.OkGreen : UiKit.GoldInk, () => { picked = id; Render(); }, 20);
            b.interactable = sp.unlocked && !busy;
        }
    }

    private void RenderCage(CageCard card, PondClient.Cage c, PondClient.Species pick)
    {
        var s = PondClient.Current;
        int left = Remaining(c);
        bool grown = c.stage == "grown" || (c.stage == "growing" && left <= 0);
        float k = c.stage == "empty" ? 0f : grown ? 1f
            : Mathf.Clamp01(c.progress + (Time.time - PondClient.ReceivedAt) / Mathf.Max(1f, c.ready_in_s / Mathf.Max(0.01f, 1f - c.progress)));
        card.fill.anchorMax = new Vector2(k, 1f);

        if (c.stage == "empty")
        {
            card.status.text = "กระชังว่าง";
            card.info.text = "เลือกลูกปลาด้านล่าง แล้วกดปล่อยลงกระชัง";
            card.buttonText.text = pick != null ? $"ปล่อยลูก{pick.name} {pick.price} ฿" : "ปล่อยลูกปลา";
            card.button.interactable = pick != null && pick.unlocked && !busy;
            return;
        }

        string fed = $"ให้อาหารแล้ว {c.feeds}/{c.feeds_needed} วัน";
        if (grown)
        {
            card.status.text = $"{c.name} โตแล้ว!";
            card.info.text = $"{fed}\n" + (c.feeds >= c.feeds_needed ? "ได้กินครบ น้ำหนักดี" : "กินไม่ครบ น้ำหนักจะน้อยหน่อย");
            card.buttonText.text = "จับปลาขาย";
            card.button.interactable = !busy;
            return;
        }

        card.status.text = $"{c.name} กำลังโต";
        card.info.text = $"โตในอีก {Clock(left)}\n{fed}" + (c.fed_today ? " · วันนี้ให้แล้ว" : "");
        card.buttonText.text = c.fed_today ? "วันนี้ให้อาหารแล้ว" : $"ให้อาหาร {s.feed_price} ฿";
        card.button.interactable = !c.fed_today && !busy;
    }

    private void Update()
    {
        if (backdrop == null || !backdrop.activeSelf || Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 1f;
        var s = PondClient.Current;
        if (s?.cages == null || busy) return;
        var pick = Find(picked);
        for (int i = 0; i < cages.Length && i < s.cages.Length; i++) RenderCage(cages[i], s.cages[i], pick);
    }

    private async void Act(int id)
    {
        var s = PondClient.Current;
        if (busy || s?.cages == null || id < 1 || id > s.cages.Length) return;
        var c = s.cages[id - 1];
        bool grown = c.stage == "grown" || (c.stage == "growing" && Remaining(c) <= 0);
        string action = c.stage == "empty" ? "stock" : grown ? "harvest" : "feed";
        busy = true;
        Render();
        ActivityPose.Do(action == "harvest" ? "reel" : "scatter", 2f);
        try
        {
            var r = await PondClient.ActAsync(id, action, action == "stock" ? picked : "");
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play(action == "harvest" ? "coins" : "whoosh", null, 0.6f);
            if (action == "harvest") UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.3f));
            LifeClient.Instance?.Refresh();
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            CareerClient.Instance?.Reload();
        }
        catch (Exception e)
        {
            Notify(PondClient.Explain(e), UiKit.Danger);
            Refresh();
        }
        busy = false;
        Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
