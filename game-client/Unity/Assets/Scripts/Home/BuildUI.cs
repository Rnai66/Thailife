using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// โหมดก่อสร้าง: build round your home. A strip at the top of the screen (clear of the joystick and the
// action buttons, so you can walk while you build) holds the catalogue, and the buttons below it.
//   Building: pick a piece; its ghost stands in front of you (green: OK, red: too far from the house)
//     and follows as you walk. หมุน turns it, สี picks the paint, วาง builds it. With ต่อแนว on, the
//     next fence carries on from the end of the last one (turn it to go round a corner).
//   Editing (แก้ไข): the piece nearest you is picked out in yellow (ถัดไป for the next). ย้าย lifts it
//     to follow you until วางที่นี่; หมุน and สี change it at once; รื้อถอน pulls it down (half back).
public class BuildUI : MonoBehaviour
{
    private enum Mode { Place, Edit, Move }

    private const float Grid = 0.25f;
    private static BuildUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private GameObject canvasGo;
    private RectTransform panel, tiles;
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

    public static bool IsOpen => instance != null && instance.canvasGo != null && instance.canvasGo.activeSelf;

    public static void Toggle()
    {
        if (instance == null) instance = new GameObject("BuildUI").AddComponent<BuildUI>();
        if (IsOpen) instance.Close();
        else instance.Open();
    }

    private void Awake() => DontDestroyOnLoad(gameObject);

    private void Open()
    {
        var client = BuildClient.Instance;
        if (client == null || !client.IsOnline) { ScreenToast.Show("ต้องออนไลน์ก่อนจึงจะก่อสร้างได้", UiKit.Danger); return; }
        var house = BuiltPieces.Anchor;
        player = FindAnyObjectByType<PlayerController>()?.transform;
        if (house == null || player == null) { ScreenToast.Show("ก่อสร้างได้ที่บ้านของคุณ · เมนูบ้าน → กลับบ้าน", UiKit.Danger); return; }
        if (Flat(player.position - house.position).magnitude > client.Current.radius + 10f)
        {
            ScreenToast.Show("เดินไปใกล้บ้านของคุณก่อน แล้วค่อยเปิดโหมดก่อสร้าง", UiKit.Danger);
            return;
        }
        if (canvasGo == null) Build();
        canvasGo.SetActive(true);
        mode = Mode.Place;
        chainId = -1;
        Render();
    }

    private void Close()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        ClearGhost();
        BuiltPieces.Hide(-1);
        selected = -1;
        armedRemove = -1;
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

        const float W = 900f, H = 262f;
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

