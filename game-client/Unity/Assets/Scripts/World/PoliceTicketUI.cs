using UnityEngine;
using UnityEngine.UI;

// ใบสั่ง: the ticket the traffic police hand you — what for, how fast, the limit, the fine and whether
// it was paid (or let off with a warning). Built at runtime; one at a time.
public class PoliceTicketUI : MonoBehaviour
{
    private static PoliceTicketUI instance;

    private GameObject backdrop;
    private Text title, subtitle, body, total;

    public static void Show(PoliceClient.Ticket t)
    {
        if (instance == null) instance = new GameObject("PoliceTicket").AddComponent<PoliceTicketUI>();
        instance.Fill(t);
        instance.backdrop.SetActive(true);
    }

    private void Awake()
    {
        var size = UiKit.WindowSize(new Vector2(900f, 640f), 760f);
        var card = UiKit.PremiumWindow(transform, "TicketCanvas", size, 60, "ตร.", out backdrop, out title, out subtitle, Hide, out _);
        float w = size.x - 40f, h = size.y - 156f;
        var paper = UiKit.Panel("Paper", card, Color.white, new Vector2(0f, 40f), new Vector2(w - 60f, h - 150f), shadow: true);
        paper.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        body = UiKit.Label(paper, 30, "", new Vector2(0f, 20f), new Vector2(w - 110f, h - 220f), UiKit.Ink, TextAnchor.UpperLeft);
        body.lineSpacing = 1.15f;
        total = UiKit.Label(paper, 36, "", new Vector2(0f, -(h - 150f) * 0.5f + 45f), new Vector2(w - 110f, 56f), UiKit.Danger);
        total.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(card, "รับทราบ", new Vector2(0f, -h * 0.5f + 60f), new Vector2(300f, 80f), UiKit.Gold, UiKit.GoldInk, Hide, 32);
        backdrop.SetActive(false);
    }

    private void Fill(PoliceClient.Ticket t)
    {
        bool checkpoint = t.kind == "checkpoint";
        title.text = t.warning ? "ตำรวจจราจร · ตักเตือน" : "ใบสั่งเจ้าพนักงานจราจร";
        subtitle.text = checkpoint ? "ด่านตรวจ · ไม่ชะลอความเร็วผ่านด่าน" : "ขับรถเร็วเกินกว่าที่กฎหมายกำหนด";
        string place = Locations.Current != null ? Locations.Current.NameTh.Split('·')[0].Trim() : "";
        body.text = (checkpoint
                ? $"ข้อหา: ขับผ่านด่านตรวจโดยไม่หยุด/ชะลอ\nความเร็วขณะผ่านด่าน: {t.speed} กม./ชม.\nต้องชะลอให้ไม่เกิน: {t.limit} กม./ชม."
                : $"ข้อหา: ขับรถเร็วเกินกำหนด\nความเร็วที่ตรวจจับได้: {t.speed} กม./ชม.\nความเร็วที่กำหนด: {t.limit} กม./ชม. (เกิน {t.speed - t.limit})")
            + (string.IsNullOrEmpty(place) ? "" : $"\nสถานที่: {place}");
        total.text = t.warning ? "ครั้งนี้ตักเตือน · ขับช้าลงนะครับ" : $"ค่าปรับ ฿{t.fine:N0} · ชำระแล้ว";
        total.color = t.warning ? UiKit.Price : UiKit.Danger;
        if (!t.warning) UiKit.FloatAbovePlayer($"-฿{t.paid:N0} ค่าปรับ", UiKit.Danger);
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
