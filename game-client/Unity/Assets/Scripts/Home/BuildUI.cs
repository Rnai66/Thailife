using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// โหมดก่อสร้าง: build round your home, or on your farm lot or your land (whichever you're standing on
// when you open it: the `site`, BuiltPieces.AnchorOf). A strip at the top of the screen (clear of the joystick and the
// action buttons, so you can walk while you build) holds the catalogue, and the buttons below it.
//   Building: pick a piece; its ghost stands in front of you (green: OK, red: too far from the house)
//     and follows as you walk. หมุน turns it, สี picks the paint, วาง builds it. With ต่อแนว on, the
//     next fence carries on from the end of the last one (turn it to go round a corner).
//   Editing (แก้ไข): the piece nearest you is picked out in yellow (ถัดไป for the next). ย้าย lifts it
//     to follow you until วางที่นี่; หมุน and สี change it at once; รื้อถอน pulls it down (half back).
//   No walking needed (EditTouchSurface): tap a piece to pick it, put a finger on it and drag to move it
//     over the grid on the ground (BuildGrid, half-metre steps); let go to set it down. While placing,
//     tap the ground to put the new piece there, or drag its ghost. Home's own things (YardProps: the
//     banana trees, punching bag, jars, rice barn, coop) are picked and dragged the same way.
public class BuildUI : MonoBehaviour
{
    private enum Mode { Place, Edit, Move }

    private const float Grid = 0.5f;
    private static BuildUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private GameObject canvasGo;
    private RectTransform panel, tiles, tabRow;
    private string category;                  // the catalogue tab on show (Category)
    private Text title, info;
    private readonly List<Button> buttons = new List<Button>();
    private readonly List<Text> buttonLabels = new List<Text>();
    private readonly List<Action> actions = new List<Action>();

    private Mode mode;
    private string kind = "fence_wood";
    private int paint, yawTurn, selected = -1, chainId = -1, armedRemove = -1;
    private bool chain = true, busy, ghostValid;
    private GameObject ghost;
    private string ghostKey;
    private Transform player;
    private float ghostX, ghostZ, ghostYaw;
    private string site = "home";

    // touch editing
    private EditTouchSurface touch;
    private BuildGrid grid;
    private string selectedProp;              // a yard thing (YardProps) picked instead of a piece
    private bool holding;                     // a finger is carrying the ghost / the picked piece
    private bool hasSpot;                     // placing: you put the ghost somewhere with a finger
    private float spotX, spotZ;
    private float dragX, dragZ;               // where the carried piece is (house space)
    private Vector3 holdOffset;               // from the finger's ground point to the piece's middle

    private static string SiteTitle(string s) => s == "farm" ? "ก่อสร้างในแปลงเกษตร" : s == "home" ? "ก่อสร้างรอบบ้าน" : "ก่อสร้างในที่ดินของคุณ";
    private static string SiteWhere(string s) => s == "farm" ? "แปลงเกษตรของคุณ" : s == "home" ? "แปลงบ้านของคุณ" : "ที่ดินของคุณ";

    // How far from the site's middle you may wander while building there
    private float Reach(BuildClient client)
    {
        if (site == "home") return client.Current.radius;
        var b = client.SiteBounds(site);
        return b == null ? 0f : Mathf.Max(Mathf.Max(-b.x0, b.x1), Mathf.Max(-b.z0, b.z1));
    }

    // The site you're on: another site you hold whose ground you stand on (a little past its edge is
    // fine), else home when you're near it, else the nearest site within reach
    private string PickSite(BuildClient client)
    {
        string best = null;
        float bestD = float.MaxValue;
        foreach (var s in BuildClient.OtherSites)
        {
            var b = client.SiteBounds(s);
            var a = BuiltPieces.AnchorOf(s);
            if (b == null || a == null) continue;
            var l = a.InverseTransformPoint(player.position);
            if (l.x >= b.x0 - 2f && l.x <= b.x1 + 2f && l.z >= b.z0 - 3f && l.z <= b.z1 + 2f) return s;
        }
        var home = BuiltPieces.Anchor;
        if (home != null && Flat(player.position - home.position).magnitude <= client.Current.radius + 10f) return "home";
        foreach (var s in BuildClient.OtherSites)
        {
            var b = client.SiteBounds(s);
            var a = BuiltPieces.AnchorOf(s);
            if (b == null || a == null) continue;
            float d = Flat(player.position - a.position).magnitude;
            if (d < bestD && d <= Mathf.Max(b.x1, b.z1) + 10f) { best = s; bestD = d; }
        }
        return best;
    }

