using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// จัดร้านกาแฟ (CafeSite, cafe.lua), built for the phone like แต่งบ้าน: walk round your shop with the
// joystick. The piece nearest you gets a gold ring → ย้าย / หมุน / เก็บ (half the price back).
// "ซื้อของ" lists the café furniture; pick one and it rides in front of you → วางตรงนี้ buys it.
// "สไตล์ร้าน" picks the walls and floor (มินิมอล วินเทจ ลอฟท์ ไทยร่วมสมัย) and the name on the sign.
// "ถ่ายรูป" takes the screen without the buttons and opens the phone's share sheet (Facebook, LINE,
// Instagram, save to Photos…, Plugins/iOS/ThaiShare.mm).
public class CafeUI : MonoBehaviour
{
    private const float CarryDistance = 1.4f;
    private const float SelectRange = 1.6f;

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

    private static CafeClient Client => CafeClient.Instance;

    public static void Open(CafeSite site)
    {
        if (instance == null) instance = new GameObject("CafeUI").AddComponent<CafeUI>(); // the share sheet reports back to this name
        instance.site = site;
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
        UiKit.RoundButton(b, "สไตล์ร้าน", new Vector2(x0 + step * 4f, 0f), new Vector2(w, 76f), UiKit.Cream, UiKit.Ink, OpenStyle, 24);
        UiKit.RoundButton(b, "ถ่ายรูป", new Vector2(x0 + step * 5f, 0f), new Vector2(w, 76f), UiKit.Gold, UiKit.GoldInk, () => StartCoroutine(Photo()), 26);
        UiKit.RoundButton(b, "เสร็จ", new Vector2(x0 + step * 6f, 0f), new Vector2(w, 76f), UiKit.PayGreen, Color.white, Close, 26);

        carryRow = UiKit.Container(panel, new Vector2(0f, -25f)).gameObject;
        var c = carryRow.transform;
        placeButton = UiKit.RoundButton(c, "วางตรงนี้", new Vector2(-250f, 0f), new Vector2(300f, 76f), UiKit.PayGreen, Color.white, PutDown, 30);
        UiKit.RoundButton(c, "หมุน", new Vector2(40f, 0f), new Vector2(220f, 76f), UiKit.Cream, UiKit.Ink, () => carriedRot = (carriedRot + 45) % 360, 28);
        UiKit.RoundButton(c, "ยกเลิก", new Vector2(300f, 0f), new Vector2(220f, 76f), UiKit.DangerTint, UiKit.Danger, () => { CancelCarry(); ShowRows(); }, 28);

        toastText = UiKit.ToastBar(canvasGo.transform, new Vector2(0f, -240f), 700f);

        var body = UiKit.PremiumWindow(transform, "CafeShopList", UiKit.WindowSize(new Vector2(1400f, 880f), 1450f), 56, "icon:home",
            out listBackdrop, out var title, out var subtitle, () => listBackdrop.SetActive(false), out _);
        title.text = "ของแต่งร้านกาแฟ";
        subtitle.text = "เลือกแล้วเดินไปวาง · จ่ายเงินตอนวาง · เก็บคืนได้ครึ่งราคา";
        listGrid = UiKit.Container(body, new Vector2(0f, UiKit.Portrait ? 560f : 300f));
        listBackdrop.SetActive(false);

        styleBody = UiKit.PremiumWindow(transform, "CafeStyle", UiKit.WindowSize(new Vector2(1100f, 760f), 1100f), 57, "icon:home",
            out styleBackdrop, out var styleTitle, out var styleSub, () => styleBackdrop.SetActive(false), out _);
        styleTitle.text = "สไตล์และชื่อร้าน";
        styleSub.text = "ผนัง พื้น และเฟอร์นิเจอร์ทั้งร้านเปลี่ยนตามสไตล์";
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

        if (carried != null)
        {
            carried.transform.localPosition = site.SpotInFront(player.transform, CarryDistance);
            carried.transform.localRotation = Quaternion.Euler(0f, carriedRot, 0f);
            string price = carriedId == 0 ? $" · {Price(carriedItem):N0} ฿" : "";
            infoText.text = $"{Lang.T("กำลังวาง")}: {s.NameOf(carriedItem)}{price} — {Lang.T("เดินไปตรงที่ต้องการแล้วกด วางตรงนี้")}";
            placeButton.interactable = !busy;
            ring.SetActive(false);
            return;
        }

        selectedId = 0;
        float best = SelectRange;
        Vector3 me = site.Room.InverseTransformPoint(player.transform.position);
        foreach (var kv in site.Pieces)
        {
            if (kv.Value == null) continue;
            Vector3 p = kv.Value.transform.localPosition;
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(me.x, me.z));
            if (d < best) { best = d; selectedId = kv.Key; }
        }
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
            infoText.text = (s.pieces?.Length ?? 0) == 0 ? Lang.T("ร้านยังว่าง — กด ซื้อของ แล้วเริ่มจากเคาน์เตอร์บาร์กับเครื่องชงกาแฟ") : Lang.T("เดินไปใกล้ของที่ต้องการจัด");
        }
    }

    private static int Price(string item)
    {
        var items = Client?.Current?.items;
        if (items != null) foreach (var i in items) if (i.id == item) return i.price;
        return 0;
    }

    private static string ItemOf(int id)
    {
        var pieces = Client?.Current?.pieces;
        if (pieces != null) foreach (var p in pieces) if (p.id == id) return p.item;
        return "";
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
        Solid(carried, false);
        carriedItem = item;
        carriedId = 0;
        carriedRot = 180; // facing you
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
        int rot = (Mathf.RoundToInt(t.localEulerAngles.y / 15f) * 15 + 45) % 360;
        Vector3 p = t.localPosition;
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
        UiKit.Label(styleBody, 26, "ชื่อร้าน (ขึ้นป้ายหน้าร้าน)", new Vector2(0f, top), new Vector2(900f, 40f), UiKit.Muted);
        var box = UiKit.Panel("NameField", styleBody, Color.white, new Vector2(0f, top - 60f), new Vector2(upright ? 640f : 760f, 70f), shadow: true);
        box.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        var text = UiKit.Label(box, 30, "", Vector2.zero, new Vector2(upright ? 600f : 720f, 62f), UiKit.Ink, TextAnchor.MiddleCenter);
        text.supportRichText = false;
        var placeholder = UiKit.Label(box, 26, "เช่น บ้านไร่คาเฟ่", Vector2.zero, new Vector2(upright ? 600f : 720f, 62f), UiKit.Muted, TextAnchor.MiddleCenter);
        placeholder.fontStyle = FontStyle.Italic;
        nameField = box.gameObject.AddComponent<InputField>();
        nameField.textComponent = text;
        nameField.placeholder = placeholder;
        nameField.characterLimit = s.name_max > 0 ? s.name_max : 24;
        nameField.lineType = InputField.LineType.SingleLine;
        nameField.text = s.name ?? "";

        UiKit.Label(styleBody, 26, $"สไตล์ร้าน · เปลี่ยนสไตล์ {s.style_price:N0} ฿", new Vector2(0f, top - 140f), new Vector2(900f, 40f), UiKit.Muted);
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
        UiKit.RoundButton(styleBody, "บันทึกชื่อร้าน", new Vector2(0f, upright ? -330f : -290f), new Vector2(320f, 68f), UiKit.PayGreen, Color.white,
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
            Notify(Why(e.Message), UiKit.Danger);
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

    private static string Why(string m)
    {
        m ??= "";
        if (m.Contains("not enough")) return Lang.T("เงินไม่พอ");
        if (m.Contains("full")) return Lang.T("ร้านเต็มแล้ว (40 ชิ้น) เก็บบางอย่างก่อน");
        if (m.Contains("no coffee")) return Lang.T("ต้องเปิดธุรกิจร้านกาแฟที่ธนาคารก่อน");
        if (m.Contains("name too long")) return Lang.T("ชื่อร้านยาวเกินไป");
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
        string text = $"{(string.IsNullOrEmpty(s.name) ? Lang.T("ร้านกาแฟของฉัน") : s.name)} · {Lang.T("สไตล์")}{s.StyleName} · ThaiLife9";
#if UNITY_IOS && !UNITY_EDITOR
        ThaiShare_Image(photoPath, text, gameObject.name);
#else
        Debug.Log($"[Cafe] (editor) photo saved at {photoPath}");
        OnShareDone("1|editor");
#endif
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
