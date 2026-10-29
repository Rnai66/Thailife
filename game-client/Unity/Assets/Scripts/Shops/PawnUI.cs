using System;
using UnityEngine;
using UnityEngine.UI;

// Pawn shop window (โรงรับจำนำ): on the left what you can pledge — gold at today's buying price,
// spare instruments and bank assets (cars, houses, land deeds) at their second-hand market price —
// each lent at a share of that price; on the right your tickets (ตั๋วจำนำ) with the interest so
// far and the days left to redeem before the pledge is forfeit.
public class PawnUI : MonoBehaviour
{
    private const int PerPage = 5;
    private static PawnUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText, infoText, itemsPage, ticketsPage;
    private RectTransform items, tickets;
    private Coroutine toast;
    private int itemPage, ticketPage;
    private bool busy;
    private string shopName = "โรงรับจำนำ";

    public static void Show(string name)
    {
        if (instance == null) instance = new GameObject("PawnUI").AddComponent<PawnUI>();
        instance.shopName = name;
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
        var body = UiKit.PremiumWindow(transform, "PawnCanvas", UiKit.WindowSize(new Vector2(1400f, 860f), 1500f), 55, "icon:shop",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        var info = UiKit.Panel("Info", body, new Color(1f, 0.97f, 0.88f), new Vector2(0f, 300f), new Vector2(1300f, 70f), shadow: true);
        info.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        infoText = UiKit.Label(info, 22, "", Vector2.zero, new Vector2(1260f, 60f), UiKit.Ink);

        var left = UiKit.Container(body, new Vector2(-330f, -40f));
        UiKit.Label(left, 28, "ของที่จำนำได้", new Vector2(0f, 215f), new Vector2(620f, 44f), UiKit.Ink).fontStyle = FontStyle.Bold;
        items = UiKit.Container(left, Vector2.zero);
        itemsPage = Pager(left, () => { itemPage--; Render(); }, () => { itemPage++; Render(); });

        var right = UiKit.Container(body, new Vector2(330f, -40f));
        UiKit.Label(right, 28, "ตั๋วจำนำของคุณ", new Vector2(0f, 215f), new Vector2(620f, 44f), UiKit.Ink).fontStyle = FontStyle.Bold;
        tickets = UiKit.Container(right, Vector2.zero);
        ticketsPage = Pager(right, () => { ticketPage--; Render(); }, () => { ticketPage++; Render(); });

        toastText = UiKit.ToastBar(body, new Vector2(0f, -385f), 900f);
        if (UiKit.Portrait) // upright: the terms on top, what you can pledge, then your tickets
        {
            info.sizeDelta = new Vector2(690f, 110f);
            infoText.rectTransform.sizeDelta = new Vector2(660f, 100f);
            UiKit.Stack(info, new Vector2(0f, 640f));
            UiKit.Stack(left, new Vector2(0f, 300f), 1.05f);
            UiKit.Stack(right, new Vector2(0f, -280f), 1.05f);
            UiKit.Stack((RectTransform)toastText.transform.parent, new Vector2(0f, -700f));
        }
        backdrop.SetActive(false);
    }

    private Text Pager(RectTransform parent, UnityEngine.Events.UnityAction prev, UnityEngine.Events.UnityAction next)
    {
        UiKit.RoundButton(parent, "◀", new Vector2(-120f, -235f), new Vector2(70f, 50f), UiKit.Pill, UiKit.Ink, prev, 24);
        UiKit.RoundButton(parent, "▶", new Vector2(120f, -235f), new Vector2(70f, 50f), UiKit.Pill, UiKit.Ink, next, 24);
        return UiKit.Label(parent, 20, "", new Vector2(0f, -235f), new Vector2(160f, 40f), UiKit.Muted);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        try
        {
            var s = await GoldClient.GetAsync();
            if (!string.IsNullOrEmpty(s.done)) Notify(s.done, UiKit.Danger); // forfeit tickets
            Render();
        }
        catch (Exception e) { Notify(GoldClient.Explain(e), UiKit.Danger); }
    }

    private void Render()
    {
        titleText.text = shopName;
        var s = GoldClient.Current;
        subtitleText.text = s == null ? "เงินด่วน ตามราคาตลาด"
            : $"ดอกเบี้ย {s.rate_month * 100f:0.##}% ต่อเดือน · ไถ่ถอนภายใน {s.term_days} วันในเกม · ถือตั๋วได้ {s.max_tickets} ใบ";
        infoText.text = s?.price == null ? "กำลังโหลด..."
            : $"ทองรับจำนำ 90% ของราคารับซื้อ (บาทละ {s.price.buy_baht:N0} ฿) · เครื่องดนตรี รถ บ้าน ที่ดิน 70% ของราคาตลาดมือสอง · เงินสด {s.baht:N0} ฿";
        UiKit.Clear(items);
        UiKit.Clear(tickets);
        if (s == null) return;

        var list = s.pawnables;
        itemPage = Mathf.Clamp(itemPage, 0, Mathf.Max(0, (list.Length - 1) / PerPage));
        itemsPage.text = list.Length > PerPage ? $"{itemPage + 1}/{(list.Length - 1) / PerPage + 1}" : "";
        if (list.Length == 0)
            UiKit.Label(items, 22, "ไม่มีของที่จำนำได้\nซื้อทอง เครื่องดนตรี รถ หรือบ้านไว้ก่อน", new Vector2(0f, 60f), new Vector2(600f, 100f), UiKit.Muted);
        for (int i = itemPage * PerPage; i < list.Length && i < (itemPage + 1) * PerPage; i++)
        {
            var x = list[i];
            var card = Card(items, i - itemPage * PerPage, x.kind == "gold" ? new Color(1f, 0.95f, 0.8f) : Color.white);
            UiKit.Label(card, 24, x.name, new Vector2(-95f, 18f), new Vector2(380f, 34f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(card, 19, $"ราคาตลาด {x.market:N0} ฿", new Vector2(-95f, -18f), new Vector2(380f, 30f), UiKit.Muted, TextAnchor.MiddleLeft);
            var item = x;
            var b = UiKit.RoundButton(card, $"จำนำ\n{x.loan:N0} ฿", new Vector2(205f, 0f), new Vector2(170f, 70f), UiKit.Gold, UiKit.GoldInk, () => Pawn(item), 20);
            b.interactable = !busy && s.tickets.Length < s.max_tickets;
        }

        var ts = s.tickets;
        ticketPage = Mathf.Clamp(ticketPage, 0, Mathf.Max(0, (ts.Length - 1) / PerPage));
        ticketsPage.text = ts.Length > PerPage ? $"{ticketPage + 1}/{(ts.Length - 1) / PerPage + 1}" : "";
        if (ts.Length == 0)
            UiKit.Label(tickets, 22, "ยังไม่มีตั๋วจำนำ", new Vector2(0f, 60f), new Vector2(600f, 60f), UiKit.Muted);
        for (int i = ticketPage * PerPage; i < ts.Length && i < (ticketPage + 1) * PerPage; i++)
        {
            var t = ts[i];
            bool urgent = t.days_left <= 10;
            var card = Card(tickets, i - ticketPage * PerPage, urgent ? new Color(1f, 0.9f, 0.88f) : Color.white);
            UiKit.Label(card, 22, $"#{t.id} {t.name}", new Vector2(-95f, 22f), new Vector2(380f, 30f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(card, 18, $"เงินต้น {t.loan:N0} + ดอก {t.interest:N0} ฿", new Vector2(-95f, -6f), new Vector2(380f, 26f), UiKit.Muted, TextAnchor.MiddleLeft);
            UiKit.Label(card, 18, $"เหลือ {t.days_left} วัน", new Vector2(-95f, -30f), new Vector2(380f, 24f), urgent ? UiKit.Danger : UiKit.Muted, TextAnchor.MiddleLeft);
            int id = t.id;
            var b = UiKit.RoundButton(card, $"ไถ่ถอน\n{t.redeem:N0} ฿", new Vector2(205f, 0f), new Vector2(170f, 70f), UiKit.PayGreen, Color.white, () => Redeem(id), 20);
            b.interactable = !busy && s.baht >= t.redeem;
        }
    }

    private static RectTransform Card(RectTransform parent, int row, Color tint)
    {
        var card = UiKit.Panel("Card", parent, tint, new Vector2(0f, 160f - row * 86f), new Vector2(620f, 78f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        return card;
    }

    private void Pawn(GoldClient.Pawnable item) => Act(() => GoldClient.PawnAsync(item, item.units));

    private void Redeem(int ticket) => Act(() => GoldClient.RedeemAsync(ticket));

    private async void Act(Func<System.Threading.Tasks.Task<GoldClient.State>> call)
    {
        if (busy) return;
        busy = true;
        Render();
        try
        {
            var r = await call();
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
