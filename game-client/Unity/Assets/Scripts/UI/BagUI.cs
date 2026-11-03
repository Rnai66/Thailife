using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// The bag (กระเป๋า), opened from the HUD, laid out like a survival game's inventory:
//  · the inventory: your money, one tab per group from bag.lua and a scrolling grid of item slots;
//    tap a slot to see it, then use it — eat food, take up a rod or an instrument, wear an outfit
//  · the character: name, level and XP, a live 3D model (LookPreview) between the gear slots —
//    rod, instrument and vehicle on the left, hair, top, trousers and outfit on the right
// Landscape puts the two side by side; upright stacks the character over the inventory.
public class BagUI : MonoBehaviour
{
    [Serializable] public class Item { public string id, name, unit, note, action; public int count; }
    [Serializable] public class Section { public string title; public Item[] items; }
    [Serializable] public class Equipped { public string rod, rod_name, instrument, instrument_name, vehicle, vehicle_name, outfit_name; public int outfit; }
    [Serializable] public class Contents { public Section[] sections; public Equipped equipped; }
    [Serializable] private class UseRequest { public string id; }

    // A gear slot on the character: what it shows and where tapping it leads
    private class Gear
    {
        public string Title, Name, Note, Art, Glyph, Tab;
        public Color Tint;
        public bool Empty;
    }

    private static readonly Color Slate = new Color32(0x25, 0x29, 0x27, 0xFF);
    private static readonly Color SlateLight = new Color32(0x33, 0x39, 0x36, 0xFF);
    private static readonly Color SlotFill = new Color32(0x3A, 0x41, 0x3D, 0xFF);
    private static readonly Color SlotEdge = new Color32(0x55, 0x5E, 0x59, 0xFF);
    private static readonly Color Picked = new Color32(0x4F, 0xC3, 0xB4, 0xFF);
    private static readonly Color Faint = new Color(1f, 1f, 1f, 0.18f);

    private static BagUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, detailName, detailNote, toastText, moneyText, nameText, levelText, energyText;
    private RectTransform tabs, viewport, grid, gearLeft, gearRight, xpFill;
    private ScrollRect scroll;
    private RawImage view;
    private LookPreview preview;
    private Button actionButton;
    private Text actionLabel;
    private Coroutine toast;
    private Contents contents;
    private int tab;
    private Item picked;
    private Gear pickedGear;
    private bool busy, loading;

