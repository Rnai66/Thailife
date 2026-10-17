using System;
using System.Collections.Generic;
using System.Text;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Shop window. Prices, money and ownership come from the server (economy.lua).
//   "community" (ร้านค้าชุมชน): sell all produce + buy seeds, feed, chickens, rods, barn space
//   "camp" (ค่ายมวย front desk): buy the camp membership
// Buying works like a cart: pick quantities, then pay once (shop_checkout validates the whole cart).
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

    private readonly Dictionary<string, int> cart = new Dictionary<string, int>();
    private WalletClient.Snapshot last;
    private Text cartText;
    private Button payButton;
    private Button clearButton;

    public static void Show(string shop = "community")
    {
        if (instance == null)
            instance = new GameObject("ShopUI").AddComponent<ShopUI>();
        if (instance.shopId != shop) instance.cart.Clear();
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
            Render(await wallet.RefreshAsync(), "");
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

    // ── Cart ────────────────────────────────────────────────────────────────────
    private int InCart(string id) => cart.TryGetValue(id, out int n) ? n : 0;

    private void ChangeQty(WalletClient.CatalogItem item, int delta)
    {
        int max = item.consumable ? 99 : 1;
        int qty = Mathf.Clamp(InCart(item.id) + delta, 0, max);
        if (qty == 0) cart.Remove(item.id); else cart[item.id] = qty;
        Render(last, null);
    }

    private int CartTotal()
    {
        int total = 0;
        if (last?.catalog == null) return 0;
        foreach (var item in last.catalog) total += item.price * InCart(item.id);
        return total;
    }

    private async void Checkout()
    {
        var wallet = WalletClient.Instance;
        if (busy || wallet == null || !wallet.IsOnline || cart.Count == 0) return;

        busy = true;
        try
        {
            var result = await wallet.CheckoutAsync(cart);
            cart.Clear();
            Render(result, $"จ่ายเงิน {result.spent:N0} บาท เรียบร้อย ได้ของครบแล้ว!");
            FarmClient.Instance?.Refresh(); // seeds / barn size changed
            CoopClient.Instance?.Refresh(); // chickens / feed changed
            LifeClient.Instance?.Refresh(); // food in the bag
            if (MusicClient.Instance != null) _ = MusicClient.Instance.RefreshAsync(); // instruments
            FloatAbovePlayer($"-{result.spent} ฿", new Color(1f, 0.6f, 0.3f));
        }
        catch (ApiResponseException e)
        {
            string why = e.Message.Contains("not enough") ? "เงินไม่พอจ่ายทั้งตะกร้า — ลดจำนวนลงก่อน"
                : e.Message.Contains("coop full") ? "ไก่เกินเล้า (เล้าจุ 8 ตัว) — ลดจำนวนไก่ในตะกร้า"
                : e.Message.Contains("barn max") ? "ขยายยุ้งได้สูงสุด 500 กก. — ลดจำนวนในตะกร้า"
                : e.Message.Contains("already") ? "มีของบางชิ้นอยู่แล้ว — เอาออกจากตะกร้า"
                : "จ่ายเงินไม่สำเร็จ";
            Render(last, why);
        }
        catch (Exception e)
        {
            Render(last, "จ่ายเงินไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
    }

    private void ClearCart()
    {
        cart.Clear();
        Render(last, "ล้างตะกร้าแล้ว");
    }

    private void Render(WalletClient.Snapshot snapshot, string message)
    {
        last = snapshot;
        titleText.text = IsCommunity ? "ร้านค้าชุมชน"
            : shopId == "city_food" ? "ร้านอาหารในเมือง — ข้าวมันไก่ · กาแฟ"
            : shopId == "music" ? "ร้านเครื่องดนตรี — พื้นบ้านและสากล"
            : "ค่ายมวย — โต๊ะรับสมัคร";
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
        int index = 0;
        if (snapshot?.catalog != null)
        {
            foreach (var item in snapshot.catalog)
            {
                if (item.shop != shopId) continue;
                AddItemRow(item, index++, snapshot.baht);
            }
        }

        // Cart summary and pay button
        var summary = new StringBuilder();
        int count = 0;
        if (snapshot?.catalog != null)
            foreach (var item in snapshot.catalog)
            {
                int n = InCart(item.id);
                if (n == 0) continue;
                summary.Append(count++ == 0 ? "" : ", ").Append(n > 1 ? $"{item.name} ×{n}" : item.name);
            }
        int total = CartTotal();
        bool affordable = snapshot != null && total <= snapshot.baht;
        cartText.text = count == 0 ? "ตะกร้า: ว่าง — กด + เพื่อเลือกของ"
            : $"ตะกร้า: {summary} · รวม {total:N0} บาท" + (affordable ? "" : " (เงินไม่พอ)");
        cartText.color = affordable ? new Color(0.3f, 0.15f, 0.05f) : new Color(0.75f, 0.15f, 0.1f);
        payButton.interactable = !busy && count > 0 && affordable;
        clearButton.interactable = !busy && count > 0;

        if (message != null) messageText.text = message;
    }

    // Three columns, up to four rows
    private void AddItemRow(WalletClient.CatalogItem item, int index, int baht)
    {
        var row = MakeRect("Item", itemList, new Color(1f, 1f, 1f, 0.6f));
        row.anchoredPosition = new Vector2((index % 3 - 1) * 480f, -(index / 3) * 112f);
        row.sizeDelta = new Vector2(465f, 100f);

        var title = MakeLabel(row, 28, $"{item.name} — {item.price}฿", new Vector2(-75f, 22f), new Vector2(300f, 44f), new Color(0.3f, 0.15f, 0.05f));
        title.alignment = TextAnchor.MiddleLeft;
        string detail = string.IsNullOrEmpty(item.have) ? item.desc : $"{item.desc} · {item.have}";
        var desc = MakeLabel(row, 20, detail, new Vector2(-75f, -22f), new Vector2(300f, 44f), new Color(0.35f, 0.3f, 0.25f));
        desc.alignment = TextAnchor.MiddleLeft;

        int qty = InCart(item.id);
        if (item.owned)
        {
            MakeButton(row, "มีแล้ว", new Vector2(160f, 0f), new Vector2(130f, 76f), new Color(0.7f, 0.85f, 0.7f), () => { }).interactable = false;
        }
        else if (!item.consumable)
        {
            // One-only item: toggle in/out of the cart
            MakeButton(row, qty > 0 ? "ในตะกร้า" : "ใส่ตะกร้า", new Vector2(160f, 0f), new Vector2(130f, 76f),
                qty > 0 ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.75f, 1f), () => ChangeQty(item, qty > 0 ? -1 : 1));
        }
        else
        {
            // - qty +
            MakeButton(row, "-", new Vector2(110f, 0f), new Vector2(50f, 70f), new Color(0.9f, 0.75f, 0.7f), () => ChangeQty(item, -1)).interactable = qty > 0;
            var count = MakeLabel(row, 30, qty.ToString(), new Vector2(160f, 0f), new Vector2(46f, 60f), new Color(0.3f, 0.15f, 0.05f));
            count.fontStyle = FontStyle.Bold;
            MakeButton(row, "+", new Vector2(210f, 0f), new Vector2(50f, 70f), new Color(0.45f, 0.75f, 1f), () => ChangeQty(item, 1));
        }
        if (qty > 0) row.GetComponent<Image>().color = new Color(1f, 0.93f, 0.6f, 0.9f); // highlight items in the cart
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

        font = ThaiFont.Font;
        var brown = new Color(0.3f, 0.15f, 0.05f);

        panel = MakeRect("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f)).gameObject;
        var backdrop = (RectTransform)panel.transform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;

        var card = MakeRect("Card", panel.transform, new Color(0.98f, 0.95f, 0.86f));
        card.sizeDelta = new Vector2(1500f, 1000f);

        titleText = MakeLabel(card, 52, "", new Vector2(0f, 445f), new Vector2(940f, 70f), brown);
        balanceText = MakeLabel(card, 34, "", new Vector2(0f, 385f), new Vector2(940f, 50f), new Color(0.1f, 0.45f, 0.2f));
        basketText = MakeLabel(card, 28, "", new Vector2(-170f, 310f), new Vector2(1060f, 90f), brown);
        sellButton = MakeButton(card, "ขายทั้งหมด", new Vector2(560f, 310f), new Vector2(280f, 84f), new Color(1f, 0.8f, 0.3f), SellAll);

        itemList = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
        itemList.SetParent(card, false);
        itemList.anchoredPosition = new Vector2(0f, 205f);

        cartText = MakeLabel(card, 28, "", new Vector2(-230f, -290f), new Vector2(980f, 70f), new Color(0.3f, 0.15f, 0.05f));
        payButton = MakeButton(card, "จ่ายเงิน", new Vector2(420f, -290f), new Vector2(220f, 80f), new Color(0.45f, 0.85f, 0.45f), Checkout);
        clearButton = MakeButton(card, "ล้าง", new Vector2(620f, -290f), new Vector2(140f, 80f), new Color(0.9f, 0.75f, 0.7f), ClearCart);

        messageText = MakeLabel(card, 34, "", new Vector2(0f, -375f), new Vector2(1400f, 56f), new Color(0.6f, 0.2f, 0.05f));
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
