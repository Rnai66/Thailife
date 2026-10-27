using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// สอยดาว: a dry, leafless tree hung with little paper tickets, each rolled up and tied with string.
// Pick one (the ticket costs the booth price), it unrolls to show its number, and the number wins
// a prize from the shelves — mostly goods people gave the temple as merit (มาม่า ยาสีฟัน ผงซักฟอก…),
// now and then a big one (หม้อหุงข้าว จักรยาน ทีวี ตู้เย็น). The server picks the prize.
public class SoiDaoUI : MonoBehaviour
{
    private static SoiDaoUI instance;

    private static readonly (string icon, string name, bool big)[] Shelf =
    {
        ("food_mama", "มาม่า", false), ("sell_goods_soap", "สบู่", false), ("sell_goods_toothpaste", "ยาสีฟัน", false),
        ("sell_goods_detergent", "ผงซักฟอก", false), ("sell_goods_fish_sauce", "น้ำปลา", false), ("sell_goods_spoons", "ช้อนส้อม", false),
        ("sell_goods_basin", "กะละมัง", false), ("furn_mat", "เสื่อ", false), ("sell_goods_flask", "กระติกน้ำ", false),
        ("sell_goods_backpack", "กระเป๋าเป้", false), ("sell_goods_futon", "ฟูก", true), ("sell_goods_rice_cooker", "หม้อหุงข้าว", true),
        ("goods_bicycle", "จักรยาน", true), ("furn_tv", "ทีวี", true), ("furn_fridge", "ตู้เย็น", true),
    };

    private static readonly Color[] Ties = { new Color(0.95f, 0.35f, 0.55f), new Color(0.3f, 0.7f, 0.4f), new Color(0.3f, 0.55f, 0.95f), new Color(1f, 0.75f, 0.2f) };

    private GameObject backdrop;
    private RectTransform body, tree, tickets, paper;
    private Text title, subtitle, hint, toast, paperText;
    private readonly List<RectTransform> hanging = new List<RectTransform>();
    private Coroutine toastRun;
    private string boothTitle = "สอยดาว";
    private int price = 40;
    private bool busy;
    private GameObject prizeLayer;

    public static void Show(string boothName, int ticketPrice)
    {
        if (instance == null) instance = new GameObject("SoiDaoUI").AddComponent<SoiDaoUI>();
        instance.boothTitle = boothName;
        instance.price = ticketPrice;
        instance.title.text = boothName;
        instance.hint.text = $"เลือกสลาก 1 ใบจากต้นไม้ · ใบละ {ticketPrice} บาท";
        instance.Restock();
        instance.backdrop.SetActive(true);
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (prizeLayer != null) Destroy(prizeLayer);
    }

