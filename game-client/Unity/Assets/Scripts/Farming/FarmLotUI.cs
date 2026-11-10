using System;
using UnityEngine;
using UnityEngine.UI;

// Arrange your own farm lot (แปลงเกษตร, farmlot.lua) from the sign at its front: the four corners as a
// little plan (the road at the bottom) — tap one zone, then another to swap them, and save (layout_fee)
// — and the fence on each side: tap a fence to build it (paid once; "ไม่มีรั้ว" takes it down).
// Someone living in town (เมือง / กรุงเทพฯ) also picks which หมู่ their farm is in (free move, everything
// comes along); a village resident's farm always goes with their house.
public class FarmLotUI : MonoBehaviour
{
    private static FarmLotUI instance;

    private static readonly string[] SideNames = { "ด้านหน้า", "ด้านหลัง", "ด้านซ้าย", "ด้านขวา" };
    // plan cells: index = slot (0 front left, 1 front right, 2 back left, 3 back right)
    private static readonly Vector2[] CellAt = { new Vector2(-135f, -88f), new Vector2(135f, -88f), new Vector2(-135f, 88f), new Vector2(135f, 88f) };

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText, saveText;
    private readonly Button[] cells = new Button[4];
    private readonly Text[] cellText = new Text[4];
    private readonly Button[,] fenceButtons = new Button[4, 5];
    private Button save, moveButton;
    private GameObject mooRow;
    private Text mooText;
    private int mooPick;
    // the 12 หมู่ (farmlot.lua VILLAGE_MOO_NAMES)
    private static readonly string[] MooNames = { "บ้านโคกสะอาด", "บ้านหนองแวง", "บ้านโนนสูง", "บ้านดอนแดง", "บ้านนาดี", "บ้านหนองไผ่",
        "บ้านโพนทอง", "บ้านสร้างแก้ว", "บ้านท่าลาด", "บ้านหัวช้าง", "บ้านนาคำ", "บ้านดงเย็น" };
    private Coroutine toast;
    private string[] pending;
    private int picked = -1;
    private bool busy;

    public static void Show()
    {
        if (instance == null) instance = new GameObject("FarmLotUI").AddComponent<FarmLotUI>();
        instance.backdrop.SetActive(true);
        instance.pending = null;
        instance.picked = -1;
        instance.mooPick = 0;
        instance.Render();
        if (FarmLots.Current != null) _ = FarmLots.Current.Refresh();
        else instance.FetchMine(); // opened away from the village (the move-house window in town)
    }

