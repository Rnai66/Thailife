using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Coop window (เล้าไก่), premium layout: three status cards (feed, eggs, fertile eggs) with their
// actions on top, then a card per chicken (feather colour, sex, beauty bar, sell price).
// Built at runtime; opened by the coop gate (WindowSpot).
public class CoopUI : MonoBehaviour
{
    private static CoopUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private Text feedValue, feedNote, eggValue, eggNote, fertileValue, fertileNote;
    private Button feedButton, sellEggsButton, hatchButton;
    private RectTransform list;
    private Coroutine toast;
    private bool busy;
    private float nextRender;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("CoopUI").AddComponent<CoopUI>();
        instance.backdrop.SetActive(true);
        CoopClient.Instance?.Refresh();
        instance.Render(rebuildList: true);
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "CoopCanvas", UiKit.WindowSize(new Vector2(1640f, 940f), 1700f), 50, "icon:farm",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "เล้าไก่";

        feedButton = StatusCard(body, -540f, "อาหารไก่", out feedValue, out feedNote, "ให้อาหาร", UiKit.Gold, UiKit.GoldInk, () => Act("feed"));
        sellEggsButton = StatusCard(body, 0f, "ไข่ไก่", out eggValue, out eggNote, "ขายไข่ทั้งหมด", UiKit.PayGreen, Color.white, () => Act("sell_eggs"));
        hatchButton = StatusCard(body, 540f, "ไข่มีเชื้อ (ไข่แม่พันธุ์)", out fertileValue, out fertileNote, "ฟักไข่ 1 ฟอง", new Color32(0x37, 0x8A, 0xDD, 0xFF), Color.white, () => Act("hatch"));

        var heading = UiKit.Label(body, 30, "ไก่ในเล้า", new Vector2(-640f, 165f), new Vector2(300f, 44f), UiKit.Ink, TextAnchor.MiddleLeft);
        heading.fontStyle = FontStyle.Bold;
        if (UiKit.Portrait) // upright: the three status cards stacked, the chickens in two columns below
        {
            UiKit.Stack((RectTransform)feedButton.transform.parent, new Vector2(0f, 640f), 1.2f);
            UiKit.Stack((RectTransform)sellEggsButton.transform.parent, new Vector2(0f, 400f), 1.2f);
            UiKit.Stack((RectTransform)hatchButton.transform.parent, new Vector2(0f, 160f), 1.2f);
            UiKit.Stack(heading.rectTransform, new Vector2(-190f, 15f));
        }
        list = UiKit.Container(body, new Vector2(0f, 0f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, -340f), 760f);

