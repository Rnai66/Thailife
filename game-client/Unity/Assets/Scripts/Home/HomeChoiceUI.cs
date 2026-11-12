using System;
using UnityEngine;
using UnityEngine.UI;

// เลือกที่อยู่ / ย้ายบ้าน: first where to live — บ้านนอก (the village), ในเมือง (town) or กรุงเทพฯ —
// then which house, then which lot (บ้านเลขที่) in which หมู่: a grid of the หมู่'s lots (free ones
// green, your friends' blue, yours gold), arrows through the 12 หมู่ and a jump to the next หมู่ where
// friends live — or let the game pick. Asked once when you start (free); later "ย้ายบ้าน" in the
// settings moves you for a fee. Choosing takes you straight to your new porch (HomeClient.GoHome).
// A village resident whose farm lot (FarmLots) ended up in another หมู่ can bring it over here, free.
public class HomeChoiceUI : MonoBehaviour
{
    private static HomeChoiceUI instance;

    private GameObject backdrop;
    private Text title, subtitle, toast;
    private RectTransform list;
    private Coroutine toastRun;
    private bool busy, first, portrait;
    private float bodyW, bodyH;
    private int pickedLot, shownMoo;
    private int version; // drops answers for a page that has since been left

    private static readonly (string area, string icon)[] Icons = { ("village", "farm"), ("city", "city"), ("bangkok", "temple") };

