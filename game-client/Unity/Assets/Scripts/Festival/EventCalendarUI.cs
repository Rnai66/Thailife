using UnityEngine;
using UnityEngine.UI;

// Event calendar (ปฏิทินงาน): the coming traditional festivals (งานวัด/งานประเพณี), the monthly
// events in the village, tambon and city, and the national beach concerts — with where and when.
// Opened from the "ปฏิทินงาน" button on the HUD; the list comes from festival.lua.
public class EventCalendarUI : MonoBehaviour
{
    private const int Rows = 10;

    private static EventCalendarUI instance;

    private GameObject backdrop;
    private RectTransform list;
    private Text emptyText;
    private float nextTick;
    private bool portrait;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("EventCalendarUI").AddComponent<EventCalendarUI>();
        instance.backdrop.SetActive(true);
        _ = FestivalClient.Instance?.RefreshAsync();
        instance.Render();
    }

    private void Close() => backdrop.SetActive(false);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "CalendarCanvas", UiKit.WindowSize(new Vector2(1500f, 930f), 1480f), 56, "icon:fair",
            out backdrop, out var title, out var subtitle, Close, out _);
        title.text = "ปฏิทินงานและอีเวนต์";
        subtitle.text = "งานวัด · งานประจำเดือน · คอนเสิร์ตระดับประเทศ";
        list = UiKit.Container(body, Vector2.zero);
        emptyText = UiKit.Label(body, 28, "ต้องออนไลน์ก่อนจึงจะเห็นปฏิทิน", Vector2.zero, new Vector2(680f, 60f), UiKit.Muted);
        backdrop.SetActive(false);
        if (FestivalClient.Instance != null) FestivalClient.Instance.Changed += _ => { if (backdrop.activeSelf) Render(); };
    }

    private void Update()
    {
        if (!backdrop.activeSelf || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        Render(); // keep the countdowns ticking
    }

    private void Render()
    {
        UiKit.Clear(list);
        var entries = FestivalClient.Instance?.Current?.calendar;
        emptyText.gameObject.SetActive(entries == null || entries.Length == 0);
        if (entries == null) return;

        float elapsed = FestivalClient.Instance.SecondsSinceRefresh;
        if (portrait) { RenderPortrait(entries, elapsed); return; }
        for (int i = 0; i < entries.Length && i < Rows; i++)
        {
            var e = entries[i];
            float y = 300f - i * 68f;
            var row = UiKit.Panel("Row", list, e.open ? new Color32(0xFF, 0xF3, 0xD0, 0xFF) : Color.white, new Vector2(0f, y), new Vector2(1400f, 60f));
            row.gameObject.AddComponent<Outline>().effectColor = e.open ? UiKit.Gold : UiKit.CardBorder;

            UiKit.Label(row, 24, e.date, new Vector2(-600f, 0f), new Vector2(180f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
            UiKit.PillLabel(row, e.kind, new Vector2(-420f, 0f), new Vector2(170f, 40f), KindColor(e.kind), Color.white, 20);
            UiKit.Label(row, 26, e.name, new Vector2(-110f, 0f), new Vector2(430f, 50f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(row, 22, e.where, new Vector2(270f, 0f), new Vector2(300f, 50f), UiKit.Muted, TextAnchor.MiddleLeft);

            string status = e.open
                ? $"กำลังจัด! เหลือ {Duration(e.ends_s - elapsed)}"
                : $"อีก {Duration(e.in_s - elapsed)}";
            UiKit.Label(row, 22, status, new Vector2(570f, 0f), new Vector2(260f, 50f), e.open ? UiKit.PayGreen : UiKit.Price, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;
        }
        UiKit.Label(list, 20, "เวลาเป็นเวลาจริง (1 วันในเกม = 24 นาที) · งานประเพณีจัดทั้งที่วัดในหมู่บ้านและในเมือง/ตำบล",
            new Vector2(0f, 300f - Rows * 68f), new Vector2(1400f, 40f), UiKit.Muted);
    }

    // Upright: one card per event, three lines (date + kind / name / place + countdown)
    private void RenderPortrait(FestivalClient.CalendarEntry[] entries, float elapsed)
    {
        for (int i = 0; i < entries.Length && i < Rows; i++)
        {
            var e = entries[i];
            float y = 570f - i * 118f;
            var row = UiKit.Panel("Row", list, e.open ? new Color32(0xFF, 0xF3, 0xD0, 0xFF) : Color.white, new Vector2(0f, y), new Vector2(690f, 108f));
            row.gameObject.AddComponent<Outline>().effectColor = e.open ? UiKit.Gold : UiKit.CardBorder;

            UiKit.Label(row, 24, e.date, new Vector2(-200f, 32f), new Vector2(260f, 36f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.PillLabel(row, e.kind, new Vector2(245f, 32f), new Vector2(170f, 36f), KindColor(e.kind), Color.white, 20);
            UiKit.Label(row, 28, e.name, new Vector2(0f, -2f), new Vector2(660f, 38f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Label(row, 21, e.where, new Vector2(-150f, -36f), new Vector2(360f, 32f), UiKit.Muted, TextAnchor.MiddleLeft);
            string status = e.open ? $"กำลังจัด! เหลือ {Duration(e.ends_s - elapsed)}" : $"อีก {Duration(e.in_s - elapsed)}";
            UiKit.Label(row, 22, status, new Vector2(160f, -36f), new Vector2(340f, 32f), e.open ? UiKit.PayGreen : UiKit.Price, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;
        }
        var note = UiKit.Label(list, 20, "เวลาเป็นเวลาจริง (1 วันในเกม = 24 นาที)\nงานประเพณีจัดทั้งที่วัดในหมู่บ้านและในเมือง/ตำบล",
            new Vector2(0f, 570f - Rows * 118f + 20f), new Vector2(680f, 60f), UiKit.Muted);
        note.lineSpacing = 1.1f;
    }

    private static Color KindColor(string kind) =>
        kind == "ระดับประเทศ" ? new Color32(0x7B, 0x3F, 0xC4, 0xFF)
        : kind == "งานประเพณี" ? new Color32(0xC8, 0x8A, 0x1E, 0xFF)
        : (Color)UiKit.PayGreen;

    private static string Duration(float seconds)
    {
        int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int h = s / 3600, m = s % 3600 / 60;
        return h > 0 ? $"{h} ชม. {m} นาที" : m > 0 ? $"{m} นาที" : "ไม่ถึงนาที";
    }
}
