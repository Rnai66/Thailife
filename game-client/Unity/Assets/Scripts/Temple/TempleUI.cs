using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// The temple window, opened at the abbot's kuti or the amulet pavilion (TempleCounter):
//  · เจ้าอาวาส — ordain (บวช), leave the monkhood (สึก) or come out a หมอผี at monk Lv 30, your บารมี,
//    adopt a temple dog or raise a กุมารทอง to guard the house, and what came for your home lately
//  · บูชาพระเครื่อง — amulets and yantra cloths for the home shrine; each keeps ghosts and thieves away
public class TempleUI : MonoBehaviour
{
    public const string Abbot = "abbot", Amulets = "amulets";

    private static TempleUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, statusText, infoText, toastText;
    private RectTransform tabs, content, viewport;
    private ScrollRect scroll;
    private Coroutine toast;
    private string tab = Abbot;
    private string armed;
    private bool busy;

    public static void Show(string which)
    {
        if (instance == null) instance = new GameObject("TempleUI").AddComponent<TempleUI>();
        instance.tab = which;
        instance.armed = null;
        instance.backdrop.SetActive(true);
        instance.scroll.verticalNormalizedPosition = 1f;
        instance.Render();
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "TempleCanvas", UiKit.WindowSize(new Vector2(1400f, 940f), 1500f), 55, "icon:temple",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        float w = upright ? 700f : 1300f;
        var card = UiKit.Panel("Status", body, Color.white, new Vector2(0f, upright ? 560f : 300f), new Vector2(w, upright ? 190f : 150f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        statusText = UiKit.Label(card, 30, "", new Vector2(0f, upright ? 50f : 36f), new Vector2(w - 40f, 48f), UiKit.Ink);
        statusText.fontStyle = FontStyle.Bold;
        infoText = UiKit.Label(card, 21, "", new Vector2(0f, upright ? -36f : -28f), new Vector2(w - 40f, upright ? 100f : 70f), UiKit.Muted);
        tabs = UiKit.Container(body, new Vector2(0f, upright ? 420f : 180f));

        Vector2 size = upright ? new Vector2(w, 1000f) : new Vector2(w, 500f);
        viewport = UiKit.Rect("Viewport", body, new Color(0f, 0f, 0f, 0f));
        viewport.anchoredPosition = new Vector2(0f, upright ? -120f : -125f);
        viewport.sizeDelta = size;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = size;
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        toastText = UiKit.ToastBar(body, new Vector2(0f, upright ? -640f : -380f), 900f);
        backdrop.SetActive(false);
        if (TempleClient.Instance != null) TempleClient.Instance.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        if (TempleClient.Instance != null) TempleClient.Instance.Changed -= OnChanged;
        if (instance == this) instance = null;
    }

    private void OnChanged(TempleClient.State _)
    {
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    private async void Refresh()
    {
        var t = TempleClient.Instance;
        if (t == null) return;
        try { await t.RefreshAsync(); } catch (Exception) { }
    }

    // ── Layout helpers: everything in the scrolling content hangs from its top edge ──────────
    private float y;

    private T Top<T>(T c) where T : Component
    {
        var rt = (RectTransform)c.transform;
        var p = rt.anchoredPosition;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = p;
        return c;
    }

    private void Heading(string text)
    {
        y -= 26f;
        Top(UiKit.Label(content, 26, text, new Vector2(0f, y), new Vector2(content.sizeDelta.x - 40f, 40f), UiKit.Price)).fontStyle = FontStyle.Bold;
        y -= 34f;
    }

    private Button Wide(string text, Color fill, Color ink, bool enabled, Action onClick, int fontSize = 24, float height = 84f)
    {
        y -= height * 0.5f;
        var b = UiKit.RoundButton(content, text, new Vector2(0f, y), new Vector2(Mathf.Min(760f, content.sizeDelta.x - 40f), height), fill, ink, () => onClick?.Invoke(), fontSize);
        Top(b);
        b.interactable = enabled && !busy && onClick != null;
        y -= height * 0.5f + 14f;
        return b;
    }

    private void Note(string text, Color color, int size = 21)
    {
        y -= 22f;
        Top(UiKit.Label(content, size, text, new Vector2(0f, y), new Vector2(content.sizeDelta.x - 60f, 40f), color));
        y -= 26f;
    }

    // ── Drawing ───────────────────────────────────────────────────────────────────────────
    private void Render()
    {
        bool upright = UiKit.Portrait;
        titleText.text = tab == Abbot ? "กุฏิเจ้าอาวาส" : "ศาลาบูชาพระเครื่อง";
        subtitleText.text = tab == Abbot ? "บวช สึก เลี้ยงหมาวัดและกุมารทอง" : "พระเครื่องและผ้ายันต์ ขึ้นหิ้งพระที่บ้าน กันผีกันขโมย";
        UiKit.Clear(tabs);
        float tw = upright ? 330f : 360f;
        foreach (var (id, label, x) in new[] { (Abbot, "เจ้าอาวาส", -tw * 0.5f - 6f), (Amulets, "บูชาพระเครื่อง", tw * 0.5f + 6f) })
        {
            bool on = tab == id;
            string which = id;
            UiKit.RoundButton(tabs, label, new Vector2(x, 0f), new Vector2(tw, 60f), on ? UiKit.Teak : UiKit.Pill, on ? UiKit.GoldText : UiKit.Ink,
                () => { tab = which; armed = null; scroll.verticalNormalizedPosition = 1f; Render(); }, 24);
        }
        UiKit.Clear(content);
        y = 0f;

        var s = TempleClient.Instance?.Current;
        if (s == null)
        {
            statusText.text = "กำลังโหลด…";
            infoText.text = "ต้องออนไลน์ก่อน";
            return;
        }
        statusText.text = s.ordained ? $"พระภิกษุ · พระเลเวล {s.monk_level}" + (s.exorcist ? $" · หมอผีเลเวล {s.exorcist_level}" : "")
            : s.exorcist ? $"หมอผีเลเวล {s.exorcist_level} · พระเลเวล {s.monk_level}" : "ฆราวาส (ยังไม่ได้บวช)";
        infoText.text = $"บารมี Lv {s.barami} ({s.barami_xp}/{s.barami_to_next}) · บ้านกันผี {s.guard}% · กันขโมย {s.luck}%"
                        + (s.dog ? " · มีหมาเฝ้าบ้าน" : "") + (s.kuman ? " · มีกุมารทอง" : "")
                        + (tab == Amulets ? "\nพุทธคุณติดตัว: " + PowerText(s.my_powers, "ยังไม่ได้ห้อยพระ")
                                          : "\nนั่งสมาธิในโบสถ์หน้าพระประธานเพื่อเพิ่มบารมี (จุดธูปเทียนได้บารมีมากขึ้น)");

        if (tab == Abbot) RenderAbbot(s);
        else RenderAmulets(s);
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(viewport.sizeDelta.y, -y + 20f));
    }

    private void RenderAbbot(TempleClient.State s)
    {
        Heading("บวชเรียน");
        if (!s.ordained)
        {
            bool confirm = armed == "ordain";
            Wide(confirm ? "แตะอีกครั้ง ยืนยันบวช" : $"บวชเป็นพระ · ค่าอัฐบริขาร {s.ordain_cost:N0} บาท (เลเวล {s.ordain_level}+)",
                UiKit.Gold, UiKit.GoldInk, true, () => Arm("ordain", t => t.OrdainAsync()));
            Note("บวชแล้วห่มจีวร โกนผม นั่งสมาธิเพื่อเลื่อนเลเวลพระ · ทำวัตรเช้า 05-07 น. และเย็น 18-20 น. ได้ XP สองเท่า", UiKit.Muted);
            if (!s.exorcist) Note($"เป็นพระถึงเลเวล {s.exorcist_monk_level} แล้วสึกออกมาเป็น \"หมอผี\" ปราบผีได้", UiKit.Price);
        }
        else
        {
            bool canExorcist = s.monk_level >= s.exorcist_monk_level;
            bool confirmX = armed == "exorcist";
            Wide(confirmX ? "แตะอีกครั้ง ยืนยันสึกเป็นหมอผี" : canExorcist ? "สึกออกมาเป็นหมอผี" : $"สึกเป็นหมอผี (ต้องพระเลเวล {s.exorcist_monk_level} · ตอนนี้ {s.monk_level})",
                new Color(0.35f, 0.15f, 0.45f), Color.white, canExorcist, () => Arm("exorcist", t => t.DisrobeAsync(true)));
            bool confirm = armed == "disrobe";
            Wide(confirm ? "แตะอีกครั้ง ยืนยันสึก" : "สึกกลับไปเป็นฆราวาส", UiKit.DangerTint, UiKit.Danger, true, () => Arm("disrobe", t => t.DisrobeAsync(false)));
            Note($"พระเลเวล {s.monk_level} · XP {s.monk_xp}/{s.monk_to_next} · นั่งสมาธิในโบสถ์เพื่อเลื่อนเลเวล", UiKit.Muted);
        }
        if (s.exorcist)
            Note("หมอผีออกปราบผีได้ตอนกลางคืน (19:00-05:00) ที่ป่าช้าหลังวัด ป่าชุมชน และป่าตำบล", new Color(0.45f, 0.2f, 0.55f));

        Heading("ผู้เฝ้าบ้าน");
        foreach (var id in new[] { "dog", "kuman" })
            ItemButton(s, Find(s, id), id == "dog" ? "หมาเห่าไล่ขโมยตอนกลางคืน" : "เตือนภัยเมื่อผีหรือขโมยมาที่บ้าน");

        Heading("เหตุการณ์ที่บ้านช่วงนี้");
        if (s.log == null || s.log.Length == 0) Note("ยังไม่มีอะไรเกิดขึ้น บ้านสงบดี", UiKit.Muted);
        else foreach (var n in s.log) Note(n.text, n.saved ? UiKit.OkGreen : UiKit.Danger, 20);
    }

    private void RenderAmulets(TempleClient.State s)
    {
        if (s.items == null) return;
        bool upright = UiKit.Portrait;
        int cols = upright ? 2 : 4;
        float cw = upright ? 330f : 300f, ch = 340f;
        int worn = TempleClient.Instance != null ? TempleClient.Instance.WornCount : 0;
        int wornMax = s.worn_max > 0 ? s.worn_max : 3;
        int i = 0;
        y -= 10f;
        foreach (var it in s.items)
        {
            if (it.kind == "guardian") continue;
            int row = i / cols, col = i % cols;
            i++;
            var pos = new Vector2((col - (cols - 1) * 0.5f) * (cw + 14f), y - row * (ch + 14f) - ch * 0.5f);
            var card = Top(UiKit.Panel("Amulet", content, it.worn ? new Color(1f, 0.95f, 0.8f) : it.owned ? UiKit.OkTint : Color.white, pos, new Vector2(cw, ch), shadow: true));
            card.gameObject.AddComponent<Outline>().effectColor = it.worn ? UiKit.Gold : it.owned ? UiKit.PayGreen : UiKit.CardBorder;
            var pic = UiKit.Rect("Art", card, Color.white);
            pic.GetComponent<Image>().sprite = AmuletArt.Get(it.id);
            pic.GetComponent<Image>().preserveAspect = true;
            pic.anchoredPosition = new Vector2(0f, 94f);
            pic.sizeDelta = new Vector2(120f, 120f);
            if (it.worn) UiKit.PillLabel(card, "ห้อยคออยู่", new Vector2(-cw * 0.5f + 70f, ch * 0.5f - 26f), new Vector2(120f, 36f), UiKit.Gold, UiKit.GoldInk, 17);
            UiKit.Label(card, 24, it.name, new Vector2(0f, 14f), new Vector2(cw - 20f, 34f), UiKit.Ink).fontStyle = FontStyle.Bold;
            bool hasPowers = it.powers != null && it.powers.Length > 0;
            UiKit.Label(card, 17, hasPowers ? "พุทธคุณ: " + PowerText(it.powers, "") : "ผ้ายันต์ติดบ้าน (ไม่ได้ห้อยคอ)",
                new Vector2(0f, -26f), new Vector2(cw - 20f, 46f), hasPowers ? UiKit.Price : UiKit.Muted);
            UiKit.Label(card, 16, $"กันผี +{it.guard}% · กันขโมย +{it.luck}%", new Vector2(0f, -64f), new Vector2(cw - 20f, 24f), UiKit.Muted);
            bool locked = s.barami < it.barami;
            string id = it.id;
            string label;
            Color fill, ink;
            bool able;
            Func<TempleClient, Task<TempleClient.State>> act;
            if (!it.owned)
            {
                label = locked ? $"ต้องบารมี Lv {it.barami}" : $"บูชา {it.price:N0} บาท";
                fill = UiKit.Gold; ink = UiKit.GoldInk; able = !locked;
                act = t => t.BuyAsync(id);
            }
            else if (!hasPowers)
            {
                label = "อยู่บนหิ้งพระแล้ว";
                fill = UiKit.Pill; ink = UiKit.OkGreen; able = false;
                act = null;
            }
            else if (it.worn)
            {
                label = "ถอดขึ้นหิ้งพระ";
                fill = UiKit.Pill; ink = UiKit.Danger; able = true;
                act = t => t.WearAsync(id, false);
            }
            else
            {
                bool room = worn < wornMax;
                label = room ? "ห้อยคอ" : $"ห้อยคอ (เต็ม {wornMax} องค์)";
                fill = UiKit.Teak; ink = UiKit.GoldText; able = room;
                act = t => t.WearAsync(id, true);
            }
            var b = UiKit.RoundButton(card, label, new Vector2(0f, -122f), new Vector2(cw - 40f, 58f), fill, ink,
                () => { if (act != null) Do(act); }, 21);
            b.interactable = able && !busy;
        }
        int rows = (i + cols - 1) / cols;
        y -= rows * (ch + 14f) + 10f;
        Note("พระเครื่องและผ้ายันต์ที่บูชาแล้ว อยู่บนหิ้งพระที่บ้าน ยิ่งมากยิ่งกันผีกันขโมยได้ดี", UiKit.Muted);
        Note($"ห้อยพระได้ {wornMax} องค์ ได้พุทธคุณติดตัว: แคล้วคลาด = หลบหมัดเท้า · หนังเหนียว = เจ็บน้อยลง", UiKit.Price, 19);
        Note("ป้องกัน = กันผีเข้าบ้าน · บังตา = ขโมยมองไม่เห็นบ้าน · มหานิยม = ขายของ/ภารกิจได้เงินเพิ่ม · เสน่ห์ = ทิปดนตรีเพิ่ม", UiKit.Price, 19);
    }

    private static string PowerText(TempleClient.Power[] powers, string none)
    {
        if (powers == null || powers.Length == 0) return none;
        var parts = new string[powers.Length];
        for (int i = 0; i < powers.Length; i++) parts[i] = $"{powers[i].name} +{powers[i].value}%";
        return string.Join(" · ", parts);
    }

    private static TempleClient.Item Find(TempleClient.State s, string id)
    {
        if (s.items == null) return null;
        foreach (var it in s.items) if (it.id == id) return it;
        return null;
    }

    private void ItemButton(TempleClient.State s, TempleClient.Item it, string what)
    {
        if (it == null) return;
        bool locked = s.barami < it.barami;
        string id = it.id;
        string text = it.owned ? $"{it.name} · อยู่ที่บ้านแล้ว" : locked ? $"{it.name} · ต้องบารมี Lv {it.barami}" : $"รับ{it.name}ไปเลี้ยง · ทำบุญ {it.price:N0} บาท";
        Wide(text, it.owned ? UiKit.OkTint : UiKit.Gold, it.owned ? UiKit.OkGreen : UiKit.GoldInk, !it.owned && !locked, () => Do(t => t.BuyAsync(id)), 22, 76f);
        Note(what, UiKit.Muted, 19);
    }

    // Big steps take two taps
    private void Arm(string what, Func<TempleClient, Task<TempleClient.State>> action)
    {
        if (armed == what) Do(action);
        else { armed = what; Render(); }
    }

    private async void Do(Func<TempleClient, Task<TempleClient.State>> action)
    {
        var t = TempleClient.Instance;
        if (busy || t == null) return;
        busy = true;
        armed = null;
        Render();
        try
        {
            var r = await action(t);
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play("bell");
        }
        catch (ApiResponseException e) { Notify(TempleClient.Explain(e), UiKit.Danger); }
        catch (Exception) { Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger); }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