    public static bool IsOpen => instance != null && instance.canvasGo != null && instance.canvasGo.activeSelf;

    public static void Toggle()
    {
        if (instance == null) instance = new GameObject("BuildUI").AddComponent<BuildUI>();
        if (IsOpen) instance.Close();
        else instance.Open();
    }

    private void Awake() => DontDestroyOnLoad(gameObject);

    private bool opening;

    private async void Open()
    {
        var client = BuildClient.Instance;
        if (client == null || !client.IsOnline) { ScreenToast.Show("ต้องออนไลน์ก่อนจึงจะก่อสร้างได้", UiKit.Danger); return; }
        if (opening) return;
        opening = true;
        // the sites you hold may have changed since you came online (a farm lot, land from the bank)
        try { await client.RefreshAsync(); }
        catch (Exception) { }
        finally { opening = false; }
        if (this == null || IsOpen || client == null || client.Current == null) return;
        player = FindAnyObjectByType<PlayerController>()?.transform;
        if (player == null) return;
        var picked = PickSite(client);
        if (picked == null)
        {
            ScreenToast.Show(BuiltPieces.Anchor == null && client.SiteBounds("farm") == null
                ? "ก่อสร้างได้ที่บ้าน แปลงเกษตร หรือที่ดินของคุณ"
                : "เดินไปที่บ้าน แปลงเกษตร หรือที่ดินของคุณก่อน แล้วค่อยเปิดโหมดก่อสร้าง", UiKit.Danger);
            return;
        }
        site = picked;
        if (canvasGo == null) Build();
        canvasGo.SetActive(true);
        mode = Mode.Place;
        chainId = -1;
        hasSpot = false;
        Render();
    }

