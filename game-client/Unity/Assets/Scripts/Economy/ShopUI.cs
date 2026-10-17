using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Community shop window: shows today's fish prices and the basket, and sells all fish for baht.
// Built at runtime; prices and money come from the server (economy.lua).
public class ShopUI : MonoBehaviour
{
    private static ShopUI instance;

    private GameObject panel;
    private Text balanceText;
    private Text basketText;
    private Text totalText;
    private Button sellButton;
    private bool busy;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("ShopUI").AddComponent<ShopUI>();
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
        sellButton.interactable = false;
        try
        {
            var result = await wallet.SellAllFishAsync();
            Render(result, result.sold > 0 ? $"ขายปลา {result.sold} ตัว ได้ {result.earned} บาท" : "ไม่มีปลาให้ขาย ไปตกที่คลองในตำบลก่อนนะ");

            var player = FindAnyObjectByType<PlayerController>();
            if (result.earned > 0 && player != null)
                FloatingText.Spawn(player.transform.position + Vector3.up * 2f, $"+{result.earned} ฿", new Color(1f, 0.85f, 0.2f));
        }
        catch (Exception e)
        {
            Render(wallet.Current, "ขายไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
    }

    private void Render(WalletClient.Snapshot snapshot, string message)
    {
        balanceText.text = snapshot != null ? $"เงินของคุณ: {snapshot.baht:N0} บาท" : "";

        var lines = new StringBuilder();
        int fishCount = 0;
        if (snapshot?.fish != null)
        {
            foreach (var fish in snapshot.fish)
            {
                lines.AppendLine(fish.count > 0
                    ? $"{fish.name}  {fish.count} ตัว × {fish.price} ฿ = {fish.count * fish.price} ฿"
                    : $"{fish.name}  (รับซื้อตัวละ {fish.price} ฿)");
                fishCount += fish.count;
            }
        }
        basketText.text = lines.ToString();

        totalText.text = message ?? (fishCount > 0 ? $"รวมขายได้ {snapshot.total_value:N0} บาท" : "ยังไม่มีปลาในข้อง");
        sellButton.interactable = !busy && snapshot != null && fishCount > 0;
    }

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

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var brown = new Color(0.3f, 0.15f, 0.05f);

        panel = MakeRect("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f)).gameObject;
        var backdrop = (RectTransform)panel.transform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;

        var card = MakeRect("Card", panel.transform, new Color(0.98f, 0.95f, 0.86f));
        card.sizeDelta = new Vector2(1000f, 820f);

        MakeLabel(card, font, 52, "ร้านค้าชุมชน — รับซื้อปลา", new Vector2(0f, 345f), new Vector2(940f, 80f), brown);
        balanceText = MakeLabel(card, font, 40, "", new Vector2(0f, 265f), new Vector2(940f, 60f), new Color(0.1f, 0.45f, 0.2f));
        basketText = MakeLabel(card, font, 36, "", new Vector2(0f, 60f), new Vector2(900f, 320f), brown);
        totalText = MakeLabel(card, font, 40, "", new Vector2(0f, -150f), new Vector2(940f, 70f), new Color(0.6f, 0.2f, 0.05f));

        sellButton = MakeButton(card, font, "ขายปลาทั้งหมด", new Vector2(-200f, -300f), new Color(1f, 0.8f, 0.3f), SellAll);
        MakeButton(card, font, "ปิด", new Vector2(240f, -300f), new Color(0.9f, 0.45f, 0.4f), Hide);
    }

    private static RectTransform MakeRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static Text MakeLabel(Transform parent, Font font, int size, string text, Vector2 position, Vector2 box, Color color)
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
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = size;
        return label;
    }

    private static Button MakeButton(Transform parent, Font font, string text, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect("Button", parent, color);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(400f, 110f);
        var button = rt.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        MakeLabel(rt, font, 42, text, Vector2.zero, new Vector2(380f, 110f), new Color(0.2f, 0.1f, 0.05f));
        return button;
    }
}