    private async void FetchMine()
    {
        try { await FarmLots.FetchMineAsync(); }
        catch (Exception e) { Notify(FarmLots.Explain(e), UiKit.Danger); }
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        bool portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "FarmLotCanvas", UiKit.WindowSize(new Vector2(1500f, 860f), 1300f), 55, "icon:farm",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "แปลงเกษตรของฉัน · โคกหนองนา";

        // the plan: four corners, the road along the bottom
        var plan = UiKit.Container(body, portrait ? new Vector2(0f, 330f) : new Vector2(-380f, 60f));
        UiKit.Label(plan, 28, "จัดโซน · แตะโซนหนึ่ง แล้วแตะอีกโซนเพื่อสลับกัน", new Vector2(0f, 230f), new Vector2(620f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            cells[i] = UiKit.RoundButton(plan, "", CellAt[i], new Vector2(255f, 165f), Color.white, UiKit.Ink, () => Pick(slot), 28);
            cellText[i] = cells[i].GetComponentInChildren<Text>();
        }
        UiKit.Panel("Road", plan, new Color(0.55f, 0.55f, 0.57f), new Vector2(0f, -205f), new Vector2(560f, 34f));
        UiKit.Label(plan, 22, "ถนน (หน้าแปลง)", new Vector2(0f, -205f), new Vector2(400f, 32f), Color.white);
        save = UiKit.RoundButton(plan, "", new Vector2(0f, -275f), new Vector2(420f, 74f), UiKit.Gold, UiKit.GoldInk, Save, 27);
        saveText = save.GetComponentInChildren<Text>();

        // fences: one row per side, a button per kind
        var rows = UiKit.Container(body, portrait ? new Vector2(0f, -230f) : new Vector2(330f, 60f));
        UiKit.Label(rows, 28, "สร้างรั้ว · จ่ายครั้งเดียวต่อด้าน", new Vector2(0f, 230f), new Vector2(660f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
        for (int s = 0; s < 4; s++)
        {
            float y = 150f - s * 105f;
            UiKit.Label(rows, 26, SideNames[s], new Vector2(-295f, y), new Vector2(130f, 40f), UiKit.Ink, TextAnchor.MiddleLeft);
            for (int k = 0; k < 5; k++)
            {
                int side = s, kind = k;
                fenceButtons[s, k] = UiKit.RoundButton(rows, "", new Vector2(-160f + k * 118f, y), new Vector2(112f, 88f), Color.white, UiKit.Ink, () => Fence(side, kind), 19);
            }
        }
        // which หมู่ (town residents only): ◀ หมู่ที่ n ▶  [ย้ายแปลงมาหมู่นี้]
        var row = UiKit.Container(body, portrait ? new Vector2(0f, -510f) : new Vector2(-380f, -300f));
        mooRow = row.gameObject;
        UiKit.RoundButton(row, "◀", new Vector2(-290f, 0f), new Vector2(70f, 70f), UiKit.Pill, UiKit.Ink, () => StepMoo(-1), 28, circle: true);
        mooText = UiKit.Label(row, 24, "", new Vector2(-110f, 0f), new Vector2(270f, 70f), UiKit.Ink);
        mooText.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(row, "▶", new Vector2(70f, 0f), new Vector2(70f, 70f), UiKit.Pill, UiKit.Ink, () => StepMoo(1), 28, circle: true);
        moveButton = UiKit.RoundButton(row, "ย้ายแปลงมาหมู่นี้", new Vector2(245f, 0f), new Vector2(230f, 70f), UiKit.PayGreen, Color.white, MoveHere, 22);

        toastText = UiKit.ToastBar(body, new Vector2(0f, portrait ? -560f : -320f), 1000f);
        backdrop.SetActive(false);
        FarmLots.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        FarmLots.Changed -= OnChanged;
        if (instance == this) instance = null;
    }

    private void OnChanged(FarmLots.View v)
    {
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    private static string ModuleName(FarmLots.View v, string id)
    {
        if (v?.modules != null) foreach (var m in v.modules) if (m.id == id) return m.name;
        return id;
    }

    private string[] Layout()
    {
        var mine = FarmLots.Last?.mine;
        return pending ?? (mine?.layout != null && mine.layout.Length == 4 ? mine.layout : new[] { "rice", "pond", "cows", "coop" });
    }

    private void Render()
    {
        var v = FarmLots.Last;
        subtitleText.text = v?.mine != null && !string.IsNullOrEmpty(v.mine.address) ? v.mine.address : "กำลังโหลด…";
        var layout = Layout();
        for (int i = 0; i < 4; i++)
        {
            cellText[i].text = ModuleName(v, layout[i]);
            cells[i].GetComponent<Image>().color = i == picked ? UiKit.Gold : new Color(0.93f, 0.97f, 0.88f);
        }
        bool changed = pending != null;
        save.interactable = changed && !busy;
        saveText.text = changed ? $"บันทึกการจัดโซน · {(v != null ? v.layout_fee : 500):N0} บาท" : "ยังไม่ได้สลับโซน";

        // the หมู่ picker: only for those whose house isn't in the village
        bool town = !HomeClient.LivesIn("village");
        mooRow.SetActive(town);
        if (town)
        {
            int current = v?.mine != null ? v.mine.moo : 0;
            if (mooPick < 1) mooPick = current > 0 ? current : 1;
            bool here = mooPick == current;
            mooText.text = $"หมู่ที่ {mooPick} {MooNames[mooPick - 1]}" + (here ? "\n(แปลงอยู่หมู่นี้)" : "");
            moveButton.interactable = !busy && !here && v != null;
        }

        var fences = v?.mine?.fences ?? new FarmLots.Fences();
        string[] now = { fences.front, fences.back, fences.left, fences.right };
        for (int s = 0; s < 4; s++)
        for (int k = 0; k < 5; k++)
        {
            var b = fenceButtons[s, k];
            var opt = v?.fences_list != null && k < v.fences_list.Length ? v.fences_list[k] : null;
            b.gameObject.SetActive(opt != null);
            if (opt == null) continue;
            bool on = now[s] == opt.id;
            b.GetComponentInChildren<Text>().text = opt.price > 0 && !on ? $"{opt.name}\n{opt.price:N0}" : opt.name;
            b.GetComponent<Image>().color = on ? UiKit.PayGreen : Color.white;
            b.GetComponentInChildren<Text>().color = on ? Color.white : UiKit.Ink;
            b.interactable = !busy && !on;
        }
    }

    private void Pick(int slot)
    {
        if (busy) return;
        if (picked < 0) { picked = slot; Render(); return; }
        if (picked != slot)
        {
            var layout = (string[])Layout().Clone();
            (layout[picked], layout[slot]) = (layout[slot], layout[picked]);
            var saved = FarmLots.Last?.mine?.layout;
            pending = saved != null && string.Join(",", saved) == string.Join(",", layout) ? null : layout;
        }
        picked = -1;
        Render();
    }

    private async void Save()
    {
        if (busy || pending == null) return;
        busy = true;
        Render();
        try
        {
            var v = await FarmLots.SetLayoutAsync(pending);
            pending = null;
            Notify(v.done ?? "บันทึกแล้ว", UiKit.PayGreen);
        }
        catch (Exception e) { Notify(FarmLots.Explain(e), UiKit.Danger); }
        finally { busy = false; if (this != null) Render(); }
    }

    private async void Fence(int side, int kind)
    {
        var opt = FarmLots.Last?.fences_list != null && kind < FarmLots.Last.fences_list.Length ? FarmLots.Last.fences_list[kind] : null;
        if (busy || opt == null) return;
        busy = true;
        Render();
        try
        {
            var v = await FarmLots.SetFenceAsync(FarmLot.Sides[side], opt.id);
            Notify(v.done ?? "เรียบร้อย", UiKit.PayGreen);
        }
        catch (Exception e) { Notify(FarmLots.Explain(e), UiKit.Danger); }
        finally { busy = false; if (this != null) Render(); }
    }

    private void StepMoo(int by)
    {
        if (busy) return;
        mooPick = (mooPick - 1 + by + MooNames.Length) % MooNames.Length + 1;
        Render();
    }

    // Move the farm to the หมู่ picked (free; rice, fish and animals come along)
    private async void MoveHere()
    {
        if (busy || mooPick < 1) return;
        busy = true;
        Render();
        try
        {
            var v = await FarmLots.MoveAsync(mooPick);
            Notify(v.done ?? "ย้ายแปลงเกษตรแล้ว", UiKit.PayGreen);
        }
        catch (Exception e) { Notify(FarmLots.Explain(e), UiKit.Danger); }
        finally { busy = false; if (this != null) Render(); }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
