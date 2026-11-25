using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// จัดร้านกาแฟ (CafeSite, cafe.lua), built for the phone like แต่งบ้าน. Nothing moves until you choose
// it: tap a piece to pick it (gold ring) → ย้าย / หมุน / เก็บ (half the price back), or drag the picked
// piece with a finger. Touching a piece that isn't picked only picks it; the camera turns otherwise.
// "ซื้อของ" lists the café furniture; pick one and it appears in front of you → วางตรงนี้ buys it.
// "สไตล์ร้าน" picks the walls and floor (มินิมอล วินเทจ ลอฟท์ ไทยร่วมสมัย) and the name on the sign.
// No walking needed (EditTouchSurface): tap a piece to pick it, put a finger on it and drag it over the
// floor grid (BuildGrid, half-metre steps); letting go saves where it went. A new piece from ซื้อของ can
// be tapped into place or dragged too, then วางตรงนี้ buys it.
// "ถ่ายรูป" takes the screen without the buttons and opens the phone's share sheet (Facebook, LINE,
// Instagram, save to Photos…, Plugins/iOS/ThaiShare.mm).
// Every piece stays inside the room by its own size (a long counter turned sideways can't poke through
// a wall), and things that hang on a wall — pictures, the neon sign, shelves, the key rack — go flat
// against the nearest wall, facing into the room. The same window furnishes the hotel lobby (CafeSite
// kind "hotel"), opened from บริหารโรงแรม, and the shophouse on your city land (kind "shophouse").
public class CafeUI : MonoBehaviour
{
    private const float CarryDistance = 1.4f;
    private const float Cell = 0.5f;

    private static CafeUI instance;
    public static bool IsOpen => instance != null && instance.bar != null && instance.bar.activeSelf;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ThaiShare_Image(string path, string text, string receiver);
#endif

    private CafeSite site;
    private GameObject bar, browseRow, carryRow, ring;
    private Text infoText, toastText;
    private Button moveButton, rotateButton, storeButton, placeButton;
    private Coroutine toast;

    private GameObject listBackdrop, styleBackdrop;
    private RectTransform listGrid, styleBody;
    private InputField nameField;
    private string pickStyle;

    private GameObject carried;
    private string carriedItem;
    private int carriedId, carriedRot;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int selectedId;
    private bool busy;

    // touch editing
    private EditTouchSurface touch;
    private BuildGrid grid;
    private int pinnedId;        // picked with a finger: stays picked wherever you stand
    private bool fingerPlaced;   // the carried piece was put somewhere with a finger: it stays there
    private bool holding;        // a finger is on the carried piece
    private Vector3 holdOffset;
    private Vector3 carriedMin, carriedMax; // the carried piece's footprint, in its own space (unturned)

    private Text listTitle, listSubtitle, styleTitle, styleSubtitle;

    private CafeClient Client => site != null && site.Client != null ? site.Client : CafeClient.Instance;
    private bool Hotel => site != null && site.Kind == "hotel";
    private bool Shop => site != null && site.Kind == "shophouse";
    // The same words for the café, the hotel lobby and the shophouse
    private string Say(string cafe, string hotel, string shophouse) => Hotel ? hotel : Shop ? shophouse : cafe;

    // Hung on a wall rather than stood on the floor
    private static bool OnWall(string item) =>
        item == "art_frame" || item == "neon_sign" || item == "shelf_cups" || item == "bookshelf" || item == "key_rack";

    public static void Open(CafeSite site)
    {
        if (instance == null) instance = new GameObject("CafeUI").AddComponent<CafeUI>(); // the share sheet reports back to this name
        instance.site = site;
        var ui = instance;
        ui.listTitle.text = ui.Say("ของแต่งร้านกาแฟ", "ของแต่งล็อบบี้โรงแรม", "ของแต่งตึกแถว");
        ui.styleTitle.text = ui.Say("สไตล์และชื่อร้าน", "สไตล์และชื่อโรงแรม", "สไตล์และชื่อตึกแถว");
        ui.styleSubtitle.text = ui.Say("ผนัง พื้น และเฟอร์นิเจอร์ทั้งร้านเปลี่ยนตามสไตล์", "ผนัง พื้น และเฟอร์นิเจอร์ทั้งล็อบบี้เปลี่ยนตามสไตล์", "ผนัง พื้น และเฟอร์นิเจอร์ชั้นล่างเปลี่ยนตามสไตล์");
        instance.pinnedId = instance.selectedId = 0;
        instance.holding = false;
        instance.touch.gameObject.SetActive(true);
        instance.bar.SetActive(true);
        instance.ShowRows();
    }