        backdrop.SetActive(false);
        if (CoopClient.Instance != null) CoopClient.Instance.Changed += _ => Render(rebuildList: true);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static Button StatusCard(RectTransform body, float x, string title, out Text value, out Text note,
        string action, Color buttonColor, Color buttonInk, UnityEngine.Events.UnityAction onClick)
    {
        var card = UiKit.Panel("Status", body, Color.white, new Vector2(x, 285f), new Vector2(510f, 190f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(card, 27, title, new Vector2(-15f, 66f), new Vector2(460f, 38f), UiKit.Muted, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        value = UiKit.Label(card, 42, "", new Vector2(-15f, 22f), new Vector2(460f, 56f), UiKit.Ink, TextAnchor.MiddleLeft);
        value.fontStyle = FontStyle.Bold;
        // the note sits left of the button, two lines if it needs them
        note = UiKit.Label(card, 24, "", new Vector2(-95f, -40f), new Vector2(300f, 70f), UiKit.Muted, TextAnchor.MiddleLeft);
        note.lineSpacing = 1.05f;
        return UiKit.RoundButton(card, action, new Vector2(150f, -50f), new Vector2(190f, 64f), buttonColor, buttonInk, onClick, 26);
    }

    private void Update()
    {
        // Countdowns tick every second (the chicken cards are only rebuilt when the data changes,
        // so buttons don't get replaced under the player's finger)
        if (backdrop.activeSelf && Time.time >= nextRender) Render(rebuildList: false);
    }

    private void Render(bool rebuildList)
    {
        nextRender = Time.time + 1f;
        var coop = CoopClient.Instance;
        if (coop == null || !coop.IsOnline)
        {
            subtitleText.text = "ต้องออนไลน์ก่อนถึงจะดูเล้าได้";
            feedValue.text = eggValue.text = fertileValue.text = "-";
            feedNote.text = eggNote.text = fertileNote.text = "";
            UiKit.Clear(list);
            feedButton.interactable = sellEggsButton.interactable = hatchButton.interactable = false;
            return;
        }

        var s = coop.Current;
        float fed = s.fed_for_s - coop.Age;
        int count = (s.chickens?.Length ?? 0) + (s.hatching_s?.Length ?? 0);
        subtitleText.text = $"ไก่ {count}/{s.capacity} ตัว · หมู่บ้าน";

        feedValue.text = fed > 0 ? $"อิ่มอีก {CoopClient.Countdown(fed)}" : "หิว!";
        feedValue.color = fed > 0 ? UiKit.OkGreen : UiKit.Danger;
        feedNote.text = $"มีอาหารไก่ {s.feed_bags} ถุง" + (fed > 0 ? " · แม่ไก่ออกไข่อยู่" : " · ให้อาหารแม่ไก่ถึงจะออกไข่");

        eggValue.text = $"{s.eggs} ฟอง";
        int eggTotal = s.eggs * s.egg_price + s.fertile_eggs * s.fertile_egg_price;
        eggNote.text = $"ฟองละ {s.egg_price}฿ · ขายรวมไข่มีเชื้อได้ {eggTotal:N0}฿";

        fertileValue.text = $"{s.fertile_eggs} ฟอง";
        string hatching = "";
        if (s.hatching_s != null && s.hatching_s.Length > 0)
        {
            var times = new System.Collections.Generic.List<string>();
            foreach (int t in s.hatching_s) times.Add(CoopClient.Countdown(t - coop.Age));
            hatching = $" · กำลังฟัก {string.Join(", ", times)}";
        }
        fertileNote.text = $"ฟองละ {s.fertile_egg_price}฿ (ตามความสวยพ่อแม่){hatching}";

        feedButton.interactable = !busy && s.feed_bags > 0;
        sellEggsButton.interactable = !busy && s.eggs + s.fertile_eggs > 0;
        hatchButton.interactable = !busy && s.fertile_eggs > 0 && count < s.capacity;

        if (!rebuildList) return;
        UiKit.Clear(list);
        if (s.chickens == null || s.chickens.Length == 0)
        {
            UiKit.Label(list, 30, "ยังไม่มีไก่ — ซื้อแม่ไก่ พ่อไก่ และอาหารไก่ได้ที่ร้านค้าชุมชน (แท็บเลี้ยงไก่)",
                UiKit.Portrait ? new Vector2(0f, -300f) : new Vector2(0f, 20f), UiKit.Portrait ? new Vector2(660f, 120f) : new Vector2(1400f, 60f), UiKit.Muted);
            return;
        }
        for (int i = 0; i < s.chickens.Length; i++)
            AddChickenCard(s.chickens[i], i, coop.Age);
    }

    // Four cards per row, two rows (the coop holds 8)
    private void AddChickenCard(CoopClient.Chicken c, int index, float age)
    {
        bool upright = UiKit.Portrait; // upright: two per row, four rows
        float x = upright ? (index % 2 == 0 ? -177f : 177f) : -585f + (index % 4) * 390f;
        float y = upright ? -100f - (index / 2) * 188f : 45f - (index / 4) * 200f;
        var card = UiKit.Panel("Chicken", list, Color.white, new Vector2(x, y), new Vector2(370f, 192f), shadow: true);
        if (upright) card.localScale = new Vector3(0.95f, 0.95f, 1f);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;

        // Feather colour disc
        var disc = UiKit.Rect("Feather", card, CoopClient.FeatherColor(c.color));
        disc.GetComponent<Image>().sprite = UiKit.CircleSprite;
        disc.anchoredPosition = new Vector2(-135f, 46f);
        disc.sizeDelta = new Vector2(72f, 72f);
        disc.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;

        // name and sex/age beside the feather disc
        var name = UiKit.Label(card, 30, c.name, new Vector2(40f, 66f), new Vector2(250f, 40f), UiKit.Ink, TextAnchor.MiddleLeft);
        name.fontStyle = FontStyle.Bold;
        string stage = c.adult ? CoopClient.SexTh(c) : $"ลูกไก่ · โตใน {CoopClient.Countdown(c.grown_in_s - age)}";
        UiKit.PillLabel(card, stage, new Vector2(40f, 26f), new Vector2(250f, 38f),
            c.sex == "rooster" ? UiKit.DangerTint : UiKit.Pill, c.sex == "rooster" ? UiKit.Danger : UiKit.Price, 23).alignment = TextAnchor.MiddleCenter;

        // feather colour on its own full-width line, then beauty and its bar — nothing shares a spot
        UiKit.Label(card, 23, $"สี{c.color}", new Vector2(0f, -16f), new Vector2(340f, 32f), UiKit.Ink, TextAnchor.MiddleLeft);
        UiKit.Label(card, 22, $"ความสวย {c.beauty}", new Vector2(-95f, -46f), new Vector2(150f, 28f), UiKit.Muted, TextAnchor.MiddleLeft);
        var track = UiKit.Panel("BeautyTrack", card, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(75f, -46f), new Vector2(190f, 14f));
        var fill = UiKit.Panel("BeautyFill", track, UiKit.Gold, Vector2.zero, new Vector2(190f * Mathf.Clamp01(c.beauty / 100f), 14f));
        fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);

        var sell = UiKit.RoundButton(card, $"ขาย {c.price:N0}฿", new Vector2(0f, -76f), new Vector2(340f, 36f),
            UiKit.Pill, UiKit.Price, () => Act("sell_chicken", c.id), 24);
        sell.interactable = !busy;
    }

    private async void Act(string action, int chicken = 0)
    {
        var coop = CoopClient.Instance;
        if (busy || coop == null) return;
        busy = true;
        if (action == "feed") ActivityPose.Do("scatter", 1.8f); // throw the feed for the hens
        try
        {
            var r = await coop.ActAsync(action, chicken);
            switch (action)
            {
                case "feed": Notify("ให้อาหารแล้ว แม่ไก่จะออกไข่เรื่อยๆ", UiKit.PayGreen); break;
                case "sell_eggs": Notify($"ขายไข่ {r.sold} ฟอง ได้ {r.earned:N0} บาท", UiKit.PayGreen); UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.2f)); break;
                case "hatch": Notify("เริ่มฟักไข่แล้ว ลูกไก่จะได้ความสวยจากพ่อแม่", UiKit.PayGreen); break;
                case "sell_chicken": Notify($"ขาย{r.sold_name} ได้ {r.earned:N0} บาท", UiKit.PayGreen); UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.2f)); break;
            }
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("energy") ? "พลังงานไม่พอ — กินอะไรหน่อย หรือไปนอนพักที่ชานบ้าน"
                : e.Message.Contains("feed") ? "ไม่มีอาหารไก่ — ซื้อที่ร้านค้าชุมชน"
                : e.Message.Contains("full") ? "เล้าเต็มแล้ว (8 ตัว)"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่" : "ทำไม่ได้ตอนนี้", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Coop] {e.Message}");
        }
        busy = false;
        Render(rebuildList: true);
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
