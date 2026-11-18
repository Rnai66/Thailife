using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Bank window (ธนาคาร), premium layout with three tabs:
//   บัญชี     savings (deposit / withdraw, interest) and loans (borrow / repay)
//   ทรัพย์สิน  land and cars to buy
//   ธุรกิจ     your businesses (staff, takings), opening a new one in a rented building
//   สมุดบัญชี  the statement: what came into savings by itself (business takings, land rent, interest)
public class BankUI : MonoBehaviour
{
    private static readonly long[] Amounts = { 1000, 10000, 100000, 1000000, -1 }; // -1 = ทั้งหมด

    private static BankUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private RectTransform tabs, content;
    private Coroutine toast;
    private string tab = "account";
    private long amount = 10000;
    private string pickType = "coffee", pickBuilding = "shop_left_3";
    private bool busy;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("BankUI").AddComponent<BankUI>();
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private bool P; // upright layout

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "BankCanvas", UiKit.WindowSize(new Vector2(1500f, 920f), 1560f), 55, "฿",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "ธนาคาร";
        P = UiKit.Portrait;
        tabs = UiKit.Container(body, new Vector2(0f, P ? 650f : 320f));
        content = UiKit.Container(body, new Vector2(0f, P ? 0f : -30f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, P ? -620f : -350f), 800f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        var bank = BankClient.Instance;
        if (bank == null) return;
        try { await bank.RefreshAsync(); } catch (Exception) { }
        if (backdrop.activeSelf) Render();
    }

    // ── Rendering ───────────────────────────────────────────────────────────────
    private void Render()
    {
        UiKit.Clear(tabs);
        UiKit.Clear(content);
        var bank = BankClient.Instance;
        if (bank == null || !bank.IsOnline)
        {
            subtitleText.text = "ต้องออนไลน์ก่อน";
            return;
        }
        var s = bank.Current;
        subtitleText.text = $"ระดับผู้เล่น Lv {s.level} · เงินสด {s.cash:N0} ฿ · เงินฝาก {s.savings:N0} ฿ · หนี้ {s.loan:N0} ฿";

        string[] ids = { "account", "assets", "business", "inbox" };
        string[] names = { P ? "บัญชี" : "บัญชี / สินเชื่อ", P ? "ที่ดิน/รถ" : "ที่ดิน / รถ", "ธุรกิจ", "สมุดบัญชี" };
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            bool on = tab == id;
            string label = names[i];
            if (id == "inbox" && NewEntries(s) > 0) label += $" ({NewEntries(s)})";
            UiKit.RoundButton(tabs, label, new Vector2((i - 1.5f) * (P ? 170f : 300f), 0f), new Vector2(P ? 162f : 280f, 70f),
                on ? UiKit.Gold : UiKit.Pill, on ? UiKit.GoldInk : UiKit.Price, () => { tab = id; Render(); }, P ? 22 : 28);
        }

