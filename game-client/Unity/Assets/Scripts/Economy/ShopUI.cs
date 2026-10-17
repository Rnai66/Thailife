using System;
using System.Text;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Shop window. Prices, money and ownership come from the server (economy.lua).
//   "community" (ร้านค้าชุมชน): sell all fish + buy fishing rods
//   "camp" (ค่ายมวย front desk): buy the camp membership
// Built at runtime; one instance per scene.
public class ShopUI : MonoBehaviour
{
    private static ShopUI instance;

    private string shopId = "community";
    private GameObject panel;
    private Text titleText;
    private Text balanceText;
    private Text basketText;
    private Text messageText;
    private Button sellButton;
    private RectTransform itemList;
    private Font font;
    private bool busy;

    public static void Show(string shop = "community")
    {
        if (instance == null)
            instance = new GameObject("ShopUI").AddComponent<ShopUI>();
        instance.shopId = shop;
        instance.panel.SetActive(true);
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.panel.SetActive(false);
    }

    private void Awake()
    {
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private bool IsCommunity => shopId == "community";

    private async void Refresh()
    {
        var wallet = WalletClient.Instance;
        if (wallet == null || !wallet.IsOnline)
        {
            Render(null, "ต้องออนไลน์ก่อนถึงจะซื้อขายได้");
            return;
        }

        Render(wallet.Current, "กำลังโหลด...");
        try
        {
            Render(await wallet.RefreshAsync(), null);
        }
        catch (Exception e)
        {
            Render(wallet.Current, "โหลดไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Shop] {e.Message}");
        }
    }

    private async void SellAll()
    {
        var wallet = WalletClient.Instance;
        if (busy || wallet == null || !wallet.IsOnline) return;

        busy = true;
        try
        {
            var result = await wallet.SellAllFishAsync();
            Render(result, result.earned > 0 ? $"ขายผลผลิตได้ {result.earned} บาท" : "ยังไม่มีอะไรให้ขาย — ตกปลาที่คลองในตำบล หรือทำนาที่โคก หนอง นา");
            if (result.earned > 0) FloatAbovePlayer($"+{result.earned} ฿", new Color(1f, 0.85f, 0.2f));
            FarmClient.Instance?.Refresh(); // the barn is empty now
        }
        catch (Exception e)
        {
            Render(wallet.Current, "ขายไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
    }

    private async void Buy(WalletClient.CatalogItem item)
    {
        var wallet = WalletClient.Instance;
        if (busy || wallet == null || !wallet.IsOnline) return;

        busy = true;
        try
        {
            var result = await wallet.BuyAsync(item.id);
            Render(result, $"ซื้อ{item.name}แล้ว!");
            FarmClient.Instance?.Refresh(); // seeds / barn size changed
            FloatAbovePlayer($"-{item.price} ฿", new Color(1f, 0.6f, 0.3f));
        }
        catch (ApiResponseException e)
        {
            string why = e.Message.Contains("not enough") ? $"เงินไม่พอ ต้องมี {item.price} บาท"
                : e.Message.Contains("already") ? "มีอยู่แล้ว" : "ซื้อไม่สำเร็จ";
            Render(wallet.Current, why);
        }
        catch (Exception e)
        {
            Render(wallet.Current, "ซื้อไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
    }

    private void Render(WalletClient.Snapshot snapshot, string message)
    {
        titleText.text = IsCommunity ? "ร้านค้าชุมชน" : "ค่ายมวย — โต๊ะรับสมัคร";
        balanceText.text = snapshot != null
            ? $"เงินของคุณ: {snapshot.baht:N0} บาท" + (IsCommunity && !string.IsNullOrEmpty(snapshot.rod_name) ? $"   ·   ใช้{snapshot.rod_name}" : "")
            : "";

        // Fish basket (community shop only)
        int fishCount = 0;
        var lines = new StringBuilder();
        if (IsCommunity && snapshot?.fish != null)
        {
            foreach (var fish in snapshot.fish)
            {
                if (fish.count > 0) lines.Append($"{fish.name} {fish.count}×{fish.price}฿   ");
                fishCount += fish.count;
            }
        }
        basketText.text = !IsCommunity ? "" :
            fishCount > 0 ? $"รับซื้อ: {lines}= {snapshot.total_value:N0} บาท" : "รับซื้อปลาและข้าวเปลือก: ยังไม่มีของให้ขาย";
        basketText.gameObject.SetActive(IsCommunity);
        sellButton.gameObject.SetActive(IsCommunity);
        sellButton.interactable = !busy && fishCount > 0;

        // Items for sale in this shop
        foreach (Transform child in itemList) Destroy(child.gameObject);
        float y = 0f;
        if (snapshot?.catalog != null)
        {
            foreach (var item in snapshot.catalog)
            {
                if (item.shop != shopId) continue;
                AddItemRow(item, y, snapshot.baht);
                y -= 106f;
            }
        }

        messageText.text = message ?? "";
    }

    private void AddItemRow(WalletClient.CatalogItem item, float y, int baht)
    {
        var row = MakeRect("Item", itemList, new Color(1f, 1f, 1f, 0.6f));
        row.anchoredPosition = new Vector2(0f, y);
        row.sizeDelta = new Vector2(900f, 98f);

        var title = MakeLabel(row, 32, $"{item.name} — {item.price} บาท", new Vector2(-150f, 20f), new Vector2(560f, 44f), new Color(0.3f, 0.15f, 0.05f));
        title.alignment = TextAnchor.MiddleLeft;
        string detail = string.IsNullOrEmpty(item.have) ? item.desc : $"{item.desc} · {item.have}";
        var desc = MakeLabel(row, 26, detail, new Vector2(-150f, -22f), new Vector2(560f, 40f), new Color(0.35f, 0.3f, 0.25f));
        desc.alignment = TextAnchor.MiddleLeft;

        string label = item.owned ? "มีแล้ว" : baht >= item.price ? "ซื้อ" : "เงินไม่พอ";
        var button = MakeButton(row, label, new Vector2(330f, 0f), new Vector2(220f, 80f),
            item.owned ? new Color(0.7f, 0.85f, 0.7f) : new Color(0.45f, 0.75f, 1f), () => Buy(item));
        button.interactable = !item.owned && baht >= item.price && !busy;
    }

    private void FloatAbovePlayer(string text, Color color)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) FloatingText.Spawn(player.transform.position + Vector3.up * 2f, text, color);
    }

    // ── UI construction ─────────────────────────────────────────────────────────
    private void Build()
    {
        var canvasGo = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var brown = new Color(0.3f, 0.15f, 0.05f);

        panel = MakeRect("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f)).gameObject;
        var backdrop = (RectTransform)panel.transform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;

        var card = MakeRect("Card", panel.transform, new Color(0.98f, 0.95f, 0.86f));
        card.sizeDelta = new Vector2(1000f, 1000f);

        titleText = MakeLabel(card, 52, "", new Vector2(0f, 445f), new Vector2(940f, 70f), brown);
        balanceText = MakeLabel(card, 34, "", new Vector2(0f, 385f), new Vector2(940f, 50f), new Color(0.1f, 0.45f, 0.2f));
        basketText = MakeLabel(card, 28, "", new Vector2(-130f, 310f), new Vector2(680f, 90f), brown);
        sellButton = MakeButton(card, "ขายทั้งหมด", new Vector2(340f, 310f), new Vector2(260f, 84f), new Color(1f, 0.8f, 0.3f), SellAll);

        itemList = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
        itemList.SetParent(card, false);
        itemList.anchoredPosition = new Vector2(0f, 205f);

        messageText = MakeLabel(card, 34, "", new Vector2(0f, -375f), new Vector2(940f, 56f), new Color(0.6f, 0.2f, 0.05f));
        MakeButton(card, "ปิด", new Vector2(0f, -445f), new Vector2(300f, 80f), new Color(0.9f, 0.45f, 0.4f), Hide);
    }

    private static RectTransform MakeRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private Text MakeLabel(Transform parent, int size, string text, Vector2 position, Vector2 box, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 16;
        label.resizeTextMaxSize = size;
        return label;
    }

    private Button MakeButton(Transform parent, string text, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect("Button", parent, color);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var button = rt.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        MakeLabel(rt, 38, text, Vector2.zero, size - new Vector2(16f, 8f), new Color(0.2f, 0.1f, 0.05f));
        return button;
    }
}
