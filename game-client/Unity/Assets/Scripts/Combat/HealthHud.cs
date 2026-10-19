using UnityEngine;
using UnityEngine.UI;

// Local player's health bar, career level/XP (top-left) and a "KO!" banner. Builds its own canvas at runtime.
[RequireComponent(typeof(PlayerCombat))]
public class HealthHud : MonoBehaviour
{
    private PlayerCombat combat;
    private RectTransform fill;
    private Image fillImage;
    private Text label;
    private GameObject koBanner;
    private RectTransform xpFill;
    private Text careerLabel;
    private CareerClient career;
    private Text fishLabel;
    private FishingClient fishing;
    private Text moneyLabel;
    private WalletClient wallet;
    private Text clockLabel;
    private Text nameLabel;
    private ProfileClient profile;
    private RectTransform energyFill;
    private Text energyLabel;

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
        if (fishing != null) fishing.InventoryChanged -= RefreshFish;
        if (wallet != null) wallet.Changed -= RefreshMoney;
        if (profile != null) profile.NameChanged -= RefreshName;
    }

    private void Start()
    {
        Refresh(combat.Health);

        career = CareerClient.Instance;
        if (career != null) career.StatsChanged += RefreshCareer;
        RefreshCareer(null, career != null ? career.Current : null);

        fishing = FishingClient.Instance;
        if (fishing != null) fishing.InventoryChanged += RefreshFish;
        RefreshFish(fishing != null ? fishing.Current : null);

        wallet = WalletClient.Instance;
        if (wallet != null) wallet.Changed += RefreshMoney;
        RefreshMoney(wallet != null ? wallet.Current : null);

        profile = ProfileClient.Instance;
        if (profile != null) profile.NameChanged += RefreshName;
        RefreshName(profile != null ? profile.DisplayName : null);
    }

    private void RefreshName(string name)
    {
        nameLabel.text = string.IsNullOrWhiteSpace(name) ? "ตั้งชื่อ" : $"{name}  (แตะเพื่อเปลี่ยน)";
    }

    // Clock and energy change continuously; refresh a few times a second
    private Image questImage;
    private Text questLabel;
    private float nextTick;

    private void Update()
    {
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;

        clockLabel.text = GameClock.Display;
        if (career == null || career.Current == null) RefreshCareer(null, null); // keep the offline reason current

        var life = LifeClient.Instance;
        bool online = life != null && life.IsOnline;
        float energy = online ? life.Energy : 0f;
        float max = online ? life.Current.max_energy : 100f;
        energyFill.anchorMax = new Vector2(Mathf.Clamp01(energy / max), 1f);
        var q = QuestClient.Instance;
        if (q != null && q.IsOnline)
        {
            bool reward = q.HasReward;
            questLabel.text = reward ? "ภารกิจ: กดรับรางวัล!" : $"ภารกิจวันนี้ {q.DoneCount}/{q.Current.quests?.Length ?? 0}";
            questImage.color = reward ? UiKit.Gold : UiKit.Teak;
            questLabel.color = reward ? UiKit.GoldInk : UiKit.GoldText;
        }
        else questLabel.text = "ภารกิจวันนี้";

        energyLabel.text = online ? $"พลังงาน {energy:0}/{max:0}" + (life.IsSleeping ? " (นอนอยู่)" : "") : "พลังงาน —";
    }

    private void RefreshMoney(WalletClient.Snapshot snapshot)
    {
        moneyLabel.text = snapshot == null ? "" : $"เงิน {snapshot.baht:N0} บาท";
    }

    private void RefreshFish(FishingClient.Inventory inventory)
    {
        fishLabel.text = inventory == null || inventory.total == 0
            ? ""
            : $"ปลาในข้อง {inventory.total} ตัว · ใหญ่สุด {inventory.biggest_name} {inventory.biggest_kg:0.00} กก.";
    }

    private void RefreshCareer(CareerClient.Stats previous, CareerClient.Stats stats)
    {
        if (stats == null)
        {
            var conn = FindAnyObjectByType<NakamaConnection>();
            string why = conn != null && !string.IsNullOrEmpty(conn.LastError) ? $" — {conn.LastError}" : "";
            careerLabel.text = $"ออฟไลน์{why} (ลองต่อใหม่ทุก 10 วิ)";
            xpFill.anchorMax = new Vector2(0f, 1f);
            return;
        }

        bool maxed = stats.xp_to_next <= 0;
        careerLabel.text = maxed
            ? $"Lv {stats.level} {CareerClient.RankFor(stats.level)} · MAX"
            : $"Lv {stats.level} {CareerClient.RankFor(stats.level)} · XP {stats.xp}/{stats.xp_to_next}";
        xpFill.anchorMax = new Vector2(maxed ? 1f : (float)stats.xp / stats.xp_to_next, 1f);
    }

    private void Refresh(int health)
    {
        float ratio = combat.MaxHealth > 0 ? (float)health / combat.MaxHealth : 0f;
        fill.anchorMax = new Vector2(ratio, 1f);
        fillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.35f), ratio);
        label.text = $"HP {health}/{combat.MaxHealth}";
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

        // Bar background, inset from the top-left corner (clear of the notch in landscape)
        var background = CreateRect("HealthBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        background.anchorMin = background.anchorMax = new Vector2(0f, 1f);
        background.pivot = new Vector2(0f, 1f);
        background.anchoredPosition = new Vector2(120f, -50f);
        background.sizeDelta = new Vector2(520f, 44f);

        fill = CreateRect("Fill", background, Color.green);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        fillImage = fill.GetComponent<Image>();

        label = CreateText("Label", background, font, 28, TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        // Career: XP bar + level/rank text under the health bar
        var xpBackground = CreateRect("XpBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        xpBackground.anchorMin = xpBackground.anchorMax = new Vector2(0f, 1f);
        xpBackground.pivot = new Vector2(0f, 1f);
        xpBackground.anchoredPosition = new Vector2(120f, -100f);
        xpBackground.sizeDelta = new Vector2(520f, 14f);

        xpFill = CreateRect("Fill", xpBackground, new Color(0.4f, 0.85f, 1f));
        xpFill.anchorMin = Vector2.zero;
        xpFill.anchorMax = new Vector2(0f, 1f);
        xpFill.offsetMin = new Vector2(2f, 2f);
        xpFill.offsetMax = new Vector2(-2f, -2f);

        careerLabel = CreateText("Career", canvasGo.transform, font, 30, TextAnchor.MiddleLeft);
        var careerRect = careerLabel.rectTransform;
        careerRect.anchorMin = careerRect.anchorMax = new Vector2(0f, 1f);
        careerRect.pivot = new Vector2(0f, 1f);
        careerRect.anchoredPosition = new Vector2(120f, -120f);
        careerRect.sizeDelta = new Vector2(900f, 44f);
        careerLabel.gameObject.AddComponent<Shadow>();

        fishLabel = CreateText("Fish", canvasGo.transform, font, 26, TextAnchor.MiddleLeft);
        var fishRect = fishLabel.rectTransform;
        fishRect.anchorMin = fishRect.anchorMax = new Vector2(0f, 1f);
        fishRect.pivot = new Vector2(0f, 1f);
        fishRect.anchoredPosition = new Vector2(120f, -162f);
        fishRect.sizeDelta = new Vector2(900f, 40f);
        fishLabel.gameObject.AddComponent<Shadow>();

        moneyLabel = CreateText("Money", canvasGo.transform, font, 34, TextAnchor.MiddleRight);
        var moneyRect = moneyLabel.rectTransform;
        moneyRect.anchorMin = moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.pivot = new Vector2(1f, 1f);
        moneyRect.anchoredPosition = new Vector2(-120f, -50f);
        moneyRect.sizeDelta = new Vector2(500f, 50f);
        moneyLabel.color = new Color(1f, 0.85f, 0.2f);
        moneyLabel.gameObject.AddComponent<Shadow>();

        // Game clock and energy bar under the money (top-right)
        clockLabel = CreateText("Clock", canvasGo.transform, font, 30, TextAnchor.MiddleRight);
        var clockRect = clockLabel.rectTransform;
        clockRect.anchorMin = clockRect.anchorMax = new Vector2(1f, 1f);
        clockRect.pivot = new Vector2(1f, 1f);
        clockRect.anchoredPosition = new Vector2(-120f, -100f);
        clockRect.sizeDelta = new Vector2(600f, 44f);
        clockLabel.gameObject.AddComponent<Shadow>();

        var energyBg = CreateRect("EnergyBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        energyBg.anchorMin = energyBg.anchorMax = new Vector2(1f, 1f);
        energyBg.pivot = new Vector2(1f, 1f);
        energyBg.anchoredPosition = new Vector2(-120f, -150f);
        energyBg.sizeDelta = new Vector2(400f, 34f);
        energyFill = CreateRect("Fill", energyBg, new Color(1f, 0.8f, 0.2f));
        energyFill.anchorMin = Vector2.zero;
        energyFill.anchorMax = Vector2.one;
        energyFill.offsetMin = new Vector2(3f, 3f);
        energyFill.offsetMax = new Vector2(-3f, -3f);
        energyLabel = CreateText("EnergyLabel", energyBg, font, 24, TextAnchor.MiddleCenter);
        energyLabel.rectTransform.anchorMin = Vector2.zero;
        energyLabel.rectTransform.anchorMax = Vector2.one;
        energyLabel.rectTransform.offsetMin = energyLabel.rectTransform.offsetMax = Vector2.zero;
        energyLabel.color = new Color(0.15f, 0.1f, 0.05f);

        // Character name above the health bar; tap to rename
        var nameButton = UiKit.Button(canvasGo.transform, "", Vector2.zero, new Vector2(520f, 40f), new Color(0f, 0f, 0f, 0.35f), () => NameUI.Show(firstTime: false), 26);
        var nameRect = (RectTransform)nameButton.transform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(120f, -6f);
        nameLabel = nameButton.GetComponentInChildren<Text>();
        nameLabel.color = Color.white;
        nameLabel.alignment = TextAnchor.MiddleLeft;

        // "กิน" opens the food bag (left of the energy bar)
        var eat = UiKit.Button(canvasGo.transform, "กิน", Vector2.zero, new Vector2(110f, 48f), new Color(1f, 0.8f, 0.3f), FoodUI.Toggle, 28);
        var eatRect = (RectTransform)eat.transform;
        eatRect.anchorMin = eatRect.anchorMax = new Vector2(1f, 1f);
        eatRect.pivot = new Vector2(1f, 1f);
        eatRect.anchoredPosition = new Vector2(-535f, -143f);

        // "ภารกิจ" opens the daily quests (under the energy bar)
        var quests = UiKit.RoundButton(canvasGo.transform, "ภารกิจวันนี้", Vector2.zero, new Vector2(400f, 52f), UiKit.Teak, UiKit.GoldText, QuestUI.Toggle, 26);
        var questRect = (RectTransform)quests.transform;
        questRect.anchorMin = questRect.anchorMax = new Vector2(1f, 1f);
        questRect.pivot = new Vector2(1f, 1f);
        questRect.anchoredPosition = new Vector2(-120f, -196f);
        questImage = quests.GetComponent<Image>();
        questLabel = quests.GetComponentInChildren<Text>();

        // "ทักทาย": ไหว้, โบกมือ and preset phrases
        var greet = UiKit.RoundButton(canvasGo.transform, "ทักทาย", Vector2.zero, new Vector2(400f, 52f), UiKit.Teak, UiKit.GoldText, EmoteUI.Toggle, 26);
        var greetRect = (RectTransform)greet.transform;
        greetRect.anchorMin = greetRect.anchorMax = new Vector2(1f, 1f);
        greetRect.pivot = new Vector2(1f, 1f);
        greetRect.anchoredPosition = new Vector2(-120f, -256f);

        var ko = CreateText("KO", canvasGo.transform, font, 160, TextAnchor.MiddleCenter);
        ko.text = "KO!";
        ko.color = new Color(1f, 0.85f, 0.2f);
        ko.rectTransform.anchorMin = ko.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
        ko.rectTransform.sizeDelta = new Vector2(800f, 240f);
        koBanner = ko.gameObject;
        koBanner.SetActive(false);
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