        if (tab == "account") RenderAccount(s);
        else if (tab == "assets") RenderAssets(s);
        else if (tab == "inbox") RenderInbox(s);
        else RenderBusiness(s);
    }

    private RectTransform Card(Vector2 pos, Vector2 size)
    {
        var c = UiKit.Panel("Card", content, Color.white, pos, size, shadow: true);
        c.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        return c;
    }

    private void RenderAccount(BankClient.State s)
    {
        // Amount picker
        UiKit.Label(content, 24, "จำนวนเงิน", P ? new Vector2(-240f, 565f) : new Vector2(-620f, 250f), new Vector2(200f, 40f), UiKit.Muted, TextAnchor.MiddleLeft);
        for (int i = 0; i < Amounts.Length; i++)
        {
            long a = Amounts[i];
            bool on = amount == a;
            UiKit.RoundButton(content, a < 0 ? "ทั้งหมด" : $"{a:N0}", P ? new Vector2((i - 2) * 136f, 505f) : new Vector2(-380f + i * 200f, 250f), new Vector2(P ? 128f : 185f, 60f),
                on ? UiKit.Teak : UiKit.Pill, on ? UiKit.GoldText : UiKit.Price, () => { amount = a; Render(); }, 24);
        }

        // Upright: savings above, the loan under it
        var save = Card(P ? new Vector2(0f, 215f) : new Vector2(-335f, -10f), new Vector2(P ? 690f : 640f, P ? 400f : 420f));
        UiKit.Label(save, 30, "บัญชีออมทรัพย์", new Vector2(0f, 165f), new Vector2(580f, 46f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(save, 52, $"{s.savings:N0} ฿", new Vector2(0f, 90f), new Vector2(580f, 70f), UiKit.Price).fontStyle = FontStyle.Bold;
        UiKit.Label(save, 22, $"ดอกเบี้ย {s.savings_rate * 100f:0.##}% ต่อวันในเกม (ทบต้น)\nค่าเช่าที่ดินของคุณเข้าบัญชีนี้อัตโนมัติ", new Vector2(0f, 10f), new Vector2(580f, 70f), UiKit.Muted);
        UiKit.RoundButton(save, "ฝาก", new Vector2(-150f, -120f), new Vector2(260f, 90f), UiKit.PayGreen, Color.white,
            () => Do(b => b.DepositAsync(Pick(s.cash))), 32).interactable = !busy;
        UiKit.RoundButton(save, "ถอน", new Vector2(150f, -120f), new Vector2(260f, 90f), UiKit.Gold, UiKit.GoldInk,
            () => Do(b => b.WithdrawAsync(Pick(s.savings))), 32).interactable = !busy;

        var loan = Card(P ? new Vector2(0f, -205f) : new Vector2(335f, -10f), new Vector2(P ? 690f : 640f, P ? 400f : 420f));
        UiKit.Label(loan, 30, "สินเชื่อ", new Vector2(0f, 165f), new Vector2(580f, 46f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(loan, 52, $"{s.loan:N0} ฿", new Vector2(0f, 90f), new Vector2(580f, 70f), s.loan > 0 ? UiKit.Danger : UiKit.OkGreen).fontStyle = FontStyle.Bold;
        string limit = s.level < s.loan_level ? $"กู้ได้เมื่อ Lv {s.loan_level}+" : $"วงเงินกู้ {s.loan_limit:N0} ฿ (ตามเลเวลและทรัพย์สิน)";
        UiKit.Label(loan, 22, $"{limit}\nดอกเบี้ย {s.loan_rate * 100f:0.##}% ต่อวันในเกม", new Vector2(0f, 10f), new Vector2(580f, 70f), UiKit.Muted);
        UiKit.RoundButton(loan, "กู้เงิน", new Vector2(-150f, -120f), new Vector2(260f, 90f), UiKit.Pill, UiKit.Danger,
            () => Do(b => b.BorrowAsync(Pick(Math.Max(0, s.loan_limit - s.loan)))), 32).interactable = !busy && s.level >= s.loan_level;
        UiKit.RoundButton(loan, "ชำระหนี้", new Vector2(150f, -120f), new Vector2(260f, 90f), UiKit.PayGreen, Color.white,
            () => Do(b => b.RepayAsync(Pick(Math.Min(s.loan, s.cash)))), 32).interactable = !busy && s.loan > 0;
    }

    private long Pick(long all) => amount < 0 ? all : amount;

    // ── สมุดบัญชี ─────────────────────────────────────────────────────────────────────────
    private const string SeenKey = "bank_inbox_seen";

    // Lines newer than the last time the statement was looked at
    private static int NewEntries(BankClient.State s)
    {
        if (s.inbox == null) return 0;
        long seen = long.TryParse(PlayerPrefs.GetString(SeenKey, "0"), out long v) ? v : 0;
        int n = 0;
        foreach (var e in s.inbox) if (e.at > seen) n++;
        return n;
    }

    private void RenderInbox(BankClient.State s)
    {
        var lines = s.inbox ?? new BankClient.Entry[0];
        long newest = 0;
        foreach (var e in lines) newest = Math.Max(newest, e.at);
        long seen = long.TryParse(PlayerPrefs.GetString(SeenKey, "0"), out long v) ? v : 0;
        if (newest > seen) PlayerPrefs.SetString(SeenKey, newest.ToString());

        // the totals: today, the last 7 game days, since the takings started going to the bank
        long today = 0, week = 0;
        foreach (var e in lines)
        {
            if (e.day == s.today) today += e.amount;
            if (e.day > s.today - 7) week += e.amount;
        }
        float w = P ? 690f : 1400f;
        var head = Card(P ? new Vector2(0f, 500f) : new Vector2(0f, 215f), new Vector2(w, P ? 170f : 110f));
        string[] titles = { "วันนี้", "7 วันล่าสุด", "กำไรธุรกิจรวม" };
        long[] values = { today, week, s.income };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * (P ? 225f : 450f);
            UiKit.Label(head, P ? 20 : 22, titles[i], new Vector2(x, P ? 38f : 24f), new Vector2(P ? 220f : 420f, 34f), UiKit.Muted);
            UiKit.Label(head, P ? 30 : 34, $"{(values[i] >= 0 ? "+" : "")}{values[i]:N0} ฿", new Vector2(x, P ? -14f : -20f), new Vector2(P ? 220f : 420f, 50f),
                values[i] >= 0 ? UiKit.OkGreen : UiKit.Danger).fontStyle = FontStyle.Bold;
        }

        // the lines, newest first, in a scrolling list
        float listH = P ? 900f : 470f;
        var card = Card(P ? new Vector2(0f, -45f) : new Vector2(0f, -100f), new Vector2(w, listH));
        if (lines.Length == 0)
        {
            UiKit.Label(card, 24, "ยังไม่มีเงินเข้าอัตโนมัติ\nเปิดธุรกิจ ซื้อที่ดินให้เช่า หรือฝากเงินไว้ แล้วรายการจะขึ้นที่นี่", Vector2.zero, new Vector2(w - 60f, 120f), UiKit.Muted);
            return;
        }
        var viewport = UiKit.Rect("Viewport", card, new Color(0f, 0f, 0f, 0f));
        viewport.sizeDelta = new Vector2(w - 20f, listH - 20f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var list = new GameObject("Lines", typeof(RectTransform)).GetComponent<RectTransform>();
        list.SetParent(viewport, false);
        list.anchorMin = list.anchorMax = new Vector2(0.5f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        const float rowH = 78f;
        list.sizeDelta = new Vector2(w - 20f, Mathf.Max(listH - 20f, lines.Length * rowH + 10f));
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        float rowW = w - 50f;
        for (int i = 0; i < lines.Length; i++)
        {
            var e = lines[i];
            bool fresh = e.at > seen;
            var row = UiKit.Panel("Line", list, fresh ? new Color(1f, 0.97f, 0.86f) : (i % 2 == 0 ? new Color(0.975f, 0.97f, 0.96f) : Color.white),
                Vector2.zero, new Vector2(rowW, rowH - 8f));
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -10f - i * rowH - rowH * 0.5f);
            Color tint = e.kind == "business" ? new Color(0.85f, 0.55f, 0.15f) : e.kind == "rent" ? new Color(0.25f, 0.6f, 0.35f) : new Color(0.2f, 0.45f, 0.75f);
            string icon = e.kind == "business" ? "ร้าน" : e.kind == "rent" ? "ที่ดิน" : "ดอก";
            UiKit.PillLabel(row, icon, new Vector2(-rowW * 0.5f + 60f, 0f), new Vector2(92f, 46f), tint, Color.white, 18);
            string when = e.day == s.today ? "วันนี้" : e.day == s.today - 1 ? "เมื่อวาน" : $"{s.today - e.day} วันก่อน";
            UiKit.Label(row, P ? 22 : 24, e.label, new Vector2(-rowW * 0.5f + 120f + (rowW - 380f) * 0.5f, 12f), new Vector2(rowW - 380f, 34f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(row, 18, $"{GameClock.DateOf(e.day)} · {when}" + (fresh ? " · ใหม่" : ""), new Vector2(-rowW * 0.5f + 120f + (rowW - 380f) * 0.5f, -18f),
                new Vector2(rowW - 380f, 28f), fresh ? UiKit.Price : UiKit.Muted, TextAnchor.MiddleLeft);
            UiKit.Label(row, P ? 26 : 30, $"{(e.amount >= 0 ? "+" : "")}{e.amount:N0} ฿", new Vector2(rowW * 0.5f - 130f, 0f), new Vector2(240f, 50f),
                e.amount >= 0 ? UiKit.OkGreen : UiKit.Danger, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;
        }
    }

    private void RenderAssets(BankClient.State s)
    {
        if (s.assets == null) return;
        for (int i = 0; i < s.assets.Length; i++)
        {
            var a = s.assets[i];
            var c = Card(P ? new Vector2(i % 2 == 0 ? -175f : 175f, 480f - (i / 2) * 300f) : new Vector2(-525f + (i % 4) * 350f, 150f - (i / 4) * 300f), new Vector2(330f, 280f));
            var tile = UiKit.Panel("Icon", c, a.kind == "car" ? (Color)new Color32(0xE6, 0xF1, 0xFB, 0xFF) : UiKit.OkTint, new Vector2(0f, 85f), new Vector2(290f, 80f));
            UiKit.Label(tile, 28, a.kind == "car" ? "ยานพาหนะ" : "ที่ดิน", Vector2.zero, new Vector2(280f, 70f), a.kind == "car" ? (Color)new Color32(0x18, 0x5F, 0xA5, 0xFF) : UiKit.OkGreen).fontStyle = FontStyle.Bold;
            UiKit.Label(c, 26, a.name, new Vector2(0f, 20f), new Vector2(310f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
            UiKit.Label(c, 20, $"{a.perk}\nต้อง Lv {a.level}+", new Vector2(0f, -30f), new Vector2(310f, 56f), UiKit.Muted);
            string id = a.id;
            if (a.owned) UiKit.PillLabel(c, "เป็นเจ้าของแล้ว", new Vector2(0f, -100f), new Vector2(290f, 56f), UiKit.OkTint, UiKit.OkGreen, 24);
            else
                UiKit.RoundButton(c, $"ซื้อ {a.price:N0} ฿", new Vector2(0f, -100f), new Vector2(290f, 62f), UiKit.Gold, UiKit.GoldInk,
                    () => Do(b => b.BuyAsync(id)), 26).interactable = !busy && s.level >= a.level && s.cash >= a.price;
        }
    }

    private void RenderBusiness(BankClient.State s)
    {
        bool allowed = s.level >= s.business_level;
        UiKit.Label(content, 22, $"เปิดธุรกิจได้เมื่อ Lv {s.business_level}+ และมีเงินรวม {s.business_cash:N0} ฿ ขึ้นไป · กิจการละไม่เกิน {s.max_staff} พนักงาน · สูงสุด 3 กิจการ",
            P ? new Vector2(0f, 575f) : new Vector2(0f, 255f), new Vector2(P ? 680f : 1400f, P ? 70f : 40f), allowed ? UiKit.Muted : UiKit.Danger);

        // Your businesses
        int count = s.businesses?.Length ?? 0;
        for (int i = 0; i < count; i++)
        {
            var b = s.businesses[i];
            var c = Card(P ? new Vector2(0f, 345f - i * 222f) : new Vector2(-470f + i * 470f, 120f), new Vector2(P ? 600f : 450f, 210f));
            UiKit.Label(c, 28, b.name, new Vector2(0f, 75f), new Vector2(420f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
            UiKit.Label(c, 20, b.building, new Vector2(0f, 42f), new Vector2(420f, 30f), UiKit.Muted);
            UiKit.Label(c, 22, $"กำไร/วัน ~{b.per_day:N0} ฿ · เข้าบัญชีแล้ว {b.earned:N0} ฿", new Vector2(0f, 8f), new Vector2(420f, 32f), b.per_day >= 0 ? UiKit.OkGreen : UiKit.Danger);
            int slot = b.slot;
            UiKit.RoundButton(c, "-", new Vector2(-150f, -50f), new Vector2(70f, 56f), UiKit.Pill, UiKit.Danger, () => Do(x => x.StaffAsync(slot, -1)), 32).interactable = !busy && b.staff > 0;
            UiKit.Label(c, 22, $"พนักงาน {b.staff} คน", new Vector2(0f, -50f), new Vector2(220f, 40f), UiKit.Ink);
            UiKit.RoundButton(c, "+", new Vector2(150f, -50f), new Vector2(70f, 56f), UiKit.Pill, UiKit.OkGreen, () => Do(x => x.StaffAsync(slot, 1)), 32).interactable = !busy && b.staff < s.max_staff;
            UiKit.RoundButton(c, "ปิดกิจการ", new Vector2(0f, -100f), new Vector2(200f, 44f), UiKit.DangerTint, UiKit.Danger, () => Do(x => x.CloseAsync(slot)), 22).interactable = !busy && b.till == 0;
        }
        if (count == 0)
            UiKit.Label(content, 26, "ยังไม่มีธุรกิจ — เลือกประเภทและอาคารด้านล่าง แล้วกดเปิดกิจการ", P ? new Vector2(0f, 300f) : new Vector2(0f, 120f), new Vector2(P ? 640f : 1200f, P ? 100f : 60f), UiKit.Muted);
        else
            UiKit.PillLabel(content, $"กำไรเข้าบัญชีอัตโนมัติ · รวม {s.income:N0} ฿", P ? new Vector2(0f, 500f) : new Vector2(545f, 255f), new Vector2(P ? 560f : 330f, 50f),
                UiKit.OkTint, UiKit.OkGreen, P ? 20 : 17);

        // Open a new one
        // Upright: the types and the buildings each in a 2x2 grid
        var open = Card(P ? new Vector2(0f, -455f) : new Vector2(0f, -150f), new Vector2(P ? 690f : 1400f, P ? 450f : 280f));
        UiKit.Label(open, 26, "เปิดกิจการใหม่ (เช่าอาคาร)", P ? new Vector2(-110f, 195f) : new Vector2(-480f, 105f), new Vector2(420f, 40f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        Vector2 Grid(int i, float y, float y2) => P ? new Vector2(i % 2 == 0 ? -165f : 165f, i < 2 ? y : y2) : new Vector2(-480f + i * 250f, y);
        BankClient.BusinessType picked = null;
        if (s.business_types != null)
            for (int i = 0; i < s.business_types.Length; i++)
            {
                var t = s.business_types[i];
                if (t.id == pickType) picked = t;
                string id = t.id;
                UiKit.RoundButton(open, t.name, Grid(i, P ? 135f : 45f, 67f), new Vector2(P ? 320f : 230f, 60f),
                    t.id == pickType ? UiKit.Teak : UiKit.Pill, t.id == pickType ? UiKit.GoldText : UiKit.Price, () => { pickType = id; Render(); }, 24);
            }
        if (s.buildings != null)
            for (int i = 0; i < s.buildings.Length; i++)
            {
                var bd = s.buildings[i];
                string id = bd.id;
                var btn = UiKit.RoundButton(open, bd.taken ? "เช่าอยู่แล้ว" : bd.name, Grid(i, P ? -3f : -30f, -71f), new Vector2(P ? 320f : 230f, 60f),
                    bd.id == pickBuilding ? UiKit.Teak : UiKit.Pill, bd.id == pickBuilding ? UiKit.GoldText : UiKit.Price, () => { pickBuilding = id; Render(); }, 18);
                btn.interactable = !bd.taken;
            }
        if (picked != null)
        {
            UiKit.Label(open, 20, $"ลงทุน {picked.setup:N0} ฿ · ค่าเช่า {picked.rent:N0} ฿/วัน · ค่าจ้าง {picked.wage:N0} ฿/คน/วัน · รายได้เต็มที่ ~{picked.revenue:N0} ฿/วัน",
                P ? new Vector2(0f, -125f) : new Vector2(-160f, -100f), new Vector2(P ? 650f : 1000f, P ? 56f : 32f), UiKit.Muted, TextAnchor.MiddleLeft);
            UiKit.RoundButton(open, "เปิดกิจการ", P ? new Vector2(0f, -185f) : new Vector2(540f, -100f), new Vector2(P ? 360f : 260f, 64f), UiKit.Gold, UiKit.GoldInk,
                () => Do(x => x.OpenAsync(pickType, pickBuilding)), 26).interactable = !busy && allowed && count < 3 && s.cash >= picked.setup;
        }
    }

    // ── Actions ─────────────────────────────────────────────────────────────────
    private async void Do(Func<BankClient, Task<BankClient.State>> call)
    {
        var bank = BankClient.Instance;
        if (busy || bank == null) return;
        busy = true;
        try
        {
            var r = await call(bank);
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("not enough baht") ? "เงินสดไม่พอ"
                : e.Message.Contains("savings") ? "เงินฝากไม่พอ"
                : e.Message.Contains("level") ? "เลเวลยังไม่ถึง"
                : e.Message.Contains("limit") ? "เกินวงเงินกู้"
                : e.Message.Contains("million") ? "ต้องมีเงินรวมอย่างน้อย 1,000,000 บาท"
                : e.Message.Contains("taken") ? "อาคารนี้เช่าไปแล้ว"
                : e.Message.Contains("too many") ? "มีธุรกิจครบ 3 กิจการแล้ว"
                : e.Message.Contains("collect") ? "รับกำไรก่อนปิดกิจการ"
                : e.Message.Contains("nothing") ? "ยังไม่มีกำไรให้รับ"
                : e.Message.Contains("owned") ? "มีอยู่แล้ว" : "ทำรายการไม่สำเร็จ", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Bank] {e.Message}");
        }
        busy = false;
        Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
