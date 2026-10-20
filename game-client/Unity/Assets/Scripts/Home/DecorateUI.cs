using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Decorating mode (แต่งบ้าน), built for the phone: walk around the room with the joystick.
//   Browsing: the piece nearest to you is outlined by a gold ring → ย้าย / หมุน / เก็บ.
//   Carrying: the piece rides in front of you; walk to the spot → วางตรงนี้.
// "เพิ่มของ" lists the furniture you bought (ร้านค้าชุมชน, แท็บแต่งบ้าน) that isn't in the room yet.
public class DecorateUI : MonoBehaviour
{
    private const float CarryDistance = 1.3f;
    private const float SelectRange = 1.8f;

    private static DecorateUI instance;

    public static bool IsOpen => instance != null && instance.bar.activeSelf;

    private HomeInterior interior;
    private GameObject bar, browseRow, carryRow, ring;
    private Text infoText, toastText;
    private Button moveButton, rotateButton, storeButton, placeButton;
    private Coroutine toast;

    // List window
    private GameObject listBackdrop;
    private RectTransform listGrid;

    // Carrying
    private GameObject carried;
    private string carriedItem;
    private int carriedId;           // 0 = a new piece from storage
    private int carriedRot;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private int selectedId;
    private bool busy;

    public static void Open(HomeInterior interior)
    {
        if (instance == null)
            instance = new GameObject("DecorateUI").AddComponent<DecorateUI>();
        instance.interior = interior;
        instance.bar.SetActive(true);
        instance.ShowRows();
    }

    public static void Close()
    {
        if (instance == null || !instance.bar.activeSelf) return;
        instance.CancelCarry();
        instance.bar.SetActive(false);
        instance.listBackdrop.SetActive(false);
        instance.ring.SetActive(false);
        if (instance.interior != null) instance.interior.ShowPrompt();
    }

    private void Awake()
    {
        // Bar: bottom-centre, between the joystick and the action buttons
        var canvasGo = new GameObject("DecorateCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = UiKit.Panel("DecorateBar", canvasGo.transform, UiKit.TeakDark, Vector2.zero, new Vector2(1000f, 170f), shadow: true);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 24f);
        bar = panel.gameObject;
        infoText = UiKit.Label(panel, 26, "", new Vector2(0f, 50f), new Vector2(960f, 44f), UiKit.GoldText);
        infoText.fontStyle = FontStyle.Bold;

        browseRow = UiKit.Container(panel, new Vector2(0f, -25f)).gameObject;
        var b = browseRow.transform;
        UiKit.RoundButton(b, "เพิ่มของ", new Vector2(-388f, 0f), new Vector2(180f, 76f), UiKit.Gold, UiKit.GoldInk, OpenList, 28);
        moveButton = UiKit.RoundButton(b, "ย้าย", new Vector2(-194f, 0f), new Vector2(180f, 76f), UiKit.Cream, UiKit.Ink, PickUpSelected, 28);
        rotateButton = UiKit.RoundButton(b, "หมุน", new Vector2(0f, 0f), new Vector2(180f, 76f), UiKit.Cream, UiKit.Ink, RotateSelected, 28);
        storeButton = UiKit.RoundButton(b, "เก็บ", new Vector2(194f, 0f), new Vector2(180f, 76f), UiKit.DangerTint, UiKit.Danger, StoreSelected, 28);
        UiKit.RoundButton(b, "เสร็จ", new Vector2(388f, 0f), new Vector2(180f, 76f), UiKit.PayGreen, Color.white, Close, 28);

        carryRow = UiKit.Container(panel, new Vector2(0f, -25f)).gameObject;
        var c = carryRow.transform;
        placeButton = UiKit.RoundButton(c, "วางตรงนี้", new Vector2(-250f, 0f), new Vector2(300f, 76f), UiKit.PayGreen, Color.white, PutDown, 30);
        UiKit.RoundButton(c, "หมุน", new Vector2(40f, 0f), new Vector2(220f, 76f), UiKit.Cream, UiKit.Ink, () => carriedRot = (carriedRot + 90) % 360, 28);
        UiKit.RoundButton(c, "ยกเลิก", new Vector2(300f, 0f), new Vector2(220f, 76f), UiKit.DangerTint, UiKit.Danger, () => { CancelCarry(); ShowRows(); }, 28);

        toastText = UiKit.ToastBar(canvasGo.transform, new Vector2(0f, -240f), 700f);

        // "เพิ่มของ" list
        var body = UiKit.PremiumWindow(transform, "FurnitureList", new Vector2(1300f, 860f), 56, "icon:home",
            out listBackdrop, out var title, out var subtitle, () => listBackdrop.SetActive(false), out _);
        title.text = "ของในบ้าน";
        subtitle.text = "เลือกของที่จะวาง · ซื้อเพิ่มที่ร้านค้าชุมชน แท็บแต่งบ้าน";
        listGrid = UiKit.Container(body, new Vector2(0f, 270f));
        listBackdrop.SetActive(false);

        // Gold ring under the selected piece
        ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "SelectRing";
        Destroy(ring.GetComponent<Collider>());
        ring.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.78f, 0.3f) };
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
        if (!bar.activeSelf || interior == null) return;
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        if (carried != null)
        {
            carried.transform.localPosition = interior.SpotInFront(player.transform, CarryDistance);
            carried.transform.localRotation = Quaternion.Euler(0f, carriedRot, 0f);
            infoText.text = $"กำลังย้าย: {HouseClient.Instance?.NameOf(carriedItem)} — เดินไปตรงที่ต้องการแล้วกด วางตรงนี้";
            placeButton.interactable = !busy;
            ring.SetActive(false);
            return;
        }