    public static void Close()
    {
        if (instance == null || !instance.bar.activeSelf) return;
        instance.CancelCarry();
        instance.bar.SetActive(false);
        instance.listBackdrop.SetActive(false);
        instance.styleBackdrop.SetActive(false);
        instance.ring.SetActive(false);
        if (instance.grid != null) instance.grid.Hide();
        instance.pinnedId = 0;
        instance.holding = false;
        // the invisible touch layer goes too: left up it would sit over the whole screen, taking every
        // touch (the buttons under it, the camera) and picking up furniture after เสร็จ
        instance.touch.gameObject.SetActive(false);
    }

    private void Awake()
    {
        var canvasGo = new GameObject("CafeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        touch = EditTouchSurface.Make(canvasGo.transform);
        touch.Grab = Grab;
        touch.Move = DragTo;
        touch.Drop = Drop;
        touch.Tap = Tap;
        touch.HoldPixels = 40f;
        touch.gameObject.SetActive(false);

        var panel = UiKit.Panel("CafeBar", canvasGo.transform, UiKit.TeakDark, Vector2.zero, new Vector2(1260f, 170f), shadow: true);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 24f);
        bar = panel.gameObject;
        infoText = UiKit.Label(panel, 26, "", new Vector2(0f, 50f), new Vector2(1220f, 44f), UiKit.GoldText);
        infoText.fontStyle = FontStyle.Bold;

        browseRow = UiKit.Container(panel, new Vector2(0f, -25f)).gameObject;
        var b = browseRow.transform;
        float w = 166f, step = 174f, x0 = -3f * step;
        UiKit.RoundButton(b, "ซื้อของ", new Vector2(x0, 0f), new Vector2(w, 76f), UiKit.Gold, UiKit.GoldInk, OpenList, 26);
        moveButton = UiKit.RoundButton(b, "ย้าย", new Vector2(x0 + step, 0f), new Vector2(w, 76f), UiKit.Cream, UiKit.Ink, PickUpSelected, 26);
        rotateButton = UiKit.RoundButton(b, "หมุน", new Vector2(x0 + step * 2f, 0f), new Vector2(w, 76f), UiKit.Cream, UiKit.Ink, RotateSelected, 26);
        storeButton = UiKit.RoundButton(b, "เก็บ", new Vector2(x0 + step * 3f, 0f), new Vector2(w, 76f), UiKit.DangerTint, UiKit.Danger, StoreSelected, 26);
        UiKit.RoundButton(b, "สไตล์", new Vector2(x0 + step * 4f, 0f), new Vector2(w, 76f), UiKit.Cream, UiKit.Ink, OpenStyle, 24);
        UiKit.RoundButton(b, "ถ่ายรูป", new Vector2(x0 + step * 5f, 0f), new Vector2(w, 76f), UiKit.Gold, UiKit.GoldInk, () => StartCoroutine(Photo()), 26);
        UiKit.RoundButton(b, "เสร็จ", new Vector2(x0 + step * 6f, 0f), new Vector2(w, 76f), UiKit.PayGreen, Color.white, Close, 26);

        carryRow = UiKit.Container(panel, new Vector2(0f, -25f)).gameObject;
        var c = carryRow.transform;
        placeButton = UiKit.RoundButton(c, "วางตรงนี้", new Vector2(-250f, 0f), new Vector2(300f, 76f), UiKit.PayGreen, Color.white, PutDown, 30);
        UiKit.RoundButton(c, "หมุน", new Vector2(40f, 0f), new Vector2(220f, 76f), UiKit.Cream, UiKit.Ink, () => carriedRot = (carriedRot + 45) % 360, 28);
        UiKit.RoundButton(c, "ยกเลิก", new Vector2(300f, 0f), new Vector2(220f, 76f), UiKit.DangerTint, UiKit.Danger, () => { CancelCarry(); ShowRows(); }, 28);

        toastText = UiKit.ToastBar(canvasGo.transform, new Vector2(0f, -240f), 700f);

        var body = UiKit.PremiumWindow(transform, "CafeShopList", UiKit.WindowSize(new Vector2(1400f, 880f), 1450f), 56, "icon:home",
            out listBackdrop, out listTitle, out listSubtitle, () => listBackdrop.SetActive(false), out _);
        listTitle.text = "ของแต่งร้านกาแฟ";
        listSubtitle.text = "เลือกแล้วแตะหรือลากไปวาง · จ่ายเงินตอนวาง · เก็บคืนได้ครึ่งราคา · ของติดผนังจะเกาะผนังให้เอง";
        listGrid = UiKit.Container(body, new Vector2(0f, UiKit.Portrait ? 560f : 300f));
        listBackdrop.SetActive(false);

        styleBody = UiKit.PremiumWindow(transform, "CafeStyle", UiKit.WindowSize(new Vector2(1100f, 760f), 1100f), 57, "icon:home",
            out styleBackdrop, out styleTitle, out styleSubtitle, () => styleBackdrop.SetActive(false), out _);
        styleTitle.text = "สไตล์และชื่อร้าน";
        styleSubtitle.text = "ผนัง พื้น และเฟอร์นิเจอร์ทั้งร้านเปลี่ยนตามสไตล์";
        styleBackdrop.SetActive(false);

        ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "CafeSelectRing";
        Destroy(ring.GetComponent<Collider>());
        ring.GetComponent<Renderer>().sharedMaterial = CafeFurniture.Glow(new Color(1f, 0.78f, 0.3f), 0.6f);
        ring.SetActive(false);
        bar.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (ring != null) Destroy(ring);
    }