        // the catalogue: a row of tiles that scrolls sideways
        var view = UiKit.Rect("Catalogue", panel, new Color(0f, 0f, 0f, 0.25f));
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 1f);
        view.pivot = new Vector2(0.5f, 1f);
        view.anchoredPosition = new Vector2(0f, -58f);
        view.sizeDelta = new Vector2(W - 30f, 96f);
        view.gameObject.AddComponent<RectMask2D>();
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        tiles = new GameObject("Tiles", typeof(RectTransform)).GetComponent<RectTransform>();
        tiles.SetParent(view, false);
        tiles.anchorMin = tiles.anchorMax = new Vector2(0f, 0.5f);
        tiles.pivot = new Vector2(0f, 0.5f);
        tiles.sizeDelta = new Vector2(10f, 90f);
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

        if (mode == Mode.Place)
        {
            title.text = "ก่อสร้างรอบบ้าน";
            UiKit.Clear(tiles);
            float x = 6f;
            foreach (var k in state.catalog)
            {
                var id = k.id;
                bool picked = id == kind;
                var tile = UiKit.RoundButton(tiles, "", Vector2.zero, new Vector2(164f, 84f), picked ? UiKit.Gold : UiKit.Cream, UiKit.Ink, () => { kind = id; chainId = -1; Render(); }, 22);
                var rt = (RectTransform)tile.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                var label = tile.GetComponentInChildren<Text>();
                label.text = $"{k.name}\n{k.price:N0} บาท";
                label.lineSpacing = 0.9f;
                x += 172f;
            }
            tiles.sizeDelta = new Vector2(x + 6f, 90f);
            var kd = client.GetKind(kind);
            bool line = kd != null && kd.group != "build";
            int count = state.pieces?.Length ?? 0;
            info.text = $"สร้างแล้ว {count}/{state.max} ชิ้น · ห่างบ้านไม่เกิน {state.radius:0} ม.";
            SetButtons(
                ($"วาง · {kd?.price ?? 0:N0} บาท", UiKit.OkTint, (Action)PlaceNow),
                ("หมุน ⟳", UiKit.Cream, () => { yawTurn = (yawTurn + 45) % 360; }),
                (line ? (chain ? "ต่อแนว: เปิด" : "ต่อแนว: ปิด") : "วางหน้าตัว", UiKit.Cream, () => { chain = !chain; chainId = -1; Render(); }),
                ($"สี: {ColorName(paint)}", UiKit.Cream, () => { paint = (paint + 1) % Mathf.Max(1, state.colors?.Length ?? 1); Render(); }),
                ("แก้ไข", UiKit.Pill, () => { mode = Mode.Edit; ClearGhost(); SelectNearest(-1); Render(); }));
        }
        else if (mode == Mode.Edit)
        {
            var p = client.GetPiece(selected);
            var kd = p != null ? client.GetKind(p.kind) : null;
            title.text = "แก้ไขสิ่งก่อสร้าง";
            info.text = p == null ? "ยังไม่มีอะไรใกล้ตัว · เดินไปใกล้ชิ้นที่จะแก้" : $"{kd?.name ?? p.kind} · สี{ColorName(p.color)}";
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
                    Send(client.RemoveAsync(p.id), () => SelectNearest(-1));
                }),
                ("ถัดไป", UiKit.Cream, () => { SelectNearest(selected); Render(); }),
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
                    if (p == null || !ghostValid) { ScreenToast.Show("ไกลบ้านเกินไป", UiKit.Danger); return; }
                    p.x = ghostX; p.z = ghostZ; p.yaw = ghostYaw;
                    Send(client.EditAsync(p), () => { mode = Mode.Edit; ClearGhost(); BuiltPieces.Hide(-1); Render(); });
                }),
                ("หมุน ⟳", UiKit.Cream, () => { yawTurn = (yawTurn + 45) % 360; }),
                ("ยกเลิก", UiKit.Pill, () => { mode = Mode.Edit; ClearGhost(); BuiltPieces.Hide(-1); Render(); }));
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
        var house = BuiltPieces.Anchor;
        if (client?.Current == null || house == null || player == null)
        {
            Close();
            return;
        }
        if (Flat(player.position - house.position).magnitude > client.Current.radius + 14f)
        {
            ScreenToast.Show("ออกจากโหมดก่อสร้างแล้ว (ไกลบ้าน)", UiKit.Muted);
            Close();
            return;
        }

        if (mode == Mode.Place) Follow(house, client, kind, paint, true);
        else if (mode == Mode.Move)
        {
            var p = client.GetPiece(selected);
            if (p != null) Follow(house, client, p.kind, p.color, false);
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
        else
        {
            yaw = Mathf.Repeat(Mathf.Round(facing / 45f) * 45f + yawTurn, 360f);
            float reach = line ? 1.6f : 1.2f + Mathf.Max(len, kd != null && kd.id == "carport" ? 5.6f : len) * 0.5f;
            var c = local + Quaternion.Euler(0f, facing, 0f) * Vector3.forward * reach;
            x = Mathf.Round(c.x / Grid) * Grid;
            z = Mathf.Round(c.z / Grid) * Grid;
        }
        float radius = client.Current.radius;
        ghostValid = Mathf.Abs(x) <= radius && Mathf.Abs(z) <= radius;
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

    private void SelectNearest(int after)
    {
        var client = BuildClient.Instance;
        var house = BuiltPieces.Anchor;
        selected = -1;
        armedRemove = -1;
        if (client?.Current?.pieces == null || house == null || player == null) return;
        var local = house.InverseTransformPoint(player.position);
        var near = new List<(float d, int id)>();
        foreach (var p in client.Current.pieces)
        {
            float d = new Vector2(p.x - local.x, p.z - local.z).magnitude;
            if (d < 10f) near.Add((d, p.id));
        }
        if (near.Count == 0) return;
        near.Sort((a, b) => a.d.CompareTo(b.d));
        int at = near.FindIndex(n => n.id == after);
        selected = near[(at + 1) % near.Count].id;
    }

    // ── Server calls ────────────────────────────────────────────────────────────
    private void PlaceNow()
    {
        var client = BuildClient.Instance;
        if (busy || client == null) return;
        if (!ghostValid) { ScreenToast.Show($"ไกลบ้านเกินไป · สร้างได้ห่างบ้านไม่เกิน {client.Current.radius:0} เมตร", UiKit.Danger); return; }
        Send(client.PlaceAsync(kind, ghostX, ghostZ, ghostYaw, paint), () =>
        {
            var kd = client.GetKind(kind);
            if (chain && kd != null && kd.group != "build") { chainId = client.Current.placed; yawTurn = 0; }
        });
    }

    private async void Send(System.Threading.Tasks.Task<BuildClient.State> call, Action after = null)
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
                : m.Contains("too many") ? "สร้างได้สูงสุดแล้ว · รื้อบางชิ้นก่อน"
                : "ทำรายการไม่สำเร็จ ลองใหม่", UiKit.Danger);
            try { await BuildClient.Instance.RefreshAsync(); } catch (Exception) { }
        }
        finally
        {
            busy = false;
            if (IsOpen) Render();
        }
    }
}
