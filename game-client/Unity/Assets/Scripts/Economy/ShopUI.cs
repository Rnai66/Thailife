using System;
using System.Collections;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Shop window — premium layout:
//   header (shop name, coin balance, close) · category tabs (left) · item cards (centre) · cart (right)
// Shops: "community" (ร้านค้าชุมชน: food, farming, chickens, fishing + a sell tab), "city_food",
// "music", "camp". Buying is a cart paid once (shop_checkout validates the whole cart server-side);
// selling empties the fish basket and rice barn (shop_sell_fish). Built at runtime.
public class ShopUI : MonoBehaviour
{
    // Palette (teak, gold and cream)
    private static readonly Color Teak = new Color32(0x3B, 0x24, 0x16, 0xFF);
    private static readonly Color TeakDark = new Color32(0x24, 0x15, 0x0D, 0xFF);
    private static readonly Color Gold = new Color32(0xE8, 0xB6, 0x4C, 0xFF);
    private static readonly Color GoldText = new Color32(0xFA, 0xC7, 0x75, 0xFF);
    private static readonly Color Cream = new Color32(0xFB, 0xF4, 0xE6, 0xFF);
    private static readonly Color CardBorder = new Color32(0xEA, 0xDB, 0xC2, 0xFF);
    private static readonly Color Ink = new Color32(0x3B, 0x24, 0x16, 0xFF);
    private static readonly Color Muted = new Color32(0x88, 0x87, 0x80, 0xFF);
    private static readonly Color Price = new Color32(0xBA, 0x75, 0x17, 0xFF);
    private static readonly Color PayGreen = new Color32(0x2E, 0x8B, 0x57, 0xFF);
    private static readonly Color SellGreen = new Color32(0x3B, 0x6D, 0x11, 0xFF);
    private static readonly Color SellTint = new Color32(0xEA, 0xF3, 0xDE, 0xFF);
    private static readonly Color Danger = new Color32(0x99, 0x3C, 0x1D, 0xFF);
    private static readonly Color TabText = new Color32(0xD9, 0xC2, 0xA0, 0xFF);

    private class Category
    {
        public string Name;
        public Color Tile, TileInk;
        public Func<string, bool> Has;
    }

    private const string SellTab = "__sell";

    private static ShopUI instance;

    private string shopId = "community";
    private string tab;
    private GameObject backdrop;
    private Text titleText, subtitleText, balanceText, toastText;
    private RectTransform tabs, grid, cartList, cartFooter;
    private Text cartTitle, haveText, afterText, totalLabel, totalText;
    private Button payButton, clearButton;
    private Text payLabel;
    private bool busy;
    private Coroutine toastRoutine;

    private readonly Dictionary<string, int> cart = new Dictionary<string, int>();
    private WalletClient.Snapshot last;