    private static bool Upright => UiKit.Portrait;
    private static int Columns => Upright ? 5 : 6;
    private const float Cell = 132f;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.Close(); return; }
        if (instance == null) instance = new GameObject("BagUI").AddComponent<BagUI>();
        instance.Open();
    }

    private void Open()
    {
        picked = null;
        pickedGear = null;
        backdrop.SetActive(true);
        if (preview == null)
        {
            preview = LookPreview.Create();
            preview.transform.position = new Vector3(60f, -500f, 0f); // its own stage, away from the shops' preview
        }
        view.texture = preview.Texture;
        var profile = ProfileClient.Instance;
        if (profile != null) { preview.Show(profile.Look); preview.Front(); }
        Render();
        Load();
        RefreshMoney();
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (preview != null) { Destroy(preview.gameObject); preview = null; }
    }

    private void Awake()
    {
        bool upright = Upright;
        var body = UiKit.PremiumWindow(transform, "BagCanvas", UiKit.WindowSize(new Vector2(1700f, 960f), 1560f), 55, "icon:shop",
            out backdrop, out titleText, out subtitleText, Close, out _);
        body.GetComponent<Image>().color = Slate;
        titleText.text = "กระเป๋า";
        subtitleText.text = "ของทุกอย่างที่คุณมี · แตะช่องเพื่อดูและใช้";

        // ── Character ─────────────────────────────────────────────────────────────────────
        Vector2 charCentre = upright ? new Vector2(0f, 400f) : new Vector2(430f, 0f);
        var card = UiKit.Panel("Character", body, SlateLight, charCentre, upright ? new Vector2(700f, 590f) : new Vector2(760f, 780f));
        card.gameObject.AddComponent<Outline>().effectColor = SlotEdge;
        float top = upright ? 270f : 360f;
        nameText = UiKit.Cartoon(UiKit.Label(card, 34, "", new Vector2(0f, top), new Vector2(600f, 46f), UiKit.WoodInk), 2.5f);
        levelText = UiKit.Label(card, 22, "", new Vector2(0f, top - 38f), new Vector2(600f, 30f), UiKit.HeaderSub);
        var xpTrack = UiKit.Panel("Xp", card, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, top - 64f), new Vector2(460f, 12f));
        xpFill = UiKit.Rect("Fill", xpTrack, new Color(0.4f, 0.85f, 1f));
        xpFill.anchorMin = Vector2.zero;
        xpFill.anchorMax = new Vector2(0f, 1f);
        xpFill.offsetMin = new Vector2(2f, 2f);
        xpFill.offsetMax = new Vector2(-2f, -2f);

        var stage = UiKit.Panel("Stage", card, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, upright ? -20f : -10f), upright ? new Vector2(300f, 422f) : new Vector2(380f, 534f));
        stage.gameObject.AddComponent<Mask>();
        view = new GameObject("View", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        view.rectTransform.SetParent(stage, false);
        view.rectTransform.sizeDelta = stage.sizeDelta;
        view.raycastTarget = false;

        float side = upright ? 260f : 290f;
        gearLeft = UiKit.Container(card, new Vector2(-side, upright ? 160f : 230f));
        gearRight = UiKit.Container(card, new Vector2(side, upright ? 160f : 230f));

        var energy = UiKit.Panel("Energy", card, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, upright ? -258f : -350f), new Vector2(420f, 48f));
        var bolt = UiKit.Rect("Bolt", energy, new Color(1f, 0.8f, 0.2f));
        bolt.GetComponent<Image>().sprite = HudIcons.Get("bolt");
        bolt.anchoredPosition = new Vector2(-180f, 0f);
        bolt.sizeDelta = new Vector2(34f, 34f);
        energyText = UiKit.Label(energy, 24, "", new Vector2(20f, 0f), new Vector2(340f, 40f), UiKit.WoodInk);

        // ── Inventory ─────────────────────────────────────────────────────────────────────
        float invX = upright ? 0f : -400f, invW = upright ? 700f : 820f;
        float headY = upright ? 60f : 365f;
        UiKit.Cartoon(UiKit.Label(body, 30, "ช่องเก็บของ", new Vector2(invX - invW * 0.5f + 150f, headY), new Vector2(300f, 44f), UiKit.WoodInk, TextAnchor.MiddleLeft), 2f);
        var money = UiKit.Panel("Money", body, new Color(0f, 0f, 0f, 0.35f), new Vector2(invX + invW * 0.5f - 160f, headY), new Vector2(320f, 52f));
        var coin = UiKit.Rect("Coin", money, UiKit.Gold);
        coin.GetComponent<Image>().sprite = HudIcons.Get("coin");
        coin.anchoredPosition = new Vector2(-128f, 0f);
        coin.sizeDelta = new Vector2(38f, 38f);
        moneyText = UiKit.Label(money, 26, "—", new Vector2(22f, 0f), new Vector2(250f, 44f), UiKit.GoldText, TextAnchor.MiddleRight);
        moneyText.fontStyle = FontStyle.Bold;

        tabs = UiKit.Container(body, new Vector2(invX, upright ? 0f : 305f));

        Vector2 gridCentre = upright ? new Vector2(0f, -335f) : new Vector2(invX, -10f);
        Vector2 gridSize = upright ? new Vector2(700f, 450f) : new Vector2(820f, 560f);
        viewport = UiKit.Rect("Viewport", body, new Color(0f, 0f, 0f, 0.2f));
        viewport.GetComponent<Image>().sprite = UiKit.RoundedSprite;
        viewport.GetComponent<Image>().type = Image.Type.Sliced;
        viewport.anchoredPosition = gridCentre;
        viewport.sizeDelta = gridSize;
        viewport.gameObject.AddComponent<RectMask2D>();
        grid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(viewport, false);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.sizeDelta = new Vector2(gridSize.x, gridSize.y);
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Detail bar under the grid: the picked item's name and note, and what you can do with it
        float detailY = upright ? -628f : -350f, detailH = upright ? 130f : 90f;
        var detail = UiKit.Panel("Detail", body, SlateLight, new Vector2(invX, detailY), new Vector2(invW, detailH));
        detail.gameObject.AddComponent<Outline>().effectColor = SlotEdge;
        float textW = invW - 280f, textX = -invW * 0.5f + 24f + textW * 0.5f;
        detailName = UiKit.Label(detail, 28, "", new Vector2(textX, upright ? 26f : 18f), new Vector2(textW, 40f), UiKit.WoodInk, TextAnchor.MiddleLeft);
        detailName.fontStyle = FontStyle.Bold;
        detailNote = UiKit.Label(detail, 21, "", new Vector2(textX, upright ? -24f : -20f), new Vector2(textW, upright ? 56f : 32f), UiKit.HeaderSub, TextAnchor.MiddleLeft);
        actionButton = UiKit.RoundButton(detail, "", new Vector2(invW * 0.5f - 130f, 0f), new Vector2(220f, 70f), UiKit.Gold, UiKit.GoldInk, Act, 28);
        actionLabel = actionButton.GetComponentInChildren<Text>();
        toastText = UiKit.ToastBar(body, new Vector2(invX, upright ? -520f : -250f), 760f);

        backdrop.SetActive(false);

        if (WalletClient.Instance != null) WalletClient.Instance.Changed += ShowMoney;
        if (ProfileClient.Instance != null) ProfileClient.Instance.LookChanged += OnLookChanged;
    }

    private void OnDestroy()
    {
        if (WalletClient.Instance != null) WalletClient.Instance.Changed -= ShowMoney;
        if (ProfileClient.Instance != null) ProfileClient.Instance.LookChanged -= OnLookChanged;
        if (preview != null) Destroy(preview.gameObject);
        if (instance == this) instance = null;
    }

    private void ShowMoney(WalletClient.Snapshot wallet) => moneyText.text = wallet == null ? "—" : $"{wallet.baht:N0} บาท";

    private void OnLookChanged(Look look)
    {
        if (preview != null) { preview.Show(look); preview.Front(); }
        if (backdrop.activeSelf) Render();
    }

    private async void RefreshMoney()
    {
        var wallet = WalletClient.Instance;
        ShowMoney(wallet?.Current);
        if (wallet == null) return;
        try { await wallet.RefreshAsync(); } catch (Exception) { }
    }

    private async void Load()
    {
        var conn = FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected || loading) return;
        loading = true;
        try
        {
            var result = await conn.Client.RpcAsync(conn.Session, "bag_get", "{}");
            Apply(result.Payload);
        }
        catch (Exception e) { Debug.LogWarning($"[Bag] {e.Message}"); }
        finally { loading = false; }
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    private void Apply(string payload)
    {
        contents = JsonUtility.FromJson<Contents>(payload);
        if (picked != null) picked = Find(picked.id);
    }

    private Item Find(string id)
    {
        if (contents?.sections == null) return null;
        foreach (var s in contents.sections)
            if (s.items != null) foreach (var i in s.items) if (i.id == id) return i;
        return null;
    }

    // The artwork for an item: most ids match an icon directly, fish/rice/prizes use the shop's sell icons
    private static string IconOf(string id)
    {
        string[] direct = { "food_", "furn_", "inst_", "rod_", "seed_" };
        foreach (var p in direct) if (id.StartsWith(p) && UiKit.Art("Icons/" + id) != null) return id;
        if (id.StartsWith("fish_")) return "sell_" + id.Substring(5);
        if (id.StartsWith("rice_")) return "sell_" + id;
        if (id.StartsWith("goods_")) return "sell_" + id;
        switch (id)
        {
            case "chicken_feed": return "chicken_feed";
            case "chickens": case "eggs": return "chicken_hen";
            case "cows": case "cow_feed": case "milk": return "farm";
            case "coupons": case "fair_points": return "fair";
            case "asset_car_bicycle": return "goods_bicycle";
        }
        if (id.StartsWith("asset_land")) return "koknongna";
        if (id.StartsWith("crop_")) return "farm";
        return null;
    }

    // Items with no artwork get a pictogram instead
    private static string GlyphOf(string id) =>
        id.StartsWith("outfit_") ? "shirt" : id.StartsWith("rod_") ? "rod" : id.StartsWith("inst_") ? "note" : id.StartsWith("asset_car") ? "car" : null;

    // ── Drawing ───────────────────────────────────────────────────────────────────────────
    private void Render()
    {
        RenderCharacter();
        RenderInventory();
        ShowDetail();
    }

    private void RenderCharacter()
    {
        var profile = ProfileClient.Instance;
        string name = profile != null ? profile.DisplayName : null;
        nameText.text = string.IsNullOrWhiteSpace(name) ? "ตัวละครของคุณ" : name;
        var stats = CareerClient.Instance?.Current;
        if (stats != null)
        {
            bool maxed = stats.xp_to_next <= 0;
            levelText.text = $"Lv {stats.level} {CareerClient.TitleOf(stats)}" + (maxed ? " · MAX" : $" · XP {stats.xp:N0}/{stats.xp_to_next:N0}");
            xpFill.anchorMax = new Vector2(maxed ? 1f : Mathf.Clamp01((float)stats.xp / stats.xp_to_next), 1f);
        }
        else
        {
            levelText.text = "ออฟไลน์";
            xpFill.anchorMax = new Vector2(0f, 1f);
        }
        var life = LifeClient.Instance;
        energyText.text = life != null && life.IsOnline ? $"พลังงาน {life.Energy:0}/{life.Current.max_energy}" : "พลังงาน —";

        UiKit.Clear(gearLeft);
        UiKit.Clear(gearRight);
        var look = profile != null ? profile.Look : default;
        var e = contents?.equipped;
        var leftSlots = new[]
        {
            new Gear { Title = "คันเบ็ด", Name = e?.rod_name, Art = e != null ? "rod_" + e.rod : null, Glyph = "rod", Tab = "อุปกรณ์และเฟอร์นิเจอร์",
                       Note = "คันที่ใช้ตกปลาอยู่ · เปลี่ยนคันได้ในหมวดอุปกรณ์", Empty = string.IsNullOrEmpty(e?.rod_name) },
            new Gear { Title = "เครื่องดนตรี", Name = e?.instrument_name, Art = e != null ? "inst_" + e.instrument : null, Glyph = "note", Tab = "อุปกรณ์และเฟอร์นิเจอร์",
                       Note = "ชิ้นที่ใช้เล่นดนตรีอยู่ · เปลี่ยนได้ในหมวดอุปกรณ์", Empty = string.IsNullOrEmpty(e?.instrument_name) },
            new Gear { Title = "พาหนะ", Name = e?.vehicle_name, Art = e?.vehicle == "car_bicycle" ? "goods_bicycle" : null, Glyph = "car", Tab = "ทรัพย์สิน",
                       Note = string.IsNullOrEmpty(e?.vehicle_name) ? "ยังไม่มีรถ · ซื้อได้ที่ธนาคารในเมือง" : "คันที่เร็วที่สุดของคุณ", Empty = string.IsNullOrEmpty(e?.vehicle_name) },
        };
        var top = Look.ClothColors[Mathf.Clamp(look.Top, 0, Look.ClothColors.Length - 1)];
        var bottom = Look.ClothColors[Mathf.Clamp(look.Bottom, 0, Look.ClothColors.Length - 1)];
        var outfit = Look.Outfits[Mathf.Clamp(look.Outfit, 0, Look.Outfits.Length - 1)];
        var rightSlots = new[]
        {
            new Gear { Title = "ทรงผม", Name = $"{Look.HairStyles[Mathf.Clamp(look.Hair, 0, Look.HairStyles.Length - 1)].Name} · {Look.HairColors[Mathf.Clamp(look.HairColor, 0, Look.HairColors.Length - 1)].Name}",
                       Glyph = "person", Tint = Look.HairColors[Mathf.Clamp(look.HairColor, 0, Look.HairColors.Length - 1)].Color, Note = "เปลี่ยนทรงผมได้ที่ร้านตัดผมในเมือง" },
            new Gear { Title = "เสื้อ", Name = "เสื้อสี" + top.Name, Glyph = "shirt", Tint = top.Color, Note = "เปลี่ยนสีเสื้อได้ที่ร้านเสื้อผ้าในเมือง" },
            new Gear { Title = "กางเกง", Name = "กางเกงสี" + bottom.Name, Glyph = "pants", Tint = bottom.Color, Note = "เปลี่ยนสีกางเกงได้ที่ร้านเสื้อผ้าในเมือง" },
            new Gear { Title = "ชุด", Name = look.Outfit > 0 ? outfit.Name : "ไม่ได้ใส่ชุด", Glyph = "shirt", Tint = outfit.Color, Empty = look.Outfit == 0, Tab = "เสื้อผ้า",
                       Note = "ชุดที่ซื้อแล้วสวมใส่ได้ฟรีจากหมวดเสื้อผ้า · ซื้อชุดใหม่ได้ที่ร้านเสื้อผ้า" },
        };
        float step = Upright ? 128f : 150f, size = Upright ? 100f : 116f;
        for (int i = 0; i < leftSlots.Length; i++) GearSlot(gearLeft, leftSlots[i], new Vector2(0f, -i * step), size);
        for (int i = 0; i < rightSlots.Length; i++) GearSlot(gearRight, rightSlots[i], new Vector2(0f, -i * step), size);
    }

    private void GearSlot(RectTransform parent, Gear gear, Vector2 pos, float size)
    {
        bool on = pickedGear != null && pickedGear.Title == gear.Title;
        var slot = UiKit.Panel("Gear", parent, SlotFill, pos, new Vector2(size, size));
        var edge = slot.gameObject.AddComponent<Outline>();
        edge.effectColor = on ? Picked : SlotEdge;
        edge.effectDistance = on ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
        if (!gear.Empty && gear.Art != null && UiKit.Art("Icons/" + gear.Art) != null)
            UiKit.IconTile(slot, gear.Art, Vector2.zero, new Vector2(size - 18f, size - 18f));
        else
        {
            var g = UiKit.Rect("Glyph", slot, gear.Empty ? Faint : gear.Tint.a > 0f ? gear.Tint : UiKit.WoodInk);
            g.GetComponent<Image>().sprite = HudIcons.Get(gear.Glyph);
            g.sizeDelta = new Vector2(size * 0.6f, size * 0.6f);
            if (!gear.Empty) g.gameObject.AddComponent<Outline>().effectColor = gear.Tint.a > 0f ? new Color(1f, 1f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.5f); // dark hair still shows
        }
        UiKit.Label(parent, 18, gear.Title, pos + new Vector2(0f, -size * 0.5f - 12f), new Vector2(size + 30f, 24f), UiKit.HeaderSub);
        slot.gameObject.AddComponent<Button>().onClick.AddListener(() => { pickedGear = gear; picked = null; Render(); });
        foreach (var gr in slot.GetComponentsInChildren<Graphic>()) gr.raycastTarget = gr.gameObject == slot.gameObject;
    }

    private void RenderInventory()
    {
        UiKit.Clear(tabs);
        UiKit.Clear(grid);
        var sections = contents?.sections;
        if (sections == null || sections.Length == 0)
        {
            var empty = UiKit.Label(grid, 28, loading || contents == null ? "กำลังเปิดกระเป๋า…" : "กระเป๋าว่างเปล่า", new Vector2(0f, -80f), new Vector2(600f, 50f), UiKit.HeaderSub);
            TopAnchor(empty.rectTransform);
            return;
        }
        tab = Mathf.Clamp(tab, 0, sections.Length - 1);

        // Tabs: one per group (two rows when upright)
        float invW = Upright ? 700f : 820f;
        int perRow = Upright ? (sections.Length + 1) / 2 : sections.Length;
        float tabWidth = Mathf.Min(220f, invW / perRow - 8f);
        for (int i = 0; i < sections.Length; i++)
        {
            int index = i, row = i / perRow, col = i % perRow;
            int inRow = Mathf.Min(perRow, sections.Length - row * perRow);
            var pos = new Vector2((col - (inRow - 1) * 0.5f) * (tabWidth + 8f), -row * 58f);
            bool on = i == tab;
            UiKit.RoundButton(tabs, sections[i].title, pos, new Vector2(tabWidth, 52f), on ? Picked : SlotFill, on ? UiKit.TeakDark : UiKit.WoodInk,
                () => { tab = index; picked = null; pickedGear = null; scroll.verticalNormalizedPosition = 1f; Render(); }, 20);
        }

        // The grid: square slots, icon and count, scrolling when a group is long
        var items = sections[tab].items ?? new Item[0];
        int rows = Mathf.Max(1, (items.Length + Columns - 1) / Columns);
        grid.sizeDelta = new Vector2(grid.sizeDelta.x, Mathf.Max(viewport.sizeDelta.y, rows * Cell + 16f));
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            int row = i / Columns, col = i % Columns;
            var pos = new Vector2((col - (Columns - 1) * 0.5f) * Cell, -12f - row * Cell - Cell * 0.5f);
            bool on = picked != null && picked.id == it.id;
            var slot = UiKit.Panel("Slot", grid, SlotFill, pos, new Vector2(Cell - 10f, Cell - 10f));
            TopAnchor(slot);
            var edge = slot.gameObject.AddComponent<Outline>();
            edge.effectColor = on ? Picked : SlotEdge;
            edge.effectDistance = on ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
            string icon = IconOf(it.id);
            string glyph = GlyphOf(it.id);
            if (icon != null && UiKit.Art("Icons/" + icon) != null)
                UiKit.IconTile(slot, icon, new Vector2(0f, 4f), new Vector2(Cell - 34f, Cell - 34f));
            else if (glyph != null)
            {
                var g = UiKit.Rect("Glyph", slot, it.id.StartsWith("outfit_") ? OutfitTint(it.id) : UiKit.WoodInk);
                g.GetComponent<Image>().sprite = HudIcons.Get(glyph);
                g.anchoredPosition = new Vector2(0f, 8f);
                g.sizeDelta = new Vector2(Cell * 0.52f, Cell * 0.52f);
                g.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.5f);
                UiKit.Label(slot, 16, it.name, new Vector2(0f, -Cell * 0.5f + 22f), new Vector2(Cell - 20f, 22f), UiKit.HeaderSub);
            }
            else
                UiKit.Label(slot, 22, it.name, Vector2.zero, new Vector2(Cell - 24f, Cell - 30f), UiKit.WoodInk);
            if (it.count > 1 || it.unit == "กก.")
                UiKit.Cartoon(UiKit.Label(slot, 22, it.count.ToString("N0"), new Vector2(Cell * 0.5f - 44f, -Cell * 0.5f + 24f), new Vector2(76f, 28f), UiKit.WoodInk, TextAnchor.MiddleRight), 2f);
            if (it.note == "ใช้อยู่" || it.note == "ใส่อยู่") // the one in use gets a small green tick-badge
                UiKit.PillLabel(slot, it.note, new Vector2(0f, Cell * 0.5f - 20f), new Vector2(84f, 26f), UiKit.PayGreen, Color.white, 16);
            slot.gameObject.AddComponent<Button>().onClick.AddListener(() => { picked = it; pickedGear = null; Render(); });
            foreach (var g in slot.GetComponentsInChildren<Graphic>()) g.raycastTarget = g.gameObject == slot.gameObject;
        }
    }

    private static Color OutfitTint(string id)
    {
        int o = int.TryParse(id.Substring("outfit_".Length), out int n) ? n : 0;
        var c = o > 0 && o < Look.Outfits.Length ? Look.Outfits[o].Color : UiKit.WoodInk;
        return c.a > 0f ? c : UiKit.WoodInk;
    }

    // Children of the scrolling grid hang from its top edge
    private static void TopAnchor(RectTransform rt)
    {
        var p = rt.anchoredPosition;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = p;
    }

    private int TabOf(string title)
    {
        var sections = contents?.sections;
        if (sections == null) return -1;
        for (int i = 0; i < sections.Length; i++) if (sections[i].title == title) return i;
        return -1;
    }

    private void ShowDetail()
    {
        if (pickedGear != null)
        {
            detailName.text = $"{pickedGear.Title}: {(pickedGear.Empty && pickedGear.Title != "ชุด" ? "ยังไม่มี" : pickedGear.Name)}";
            detailNote.text = pickedGear.Note;
            bool go = pickedGear.Tab != null && TabOf(pickedGear.Tab) >= 0;
            actionButton.gameObject.SetActive(go);
            actionLabel.text = pickedGear.Title == "ชุด" ? "เลือกชุด" : "ดูในกระเป๋า";
            actionButton.interactable = !busy;
            return;
        }
        if (picked == null)
        {
            detailName.text = "แตะช่องเพื่อดูรายละเอียด";
            detailNote.text = "ของกินกดกินได้ทันที · คันเบ็ด เครื่องดนตรี และชุด กดใช้ได้ที่นี่";
            actionButton.gameObject.SetActive(false);
            return;
        }
        detailName.text = $"{picked.name} · {picked.count:N0} {picked.unit}";
        detailNote.text = picked.note;
        string label = picked.action == "eat" ? "กิน" : picked.action == "equip" ? "ใช้" : picked.action == "wear" ? "สวมใส่" : null;
        actionButton.gameObject.SetActive(label != null);
        actionLabel.text = label ?? "";
        actionButton.interactable = !busy && picked.count > 0;
    }

    // ── Using things ──────────────────────────────────────────────────────────────────────
    private async void Act()
    {
        if (busy) return;
        if (pickedGear != null)
        {
            int t = TabOf(pickedGear.Tab);
            if (t >= 0) { tab = t; pickedGear = null; scroll.verticalNormalizedPosition = 1f; Render(); }
            return;
        }
        if (picked == null) return;
        var item = picked;
        busy = true;
        ShowDetail();
        try
        {
            switch (item.action)
            {
                case "eat":
                {
                    var life = LifeClient.Instance;
                    if (life == null) break;
                    var r = await life.EatAsync(item.id.Substring("food_".Length));
                    Notify($"กิน{r.ate} อร่อย! พลังงาน +{r.gained}", UiKit.PayGreen);
                    UiKit.FloatAbovePlayer($"+{r.gained} พลังงาน", new Color(1f, 0.85f, 0.3f));
                    break;
                }
                case "equip":
                {
                    var conn = FindAnyObjectByType<NakamaConnection>();
                    if (conn == null || !conn.IsConnected) break;
                    var result = await conn.Client.RpcAsync(conn.Session, "bag_use", JsonUtility.ToJson(new UseRequest { id = item.id }));
                    Apply(result.Payload);
                    Notify($"ใช้{item.name}แล้ว", UiKit.PayGreen);
                    break;
                }
                case "wear":
                {
                    var profile = ProfileClient.Instance;
                    if (profile == null) break;
                    var look = profile.Look;
                    look.Outfit = int.Parse(item.id.Substring("outfit_".Length));
                    await profile.SetLookAsync(look, "wardrobe");
                    Notify(look.Outfit > 0 ? $"ใส่{item.name}แล้ว" : "เปลี่ยนกลับเป็นเสื้อผ้าลำลองแล้ว", UiKit.PayGreen);
                    break;
                }
            }
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("already full") ? "อิ่มแล้ว พลังงานเต็ม"
                : e.Message.Contains("not owned") ? "ยังไม่มีของชิ้นนี้" : "ทำไม่ได้ ลองใหม่", UiKit.Danger);
        }
        catch (Exception)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
        }
        finally
        {
            busy = false;
            Load();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