        // Nearest piece to the player
        selectedId = 0;
        float best = SelectRange;
        Vector3 me = interior.Room.InverseTransformPoint(player.transform.position);
        foreach (var kv in interior.Pieces)
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
            var piece = interior.Pieces[selectedId].transform;
            ring.SetActive(true);
            ring.transform.position = piece.position + Vector3.up * 0.01f;
            ring.transform.localScale = new Vector3(1.4f, 0.005f, 1.4f);
            infoText.text = $"เลือกอยู่: {HouseClient.Instance?.NameOf(ItemOf(selectedId))} (ชิ้นที่ใกล้คุณที่สุด)";
        }
        else
        {
            ring.SetActive(false);
            infoText.text = interior.Pieces.Count == 0 ? "ยังไม่มีของในห้อง — กด เพิ่มของ" : "เดินไปใกล้ของที่ต้องการจัด";
        }
    }

    private static string ItemOf(int id)
    {
        var placed = HouseClient.Instance?.Current?.placed;
        if (placed != null) foreach (var p in placed) if (p.id == id) return p.item;
        return "";
    }

    // ── Adding from storage ─────────────────────────────────────────────────────
    private void OpenList()
    {
        UiKit.Clear(listGrid);
        var owned = HouseClient.Instance?.Current?.owned;
        int i = 0;
        if (owned != null)
        {
            foreach (var o in owned)
            {
                var item = o;
                float x = -400f + (i % 3) * 400f, y = -(i / 3) * 128f;
                var card = UiKit.Panel("Furniture", listGrid, Color.white, new Vector2(x, y), new Vector2(380f, 116f), shadow: true);
                card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
                bool pic = UiKit.Art("Icons/furn_" + o.item) != null;
                if (pic) UiKit.IconTile(card, "furn_" + o.item, new Vector2(-138f, 0f), new Vector2(88f, 88f));
                float tx = pic ? -20f : -55f, tw = pic ? 170f : 240f;
                UiKit.Label(card, 26, o.name, new Vector2(tx, 22f), new Vector2(tw, 40f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                UiKit.Label(card, 20, $"เก็บไว้ {o.Stored} · ในห้อง {o.placed}", new Vector2(tx, -22f), new Vector2(tw, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
                UiKit.RoundButton(card, "วาง", new Vector2(125f, 0f), new Vector2(100f, 64f), UiKit.Gold, UiKit.GoldInk, () => StartNew(item.item), 26)
                    .interactable = o.Stored > 0;
                i++;
            }
        }
        if (i == 0)
            UiKit.Label(listGrid, 28, "ยังไม่มีเฟอร์นิเจอร์\nซื้อได้ที่ร้านค้าชุมชน แท็บ แต่งบ้าน", new Vector2(0f, -200f), new Vector2(900f, 110f), UiKit.Muted);
        listBackdrop.SetActive(true);
    }

    private void StartNew(string item)
    {
        listBackdrop.SetActive(false);
        interior.Freeze(true);
        carried = interior.SpawnPreview(item);
        carriedItem = item;
        carriedId = 0;
        carriedRot = 0;
        ShowRows();
    }

    // ── Moving / rotating / storing what's in the room ─────────────────────────
    private void PickUpSelected()
    {
        if (selectedId == 0 || !interior.Pieces.TryGetValue(selectedId, out var go)) return;
        interior.Freeze(true);
        carried = go;
        carriedId = selectedId;
        carriedItem = ItemOf(selectedId);
        carriedRot = Mathf.RoundToInt(go.transform.localEulerAngles.y / 90f) * 90 % 360;
        originalPosition = go.transform.localPosition;
        originalRotation = go.transform.localRotation;
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
        }
        carried = null;
        interior?.Freeze(false);
    }

    private void PutDown()
    {
        if (carried == null) return;
        Vector3 p = carried.transform.localPosition;
        int id = carriedId;
        string item = carriedItem;
        int rot = carriedRot;
        Run(id == 0 ? (Func<System.Threading.Tasks.Task<HouseClient.State>>)(() => HouseClient.Instance.PlaceAsync(item, p.x, p.z, rot))
                    : () => HouseClient.Instance.MoveAsync(id, p.x, p.z, rot),
            $"วาง{HouseClient.Instance?.NameOf(item)}แล้ว", afterCarry: true);
    }

    private void RotateSelected()
    {
        if (selectedId == 0) return;
        var t = interior.Pieces[selectedId].transform;
        int id = selectedId;
        int rot = (Mathf.RoundToInt(t.localEulerAngles.y / 90f) * 90 + 90) % 360;
        Vector3 p = t.localPosition;
        t.localRotation = Quaternion.Euler(0f, rot, 0f); // feels instant; the server confirms
        Run(() => HouseClient.Instance.MoveAsync(id, p.x, p.z, rot), null);
    }

    private void StoreSelected()
    {
        if (selectedId == 0) return;
        int id = selectedId;
        string name = HouseClient.Instance?.NameOf(ItemOf(id));
        Run(() => HouseClient.Instance.RemoveAsync(id), $"เก็บ{name}แล้ว");
    }

    private async void Run(Func<System.Threading.Tasks.Task<HouseClient.State>> call, string success, bool afterCarry = false)
    {
        if (busy || HouseClient.Instance == null) return;
        busy = true;
        try
        {
            await call();
            if (afterCarry)
            {
                if (carriedId == 0 && carried != null) Destroy(carried); // the real piece comes from the server
                carried = null;
            }
            if (success != null) Notify(success, UiKit.PayGreen);
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("outside") ? "วางนอกห้องไม่ได้"
                : e.Message.Contains("full") ? "ห้องเต็มแล้ว (30 ชิ้น)"
                : e.Message.Contains("storage") ? "ไม่มีชิ้นนี้เหลือในที่เก็บ" : "ทำไม่ได้ตอนนี้", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[House] {e.Message}");
        }
        busy = false;
        if (carried == null) interior?.Freeze(false);
        ShowRows();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