    private void ShowRows()
    {
        bool carrying = carried != null;
        browseRow.SetActive(!carrying);
        carryRow.SetActive(carrying);
    }

    private void Update()
    {
        if (!bar.activeSelf) return;
        var s = Client?.Current;
        if (site == null || s == null || !s.open) { Close(); return; }
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        if (!site.Inside(player.transform.position) && carried == null) { Close(); return; }

        ShowGrid();
        if (carried != null)
        {
            var at = fingerPlaced ? carried.transform.localPosition : site.SpotInFront(player.transform, CarryDistance);
            fingerPlaced = true; // a new piece starts in front of you, then stays where it is until you move it
            carried.transform.localPosition = Fit(at, carriedItem, carriedMin, carriedMax, ref carriedRot);
            carried.transform.localRotation = Quaternion.Euler(0f, carriedRot, 0f);
            string price = carriedId == 0 ? $" · {Price(carriedItem):N0} ฿" : "";
            infoText.text = $"{Lang.T("กำลังวาง")}: {s.NameOf(carriedItem)}{price} — {Lang.T("แตะหรือลากไปตรงที่ต้องการ แล้วกด วางตรงนี้")}";
            placeButton.interactable = !busy;
            ring.SetActive(false);
            return;
        }

        // only what you tapped is picked (never just the piece you happen to stand next to)
        selectedId = 0;
        if (pinnedId != 0 && site.Pieces.TryGetValue(pinnedId, out var pinned) && pinned != null) selectedId = pinnedId;
        else pinnedId = 0;
        bool has = selectedId != 0;
        moveButton.interactable = rotateButton.interactable = storeButton.interactable = has && !busy;
        if (has)
        {
            var piece = site.Pieces[selectedId].transform;
            string item = ItemOf(selectedId);
            float size = CafeFurniture.Size(item) * 2f + 0.3f;
            ring.SetActive(true);
            ring.transform.position = piece.position + Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(size, 0.004f, size);
            infoText.text = $"{Lang.T("เลือกอยู่")}: {s.NameOf(item)} · {Lang.T("ของในร้าน")} {s.pieces?.Length ?? 0}/{s.max_pieces}";
        }
        else
        {
            ring.SetActive(false);
            infoText.text = (s.pieces?.Length ?? 0) == 0
                ? Lang.T(Say("ร้านยังว่าง — กด ซื้อของ แล้วเริ่มจากเคาน์เตอร์บาร์กับเครื่องชงกาแฟ", "ล็อบบี้ยังว่าง — กด ซื้อของ แล้วเริ่มจากเคาน์เตอร์ต้อนรับ", "ตึกแถวยังว่าง — กด ซื้อของ แล้วจัดได้ตามใจ"))
                : Lang.T("แตะของเพื่อเลือก แล้วลากหรือกด ย้าย");
        }
    }