    private void Awake()
    {
        body = UiKit.PremiumWindow(transform, "SoiDaoCanvas", UiKit.WindowSize(new Vector2(1540f, 900f), 1440f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        subtitle.text = "สอยสลากจากต้นไม้ ลุ้นของรางวัลจากงานบุญ";

        // The tree, against the night sky of the fair
        tree = UiKit.Panel("Tree", body, new Color(0.1f, 0.13f, 0.28f), new Vector2(-345f, 18f), new Vector2(770f, 640f), shadow: true);
        tree.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        for (int i = 0; i < 14; i++) // fairy lights along the top
        {
            var bulb = UiKit.Rect("Bulb", tree, i % 3 == 0 ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.9f, 0.55f));
            bulb.GetComponent<Image>().sprite = UiKit.CircleSprite;
            bulb.anchoredPosition = new Vector2(-350f + i * 54f, 300f - Mathf.Sin(i / 13f * Mathf.PI) * 22f);
            bulb.sizeDelta = new Vector2(14f, 14f);
        }
        var mound = UiKit.Rect("Mound", tree, new Color(0.45f, 0.3f, 0.18f));
        mound.GetComponent<Image>().sprite = UiKit.CircleSprite;
        mound.anchoredPosition = new Vector2(0f, -305f);
        mound.sizeDelta = new Vector2(360f, 70f);
        var wood = new Color(0.42f, 0.28f, 0.16f);
        Branch(tree, new Vector2(0f, -300f), new Vector2(0f, -40f), 48f, wood);
        var limbs = new[]
        {
            (new Vector2(0f, -60f), new Vector2(-230f, 120f), 26f), (new Vector2(0f, -60f), new Vector2(215f, 140f), 26f),
            (new Vector2(0f, -50f), new Vector2(-20f, 240f), 22f), (new Vector2(-120f, 30f), new Vector2(-310f, 60f), 14f),
            (new Vector2(-160f, 70f), new Vector2(-140f, 250f), 14f), (new Vector2(110f, 45f), new Vector2(320f, 70f), 14f),
            (new Vector2(150f, 95f), new Vector2(120f, 260f), 14f), (new Vector2(-12f, 150f), new Vector2(-90f, 285f), 10f),
            (new Vector2(-12f, 150f), new Vector2(70f, 280f), 10f), (new Vector2(-230f, 120f), new Vector2(-300f, 190f), 9f),
            (new Vector2(215f, 140f), new Vector2(290f, 210f), 9f),
        };
        foreach (var l in limbs) Branch(tree, l.Item1, l.Item2, l.Item3, wood);
        tickets = UiKit.Container(tree, Vector2.zero);
        hint = UiKit.Label(tree, 24, "", new Vector2(0f, -252f), new Vector2(700f, 36f), new Color(1f, 0.92f, 0.7f));

        // The unrolled ticket
        paper = UiKit.Panel("Paper", tree, new Color(0.99f, 0.97f, 0.9f), new Vector2(0f, 20f), new Vector2(360f, 220f), shadow: true);
        paper.gameObject.AddComponent<Outline>().effectColor = new Color(0.8f, 0.3f, 0.4f);
        UiKit.Label(paper, 26, "สลากเลขที่", new Vector2(0f, 60f), new Vector2(340f, 40f), UiKit.Muted);
        paperText = UiKit.Cartoon(UiKit.Label(paper, 96, "", new Vector2(0f, -20f), new Vector2(340f, 120f), new Color(0.8f, 0.15f, 0.25f)), 2f);
        paper.gameObject.SetActive(false);

        // Prize shelves
        var board = UiKit.Panel("Prizes", body, Color.white, new Vector2(400f, 18f), new Vector2(640f, 640f), shadow: true);
        board.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(board, 30, "ของรางวัล", new Vector2(0f, 285f), new Vector2(600f, 44f), UiKit.Price).fontStyle = FontStyle.Bold;
        UiKit.Label(board, 19, "ของทำบุญจากวัด · รางวัลใหญ่ขอบทอง", new Vector2(0f, 250f), new Vector2(600f, 30f), UiKit.Muted);
        for (int i = 0; i < Shelf.Length; i++)
        {
            int col = i % 5, row = i / 5;
            var at = new Vector2(-248f + col * 124f, 150f - row * 170f);
            var tile = UiKit.IconTile(board, Shelf[i].icon, at, new Vector2(104f, 104f));
            if (Shelf[i].big) tile.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
            UiKit.Label(board, 19, Shelf[i].name, at + new Vector2(0f, -68f), new Vector2(120f, 28f), Shelf[i].big ? UiKit.Price : UiKit.Ink);
        }

        toast = UiKit.ToastBar(body, new Vector2(0f, -320f), 900f);
        if (UiKit.Portrait) // upright: the tree on top, the prize shelves underneath
        {
            UiKit.Stack(tree, new Vector2(0f, 334f), 0.9f);
            UiKit.Stack(board, new Vector2(0f, -292f));        }
        backdrop.SetActive(false);
    }

    private static void Branch(RectTransform parent, Vector2 from, Vector2 to, float width, Color color)
    {
        var b = UiKit.Rect("Branch", parent, color);
        Vector2 d = to - from;
        b.pivot = new Vector2(0.5f, 0f);
        b.anchoredPosition = from;
        b.sizeDelta = new Vector2(width, d.magnitude + width * 0.3f);
        b.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
        b.GetComponent<Image>().raycastTarget = false;
    }

    // Hang a fresh set of tickets on the branches
    private void Restock()
    {
        UiKit.Clear(tickets);
        hanging.Clear();
        paper.gameObject.SetActive(false);
        var spots = new[]
        {
            new Vector2(-200f, 95f), new Vector2(-160f, 60f), new Vector2(-110f, 25f), new Vector2(-270f, 52f), new Vector2(-215f, 40f),
            new Vector2(-150f, 150f), new Vector2(-145f, 215f), new Vector2(-290f, 175f), new Vector2(180f, 110f), new Vector2(130f, 60f),
            new Vector2(80f, 20f), new Vector2(250f, 60f), new Vector2(300f, 68f), new Vector2(135f, 170f), new Vector2(125f, 235f),
            new Vector2(270f, 192f), new Vector2(-15f, 90f), new Vector2(-18f, 180f), new Vector2(-60f, 245f), new Vector2(45f, 245f),
            new Vector2(-5f, 20f), new Vector2(-75f, 130f), new Vector2(60f, 130f), new Vector2(205f, 160f),
        };
        for (int i = 0; i < spots.Length; i++)
        {
            var pivot = UiKit.Rect("Ticket", tickets, new Color(0f, 0f, 0f, 0f));
            pivot.anchoredPosition = spots[i];
            pivot.sizeDelta = new Vector2(40f, 40f);
            float drop = 22f + (i * 7 % 4) * 9f;
            var str = UiKit.Rect("String", pivot, new Color(0.9f, 0.85f, 0.7f));
            str.pivot = new Vector2(0.5f, 1f);
            str.anchoredPosition = Vector2.zero;
            str.sizeDelta = new Vector2(2f, drop);
            var roll = UiKit.Rect("Roll", pivot, new Color(0.98f, 0.95f, 0.86f));
            roll.pivot = new Vector2(0.5f, 1f);
            roll.anchoredPosition = new Vector2(0f, -drop);
            roll.sizeDelta = new Vector2(24f, 54f);
            var tie = UiKit.Rect("Tie", roll, Ties[i % Ties.Length]);
            tie.anchoredPosition = new Vector2(0f, 0f);
            tie.sizeDelta = new Vector2(26f, 7f);
            var button = roll.gameObject.AddComponent<Button>();
            var picked = pivot;
            button.onClick.AddListener(() => Pick(picked));
            hanging.Add(pivot);
        }
    }

    private void Update()
    {
        if (backdrop == null || !backdrop.activeSelf) return;
        for (int i = 0; i < hanging.Count; i++)
            if (hanging[i] != null)
                hanging[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.6f + i * 0.9f) * 6f);
    }