    // ── Opening / closing ───────────────────────────────────────────────────────
    public static void Show(string shop = "community")
    {
        if (instance == null)
            instance = new GameObject("ShopUI").AddComponent<ShopUI>();
        if (instance.shopId != shop) { instance.cart.Clear(); instance.tab = null; }
        instance.shopId = shop;
        instance.backdrop.SetActive(true);
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        Build();
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private List<Category> Categories()
    {
        var list = new List<Category>();
        switch (shopId)
        {
            case "community":
                list.Add(new Category { Name = "ของกิน", Tile = new Color32(0xFA, 0xEE, 0xDA, 0xFF), TileInk = new Color32(0x85, 0x4F, 0x0B, 0xFF), Has = id => id.StartsWith("food_") });
                list.Add(new Category { Name = "ทำนา", Tile = new Color32(0xEA, 0xF3, 0xDE, 0xFF), TileInk = new Color32(0x3B, 0x6D, 0x11, 0xFF), Has = id => id.StartsWith("seed_") || id == "barn_upgrade" });
                list.Add(new Category { Name = "เลี้ยงไก่", Tile = new Color32(0xFA, 0xEC, 0xE7, 0xFF), TileInk = new Color32(0x99, 0x3C, 0x1D, 0xFF), Has = id => id.StartsWith("chicken_") });
                list.Add(new Category { Name = "แต่งบ้าน", Tile = new Color32(0xF1, 0xEF, 0xE8, 0xFF), TileInk = new Color32(0x5F, 0x5E, 0x5A, 0xFF), Has = id => id.StartsWith("furn_") });
                list.Add(new Category { Name = "ตกปลา", Tile = new Color32(0xE6, 0xF1, 0xFB, 0xFF), TileInk = new Color32(0x18, 0x5F, 0xA5, 0xFF), Has = id => id.StartsWith("rod_") });
                break;
            case "music":
                list.Add(new Category { Name = "พื้นบ้าน", Tile = new Color32(0xFB, 0xEA, 0xF0, 0xFF), TileInk = new Color32(0x99, 0x35, 0x56, 0xFF), Has = id => id == "inst_khaen" || id == "inst_so" || id == "inst_ranat" });
                list.Add(new Category { Name = "สากล", Tile = new Color32(0xEE, 0xED, 0xFE, 0xFF), TileInk = new Color32(0x3C, 0x34, 0x89, 0xFF), Has = id => id.StartsWith("inst_") && id != "inst_khaen" && id != "inst_so" && id != "inst_ranat" });
                break;
            case "market":
                list.Add(new Category { Name = "ของกิน", Tile = new Color32(0xFA, 0xEE, 0xDA, 0xFF), TileInk = new Color32(0x85, 0x4F, 0x0B, 0xFF), Has = id => id.StartsWith("food_") });
                list.Add(new Category { Name = "ราคาตลาด", Tile = new Color32(0xEA, 0xF3, 0xDE, 0xFF), TileInk = new Color32(0x3B, 0x6D, 0x11, 0xFF), Has = id => id.StartsWith("market_") });
                break;
            case "flea":
                list.Add(new Category { Name = "ของกิน", Tile = new Color32(0xFA, 0xEC, 0xE7, 0xFF), TileInk = new Color32(0x99, 0x3C, 0x1D, 0xFF), Has = id => id.StartsWith("food_") });
                list.Add(new Category { Name = "ของถูก", Tile = new Color32(0xF1, 0xEF, 0xE8, 0xFF), TileInk = new Color32(0x5F, 0x5E, 0x5A, 0xFF), Has = id => id.StartsWith("flea_") });
                break;
            case "city_food":
                list.Add(new Category { Name = "อาหาร", Tile = new Color32(0xFA, 0xEE, 0xDA, 0xFF), TileInk = new Color32(0x85, 0x4F, 0x0B, 0xFF), Has = id => id.StartsWith("food_") });
                break;
            default:
                list.Add(new Category { Name = "สมัคร", Tile = new Color32(0xFC, 0xEB, 0xEB, 0xFF), TileInk = new Color32(0xA3, 0x2D, 0x2D, 0xFF), Has = id => id == "camp_membership" });
                break;
        }
        return list;
    }

    private bool HasSellTab => shopId == "community" || shopId == "market";

    // ── Server calls ────────────────────────────────────────────────────────────
    private async void Refresh()
    {
        var wallet = WalletClient.Instance;
        if (wallet == null || !wallet.IsOnline)
        {
            Render(null);
            Toast("ต้องออนไลน์ก่อนถึงจะซื้อขายได้", Danger);
            return;
        }

        Render(wallet.Current);
        try { Render(await wallet.RefreshAsync()); }
        catch (Exception e)
        {
            Toast("โหลดข้อมูลร้านไม่สำเร็จ ลองใหม่", Danger);
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
            Render(result);
            if (result.earned > 0)
            {
                Toast($"ขายได้ {result.earned:N0} บาท", SellGreen);
                UiKit.FloatAbovePlayer($"+{result.earned} ฿", new Color(1f, 0.85f, 0.2f));
            }
            else Toast("ยังไม่มีของให้ขาย — ตกปลาที่คลอง หรือทำนาที่โคก หนอง นา", Muted);
            FarmClient.Instance?.Refresh(); // the barn is empty now
        }
        catch (Exception e)
        {
            Toast("ขายไม่สำเร็จ ลองใหม่", Danger);
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
        Render(last);
    }

    private async void Checkout()
    {
        var wallet = WalletClient.Instance;
        if (busy || wallet == null || !wallet.IsOnline || cart.Count == 0) return;

        busy = true;
        Render(last);
        try
        {
            var result = await wallet.CheckoutAsync(cart);
            cart.Clear();
            Render(result);
            Toast($"ซื้อเรียบร้อย จ่าย {result.spent:N0} บาท", PayGreen);
            FarmClient.Instance?.Refresh();  // seeds / barn size
            CoopClient.Instance?.Refresh();  // chickens / feed
            LifeClient.Instance?.Refresh();  // food in the bag
            HouseClient.Instance?.Refresh(); // furniture in storage
            if (MusicClient.Instance != null) _ = MusicClient.Instance.RefreshAsync(); // instruments
            UiKit.FloatAbovePlayer($"-{result.spent} ฿", new Color(1f, 0.6f, 0.3f));
        }
        catch (ApiResponseException e)
        {
            Toast(e.Message.Contains("not enough") ? "เงินไม่พอจ่ายทั้งตะกร้า — ลดจำนวนลงก่อน"
                : e.Message.Contains("coop full") ? "ไก่เกินเล้า (เล้าจุ 8 ตัว)"
                : e.Message.Contains("barn max") ? "ขยายยุ้งได้สูงสุด 500 กก."
                : e.Message.Contains("already") ? "มีของบางชิ้นอยู่แล้ว — เอาออกจากตะกร้า"
                : e.Message.Contains("market closed") ? $"ตลาดปิดแล้ว — เปิด {MarketOf(last)?.hours} น."
                : "จ่ายเงินไม่สำเร็จ", Danger);
        }
        catch (Exception e)
        {
            Toast("จ่ายเงินไม่สำเร็จ ลองใหม่", Danger);
            Debug.LogWarning($"[Shop] {e.Message}");
        }
        busy = false;
        Render(last);
    }

    // ── Cart ────────────────────────────────────────────────────────────────────
    private WalletClient.Market MarketOf(WalletClient.Snapshot snapshot)
    {
        if (snapshot?.markets != null)
            foreach (var m in snapshot.markets) if (m.id == shopId) return m;
        return null;
    }

    private int InCart(string id) => cart.TryGetValue(id, out int n) ? n : 0;

    private void ChangeQty(WalletClient.CatalogItem item, int delta)
    {
        int qty = Mathf.Clamp(InCart(item.id) + delta, 0, item.consumable ? 99 : 1);
        if (qty == 0) cart.Remove(item.id); else cart[item.id] = qty;
        Render(last);
    }

    private int CartTotal()
    {
        int total = 0;
        if (last?.catalog != null)
            foreach (var item in last.catalog) total += item.price * InCart(item.id);
        return total;
    }

    // ── Rendering ───────────────────────────────────────────────────────────────
    private void Render(WalletClient.Snapshot snapshot)
    {
        last = snapshot;
        var categories = Categories();
        if (tab == null || (tab != SellTab && !categories.Exists(c => c.Name == tab))) tab = categories[0].Name;

        titleText.text = shopId == "community" ? "ร้านค้าชุมชน"
            : shopId == "market" ? "ตลาดสดเทศบาล"
            : shopId == "flea" ? "ตลาดนัด"
            : shopId == "city_food" ? "ร้านอาหารในเมือง"
            : shopId == "music" ? "ร้านเครื่องดนตรี"
            : "ค่ายมวย · โต๊ะรับสมัคร";
        var market = MarketOf(snapshot);
        subtitleText.text = market != null ? $"{(market.open ? "เปิดอยู่" : "ปิดอยู่")} · เปิด {market.hours} น."
            : shopId == "community" ? "หมู่บ้าน" : "ตัวเมือง";
        balanceText.text = snapshot != null ? $"{snapshot.baht:N0} บาท" : "ออฟไลน์";

        RenderTabs(categories);
        UiKit.Clear(grid);
        if (tab == SellTab) RenderSell(snapshot);
        else RenderItems(snapshot, categories.Find(c => c.Name == tab));
        RenderCart(snapshot);
    }

    private void RenderTabs(List<Category> categories)
    {
        UiKit.Clear(tabs);
        float y = 330f;
        foreach (var c in categories)
        {
            var category = c;
            bool selected = tab == c.Name;
            UiKit.RoundButton(tabs, c.Name, new Vector2(0f, y), new Vector2(170f, 96f),
                selected ? Gold : new Color(1f, 1f, 1f, 0.04f), selected ? (Color)new Color32(0x41, 0x24, 0x02, 0xFF) : TabText,
                () => { tab = category.Name; Render(last); }, 30);
            y -= 112f;
        }
        if (HasSellTab)
        {
            bool selected = tab == SellTab;
            var sell = UiKit.RoundButton(tabs, "ขายของ", new Vector2(0f, -300f), new Vector2(170f, 96f),
                selected ? (Color)new Color32(0x63, 0x99, 0x22, 0xFF) : new Color(0.39f, 0.6f, 0.13f, 0.18f),
                selected ? Color.white : (Color)new Color32(0x97, 0xC4, 0x59, 0xFF), () => { tab = SellTab; Render(last); }, 30);
            sell.gameObject.AddComponent<Outline>().effectColor = new Color32(0x63, 0x99, 0x22, 0xFF);
        }
    }

    private void RenderItems(WalletClient.Snapshot snapshot, Category category)
    {
        if (snapshot?.catalog == null) return;
        int index = 0;
        foreach (var item in snapshot.catalog)
        {
            if (item.shop != shopId || category == null || !category.Has(item.id)) continue;
            AddCard(item, index++, snapshot.baht, category);
        }
    }

    private void AddCard(WalletClient.CatalogItem item, int index, int baht, Category category)
    {
        float x = index % 2 == 0 ? -260f : 260f;
        float y = 255f - (index / 2) * 170f;
        int qty = InCart(item.id);

        var card = UiKit.Panel("Card", grid, qty > 0 ? (Color)new Color32(0xFF, 0xF6, 0xDF, 0xFF) : Color.white, new Vector2(x, y), new Vector2(500f, 156f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = qty > 0 ? Gold : CardBorder;

        // Icon tile with the item's initial letter in the category colour
        var tile = UiKit.Panel("Icon", card, category.Tile, new Vector2(-185f, 0f), new Vector2(104f, 104f));
        var initial = UiKit.Label(tile, 56, Initial(item.name), Vector2.zero, new Vector2(100f, 100f), category.TileInk);
        initial.fontStyle = FontStyle.Bold;

        var name = UiKit.Label(card, 30, item.name, new Vector2(-10f, 46f), new Vector2(250f, 44f), Ink, TextAnchor.MiddleLeft);
        name.fontStyle = FontStyle.Bold;
        var price = UiKit.Label(card, 32, $"{item.price:N0} ฿", new Vector2(185f, 46f), new Vector2(120f, 44f), Price, TextAnchor.MiddleRight);
        price.fontStyle = FontStyle.Bold;

        // Benefit badge (green pill) and what you already have
        var badge = UiKit.Panel("Badge", card, SellTint, new Vector2(30f, 4f), new Vector2(330f, 40f));
        UiKit.Label(badge, 22, item.desc, Vector2.zero, new Vector2(316f, 38f), SellGreen);
        UiKit.Label(card, 22, item.have ?? "", new Vector2(-30f, -46f), new Vector2(250f, 36f), Muted, TextAnchor.MiddleLeft);

        if (item.owned)
        {
            UiKit.RoundButton(card, "มีแล้ว", new Vector2(170f, -44f), new Vector2(140f, 56f), new Color32(0xC0, 0xDD, 0x97, 0xFF), SellGreen, () => { }, 26)
                .interactable = false;
        }
        else if (!item.consumable)
        {
            UiKit.RoundButton(card, qty > 0 ? "ในตะกร้า" : "ใส่ตะกร้า", new Vector2(170f, -44f), new Vector2(140f, 56f),
                qty > 0 ? Gold : (Color)new Color32(0xFA, 0xEE, 0xDA, 0xFF), Ink, () => ChangeQty(item, qty > 0 ? -1 : 1), 26);
        }
        else
        {
            var minus = UiKit.RoundButton(card, "-", new Vector2(120f, -44f), new Vector2(54f, 54f), Color.white, Danger, () => ChangeQty(item, -1), 34, circle: true);
            minus.gameObject.AddComponent<Outline>().effectColor = CardBorder;
            minus.interactable = qty > 0;
            var count = UiKit.Label(card, 32, qty.ToString(), new Vector2(172f, -44f), new Vector2(50f, 54f), Ink);
            count.fontStyle = FontStyle.Bold;
            UiKit.RoundButton(card, "+", new Vector2(224f, -44f), new Vector2(54f, 54f), Gold, new Color32(0x41, 0x24, 0x02, 0xFF), () => ChangeQty(item, 1), 34, circle: true);
        }
    }

    private void RenderSell(WalletClient.Snapshot snapshot)
    {
        UiKit.Label(grid, 30, "ร้านรับซื้อผลผลิตของคุณ", new Vector2(0f, 320f), new Vector2(1000f, 50f), Ink).fontStyle = FontStyle.Bold;
        if (snapshot?.fish == null) return;

        int row = 0;
        foreach (var f in snapshot.fish)
        {
            float y = 245f - row * 92f;
            var line = UiKit.Panel("Produce", grid, f.count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.55f), new Vector2(0f, y), new Vector2(1000f, 80f), shadow: f.count > 0);
            line.gameObject.AddComponent<Outline>().effectColor = CardBorder;
            UiKit.Label(line, 28, f.name, new Vector2(-260f, 0f), new Vector2(440f, 60f), f.count > 0 ? Ink : Muted, TextAnchor.MiddleLeft);
            UiKit.Label(line, 26, $"{f.count} × {f.price} ฿", new Vector2(120f, 0f), new Vector2(260f, 60f), Muted);
            var sub = UiKit.Label(line, 30, f.count > 0 ? $"{f.count * f.price:N0} ฿" : "-", new Vector2(390f, 0f), new Vector2(180f, 60f), f.count > 0 ? SellGreen : Muted, TextAnchor.MiddleRight);
            sub.fontStyle = FontStyle.Bold;
            row++;
        }
    }

    private void RenderCart(WalletClient.Snapshot snapshot)
    {
        UiKit.Clear(cartList);
        bool selling = tab == SellTab;
        int baht = snapshot?.baht ?? 0;

        if (selling)
        {
            int value = snapshot?.total_value ?? 0;
            cartTitle.text = "ขายของ";
            haveText.text = $"เงินที่มี   {baht:N0}";
            afterText.text = $"หลังขาย   {baht + value:N0}";
            totalLabel.text = "จะได้รับ";
            totalText.text = $"{value:N0} ฿";
            totalText.color = SellGreen;
            payLabel.text = "ขายทั้งหมด";
            payButton.onClick.RemoveAllListeners();
            payButton.onClick.AddListener(SellAll);
            payButton.interactable = !busy && value > 0;
            clearButton.gameObject.SetActive(false);
            UiKit.Label(cartList, 24, "ขายปลาในข้องและข้าวในยุ้ง\nทั้งหมดในครั้งเดียว", new Vector2(0f, -40f), new Vector2(340f, 90f), Muted);
            return;
        }

        int count = 0;
        float y = 0f;
        if (snapshot?.catalog != null)
        {
            foreach (var item in snapshot.catalog)
            {
                int n = InCart(item.id);
                if (n == 0) continue;
                count += n;
                UiKit.Label(cartList, 24, n > 1 ? $"{item.name} ×{n}" : item.name, new Vector2(-50f, y), new Vector2(250f, 40f), Ink, TextAnchor.MiddleLeft);
                UiKit.Label(cartList, 24, $"{item.price * n:N0}", new Vector2(130f, y), new Vector2(100f, 40f), Muted, TextAnchor.MiddleRight);
                var divider = UiKit.Rect("Divider", cartList, CardBorder);
                divider.anchoredPosition = new Vector2(0f, y - 22f);
                divider.sizeDelta = new Vector2(330f, 2f);
                y -= 46f;
            }
        }
        if (count == 0)
            UiKit.Label(cartList, 24, "ยังว่างอยู่\nกด + ที่สินค้าเพื่อเลือก", new Vector2(0f, -40f), new Vector2(340f, 90f), Muted);

        int total = CartTotal();
        bool affordable = total <= baht;
        cartTitle.text = count > 0 ? $"ตะกร้า ({count})" : "ตะกร้า";
        haveText.text = $"เงินที่มี   {baht:N0}";
        afterText.text = affordable ? $"เหลือหลังจ่าย   {baht - total:N0}" : $"ขาดอีก   {total - baht:N0}";
        afterText.color = affordable ? Muted : Danger;
        totalLabel.text = "รวม";
        totalText.text = $"{total:N0} ฿";
        totalText.color = Price;
        payLabel.text = "จ่ายเงิน";
        payButton.onClick.RemoveAllListeners();
        payButton.onClick.AddListener(Checkout);
        payButton.interactable = !busy && count > 0 && affordable && snapshot != null;
        clearButton.gameObject.SetActive(count > 0);
    }

    // First Thai consonant (skip leading vowels เ แ โ ใ ไ) for the icon tile
    private static string Initial(string name)
    {
        foreach (char ch in name)
            if ("เแโใไ".IndexOf(ch) < 0 && !char.IsWhiteSpace(ch)) return ch.ToString();
        return name.Length > 0 ? name.Substring(0, 1) : "?";
    }

    private void Toast(string message, Color color)
    {
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message, color));
    }

    // Pop in, hold, fade out
    private IEnumerator ToastRoutine(string message, Color color)
    {
        var panel = (RectTransform)toastText.transform.parent;
        toastText.text = message;
        panel.GetComponent<Image>().color = color;
        panel.gameObject.SetActive(true);
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            panel.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.05f, t / 0.15f);
            yield return null;
        }
        panel.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(2.2f);
        panel.gameObject.SetActive(false);
    }

    // ── Construction ────────────────────────────────────────────────────────────
    private void Build()
    {
        var card = UiKit.Window(transform, "ShopCanvas", new Vector2(1640f, 940f), 50, out backdrop);
        var cardImage = card.GetComponent<Image>();
        cardImage.sprite = UiKit.RoundedSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = TeakDark;

        // Header: teak bar with gold underline
        var header = UiKit.Panel("Header", card, Teak, new Vector2(0f, 412f), new Vector2(1640f, 116f));
        var underline = UiKit.Rect("GoldLine", card, Gold);
        underline.anchoredPosition = new Vector2(0f, 353f);
        underline.sizeDelta = new Vector2(1640f, 4f);

        var logo = UiKit.Panel("Logo", header, Gold, new Vector2(-750f, 0f), new Vector2(76f, 76f));
        UiKit.Label(logo, 40, "ร", Vector2.zero, new Vector2(70f, 70f), new Color32(0x41, 0x24, 0x02, 0xFF)).fontStyle = FontStyle.Bold;
        titleText = UiKit.Label(header, 40, "", new Vector2(-470f, 14f), new Vector2(460f, 54f), new Color32(0xFA, 0xEE, 0xDA, 0xFF), TextAnchor.MiddleLeft);
        titleText.fontStyle = FontStyle.Bold;
        subtitleText = UiKit.Label(header, 24, "", new Vector2(-470f, -26f), new Vector2(460f, 34f), new Color32(0xC9, 0xA9, 0x7A, 0xFF), TextAnchor.MiddleLeft);

        var coin = UiKit.Panel("Balance", header, new Color32(0x1E, 0x12, 0x0A, 0xFF), new Vector2(470f, 0f), new Vector2(330f, 70f));
        coin.gameObject.AddComponent<Outline>().effectColor = Gold;
        var coinIcon = UiKit.Rect("Coin", coin, Gold);
        coinIcon.GetComponent<Image>().sprite = UiKit.CircleSprite;
        coinIcon.anchoredPosition = new Vector2(-120f, 0f);
        coinIcon.sizeDelta = new Vector2(44f, 44f);
        UiKit.Label(coinIcon, 26, "฿", Vector2.zero, new Vector2(40f, 40f), new Color32(0x41, 0x24, 0x02, 0xFF)).fontStyle = FontStyle.Bold;
        balanceText = UiKit.Label(coin, 32, "", new Vector2(30f, 0f), new Vector2(240f, 60f), GoldText);
        balanceText.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(header, "X", new Vector2(740f, 0f), new Vector2(70f, 70f), new Color32(0x5A, 0x3A, 0x26, 0xFF), new Color32(0xFA, 0xEE, 0xDA, 0xFF), Hide, 32, circle: true);

        // Body: tabs | items | cart
        tabs = UiKit.Container(card, new Vector2(-720f, -40f));

        var center = UiKit.Panel("Items", card, Cream, new Vector2(-100f, -60f), new Vector2(1040f, 800f));
        grid = UiKit.Container(center, Vector2.zero);

        var cartPanel = UiKit.Panel("Cart", card, new Color32(0xFF, 0xFD, 0xF8, 0xFF), new Vector2(630f, -60f), new Vector2(360f, 800f));
        cartTitle = UiKit.Label(cartPanel, 32, "ตะกร้า", new Vector2(0f, 360f), new Vector2(320f, 50f), Ink, TextAnchor.MiddleLeft);
        cartTitle.fontStyle = FontStyle.Bold;
        cartList = UiKit.Container(cartPanel, new Vector2(0f, 290f));

        cartFooter = UiKit.Container(cartPanel, new Vector2(0f, -200f));
        haveText = UiKit.Label(cartFooter, 24, "", new Vector2(0f, 60f), new Vector2(320f, 34f), Muted, TextAnchor.MiddleRight);
        afterText = UiKit.Label(cartFooter, 24, "", new Vector2(0f, 26f), new Vector2(320f, 34f), Muted, TextAnchor.MiddleRight);
        totalLabel = UiKit.Label(cartFooter, 30, "รวม", new Vector2(-90f, -26f), new Vector2(140f, 56f), Ink, TextAnchor.MiddleLeft);
        totalLabel.fontStyle = FontStyle.Bold;
        totalText = UiKit.Label(cartFooter, 46, "0 ฿", new Vector2(60f, -26f), new Vector2(200f, 60f), Price, TextAnchor.MiddleRight);
        totalText.fontStyle = FontStyle.Bold;
        payButton = UiKit.RoundButton(cartFooter, "จ่ายเงิน", new Vector2(0f, -100f), new Vector2(320f, 90f), PayGreen, Color.white, Checkout, 36);
        payLabel = payButton.GetComponentInChildren<Text>();
        clearButton = UiKit.RoundButton(cartFooter, "ล้างตะกร้า", new Vector2(0f, -170f), new Vector2(200f, 48f), new Color(1f, 1f, 1f, 0f), Danger,
            () => { cart.Clear(); Render(last); }, 24);

        // Toast: pops over the item area
        var toast = UiKit.Panel("Toast", card, PayGreen, new Vector2(-100f, -410f), new Vector2(760f, 70f), shadow: true);
        toastText = UiKit.Label(toast, 28, "", Vector2.zero, new Vector2(740f, 64f), Color.white);
        toastText.fontStyle = FontStyle.Bold;
        toast.gameObject.SetActive(false);
    }
}
