using System;
using UnityEngine;
using UnityEngine.UI;

// Gold shop window (ร้านทอง): today's board price, the last 24 game hours as a chart, what you hold
// with its average cost and profit, and buy / sell in ครึ่งสลึง. The price moves every game hour
// (90 s), so buying on a dip and selling on a rise makes money; the shop keeps a small spread.
public class GoldUI : MonoBehaviour
{
    private static GoldUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private Text boardSell, boardBuy, boardChange, boardNext, chartLow, chartHigh;
    private Text holdUnits, holdCost, holdValue, holdProfit, amountText, buyTotal, sellTotal;
    private Button buyButton, sellButton;
    private RectTransform chart;
    private Coroutine toast;
    private int units = 1;
    private bool busy;
    private float nextTick;
    private string shopName = "ร้านทอง";

    public static void Show(string name)
    {
        if (instance == null) instance = new GameObject("GoldUI").AddComponent<GoldUI>();
        instance.shopName = name;
        instance.titleText.text = name;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "GoldCanvas", UiKit.WindowSize(new Vector2(1400f, 860f), 1500f), 55, "icon:shop",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        subtitleText.text = "ทองคำแท่ง 96.5% · ราคาเปลี่ยนทุกชั่วโมงในเกม · ซื้อตอนถูก ขายตอนแพง";

        // Left: the price board and chart
        var left = UiKit.Container(body, new Vector2(-330f, 0f));
        var board = UiKit.Panel("Board", left, new Color(0.16f, 0.1f, 0.06f), new Vector2(0f, 230f), new Vector2(640f, 210f), shadow: true);
        board.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(board, 22, "ร้านขายออก (บาทละ)", new Vector2(-160f, 70f), new Vector2(300f, 34f), new Color(1f, 0.85f, 0.5f));
        UiKit.Label(board, 22, "ร้านรับซื้อ (บาทละ)", new Vector2(160f, 70f), new Vector2(300f, 34f), new Color(1f, 0.85f, 0.5f));
        boardSell = UiKit.Label(board, 48, "", new Vector2(-160f, 18f), new Vector2(300f, 64f), new Color(1f, 0.8f, 0.3f));
        boardBuy = UiKit.Label(board, 48, "", new Vector2(160f, 18f), new Vector2(300f, 64f), new Color(1f, 0.8f, 0.3f));
        boardSell.fontStyle = boardBuy.fontStyle = FontStyle.Bold;
        boardChange = UiKit.Label(board, 24, "", new Vector2(-150f, -50f), new Vector2(320f, 36f), Color.white);
        boardNext = UiKit.Label(board, 20, "", new Vector2(170f, -50f), new Vector2(280f, 36f), new Color(1f, 1f, 1f, 0.7f));
        UiKit.Label(board, 18, "1 บาททอง = 8 ครึ่งสลึง ≈ 15.2 กรัม", new Vector2(0f, -86f), new Vector2(600f, 26f), new Color(1f, 1f, 1f, 0.55f));

        var chartCard = UiKit.Panel("Chart", left, Color.white, new Vector2(0f, -110f), new Vector2(640f, 420f), shadow: true);
        chartCard.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(chartCard, 24, "ราคากลาง 24 ชั่วโมงล่าสุด", new Vector2(0f, 180f), new Vector2(600f, 36f), UiKit.Ink).fontStyle = FontStyle.Bold;
        chart = UiKit.Container(chartCard, new Vector2(20f, -15f));
        chartHigh = UiKit.Label(chartCard, 18, "", new Vector2(-255f, 135f), new Vector2(120f, 24f), UiKit.Muted, TextAnchor.MiddleLeft);
        chartLow = UiKit.Label(chartCard, 18, "", new Vector2(-255f, -165f), new Vector2(120f, 24f), UiKit.Muted, TextAnchor.MiddleLeft);

        // Right: your gold and the trade
        var right = UiKit.Container(body, new Vector2(345f, 0f));
        var hold = UiKit.Panel("Holding", right, new Color(1f, 0.97f, 0.88f), new Vector2(0f, 200f), new Vector2(620f, 270f), shadow: true);
        hold.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(hold, 26, "ทองของคุณ", new Vector2(0f, 105f), new Vector2(580f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
        holdUnits = UiKit.Label(hold, 36, "", new Vector2(0f, 55f), new Vector2(580f, 50f), UiKit.Price);
        holdUnits.fontStyle = FontStyle.Bold;
        holdCost = UiKit.Label(hold, 22, "", new Vector2(0f, 5f), new Vector2(580f, 34f), UiKit.Muted);
        holdValue = UiKit.Label(hold, 22, "", new Vector2(0f, -35f), new Vector2(580f, 34f), UiKit.Muted);
        holdProfit = UiKit.Label(hold, 30, "", new Vector2(0f, -85f), new Vector2(580f, 44f), UiKit.PayGreen);
        holdProfit.fontStyle = FontStyle.Bold;

        var trade = UiKit.Panel("Trade", right, Color.white, new Vector2(0f, -140f), new Vector2(620f, 360f), shadow: true);
        trade.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(trade, 24, "จำนวน (ครึ่งสลึง)", new Vector2(0f, 145f), new Vector2(580f, 34f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.RoundButton(trade, "−", new Vector2(-200f, 90f), new Vector2(80f, 64f), UiKit.Pill, UiKit.Ink, () => Step(-1), 36);
        amountText = UiKit.Label(trade, 34, "", new Vector2(0f, 90f), new Vector2(300f, 60f), UiKit.Ink);
        amountText.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(trade, "+", new Vector2(200f, 90f), new Vector2(80f, 64f), UiKit.Pill, UiKit.Ink, () => Step(1), 36);
        string[] quick = { "1 สลึง", "2 สลึง", "1 บาท", "5 บาท" };
        int[] amounts = { 2, 4, 8, 40 };
        for (int i = 0; i < quick.Length; i++)
        {
            int n = amounts[i];
            UiKit.RoundButton(trade, quick[i], new Vector2(-225f + i * 150f, 20f), new Vector2(140f, 52f), UiKit.Pill, UiKit.Price, () => { units = n; Render(); }, 22);
        }
        buyButton = UiKit.RoundButton(trade, "ซื้อทอง", new Vector2(-150f, -70f), new Vector2(280f, 80f), UiKit.Gold, UiKit.GoldInk, () => Trade("buy"), 30);
        sellButton = UiKit.RoundButton(trade, "ขายทอง", new Vector2(150f, -70f), new Vector2(280f, 80f), UiKit.PayGreen, Color.white, () => Trade("sell"), 30);
        buyTotal = UiKit.Label(trade, 20, "", new Vector2(-150f, -135f), new Vector2(280f, 30f), UiKit.Muted);
        sellTotal = UiKit.Label(trade, 20, "", new Vector2(150f, -135f), new Vector2(280f, 30f), UiKit.Muted);

        toastText = UiKit.ToastBar(body, new Vector2(0f, -385f), 900f);
        if (UiKit.Portrait) // upright: the board and chart on top, your gold and the trade under them
        {
            UiKit.Stack(left, new Vector2(0f, 360f), 1.05f);
            UiKit.Stack(right, new Vector2(0f, -330f), 1.05f);
            UiKit.Stack((RectTransform)toastText.transform.parent, new Vector2(0f, -700f));
        }
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        try { await GoldClient.GetAsync(); Render(); }
        catch (Exception e) { Notify(GoldClient.Explain(e), UiKit.Danger); }
    }

    private void Update()
    {
        if (!backdrop.activeSelf || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        var s = GoldClient.Current;
        if (s?.price == null) return;
        float left = s.price.next_s - (Time.time - GoldClient.ReceivedAt);
        if (left <= 0f && !busy) { nextTick = Time.time + 3f; Refresh(); return; } // a new hour: new price
        boardNext.text = $"ราคาใหม่ใน {Mathf.Max(0, Mathf.CeilToInt(left))} วิ";
    }

    private void Step(int d)
    {
        units = Mathf.Clamp(units + d, 1, GoldClient.Current?.max_units ?? 800);
        Render();
    }

    private void Render()
    {
        titleText.text = shopName;
        amountText.text = units % 8 == 0 ? $"{units / 8} บาท" : $"{units}";
        var s = GoldClient.Current;
        var p = s?.price;
        buyButton.interactable = sellButton.interactable = p != null && !busy;
        if (p == null)
        {
            boardSell.text = boardBuy.text = "—";
            holdUnits.text = "กำลังโหลด...";
            return;
        }
        boardSell.text = $"{p.sell_baht:N0}";
        boardBuy.text = $"{p.buy_baht:N0}";
        boardChange.text = p.change > 0 ? $"▲ +{p.change:N0} จากชั่วโมงก่อน" : p.change < 0 ? $"▼ {p.change:N0} จากชั่วโมงก่อน" : "● ราคาคงที่";
        boardChange.color = p.change > 0 ? new Color(0.45f, 0.95f, 0.5f) : p.change < 0 ? new Color(1f, 0.45f, 0.4f) : Color.white;

        holdUnits.text = s.units % 8 == 0 ? $"{s.units / 8} บาททอง" : $"{s.units / 8} บาท {s.units % 8} ครึ่งสลึง";
        int avg = s.units > 0 ? Mathf.RoundToInt(s.cost * 8f / s.units) : 0;
        holdCost.text = s.units > 0 ? $"ต้นทุนรวม {s.cost:N0} ฿ · เฉลี่ยบาทละ {avg:N0} ฿" : "ยังไม่มีทอง — ซื้อเก็บไว้ขายตอนราคาขึ้น";
        holdValue.text = $"ขายคืนตอนนี้ได้ {s.value:N0} ฿";
        holdProfit.text = s.units == 0 ? "" : s.profit >= 0 ? $"กำไร +{s.profit:N0} ฿" : $"ขาดทุน {s.profit:N0} ฿";
        holdProfit.color = s.profit >= 0 ? UiKit.PayGreen : UiKit.Danger;

        buyTotal.text = $"จ่าย {p.unit_sell * units:N0} ฿";
        sellTotal.text = $"รับ {p.unit_buy * units:N0} ฿";
        sellButton.interactable &= s.units >= units;
        DrawChart(p.history);
    }

    private void DrawChart(int[] history)
    {
        UiKit.Clear(chart);
        if (history == null || history.Length == 0) return;
        int lo = int.MaxValue, hi = int.MinValue;
        foreach (int v in history) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
        float pad = Mathf.Max(200f, (hi - lo) * 0.15f);
        float min = lo - pad, max = hi + pad;
        chartHigh.text = $"{hi:N0}";
        chartLow.text = $"{lo:N0}";
        const float width = 560f, height = 290f;
        float step = width / history.Length;
        for (int i = 0; i < history.Length; i++)
        {
            float t = (history[i] - min) / (max - min);
            bool up = i == 0 || history[i] >= history[i - 1];
            var bar = UiKit.Rect("Bar", chart, up ? new Color(0.95f, 0.72f, 0.2f) : new Color(0.8f, 0.45f, 0.25f));
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(-width / 2f + step * (i + 0.5f), -height / 2f);
            bar.sizeDelta = new Vector2(step - 4f, Mathf.Max(6f, t * height));
            if (i == history.Length - 1) bar.gameObject.AddComponent<Outline>().effectColor = UiKit.Ink; // now
        }
    }

    private async void Trade(string side)
    {
        if (busy) return;
        busy = true;
        Render();
        try
        {
            var r = await GoldClient.TradeAsync(side, units);
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play("coins");
        }
        catch (Exception e)
        {
            Notify(GoldClient.Explain(e), UiKit.Danger);
        }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