    private void Close()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        ClearGhost();
        BuiltPieces.Hide(-1);
        if (YardProps.Held != null) { YardProps.Held = null; YardProps.Reapply(); }
        holding = false;
        selected = -1;
        selectedProp = null;
        armedRemove = -1;
        if (grid != null) grid.Hide();
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ── Layout ─────────────────────────────────────────────────────────────────
    private void Build()
    {
        canvasGo = new GameObject("BuildCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        touch = EditTouchSurface.Make(canvasGo.transform);
        touch.Grab = Grab;
        touch.Move = DragTo;
        touch.Drop = Drop;
        touch.Tap = Tap;

        const float W = 900f, H = 420f;
        panel = UiKit.Panel("BuildPanel", canvasGo.transform, new Color(0.14f, 0.08f, 0.04f, 0.9f), Vector2.zero, new Vector2(W, H));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -96f);
        panel.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;

        title = UiKit.Label(panel, 28, "ก่อสร้างรอบบ้าน", new Vector2(-W * 0.5f + 170f, -30f), new Vector2(320f, 40f), UiKit.GoldText, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        info = UiKit.Label(panel, 22, "", new Vector2(110f, -30f), new Vector2(560f, 40f), UiKit.WoodInk, TextAnchor.MiddleRight);
        info.rectTransform.anchorMin = info.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        var close = UiKit.RoundButton(panel, "✕", new Vector2(W * 0.5f - 34f, -30f), new Vector2(48f, 48f), UiKit.Danger, Color.white, Close, 26, circle: true);
        ((RectTransform)close.transform).anchorMin = ((RectTransform)close.transform).anchorMax = new Vector2(0.5f, 1f);

        // the catalogue's tabs (รั้ว/ประตู, โรงรถ, อาคาร, ฟาร์ม, ตกแต่ง), then its picture tiles
        tabRow = new GameObject("Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
        tabRow.SetParent(panel, false);
        tabRow.anchorMin = tabRow.anchorMax = new Vector2(0.5f, 1f);
        tabRow.pivot = new Vector2(0.5f, 1f);
        tabRow.anchoredPosition = new Vector2(0f, -58f);
        tabRow.sizeDelta = new Vector2(W - 30f, 54f);

        // the catalogue: a row of picture tiles that scrolls sideways
        var view = UiKit.Rect("Catalogue", panel, new Color(0f, 0f, 0f, 0.25f));
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 1f);
        view.pivot = new Vector2(0.5f, 1f);
        view.anchoredPosition = new Vector2(0f, -118f);
        view.sizeDelta = new Vector2(W - 30f, 196f);
        view.gameObject.AddComponent<RectMask2D>();
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        tiles = new GameObject("Tiles", typeof(RectTransform)).GetComponent<RectTransform>();
        tiles.SetParent(view, false);
        tiles.anchorMin = tiles.anchorMax = new Vector2(0f, 0.5f);
        tiles.pivot = new Vector2(0f, 0.5f);
        tiles.sizeDelta = new Vector2(10f, 190f);
        scroll.content = tiles;

        // up to six buttons along the bottom
        for (int i = 0; i < 6; i++)
        {
            int index = i;
            actions.Add(null);
            var b = UiKit.RoundButton(panel, "", Vector2.zero, new Vector2(138f, 64f), UiKit.Cream, UiKit.Ink, () => actions[index]?.Invoke(), 24);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(-W * 0.5f + 82f + i * 147f, 44f);
            buttons.Add(b);
            buttonLabels.Add(b.GetComponentInChildren<Text>());
        }
    }

    private void SetButtons(params (string text, Color color, Action act)[] list)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool on = i < list.Length;
            buttons[i].gameObject.SetActive(on);
            if (!on) continue;
            buttonLabels[i].text = list[i].text;
            buttons[i].GetComponent<Image>().color = list[i].color;
            actions[i] = list[i].act;
        }
    }

    private void Render()
    {
        var client = BuildClient.Instance;
        if (client?.Current == null || canvasGo == null) return;
        var state = client.Current;
        tiles.parent.gameObject.SetActive(mode == Mode.Place);
        tabRow.gameObject.SetActive(mode == Mode.Place);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, mode == Mode.Place ? 420f : 170f);

        if (mode == Mode.Place)
        {
            title.text = SiteTitle(site);
            // tabs: only the groups there is something in
            var groups = new List<string>();
            foreach (var k in state.catalog) { var g = Category(k); if (!groups.Contains(g)) groups.Add(g); }
            if (category == null || !groups.Contains(category)) category = Category(client.GetKind(kind)) ?? (groups.Count > 0 ? groups[0] : null);
            UiKit.Clear(tabRow);
            float tabW = Mathf.Min(170f, (tabRow.sizeDelta.x - 8f * (groups.Count - 1)) / Mathf.Max(1, groups.Count));
            float tx = -(groups.Count * tabW + (groups.Count - 1) * 8f) * 0.5f + tabW * 0.5f;
            foreach (var g in groups)
            {
                var name = g;
                bool on = name == category;
                var tab = UiKit.RoundButton(tabRow, name, new Vector2(tx, -27f), new Vector2(tabW, 50f), on ? UiKit.Gold : new Color(0.35f, 0.22f, 0.12f), on ? UiKit.GoldInk : UiKit.Cream,
                    () => { category = name; Render(); }, 22);
                var trt = (RectTransform)tab.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
                tx += tabW + 8f;
            }

            UiKit.Clear(tiles);
            float x = 6f;
            foreach (var k in state.catalog)
            {
                if (Category(k) != category) continue;
                var id = k.id;
                bool picked = id == kind;
                var tile = UiKit.RoundButton(tiles, "", Vector2.zero, new Vector2(160f, 182f), picked ? UiKit.Gold : UiKit.Cream, UiKit.Ink, () => { kind = id; chainId = -1; Render(); }, 20);
                var rt = (RectTransform)tile.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                // the picture
                var pic = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
                var prt = (RectTransform)pic.transform;
                prt.SetParent(tile.transform, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
                prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -8f);
                prt.sizeDelta = new Vector2(112f, 112f);
                var raw = pic.GetComponent<RawImage>();
                raw.texture = BuildThumbs.Get(id);
                raw.raycastTarget = false;
                var label = tile.GetComponentInChildren<Text>();
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 0f);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.anchoredPosition = new Vector2(0f, 4f);
                lrt.sizeDelta = new Vector2(-8f, 60f);
                label.text = $"{k.name}\n{k.price:N0} บาท";
                label.lineSpacing = 0.85f;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = 20;
                x += 168f;
            }
            tiles.sizeDelta = new Vector2(x + 6f, 190f);
            tiles.anchoredPosition = new Vector2(0f, tiles.anchoredPosition.y);
            var kd = client.GetKind(kind);
            bool line = kd != null && kd.group != "build";
            int count = state.pieces?.Length ?? 0;
            info.text = site != "home" ? $"สร้างแล้ว {count}/{state.max} ชิ้น · วางได้ใน{SiteWhere(site)}" : client.OnLot ? $"สร้างแล้ว {count}/{state.max} ชิ้น · วางได้ในแปลงบ้านของคุณ" : $"สร้างแล้ว {count}/{state.max} ชิ้น · ห่างบ้านไม่เกิน {state.radius:0} ม.";
            SetButtons(
                ($"วาง · {kd?.price ?? 0:N0} บาท", UiKit.OkTint, (Action)PlaceNow),
                ("หมุน ⟳", UiKit.Cream, () => { yawTurn = (yawTurn + 45) % 360; }),
                (line ? (chain ? "ต่อแนว: เปิด" : "ต่อแนว: ปิด") : "วางหน้าตัว", UiKit.Cream, () => { chain = !chain; chainId = -1; Render(); }),
                ($"สี: {ColorName(paint)}", UiKit.Cream, () => { paint = (paint + 1) % Mathf.Max(1, state.colors?.Length ?? 1); Render(); }),
                ("แก้ไข / ย้าย", UiKit.Pill, () => { mode = Mode.Edit; ClearGhost(); SelectNearest(-1, null); Render(); }));
        }
        else if (mode == Mode.Edit && selectedProp != null)
        {
            var item = YardProps.Get(selectedProp);
            title.text = "จัดของในบ้าน";
            info.text = item == null ? "แตะของที่จะย้าย" : $"{item.label} · ลากเพื่อย้าย";
            SetButtons(
                ("หมุน ⟳", UiKit.Cream, () => { if (item != null) SaveProp(item, item.t.position, item.t.eulerAngles.y + 45f); }),
                ("คืนที่เดิม", UiKit.Cream, () => { if (item != null) SaveProp(item, item.home, item.homeYaw); }),
                ("ถัดไป", UiKit.Cream, () => { SelectNearest(-1, selectedProp); Render(); }),
                ("กลับไปสร้าง", UiKit.Pill, () => { mode = Mode.Place; selectedProp = null; ClearGhost(); BuiltPieces.Hide(-1); Render(); }));
        }
        else if (mode == Mode.Edit)
        {
            var p = client.GetPiece(selected);
            var kd = p != null ? client.GetKind(p.kind) : null;
            title.text = "แก้ไขสิ่งก่อสร้าง";
            info.text = p == null ? "แตะชิ้นที่จะแก้ · ลากเพื่อย้าย" : $"{kd?.name ?? p.kind} · สี{ColorName(p.color)} · ลากเพื่อย้าย";
            bool armed = p != null && armedRemove == p.id;
            SetButtons(
                ("ย้าย", UiKit.Cream, () => { if (p != null) { mode = Mode.Move; yawTurn = 0; BuiltPieces.Hide(p.id); Render(); } }),
                ("หมุน ⟳", UiKit.Cream, () => { if (p != null) { p.yaw = (p.yaw + 45f) % 360f; Send(client.EditAsync(p)); } }),
                ("เปลี่ยนสี", UiKit.Cream, () => { if (p != null) { p.color = (p.color + 1) % Mathf.Max(1, state.colors.Length); Send(client.EditAsync(p)); } }),
                (armed ? $"ยืนยันรื้อ (คืน {(kd?.price ?? 0) / 2:N0})" : "รื้อถอน", armed ? UiKit.DangerTint : UiKit.Cream, () =>
                {
                    if (p == null) return;
                    if (armedRemove != p.id) { armedRemove = p.id; Render(); return; }
                    armedRemove = -1;
                    Send(client.RemoveAsync(p.id), () => SelectNearest(-1, null));
                }),
                ("ถัดไป", UiKit.Cream, () => { SelectNearest(selected, null); Render(); }),
                ("กลับไปสร้าง", UiKit.Pill, () => { mode = Mode.Place; ClearGhost(); BuiltPieces.Hide(-1); Render(); }));
        }
        else // Move
        {
            var p = client.GetPiece(selected);
            title.text = "ย้ายสิ่งก่อสร้าง";
            info.text = "เดินไปที่ใหม่ แล้วกด วางที่นี่";
            SetButtons(
                ("วางที่นี่", UiKit.OkTint, () =>
                {
                    if (p == null || !ghostValid) { ScreenToast.Show(site == "home" ? "ไกลบ้านเกินไป" : $"ต้องวางใน{SiteWhere(site)}", UiKit.Danger); return; }
                    p.x = ghostX; p.z = ghostZ; p.yaw = ghostYaw;
                    Send(client.EditAsync(p), () => { mode = Mode.Edit; ClearGhost(); BuiltPieces.Hide(-1); Render(); });
                }),
                ("หมุน ⟳", UiKit.Cream, () => { yawTurn = (yawTurn + 45) % 360; }),
                ("ยกเลิก", UiKit.Pill, () => { mode = Mode.Edit; ClearGhost(); BuiltPieces.Hide(-1); Render(); }));
        }
    }

    // Which catalogue tab a piece is under
    private static string Category(BuildClient.Kind k)
    {
        if (k == null) return null;
        if (k.group == "fence" || k.group == "gate") return "รั้ว / ประตู";
        switch (k.id)
        {
            case "carport": case "garage": return "โรงรถ";
            case "cowshed": case "watertower": case "well": return "ฟาร์ม";
            case "planter": return "ตกแต่ง";
            default: return "อาคาร";
        }
    }

    private string ColorName(int i)
    {
        var colors = BuildClient.Instance?.Current?.colors;
        return colors != null && i >= 0 && i < colors.Length ? colors[i] : "";
    }

    // ── Every frame: the ghost, the selection ───────────────────────────────────
    private void Update()
    {
        if (!IsOpen) return;
        var client = BuildClient.Instance;
        var house = BuiltPieces.AnchorOf(site);
        if (client?.Current == null || house == null || player == null)
        {
            Close();
            return;
        }
        if (Flat(player.position - house.position).magnitude > Reach(client) + 14f)
        {
            ScreenToast.Show(site == "home" ? "ออกจากโหมดก่อสร้างแล้ว (ไกลบ้าน)" : "ออกจากโหมดก่อสร้างแล้ว (ออกนอกพื้นที่)", UiKit.Muted);
            Close();
            return;
        }

        ShowGrid(house, client);
        if (mode == Mode.Place) Follow(house, client, kind, paint, true);
        else if (mode == Mode.Move)
        {
            var p = client.GetPiece(selected);
            if (p != null) Follow(house, client, p.kind, p.color, false);
        }
        else if (selectedProp != null) // Edit: a ring under the yard thing you picked
        {
            var item = YardProps.Get(selectedProp);
            if (item == null) { ClearGhost(); return; }
            if (ghost == null || ghostKey != "prop" + item.name)
            {
                ClearGhost();
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(ghost.GetComponent<Collider>());
                ghost.GetComponent<Renderer>().sharedMaterial = BuildKit.FlatMat(new Color(1f, 0.85f, 0.2f));
                ghost.name = "BuildGhost";
                ghostKey = "prop" + item.name;
            }
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f;
            ghost.transform.position = item.t.position + Vector3.up * 0.03f;
            ghost.transform.localScale = new Vector3(item.size * 1.3f * pulse, 0.01f, item.size * 1.3f * pulse);
        }
        else if (holding) // Edit: carrying a piece with a finger
        {
            var p = client.GetPiece(selected);
            if (p == null) { ClearGhost(); return; }
            bool ok = client.Inside(site, dragX, dragZ);
            ShowGhost(p.kind, ok ? new Color(0.3f, 0.95f, 0.4f) : new Color(0.95f, 0.25f, 0.2f), $"drag{p.id}/{ok}");
            BuiltPieces.Place(ghost.transform, house, dragX, dragZ, p.yaw);
            ghost.transform.position += Vector3.up * 0.01f;
            ghost.transform.localScale = Vector3.one;
        }
        else // Edit: outline the selected piece
        {
            var p = client.GetPiece(selected);
            if (p == null) { ClearGhost(); return; }
            ShowGhost(p.kind, new Color(1f, 0.85f, 0.2f), "sel" + p.id);
            var shownPiece = BuiltPieces.Shown(p.id);
            if (shownPiece != null)
            {
                ghost.transform.SetPositionAndRotation(shownPiece.transform.position + Vector3.up * 0.02f, shownPiece.transform.rotation);
                float pulse = 1.03f + Mathf.Sin(Time.unscaledTime * 6f) * 0.015f;
                ghost.transform.localScale = new Vector3(pulse, pulse, pulse);
            }
        }
    }

    // The ghost in front of the player (or carrying on the last fence), snapped to the grid
    private void Follow(Transform house, BuildClient client, string pieceKind, int pieceColor, bool allowChain)
    {
        var kd = client.GetKind(pieceKind);
        float len = kd != null ? kd.length : 2f;
        bool line = kd != null && kd.group != "build";
        var local = house.InverseTransformPoint(player.position);
        float facing = player.eulerAngles.y - house.eulerAngles.y;
        float x, z, yaw;
        var last = allowChain && chain && line ? client.GetPiece(chainId) : null;
        if (last != null)
        {
            var lastKind = client.GetKind(last.kind);
            float lastLen = lastKind != null ? lastKind.length : 2f;
            yaw = Mathf.Repeat(last.yaw + yawTurn, 360f);
            var c = new Vector3(last.x, 0f, last.z) + Quaternion.Euler(0f, last.yaw, 0f) * Vector3.right * (lastLen * 0.5f)
                    + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (len * 0.5f);
            x = c.x; z = c.z;
        }
        else if (allowChain && hasSpot) // put down with a finger
        {
            yaw = Mathf.Repeat(yawTurn, 360f);
            x = spotX; z = spotZ;
        }
        else
        {
            yaw = Mathf.Repeat(Mathf.Round(facing / 45f) * 45f + yawTurn, 360f);
            float reach = line ? 1.6f : 1.2f + Mathf.Max(len, kd != null && kd.id == "carport" ? 5.6f : len) * 0.5f;
            var c = local + Quaternion.Euler(0f, facing, 0f) * Vector3.forward * reach;
            x = Mathf.Round(c.x / Grid) * Grid;
            z = Mathf.Round(c.z / Grid) * Grid;
        }
        ghostValid = client.Inside(site, x, z);
        ghostX = x; ghostZ = z; ghostYaw = yaw;
        ShowGhost(pieceKind, ghostValid ? new Color(0.3f, 0.95f, 0.4f) : new Color(0.95f, 0.25f, 0.2f), $"{pieceKind}/{ghostValid}");
        BuiltPieces.Place(ghost.transform, house, x, z, yaw);
        ghost.transform.position += Vector3.up * 0.01f;
        ghost.transform.localScale = Vector3.one;
    }

    private void ShowGhost(string pieceKind, Color tint, string key)
    {
        if (ghost != null && ghostKey == key) return;
        ClearGhost();
        ghost = BuildKit.Make(pieceKind, 0, null, tint);
        ghost.name = "BuildGhost";
        ghostKey = key;
    }

    private void ClearGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostKey = null;
    }

    // The next piece or yard thing near you, after the one picked now (cycling with ถัดไป)
    private void SelectNearest(int after, string afterProp)
    {
        var client = BuildClient.Instance;
        var house = BuiltPieces.AnchorOf(site);
        selected = -1;
        selectedProp = null;
        armedRemove = -1;
        if (client?.Current?.pieces == null || house == null || player == null) return;
        var local = house.InverseTransformPoint(player.position);
        var near = new List<(float d, int id, string prop)>();
        foreach (var p in client.Current.pieces)
        {
            if (BuildClient.SiteOf(p) != site) continue;
            float d = new Vector2(p.x - local.x, p.z - local.z).magnitude;
            if (d < 10f) near.Add((d, p.id, null));
        }
        if (site == "home")
            foreach (var item in YardProps.All)
            {
                if (item.t == null) continue;
                float d = Flat(item.t.position - player.position).magnitude;
                if (d < 10f) near.Add((d, -1, item.name));
            }
        if (near.Count == 0) return;
        near.Sort((a, b) => a.d.CompareTo(b.d));
        int at = afterProp != null ? near.FindIndex(n => n.prop == afterProp) : near.FindIndex(n => n.prop == null && n.id == after);
        var next = near[(at + 1) % near.Count];
        selected = next.id;
        selectedProp = next.prop;
    }

    // ── Touch: pick, drag, drop ─────────────────────────────────────────────────
    private bool FingerOnGround(Vector2 screen, Transform house, out Vector3 local)
    {
        local = default;
        if (house == null || !EditTouchSurface.GroundPoint(screen, house.position.y, out var world)) return false;
        local = house.InverseTransformPoint(world);
        local.y = 0f;
        return true;
    }

    private static float Snap(float v) => Mathf.Round(v / Grid) * Grid;

    // What is under the finger: a piece of this site (by its footprint) or a yard thing; (-1, null): nothing
    private (int id, string prop) PickAt(Vector3 local, Transform house)
    {
        var client = BuildClient.Instance;
        float best = float.MaxValue;
        (int, string) found = (-1, null);
        foreach (var p in client.Current.pieces)
        {
            if (BuildClient.SiteOf(p) != site) continue;
            var kd = client.GetKind(p.kind);
            float len = kd != null ? kd.length : 2f;
            // into the piece's own axes: along it (x) and across it (z)
            var rel = Quaternion.Euler(0f, -p.yaw, 0f) * new Vector3(local.x - p.x, 0f, local.z - p.z);
            float halfX = len * 0.5f + 0.4f, halfZ = (kd != null && kd.group == "build" ? Mathf.Max(1f, len * 0.6f) : 0.3f) + 0.4f;
            if (Mathf.Abs(rel.x) > halfX || Mathf.Abs(rel.z) > halfZ) continue;
            float d = rel.sqrMagnitude;
            if (d < best) { best = d; found = (p.id, null); }
        }
        if (site == "home")
            foreach (var item in YardProps.All)
            {
                if (item.t == null) continue;
                var l = house.InverseTransformPoint(item.t.position);
                float d = new Vector2(l.x - local.x, l.z - local.z).sqrMagnitude;
                float r = item.size * 0.6f + 0.4f;
                if (d <= r * r && d < best) { best = d; found = (-1, item.name); }
            }
        return found;
    }

    private bool Grab(Vector2 screen)
    {
        var client = BuildClient.Instance;
        var house = BuiltPieces.AnchorOf(site);
        if (busy || client?.Current == null || !FingerOnGround(screen, house, out var local)) return false;
        if (mode == Mode.Place)
        {
            if (ghost == null) return false;
            var kd = client.GetKind(kind);
            float len = kd != null ? kd.length : 2f;
            if (new Vector2(ghostX - local.x, ghostZ - local.z).magnitude > Mathf.Max(1.2f, len * 0.6f)) return false;
            holding = true;
            holdOffset = new Vector3(ghostX - local.x, 0f, ghostZ - local.z);
            return true;
        }
        if (mode != Mode.Edit) return false;
        var (id, prop) = PickAt(local, house);
        if (id < 0 && prop == null) return false;
        armedRemove = -1;
        if (prop != null)
        {
            selected = -1;
            selectedProp = prop;
            var item = YardProps.Get(prop);
            if (item == null) return false;
            var l = house.InverseTransformPoint(item.t.position);
            holdOffset = new Vector3(l.x - local.x, 0f, l.z - local.z);
        }
        else
        {
            selected = id;
            selectedProp = null;
            var p = client.GetPiece(id);
            holdOffset = new Vector3(p.x - local.x, 0f, p.z - local.z);
            dragX = p.x; dragZ = p.z;
        }
        holding = true;
        Render();
        return true;
    }

    private void DragTo(Vector2 screen)
    {
        var house = BuiltPieces.AnchorOf(site);
        if (!holding || !FingerOnGround(screen, house, out var local)) return;
        float x = Snap(local.x + holdOffset.x), z = Snap(local.z + holdOffset.z);
        if (mode == Mode.Place)
        {
            hasSpot = true;
            chainId = -1; // a fence line starts again from where you dragged it
            spotX = x; spotZ = z;
            return;
        }
        if (selectedProp != null)
        {
            var item = YardProps.Get(selectedProp);
            if (item == null) return;
            YardProps.Held = item.name;
            var world = house.TransformPoint(new Vector3(x, 0f, z));
            YardProps.Put(item, world.x - item.home.x, world.z - item.home.z, item.t.eulerAngles.y);
            return;
        }
        BuiltPieces.Hide(selected);
        dragX = x; dragZ = z;
    }

    private void Drop(Vector2 screen, bool moved)
    {
        if (!holding) return;
        holding = false;
        var client = BuildClient.Instance;
        if (mode == Mode.Place || !moved || client == null)
        {
            if (YardProps.Held != null) { YardProps.Held = null; YardProps.Reapply(); }
            BuiltPieces.Hide(-1);
            Render();
            return;
        }
        if (selectedProp != null)
        {
            var item = YardProps.Get(selectedProp);
            if (item != null) SaveProp(item, item.t.position, item.t.eulerAngles.y);
            return;
        }
        var p = client.GetPiece(selected);
        if (p == null) { BuiltPieces.Hide(-1); return; }
        if (!client.Inside(site, dragX, dragZ))
        {
            ScreenToast.Show(site == "home" ? (client.OnLot ? "ต้องวางในแปลงบ้านของคุณ" : "ไกลบ้านเกินไป") : $"ต้องวางใน{SiteWhere(site)}", UiKit.Danger);
            BuiltPieces.Hide(-1);
            return;
        }
        p.x = dragX; p.z = dragZ;
        Send(client.EditAsync(p), null, () => BuiltPieces.Hide(-1));
    }

    private void Tap(Vector2 screen)
    {
        var client = BuildClient.Instance;
        var house = BuiltPieces.AnchorOf(site);
        if (busy || client?.Current == null || !FingerOnGround(screen, house, out var local)) return;
        if (mode == Mode.Place)
        {
            // put the ghost where you tapped (a fence line starts again from there)
            hasSpot = true;
            chainId = -1;
            spotX = Snap(local.x); spotZ = Snap(local.z);
            return;
        }
        if (mode != Mode.Edit) return;
        var (id, prop) = PickAt(local, house);
        if (id < 0 && prop == null) return;
        selected = id;
        selectedProp = prop;
        armedRemove = -1;
        Render();
    }

    // Save where a yard thing stands now (world position and yaw)
    private void SaveProp(YardProps.Item item, Vector3 world, float yaw)
    {
        var client = BuildClient.Instance;
        if (client == null || item == null || busy) return;
        YardProps.Held = item.name;
        YardProps.Put(item, world.x - item.home.x, world.z - item.home.z, yaw);
        Send(client.MovePropAsync(item.name, world.x - item.home.x, world.z - item.home.z, Mathf.Repeat(yaw, 360f)), null, () =>
        {
            YardProps.Held = null;
            YardProps.Reapply();
        });
    }

    // The grid over the ground you may build on
    private void ShowGrid(Transform house, BuildClient client)
    {
        if (grid == null) grid = BuildGrid.Make("BuildGrid_ตาราง");
        float x0, x1, z0, z1;
        if (site == "home")
        {
            if (client.OnLot) { var b = client.Current.bounds; x0 = b.x0; x1 = b.x1; z0 = b.z0; z1 = b.z1; }
            else { float r = Mathf.Min(client.Current.radius, 18f); x0 = z0 = -r; x1 = z1 = r; }
        }
        else
        {
            var b = client.SiteBounds(site);
            if (b == null) { grid.Hide(); return; }
            x0 = b.x0; x1 = b.x1; z0 = b.z0; z1 = b.z1;
        }
        grid.Show(house, x0, x1, z0, z1, BuiltPieces.GroundAt(house.position, house.position.y));
    }

    // ── Server calls ────────────────────────────────────────────────────────────
    private void PlaceNow()
    {
        var client = BuildClient.Instance;
        if (busy || client == null) return;
        if (!ghostValid)
        {
            ScreenToast.Show(site != "home" ? $"ต้องวางใน{SiteWhere(site)}" : client.OnLot ? "ต้องวางในแปลงบ้านของคุณ" : $"ไกลบ้านเกินไป · สร้างได้ห่างบ้านไม่เกิน {client.Current.radius:0} เมตร", UiKit.Danger);
            return;
        }
        Send(client.PlaceAsync(kind, ghostX, ghostZ, ghostYaw, paint, site), () =>
        {
            var kd = client.GetKind(kind);
            if (chain && kd != null && kd.group != "build") { chainId = client.Current.placed; yawTurn = 0; }
        });
    }

    private async void Send(System.Threading.Tasks.Task<BuildClient.State> call, Action after = null, Action always = null)
    {
        busy = true;
        try
        {
            var state = await call;
            if (!string.IsNullOrEmpty(state.done)) ScreenToast.Show(state.done, UiKit.PayGreen);
            after?.Invoke();
        }
        catch (Exception e)
        {
            string m = e.Message;
            ScreenToast.Show(m.Contains("not enough baht") ? "เงินไม่พอ"
                : m.Contains("too far") ? "ไกลบ้านเกินไป"
                : m.Contains("outside lot") ? $"ต้องวางใน{SiteWhere(site)}"
                : m.Contains("not your site") ? "พื้นที่นี้ไม่ใช่ของคุณ"
                : m.Contains("too many") ? "สร้างได้สูงสุดแล้ว · รื้อบางชิ้นก่อน"
                : "ทำรายการไม่สำเร็จ ลองใหม่", UiKit.Danger);
            try { await BuildClient.Instance.RefreshAsync(); } catch (Exception) { }
        }
        finally
        {
            busy = false;
            always?.Invoke();
            if (IsOpen) Render();
        }
    }
}