    private async void Pick(RectTransform ticket)
    {
        if (busy || prizeLayer != null) return;
        var f = FestivalClient.Instance;
        if (f == null) return;
        busy = true;
        try
        {
            var r = await f.PlayAsync("soidao");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            if (BankClient.Instance != null) _ = BankClient.Instance.RefreshAsync();
            StartCoroutine(Reveal(ticket, r));
        }
        catch (Exception e)
        {
            Say(FestivalBooth.Explain(e, price), UiKit.Danger);
            busy = false;
        }
    }

    // The ticket flies off the branch, unrolls to show its number, then the prize card pops up
    private IEnumerator Reveal(RectTransform ticket, FestivalClient.State r)
    {
        GameAudio.Play("whoosh", null, 0.6f);
        if (ticket != null)
        {
            Vector2 from = ticket.anchoredPosition;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                ticket.anchoredPosition = Vector2.Lerp(from, new Vector2(0f, 40f), t / 0.35f);
                ticket.localScale = Vector3.one * (1f + t * 3f);
                yield return null;
            }
            hanging.Remove(ticket);
            Destroy(ticket.gameObject);
        }
        paper.gameObject.SetActive(true);
        paperText.text = r.ticket;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            paper.localScale = new Vector3(1f, Mathf.Lerp(0.08f, 1f, t / 0.4f), 1f);
            yield return null;
        }
        paper.localScale = Vector3.one;
        GameAudio.Play("bell", null, 0.6f);
        yield return new WaitForSecondsRealtime(0.9f);
        paper.gameObject.SetActive(false);
        prizeLayer = FairPrize.Show(this, body, r.big ? "รางวัลใหญ่!" : $"สลาก {r.ticket} ได้…", r.icon, r.prize, true, r.big);
        if (r.big) UiKit.FloatAbovePlayer($"สอยดาวได้ {r.prize}", new Color(1f, 0.85f, 0.3f));
        if (hanging.Count < 4) Restock();
        busy = false;
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