    private int Price(string item)
    {
        var items = Client?.Current?.items;
        if (items != null) foreach (var i in items) if (i.id == item) return i.price;
        return 0;
    }

    private string ItemOf(int id)
    {
        var pieces = Client?.Current?.pieces;
        if (pieces != null) foreach (var p in pieces) if (p.id == id) return p.item;
        return "";
    }

    // ── Touch: pick, drag, drop ─────────────────────────────────────────────────
    private bool FingerOnFloor(Vector2 screen, out Vector3 local)
    {
        local = default;
        if (site == null || !EditTouchSurface.GroundPoint(screen, site.Room.position.y, out var world)) return false;
        local = site.Room.InverseTransformPoint(world);
        local.y = 0f;
        return true;
    }

    // Snapped to the grid and kept in the room
    private Vector3 Snapped(Vector3 local)
    {
        var s = Client?.Current;
        float hw = s != null ? s.half_w : site.HalfW - 0.2f, hd = s != null ? s.half_d : site.HalfD - 0.2f;
        float x = Mathf.Round(local.x / Cell) * Cell, z = Mathf.Round(local.z / Cell) * Cell;
        return new Vector3(Mathf.Clamp(x, -hw, hw), 0f, Mathf.Clamp(z, -hd, hd));
    }

    // A piece's footprint in its own space (unturned), from what it's drawn with
    private static void Footprint(GameObject piece, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(-0.2f, 0f, -0.2f);
        max = new Vector3(0.2f, 0f, 0.2f);
        bool any = false;
        var t = piece.transform;
        foreach (var r in piece.GetComponentsInChildren<Renderer>())
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var p = Quaternion.Inverse(t.rotation) * (corner - t.position) / Mathf.Max(0.01f, t.lossyScale.x);
                if (!any) { min = max = p; any = true; }
                else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            }
        }
    }

    // Kept inside the walls by the piece's own size; wall pieces go flat on the nearest wall, facing in
    private Vector3 Fit(Vector3 local, string item, Vector3 min, Vector3 max, ref int rot)
    {
        float hw = site.HalfW - 0.02f, hd = site.HalfD - 0.02f;
        var s = Client?.Current;
        if (s != null) { hw = Mathf.Min(hw, s.half_w); hd = Mathf.Min(hd, s.half_d); }
        float sideX = Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x));
        if (OnWall(item))
        {
            float back = Mathf.Max(0f, -min.z) + 0.01f;
            float dl = local.x + hw, dr = hw - local.x, df = local.z + hd, db = hd - local.z;
            float best = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(df, db));
            if (best == db) { rot = 180; return new Vector3(Mathf.Clamp(local.x, -hw + sideX, hw - sideX), 0f, hd - back); }
            if (best == df) { rot = 0; return new Vector3(Mathf.Clamp(local.x, -hw + sideX, hw - sideX), 0f, -hd + back); }
            if (best == dl) { rot = 90; return new Vector3(-hw + back, 0f, Mathf.Clamp(local.z, -hd + sideX, hd - sideX)); }
            rot = 270;
            return new Vector3(hw - back, 0f, Mathf.Clamp(local.z, -hd + sideX, hd - sideX));
        }
        float a = rot * Mathf.Deg2Rad, c = Mathf.Abs(Mathf.Cos(a)), sn = Mathf.Abs(Mathf.Sin(a));
        float sideZ = Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z));
        float ex = Mathf.Min(hw, c * sideX + sn * sideZ), ez = Mathf.Min(hd, sn * sideX + c * sideZ);
        return new Vector3(Mathf.Clamp(local.x, -hw + ex, hw - ex), 0f, Mathf.Clamp(local.z, -hd + ez, hd - ez));
    }

    private int PieceAt(Vector3 local)
    {
        int found = 0;
        float best = float.MaxValue;
        foreach (var kv in site.Pieces)
        {
            if (kv.Value == null) continue;
            Vector3 p = kv.Value.transform.localPosition;
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(local.x, local.z));
            float reach = CafeFurniture.Size(ItemOf(kv.Key)) + 0.3f;
            if (d <= reach && d < best) { best = d; found = kv.Key; }
        }
        return found;
    }

    private bool Grab(Vector2 screen)
    {
        if (!IsOpen || busy || site == null || listBackdrop.activeSelf || styleBackdrop.activeSelf || !FingerOnFloor(screen, out var local)) return false;
        if (carried != null)
        {
            Vector3 c = carried.transform.localPosition;
            if (Vector2.Distance(new Vector2(c.x, c.z), new Vector2(local.x, local.z)) > CafeFurniture.Size(carriedItem) + 0.5f) return false;
            holdOffset = c - local;
            holding = true;
            fingerPlaced = true;
            return true;
        }
        int id = PieceAt(local);
        if (id == 0) return false;
        if (id != pinnedId) return false; // not picked yet: this touch picks it (Tap) or turns the camera
        selectedId = id;
        PickUpSelected();
        if (carried == null) return false;
        holdOffset = carried.transform.localPosition - local;
        holding = true;
        return true;
    }

    private void DragTo(Vector2 screen)
    {
        if (!holding || carried == null || !FingerOnFloor(screen, out var local)) return;
        fingerPlaced = true;
        carried.transform.localPosition = Snapped(local + holdOffset);
    }

    private void Drop(Vector2 screen, bool moved)
    {
        if (!holding) return;
        holding = false;
        if (carried == null) return;
        if (carriedId == 0) return;          // a new piece: วางตรงนี้ buys it
        var at = carried.transform.localPosition;
        if (!moved || (at - originalPosition).sqrMagnitude < 0.01f) { CancelCarry(); ShowRows(); return; } // didn't go anywhere
        PutDown();
    }

    private void Tap(Vector2 screen)
    {
        if (!IsOpen || busy || site == null || listBackdrop.activeSelf || styleBackdrop.activeSelf || !FingerOnFloor(screen, out var local)) return;
        if (carried != null)
        {
            fingerPlaced = true;
            carried.transform.localPosition = Snapped(local);
            return;
        }
        pinnedId = PieceAt(local);
    }

    private void ShowGrid()
    {
        if (grid == null) grid = BuildGrid.Make("CafeGrid_ตาราง");
        var s = Client?.Current;
        float hw = s != null ? s.half_w : site.HalfW - 0.2f, hd = s != null ? s.half_d : site.HalfD - 0.2f;
        grid.Show(site.Room, -hw, hw, -hd, hd, site.Room.position.y, Cell, site.Room.lossyScale.x);
    }

    // ── Buying ──────────────────────────────────────────────────────────────────
    private void OpenList()
    {
        UiKit.Clear(listGrid);
        var s = Client.Current;
        bool upright = UiKit.Portrait;
        int i = 0;
        foreach (var it in s.items ?? new CafeClient.Item[0])
        {
            var item = it;
            int cols = upright ? 2 : 4;
            float x = upright ? (i % 2 == 0 ? -172f : 172f) : -495f + (i % 4) * 330f, y = -(i / cols) * (upright ? 106f : 112f);
            var card = UiKit.Panel("CafeItem", listGrid, Color.white, new Vector2(x, y), new Vector2(316f, 100f), shadow: true);
            if (upright) card.localScale = new Vector3(0.98f, 0.98f, 1f);
            card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
            UiKit.Label(card, 23, it.name, new Vector2(-48f, 18f), new Vector2(210f, 36f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(card, 21, $"{it.price:N0} ฿", new Vector2(-48f, -20f), new Vector2(210f, 32f), UiKit.Price, TextAnchor.MiddleLeft);
            UiKit.RoundButton(card, "วาง", new Vector2(106f, 0f), new Vector2(86f, 60f), UiKit.Gold, UiKit.GoldInk, () => StartNew(item.id), 24)
                .interactable = s.cash >= it.price && (s.pieces?.Length ?? 0) < s.max_pieces;
            i++;
        }
        listBackdrop.SetActive(true);
    }

    private void StartNew(string item)
    {
        listBackdrop.SetActive(false);
        carried = CafeFurniture.Build(item, site.Style);
        carried.transform.SetParent(site.Room, false);
        Footprint(carried, out carriedMin, out carriedMax);
        Solid(carried, false);
        carriedItem = item;
        carriedId = 0;
        carriedRot = 180; // facing you
        fingerPlaced = false;
        ShowRows();
    }

    // ── Moving / rotating / storing ─────────────────────────────────────────────
    private void PickUpSelected()
    {
        if (selectedId == 0 || !site.Pieces.TryGetValue(selectedId, out var go)) return;
        carried = go;
        carriedId = selectedId;
        carriedItem = ItemOf(selectedId);
        carriedRot = Mathf.RoundToInt(go.transform.localEulerAngles.y / 15f) * 15 % 360;
        originalPosition = go.transform.localPosition;
        originalRotation = go.transform.localRotation;
        Footprint(go, out carriedMin, out carriedMax);
        fingerPlaced = true; // ย้าย keeps it where it is: tap or drag it to the new spot
        Solid(go, false);
        ShowRows();
    }

    private void CancelCarry()
    {
        if (carried == null) return;
        if (carriedId == 0) Destroy(carried);
        else
        {
            carried.transform.localPosition = originalPosition;
            carried.transform.localRotation = originalRotation;
            Solid(carried, true);
        }
        carried = null;
    }

    private static void Solid(GameObject go, bool on)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = on;
    }

    private void PutDown()
    {
        if (carried == null) return;
        Vector3 p = carried.transform.localPosition;
        int id = carriedId, rot = carriedRot;
        string item = carriedItem;
        string name = Client.Current.NameOf(item);
        Run(id == 0 ? (Func<Task<CafeClient.State>>)(() => Client.PlaceAsync(item, p.x, p.z, rot))
                    : () => Client.MoveAsync(id, p.x, p.z, rot),
            id == 0 ? $"{Lang.T("วาง")}{name} −{Price(item):N0} ฿" : $"{Lang.T("ย้าย")}{name}{Lang.T("แล้ว")}", afterCarry: true);
    }

    private void RotateSelected()
    {
        if (selectedId == 0) return;
        var t = site.Pieces[selectedId].transform;
        int id = selectedId;
        string item = ItemOf(id);
        int rot = (Mathf.RoundToInt(t.localEulerAngles.y / 15f) * 15 + 45) % 360;
        Footprint(t.gameObject, out var min, out var max);
        Vector3 p = Fit(t.localPosition, item, min, max, ref rot); // turned, it must still fit (wall pieces stay on their wall)
        t.localPosition = p;
        t.localRotation = Quaternion.Euler(0f, rot, 0f);
        Run(() => Client.MoveAsync(id, p.x, p.z, rot), null);
    }

    private void StoreSelected()
    {
        if (selectedId == 0) return;
        int id = selectedId;
        Run(() => Client.RemoveAsync(id), $"{Lang.T("เก็บ")}{Client.Current.NameOf(ItemOf(id))} +{Price(ItemOf(id)) / 2:N0} ฿");
    }

    // ── Style and name ──────────────────────────────────────────────────────────
    private void OpenStyle()
    {
        var s = Client.Current;
        pickStyle = s.style;
        UiKit.Clear(styleBody);
        bool upright = UiKit.Portrait;
        float top = upright ? 380f : 230f;
        UiKit.Label(styleBody, 26, Say("ชื่อร้าน (ขึ้นป้ายหน้าร้าน)", "ชื่อโรงแรม (ขึ้นป้ายหน้าโรงแรม)", "ชื่อตึกแถว (ขึ้นป้ายหน้าตึก · เว้นว่างไว้ = ตึกแถวของคุณ)"), new Vector2(0f, top), new Vector2(900f, 40f), UiKit.Muted);
        var box = UiKit.Panel("NameField", styleBody, Color.white, new Vector2(0f, top - 60f), new Vector2(upright ? 640f : 760f, 70f), shadow: true);
        box.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        var text = UiKit.Label(box, 30, "", Vector2.zero, new Vector2(upright ? 600f : 720f, 62f), UiKit.Ink, TextAnchor.MiddleCenter);
        text.supportRichText = false;
        var placeholder = UiKit.Label(box, 26, Say("เช่น บ้านไร่คาเฟ่", "เช่น บ้านริมทางรีสอร์ท", "เช่น รุ่งเรืองพาณิชย์"), Vector2.zero, new Vector2(upright ? 600f : 720f, 62f), UiKit.Muted, TextAnchor.MiddleCenter);
        placeholder.fontStyle = FontStyle.Italic;
        nameField = box.gameObject.AddComponent<InputField>();
        nameField.textComponent = text;
        nameField.placeholder = placeholder;
        nameField.characterLimit = s.name_max > 0 ? s.name_max : 24;
        nameField.lineType = InputField.LineType.SingleLine;
        nameField.text = s.name ?? "";

        UiKit.Label(styleBody, 26, $"สไตล์{Say("ร้าน", "โรงแรม", "ตึกแถว")} · เปลี่ยนสไตล์ {s.style_price:N0} ฿", new Vector2(0f, top - 140f), new Vector2(900f, 40f), UiKit.Muted);
        var styles = s.styles ?? new CafeClient.Style[0];
        for (int i = 0; i < styles.Length; i++)
        {
            var st = styles[i];
            var look = CafeStyle.Of(st.id);
            Vector2 at = upright ? new Vector2(i % 2 == 0 ? -170f : 170f, top - 240f - (i / 2) * 150f) : new Vector2(-375f + i * 250f, top - 260f);
            var card = UiKit.Panel("Style_" + st.id, styleBody, Color.white, at, new Vector2(230f, 140f), shadow: true);
            card.gameObject.AddComponent<Outline>().effectColor = st.id == s.style ? UiKit.Gold : UiKit.CardBorder;
            // swatches: wall, feature, floor, cloth
            Color[] sw = { look.Wall, look.Feature, look.Floor, look.Cloth };
            for (int k = 0; k < 4; k++)
                UiKit.Rect("Swatch", card, sw[k]).anchoredPosition = new Vector2(-75f + k * 50f, 30f);
            foreach (Transform t in card) if (t.name == "Swatch") ((RectTransform)t).sizeDelta = new Vector2(44f, 44f);
            string id = st.id;
            UiKit.RoundButton(card, st.name, new Vector2(0f, -32f), new Vector2(200f, 56f), id == s.style ? UiKit.Teak : UiKit.Pill,
                id == s.style ? UiKit.GoldText : UiKit.Ink, () => SaveStyle(id), 22).interactable = !busy && (id == s.style || s.cash >= s.style_price);
        }
        UiKit.RoundButton(styleBody, Say("บันทึกชื่อร้าน", "บันทึกชื่อโรงแรม", "บันทึกชื่อตึกแถว"), new Vector2(0f, upright ? -330f : -290f), new Vector2(320f, 68f), UiKit.PayGreen, Color.white,
            () => SaveStyle(null), 26);
        styleBackdrop.SetActive(true);
    }

    private void SaveStyle(string style)
    {
        string name = nameField != null ? nameField.text.Trim() : null;
        Run(() => Client.StyleAsync(style ?? Client.Current.style, name), null, after: () =>
        {
            if (styleBackdrop.activeSelf) OpenStyle();
        });
    }

    // ── Server calls ────────────────────────────────────────────────────────────
    private async void Run(Func<Task<CafeClient.State>> call, string success, bool afterCarry = false, Action after = null)
    {
        if (busy || Client == null) return;
        busy = true;
        try
        {
            var s = await call();
            if (afterCarry)
            {
                if (carriedId == 0 && carried != null) Destroy(carried); // the real piece comes from the server
                carried = null;
            }
            Notify(success ?? s.done ?? "", UiKit.PayGreen);
            if (afterCarry || success != null) GameAudio.Play("coins", null, 0.5f);
        }
        catch (ApiResponseException e)
        {
            Notify(Why(e.Message, Hotel, Shop), UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify(Lang.T("เกิดข้อผิดพลาด ลองใหม่"), UiKit.Danger);
            Debug.LogWarning($"[Cafe] {e.Message}");
        }
        busy = false;
        ShowRows();
        after?.Invoke();
    }

    private static string Why(string m, bool hotel, bool shophouse)
    {
        m ??= "";
        if (m.Contains("not enough")) return Lang.T("เงินไม่พอ");
        if (m.Contains("full")) return hotel ? Lang.T("ล็อบบี้เต็มแล้ว (40 ชิ้น) เก็บบางอย่างก่อน") : shophouse ? Lang.T("ตึกแถวเต็มแล้ว (40 ชิ้น) เก็บบางอย่างก่อน") : Lang.T("ร้านเต็มแล้ว (40 ชิ้น) เก็บบางอย่างก่อน");
        if (m.Contains("no shophouse")) return Lang.T("ต้องสร้างตึกแถวบนที่ดินในเมืองก่อน");
        if (m.Contains("no coffee")) return Lang.T("ต้องเปิดธุรกิจร้านกาแฟที่ธนาคารก่อน");
        if (m.Contains("no hotel")) return Lang.T("ต้องเปิดธุรกิจโรงแรมที่ธนาคารก่อน");
        if (m.Contains("name too long")) return hotel ? Lang.T("ชื่อโรงแรมยาวเกินไป") : shophouse ? Lang.T("ชื่อตึกแถวยาวเกินไป") : Lang.T("ชื่อร้านยาวเกินไป");
        return Lang.T("ทำไม่ได้ตอนนี้");
    }

    private void Notify(string message, Color color)
    {
        if (string.IsNullOrEmpty(message)) return;
        toast = UiKit.Toast(this, toastText, message, color, toast);
    }

    // ── Photo to share ──────────────────────────────────────────────────────────
    private string photoPath;

    private IEnumerator Photo()
    {
        if (busy) yield break;
        busy = true;
        ring.SetActive(false);
        // every on-screen panel off for one frame: just the shop
        var hidden = new System.Collections.Generic.List<Canvas>();
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.enabled && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { c.enabled = false; hidden.Add(c); }
        yield return new WaitForEndOfFrame();
        Texture2D shot = null;
        try
        {
            shot = ScreenCapture.CaptureScreenshotAsTexture();
            photoPath = Path.Combine(Application.persistentDataPath, "ThaiLife9_Cafe.png");
            File.WriteAllBytes(photoPath, shot.EncodeToPNG());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cafe] photo failed: {e.Message}");
            photoPath = null;
        }
        finally
        {
            if (shot != null) Destroy(shot);
            foreach (var c in hidden) if (c != null) c.enabled = true;
            busy = false;
        }
        if (photoPath == null) { Notify(Lang.T("ถ่ายรูปไม่ได้ ลองใหม่"), UiKit.Danger); yield break; }
        GameAudio.Play("whoosh", null, 0.5f);
        var s = Client.Current;
        string text = $"{(string.IsNullOrEmpty(s.name) ? Lang.T(Say("ร้านกาแฟของฉัน", "โรงแรมของฉัน", "ตึกแถวของฉัน")) : s.name)} · {Lang.T("สไตล์")}{s.StyleName} · ThaiLife9";
#if UNITY_IOS && !UNITY_EDITOR
        ThaiShare_Image(photoPath, text, gameObject.name);
#elif UNITY_ANDROID && !UNITY_EDITOR
        androidSharing = AndroidShare.ShareImage(photoPath, text);
        if (!androidSharing) Notify(Lang.T("แชร์ไม่ได้ ลองใหม่"), UiKit.Danger);
#else
        Debug.Log($"[Cafe] (editor) photo saved at {photoPath}");
        OnShareDone("1|editor");
#endif
    }

    // Android: the share sheet gives no answer; count it when the player comes back to the game
    private bool androidSharing;
    private void OnApplicationFocus(bool focus)
    {
        if (!focus || !androidSharing) return;
        androidSharing = false;
        OnShareDone("1|android");
    }

    // From the native share sheet: "1|<activity type>" shared or saved, "0|" closed
    public void OnShareDone(string message)
    {
        var parts = (message ?? "").Split('|');
        if (parts.Length == 0 || parts[0] != "1") return;
        string app = parts.Length > 1 ? parts[1] : "";
        Notify(app.Contains("SaveToCameraRoll") ? Lang.T("บันทึกรูปร้านลงคลังภาพแล้ว") : Lang.T("แชร์รูปร้านแล้ว"), UiKit.PayGreen);
    }
}