    public static void Show(bool firstTime = false)
    {
        var home = HomeClient.Instance;
        if (home?.Current?.areas == null)
        {
            UiKit.FloatAbovePlayer("ต้องออนไลน์ก่อน", new Color(1f, 0.6f, 0.4f));
            return;
        }
        if (instance == null) instance = new GameObject("HomeChoiceUI").AddComponent<HomeChoiceUI>();
        instance.first = firstTime;
        instance.backdrop.SetActive(true);
        instance.ShowAreas();
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var size = UiKit.WindowSize(new Vector2(1320f, 840f), 1400f);
        var body = UiKit.PremiumWindow(transform, "HomeChoiceCanvas", size, 58, "icon:home", out backdrop, out title, out subtitle, Close, out _);
        bodyW = size.x - 40f;
        bodyH = size.y - 156f;
        list = UiKit.Container(body, Vector2.zero);
        toast = UiKit.ToastBar(body, new Vector2(0f, -bodyH * 0.5f + 40f), 900f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Close() => backdrop.SetActive(false);

    private HomeClient.State State => HomeClient.Instance?.Current;

    private void Heading(string head)
    {
        var s = State;
        title.text = first ? "เลือกที่อยู่" : "ย้ายบ้าน";
        subtitle.text = first ? $"{head} · เลือกครั้งแรกฟรี" : $"{head} · ค่าย้ายบ้าน {s.move_fee:N0} บาท · ตอนนี้อยู่{s.style_name}";
    }

    // Step 1: which part of the country
    private void ShowAreas()
    {
        version++;
        UiKit.Clear(list);
        Heading("จะเริ่มชีวิตที่ไหนดี?");
        var areas = State.areas;
        for (int i = 0; i < areas.Length; i++)
        {
            var a = areas[i];
            string icon = "home";
            foreach (var (id, ic) in Icons) if (id == a.id) icon = ic;
            string houses = a.styles != null ? $"บ้าน {a.styles.Length} แบบ" : "";
            Card(i, areas.Length, icon, a.name, $"{a.desc}\n{houses}", a.id == State.area && State.chosen ? "ที่อยู่ตอนนี้" : null,
                "ดูบ้าน", () => ShowStyles(a));
        }
        if (!first && State.chosen) // opened from the HUD: the quick way back to your porch
            UiKit.RoundButton(list, "กลับบ้าน", new Vector2(portrait ? 0f : -bodyW * 0.5f + 180f, -bodyH * 0.5f + (portrait ? 110f : 50f)),
                new Vector2(300f, 64f), UiKit.Gold, UiKit.GoldInk, () => { Close(); HomeClient.Instance?.GoHome(); }, 24);
        if (!first && State.chosen && State.area == "village" && State.moo > 0) OfferFarmMove(version);
        if (!first && State.chosen && State.area != "village") // living in town: pick the หมู่ your farm is in
            UiKit.RoundButton(list, "แปลงเกษตร · เลือกหมู่", new Vector2(portrait ? 0f : bodyW * 0.5f - 250f, -bodyH * 0.5f + (portrait ? 190f : 50f)),
                new Vector2(460f, 64f), UiKit.PayGreen, Color.white, () => { Close(); FarmLotUI.Show(); }, 22);
    }

    // Your farm lot is in another หมู่ than your house: bring it over (free, everything on it comes along)
    private async void OfferFarmMove(int page)
    {
        FarmLots.View v;
        try { v = await FarmLots.FetchMineAsync(); }
        catch (Exception) { return; }
        if (this == null || page != version || v?.mine == null || v.mine.moo == State.moo) return;
        UiKit.RoundButton(list, $"ย้ายแปลงเกษตรมาหมู่ที่ {State.moo} (ฟรี)", new Vector2(portrait ? 0f : bodyW * 0.5f - 250f, -bodyH * 0.5f + (portrait ? 190f : 50f)),
            new Vector2(460f, 64f), UiKit.PayGreen, Color.white, MoveFarm, 22);
    }

    private async void MoveFarm()
    {
        if (busy) return;
        busy = true;
        try
        {
            var v = await FarmLots.MoveAsync();
            Say(v.done ?? "ย้ายแปลงเกษตรแล้ว", UiKit.PayGreen);
            ShowAreas();
        }
        catch (Exception e) { Say(FarmLots.Explain(e), UiKit.Danger); }
        busy = false;
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    // Step 2: which house there
    private void ShowStyles(HomeClient.Area a)
    {
        version++;
        UiKit.Clear(list);
        Heading($"เลือกบ้านใน{a.name}");
        for (int i = 0; i < a.styles.Length; i++)
        {
            var s = a.styles[i];
            bool current = State.chosen && State.style == s.id;
            Card(i, a.styles.Length, "home", s.name, s.desc, current ? "บ้านปัจจุบัน" : null,
                "เลือกแปลง", () => ShowLots(a, s, State.chosen && State.area == a.id && State.moo > 0 ? State.moo : 1));
        }
        UiKit.RoundButton(list, "← เลือกพื้นที่อื่น", new Vector2(portrait ? 0f : -bodyW * 0.5f + 180f, -bodyH * 0.5f + (portrait ? 110f : 50f)),
            new Vector2(300f, 64f), UiKit.Pill, UiKit.Ink, ShowAreas, 24);
    }

    // A card: side by side in landscape, one under another upright
    private void Card(int index, int count, string icon, string name, string desc, string badge, string button, Action onClick)
    {
        Vector2 size, at;
        if (portrait)
        {
            float h = Mathf.Min(270f, (bodyH - 200f) / count - 16f);
            size = new Vector2(bodyW - 40f, h);
            at = new Vector2(0f, bodyH * 0.5f - 30f - h * 0.5f - index * (h + 16f));
        }
        else
        {
            float w = Mathf.Min(400f, (bodyW - 40f) / count - 20f);
            size = new Vector2(w, bodyH - 170f);
            at = new Vector2((index - (count - 1) * 0.5f) * (w + 20f), 20f);
        }
        var card = UiKit.Panel("Card", list, Color.white, at, size, shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        if (portrait)
        {
            float x0 = -size.x * 0.5f;
            UiKit.IconTile(card, icon, new Vector2(x0 + 90f, 0f), new Vector2(130f, 130f));
            OneLineFit.Apply(UiKit.Cartoon(UiKit.Label(card, 32, name, new Vector2(x0 + 180f + (size.x - 420f) * 0.5f, size.y * 0.5f - 40f), new Vector2(size.x - 420f, 50f), UiKit.Ink, TextAnchor.MiddleLeft), 1f), 22);
            UiKit.Label(card, 26, desc, new Vector2(x0 + 180f + (size.x - 420f) * 0.5f, -14f), new Vector2(size.x - 420f, size.y - 90f), UiKit.Muted, TextAnchor.UpperLeft);
            if (badge != null) UiKit.PillLabel(card, badge, new Vector2(size.x * 0.5f - 115f, size.y * 0.5f - 34f), new Vector2(200f, 40f), UiKit.OkTint, UiKit.OkGreen, 20);
            var b = UiKit.RoundButton(card, button, new Vector2(size.x * 0.5f - 115f, -size.y * 0.5f + 50f), new Vector2(200f, 66f), UiKit.Gold, UiKit.GoldInk, () => onClick?.Invoke(), 24);
            b.interactable = onClick != null;
        }
        else
        {
            float top = size.y * 0.5f;
            UiKit.IconTile(card, icon, new Vector2(0f, top - 110f), new Vector2(160f, 160f));
            OneLineFit.Apply(UiKit.Cartoon(UiKit.Label(card, 32, name, new Vector2(0f, top - 230f), new Vector2(size.x - 30f, 56f), UiKit.Ink), 1f), 22);
            UiKit.Label(card, 26, desc, new Vector2(0f, top - 330f), new Vector2(size.x - 40f, 140f), UiKit.Muted, TextAnchor.UpperCenter);
            if (badge != null) UiKit.PillLabel(card, badge, new Vector2(0f, top - 24f), new Vector2(200f, 38f), UiKit.OkTint, UiKit.OkGreen, 20);
            var b = UiKit.RoundButton(card, button, new Vector2(0f, -top + 52f), new Vector2(size.x - 60f, 72f), UiKit.Gold, UiKit.GoldInk, () => onClick?.Invoke(), 26);
            b.interactable = onClick != null;
        }
    }

    // Step 3: which lot, in which หมู่
    private async void ShowLots(HomeClient.Area a, HomeClient.Style s, int moo)
    {
        int page = ++version;
        UiKit.Clear(list);
        pickedLot = 0;
        shownMoo = moo;
        Heading($"{s.name} · เลือกบ้านเลขที่");
        LoadingFlower.Show(UiKit.Label(list, 28, "", Vector2.zero, new Vector2(600f, 50f), UiKit.Muted), "กำลังโหลดแปลง…");
        HomeClient.LotsView v;
        try { v = await HomeClient.Instance.LotsAsync(a.id, moo); }
        catch (Exception e) { if (this != null && page == version) Say(HomeClient.Explain(e), UiKit.Danger); return; }
        if (this == null || page != version || v?.lots == null) return;
        RenderLots(a, s, v);
    }

    private void RenderLots(HomeClient.Area a, HomeClient.Style s, HomeClient.LotsView v)
    {
        UiKit.Clear(list);
        float top = bodyH * 0.5f;
        HomeClient.MooEntry here = null;
        if (v.moos != null) foreach (var m in v.moos) if (m.no == v.moo) here = m;
        int free = here != null ? here.total - here.used : 0;

        // the หมู่: arrows either side of its name, how full it is and how many friends live there
        UiKit.RoundButton(list, "◀", new Vector2(-bodyW * 0.5f + 70f, top - 50f), new Vector2(90f, 70f), UiKit.Pill, UiKit.Ink, () => ShowLots(a, s, v.moo <= 1 ? 12 : v.moo - 1), 30);
        UiKit.RoundButton(list, "▶", new Vector2(bodyW * 0.5f - 70f, top - 50f), new Vector2(90f, 70f), UiKit.Pill, UiKit.Ink, () => ShowLots(a, s, v.moo >= 12 ? 1 : v.moo + 1), 30);
        var name = UiKit.Cartoon(UiKit.Label(list, 32, v.moo_name, new Vector2(0f, top - 36f), new Vector2(bodyW - 260f, 46f), UiKit.Ink), 1f);
        OneLineFit.Apply(name, 22);
        UiKit.Label(list, 24, $"ว่าง {free}/{v.lot_count} แปลง" + (here != null && here.friends > 0 ? $" · เพื่อนอยู่ที่นี่ {here.friends} คน" : ""),
            new Vector2(0f, top - 76f), new Vector2(bodyW - 260f, 34f), here != null && here.friends > 0 ? UiKit.OkGreen : UiKit.Muted);

        // the next หมู่ with friends in it (after this one, round the 12)
        int friendsMoo = 0;
        if (v.moos != null)
            for (int k = 1; k <= 12 && friendsMoo == 0; k++)
            {
                int m = (v.moo - 1 + k) % 12 + 1;
                foreach (var e in v.moos) if (e.no == m && e.friends > 0 && m != v.moo) friendsMoo = m;
            }
        float legendY = top - 120f;
        UiKit.Label(list, 21, "เขียว = ว่าง · ฟ้า = เพื่อน · ทอง = บ้านคุณ · เทา = มีคนอยู่", new Vector2(portrait ? 0f : -150f, legendY),
            new Vector2(portrait ? bodyW - 40f : 820f, 32f), UiKit.Muted, portrait ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
        if (friendsMoo > 0)
            UiKit.RoundButton(list, $"ไปหมู่ที่มีเพื่อน (หมู่ {friendsMoo})", new Vector2(portrait ? 0f : bodyW * 0.5f - 200f, portrait ? legendY - 48f : legendY),
                new Vector2(360f, 48f), UiKit.PayGreen, Color.white, () => ShowLots(a, s, friendsMoo), 20);
        if (a.id == "village")
            UiKit.Label(list, 20, "แปลง 1–6 ซอยข้างศาลากลางบ้าน · 7–22 ซอยตะวันออก", new Vector2(0f, legendY - (portrait ? 92f : 38f)), new Vector2(bodyW - 40f, 30f), UiKit.Muted);

        // the lots
        int cols = portrait ? 5 : 8;
        float cw = portrait ? 128f : 148f, ch = portrait ? 84f : 74f, gap = 8f;
        float gridTop = legendY - (portrait ? 150f : 82f);
        var buttons = new Button[v.lots.Length];
        Button confirm = null;
        Text confirmText = null;
        for (int i = 0; i < v.lots.Length; i++)
        {
            var l = v.lots[i];
            int col = i % cols, row = i / cols;
            var at = new Vector2((col - (cols - 1) * 0.5f) * (cw + gap), gridTop - ch * 0.5f - row * (ch + gap));
            Color fill = l.mine ? UiKit.Gold : l.vacant ? new Color(0.84f, 0.94f, 0.8f) : l.friend ? new Color(0.72f, 0.85f, 0.98f) : new Color(0.86f, 0.86f, 0.84f);
            string who = l.mine ? "บ้านคุณ" : l.vacant ? "ว่าง" : Short(l.owner);
            int no = l.no;
            var b = UiKit.RoundButton(list, $"{l.no}\n{who}", at, new Vector2(cw, ch), fill, UiKit.Ink, () =>
            {
                pickedLot = no;
                for (int k = 0; k < buttons.Length; k++)
                    if (buttons[k] != null && v.lots[k].vacant)
                        buttons[k].GetComponent<Image>().color = v.lots[k].no == no ? UiKit.PayGreen : new Color(0.84f, 0.94f, 0.8f);
                if (confirm != null)
                {
                    confirm.interactable = true;
                    confirmText.text = l.mine ? $"เปลี่ยนเป็น{s.name}ที่บ้านเลขที่ {no}" : $"อยู่บ้านเลขที่ {no} หมู่ที่ {v.moo}";
                }
            }, 20);
            b.interactable = l.vacant || (l.mine && State.style != s.id); // your own lot: change the house on it
            buttons[i] = b;
        }

        // back · let the game pick · take the chosen lot
        float by = -bodyH * 0.5f + (portrait ? 110f : 50f);
        if (portrait)
        {
            confirm = UiKit.RoundButton(list, "แตะแปลงที่ว่างเพื่อเลือก", new Vector2(0f, by + 160f), new Vector2(bodyW - 80f, 70f), UiKit.Gold, UiKit.GoldInk, () => Choose(a, s, v.moo, pickedLot), 24);
            UiKit.RoundButton(list, "ให้เกมเลือกแปลงให้", new Vector2(160f, by + 80f), new Vector2(330f, 60f), UiKit.Pill, UiKit.Ink, () => Choose(a, s, 0, 0), 22);
            UiKit.RoundButton(list, "← เลือกแบบบ้าน", new Vector2(-160f, by + 80f), new Vector2(330f, 60f), UiKit.Pill, UiKit.Ink, () => ShowStyles(a), 22);
        }
        else
        {
            UiKit.RoundButton(list, "← เลือกแบบบ้าน", new Vector2(-bodyW * 0.5f + 170f, by), new Vector2(300f, 64f), UiKit.Pill, UiKit.Ink, () => ShowStyles(a), 24);
            UiKit.RoundButton(list, "ให้เกมเลือกแปลงให้", new Vector2(-40f, by), new Vector2(300f, 64f), UiKit.Pill, UiKit.Ink, () => Choose(a, s, 0, 0), 24);
            confirm = UiKit.RoundButton(list, "แตะแปลงที่ว่างเพื่อเลือก", new Vector2(bodyW * 0.5f - 230f, by), new Vector2(420f, 64f), UiKit.Gold, UiKit.GoldInk, () => Choose(a, s, v.moo, pickedLot), 24);
        }
        confirmText = confirm.GetComponentInChildren<Text>();
        confirm.interactable = false;
    }

    private static string Short(string owner)
    {
        if (string.IsNullOrEmpty(owner)) return "มีคนอยู่";
        return owner.Length > 9 ? owner.Substring(0, 8) + "…" : owner;
    }

    // moo/lot 0: the first free lot (your own หมู่ first)
    private async void Choose(HomeClient.Area a, HomeClient.Style s, int moo, int lot)
    {
        var home = HomeClient.Instance;
        if (busy || home == null) return;
        if (moo > 0 && lot <= 0) return;
        busy = true;
        try
        {
            var r = moo > 0 ? await home.ChooseLotAsync(a.id, s.id, moo, lot) : await home.ChooseAsync(a.id, s.id); // takes you there
            if (a.id == "village" && (FarmLots.Last?.mine == null || FarmLots.Last.mine.moo != r.moo))
            {
                try { await FarmLots.MoveAsync(); } catch (Exception) { } // the farm follows to your new หมู่ when there's room
            }
            if (r.paid > 0) UiKit.FloatAbovePlayer($"-{r.paid:N0} ฿", new Color(1f, 0.6f, 0.3f));
            UiKit.FloatAbovePlayer(r.done ?? "ได้บ้านแล้ว!", new Color(1f, 0.85f, 0.3f));
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Close();
        }
        catch (Exception e)
        {
            Say(HomeClient.Explain(e), UiKit.Danger);
        }
        busy = false;
    }
}
