using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The game HUD, kept as clean as possible: the energy bar in the top-left corner with round buttons
// down the left side (กระเป๋า, เมนู — the menu folds out a second column: your name, profession,
// quests, food, greetings, calendar, settings), the minimap and the time in the top-right corner and
// a "KO!" banner. Money, level and gear are in the bag (BagUI). Builds its own canvas at runtime.
[RequireComponent(typeof(PlayerCombat))]
public class HealthHud : MonoBehaviour
{
    private const float Disc = 96f, Step = 128f;
    private static readonly Color DiscColor = new Color(0.17f, 0.11f, 0.07f, 0.88f);
    private static readonly Color DiscAlert = new Color(0.86f, 0.6f, 0.16f, 0.95f);

    private PlayerCombat combat;
    private GameObject koBanner;
    private CareerClient career;
    private Text clockLabel, offlineLabel;
    private ProfileClient profile;
    private RectTransform energyFill;
    private Text energyLabel;

    private RectTransform menuColumn;
    private Image menuDisc, menuGlyph, questDisc, calendarDisc;
    private Text menuCaption, questCaption, nameCaption, careerCaption;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        Build();
    }

    private void OnEnable() => combat.HealthChanged += Refresh;

    private void OnDisable()
    {
        combat.HealthChanged -= Refresh;
        if (career != null) career.StatsChanged -= RefreshCareer;
        if (profile != null) profile.NameChanged -= RefreshName;
    }

    private void Start()
    {
        Refresh(combat.Health);

        career = CareerClient.Instance;
        if (career != null) career.StatsChanged += RefreshCareer;
        RefreshCareer(null, career != null ? career.Current : null);

        profile = ProfileClient.Instance;
        if (profile != null) profile.NameChanged += RefreshName;
        RefreshName(profile != null ? profile.DisplayName : null);
    }

    private void RefreshName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { nameCaption.text = "ตั้งชื่อ"; return; }
        nameCaption.text = name.Length > 10 ? name.Substring(0, 9) + "…" : name;
    }

    // Clock, energy and the alerts change continuously; refresh a few times a second
    private float nextTick;

    private void Update()
    {
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;

        clockLabel.text = $"{GameClock.Hour:00}:{GameClock.Minute:00} ({GameClock.PeriodTh})";
        var fest = FestivalClient.Instance;
        bool eventOn = fest != null && fest.IsOpen;
        var quest = QuestClient.Instance;
        bool questOnline = quest != null && quest.IsOnline;
        bool reward = questOnline && quest.HasReward;
        bool open = menuColumn.gameObject.activeSelf;
        menuCaption.text = open ? "ปิด" : eventOn ? "มีงานวันนี้!" : reward ? "รับรางวัล!" : "เมนู";
        menuDisc.color = !open && (eventOn || reward) ? DiscAlert : DiscColor; // golden when an event is on or a reward waits
        menuGlyph.sprite = HudIcons.Get(open ? "close" : "menu");
        calendarDisc.color = eventOn ? DiscAlert : DiscColor;
        questDisc.color = reward ? DiscAlert : DiscColor;
        questCaption.text = !questOnline ? "ภารกิจ" : reward ? "รับรางวัล!" : $"ภารกิจ {quest.DoneCount}/{quest.Current.quests?.Length ?? 0}";
        if (career == null || career.Current == null) RefreshCareer(null, null); // keep the offline reason current

        var life = LifeClient.Instance;
        bool online = life != null && life.IsOnline;
        float energy = online ? life.Energy : 0f;
        float max = online ? life.Current.max_energy : 100f;
        energyFill.anchorMax = new Vector2(Mathf.Clamp01(energy / max), 1f);
        energyLabel.text = online ? $"พลังงาน {energy:0}/{max:0}" + (life.IsSleeping ? " (นอนอยู่)" : life.IsInjured ? " · บาดเจ็บ!" : "") : "พลังงาน —";
    }

    private void RefreshCareer(CareerClient.Stats previous, CareerClient.Stats stats)
    {
        if (stats == null)
        {
            var conn = FindAnyObjectByType<NakamaConnection>();
            string why = conn != null && !string.IsNullOrEmpty(conn.LastError) ? $" — {conn.LastError}" : "";
            offlineLabel.text = $"ออฟไลน์{why} (ลองต่อใหม่ทุก 10 วิ)";
            careerCaption.text = "อาชีพ";
            return;
        }
        offlineLabel.text = "";
        careerCaption.text = $"Lv {stats.level}";
    }

    private void Refresh(int health)
    {
        koBanner.SetActive(combat.IsKnockedOut || health <= 0);
    }

    private void Build()
    {
        var canvasGo = new GameObject("HealthHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var font = ThaiFont.Font;

        // Energy bar, inset from the top-left corner (clear of the notch in landscape)
        var energyBg = CreateRect("EnergyBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        Frame(energyBg);
        energyBg.anchorMin = energyBg.anchorMax = new Vector2(0f, 1f);
        energyBg.pivot = new Vector2(0f, 1f);
        energyBg.anchoredPosition = new Vector2(120f, -26f);
        energyBg.sizeDelta = new Vector2(400f, 40f);
        energyFill = CreateRect("Fill", energyBg, new Color(1f, 0.8f, 0.2f));
        energyFill.anchorMin = Vector2.zero;
        energyFill.anchorMax = Vector2.one;
        energyFill.offsetMin = new Vector2(4f, 4f);
        energyFill.offsetMax = new Vector2(-4f, -4f);
        energyLabel = CreateText("EnergyLabel", energyBg, font, 24, TextAnchor.MiddleCenter);
        energyLabel.rectTransform.anchorMin = Vector2.zero;
        energyLabel.rectTransform.anchorMax = Vector2.one;
        energyLabel.rectTransform.offsetMin = energyLabel.rectTransform.offsetMax = Vector2.zero;
        energyLabel.color = new Color(0.15f, 0.1f, 0.05f);

        // Only shown while the server can't be reached
        offlineLabel = CreateText("Offline", canvasGo.transform, font, 22, TextAnchor.MiddleLeft);
        var offlineRect = offlineLabel.rectTransform;
        offlineRect.anchorMin = offlineRect.anchorMax = new Vector2(0f, 1f);
        offlineRect.pivot = new Vector2(0f, 1f);
        offlineRect.anchoredPosition = new Vector2(540f, -26f);
        offlineRect.sizeDelta = new Vector2(420f, 40f);
        offlineLabel.resizeTextForBestFit = true;
        offlineLabel.resizeTextMinSize = 14;
        offlineLabel.resizeTextMaxSize = 22;
        UiKit.Cartoon(offlineLabel, 2f);

        // ── Right column (top-right): the round minimap in the corner, the time under it ──
        const float Right = -120f, Map = 250f, Below = -12f - Map - 10f;

        var mapRect = CreateRect("MiniMap", canvasGo.transform, Color.white);
        mapRect.GetComponent<Image>().raycastTarget = true; // tap to zoom
        mapRect.sizeDelta = new Vector2(Map, Map);
        Pin(mapRect, new Vector2(Right, -12f));
        mapRect.gameObject.AddComponent<MiniMap>().Configure(transform);

        clockLabel = CreateText("Clock", canvasGo.transform, font, 26, TextAnchor.MiddleCenter);
        var clockRect = clockLabel.rectTransform;
        clockRect.anchorMin = clockRect.anchorMax = new Vector2(1f, 1f);
        clockRect.pivot = new Vector2(1f, 1f);
        clockRect.anchoredPosition = new Vector2(Right, Below);
        clockRect.sizeDelta = new Vector2(Map, 38f);
        clockLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        UiKit.Cartoon(clockLabel, 2.5f);

        // ── Left side, top down: กระเป๋า and เมนู; the menu folds out a second column beside them ──
        var left = Column(canvasGo.transform, "HudButtons", 120f);
        Round(left, 0, "bag", "กระเป๋า", BagUI.Toggle, out _, out _, out _);
        Round(left, 1, "menu", "เมนู", ToggleMenu, out menuDisc, out menuGlyph, out menuCaption);

        menuColumn = Column(canvasGo.transform, "MenuColumn", 120f + Disc + 34f);
        int row = 0;
        void Entry(string icon, string caption, UnityAction open, out Image disc, out Text label) =>
            Round(menuColumn, row++, icon, caption, () => { open(); ToggleMenu(); }, out disc, out _, out label);
        Entry("person", "ตั้งชื่อ", () => NameUI.Show(firstTime: false), out _, out nameCaption);
        Entry("star", "อาชีพ", ProfessionUI.Toggle, out _, out careerCaption);
        Entry("quest", "ภารกิจ", QuestUI.Toggle, out questDisc, out questCaption);
        Entry("food", "กิน", FoodUI.Toggle, out _, out _);
        Entry("chat", "ทักทาย", EmoteUI.Toggle, out _, out _);
        Entry("calendar", "ปฏิทินงาน", EventCalendarUI.Toggle, out calendarDisc, out _);
        Entry("gear", "ตั้งค่า", SoundUI.Toggle, out _, out _);
        menuColumn.gameObject.SetActive(false);

        var ko = CreateText("KO", canvasGo.transform, font, 160, TextAnchor.MiddleCenter);
        ko.text = "KO!";
        ko.color = new Color(1f, 0.85f, 0.2f);
        ko.rectTransform.anchorMin = ko.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
        ko.rectTransform.sizeDelta = new Vector2(800f, 240f);
        koBanner = ko.gameObject;
        koBanner.SetActive(false);
    }

    private void ToggleMenu() => menuColumn.gameObject.SetActive(!menuColumn.gameObject.activeSelf);

    // A column of round buttons pinned to the top-left, under the energy bar
    private static RectTransform Column(Transform parent, string name, float x)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -86f);
        rt.sizeDelta = new Vector2(Disc, Disc);
        return rt;
    }

    // A round button: dark disc with a gold ring, a white pictogram and a small caption under it
    private static void Round(RectTransform column, int row, string icon, string caption, UnityAction onClick, out Image disc, out Image glyph, out Text label)
    {
        var rt = CreateRect("Round_" + icon, column, DiscColor);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(Disc * 0.5f, -Disc * 0.5f - row * Step);
        rt.sizeDelta = new Vector2(Disc, Disc);
        disc = rt.GetComponent<Image>();
        disc.sprite = UiKit.CircleSprite;
        disc.raycastTarget = true;
        var ring = rt.gameObject.AddComponent<Outline>();
        ring.effectColor = UiKit.Gold;
        ring.effectDistance = new Vector2(2.5f, -2.5f);
        rt.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        rt.gameObject.AddComponent<PressScale>();

        var g = CreateRect("Glyph", rt, UiKit.WoodInk);
        g.sizeDelta = new Vector2(Disc * 0.56f, Disc * 0.56f);
        glyph = g.GetComponent<Image>();
        glyph.sprite = HudIcons.Get(icon);

        label = UiKit.Cartoon(UiKit.Label(rt, 20, caption, new Vector2(0f, -Disc * 0.5f - 13f), new Vector2(150f, 26f), UiKit.WoodInk), 2f);
    }

    // Anchor a rect by its top-right corner to the top-right of its parent
    private static void Pin(Component button, Vector2 position)
    {
        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = position;
    }

    // Bars sit in a rounded dark-wood track with a gold edge (game-art style)
    private static void Frame(RectTransform bar)
    {
        var img = bar.GetComponent<Image>();
        img.sprite = UiKit.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.24f, 0.13f, 0.06f, 0.9f);
        var o = bar.gameObject.AddComponent<Outline>();
        o.effectColor = UiKit.Gold;
        o.effectDistance = new Vector2(2f, -2f);
    }

    private static RectTransform CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
