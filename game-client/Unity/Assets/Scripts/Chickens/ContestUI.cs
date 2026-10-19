using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Chicken beauty contest (ประกวดไก่สวย) at the monthly temple fair (งานวัด), premium layout:
// left = your entrant, fair status, prizes and the enter button; right = the results board.
// Your most beautiful adult chicken competes with four villagers' chickens; the server scores it.
public class ContestUI : MonoBehaviour
{
    private static readonly Color Silver = new Color32(0xB4, 0xB2, 0xA9, 0xFF);
    private static readonly Color Bronze = new Color32(0xD8, 0x5A, 0x30, 0xFF);

    private static ContestUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private Text entrantName, entrantInfo, fairText;
    private Image entrantDisc, fairPill;
    private RectTransform beautyFill, board;
    private Button enterButton;
    private Coroutine toast;
    private bool busy;
    private float nextRender;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("ContestUI").AddComponent<ContestUI>();
        instance.backdrop.SetActive(true);
        CoopClient.Instance?.Refresh();
        instance.Render();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "ContestCanvas", new Vector2(1500f, 900f), 50, "icon:fair",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "ประกวดไก่สวย";
        subtitleText.text = "งานวัดตามประเพณี · สงกรานต์ ออกพรรษา กฐิน ลอยกระทง ฯลฯ";

        // ── Left column: entrant, fair status, prizes, enter ──
        var entrant = UiKit.Panel("Entrant", body, Color.white, new Vector2(-370f, 230f), new Vector2(660f, 190f), shadow: true);
        entrant.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(entrant, 24, "ตัวแทนของคุณ", new Vector2(40f, 65f), new Vector2(460f, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
        var disc = UiKit.Rect("Feather", entrant, Color.gray);
        disc.GetComponent<Image>().sprite = UiKit.CircleSprite;
        disc.anchoredPosition = new Vector2(-240f, 0f);
        disc.sizeDelta = new Vector2(110f, 110f);
        entrantDisc = disc.GetComponent<Image>();
        entrantName = UiKit.Label(entrant, 36, "", new Vector2(40f, 20f), new Vector2(460f, 50f), UiKit.Ink, TextAnchor.MiddleLeft);
        entrantName.fontStyle = FontStyle.Bold;
        entrantInfo = UiKit.Label(entrant, 24, "", new Vector2(40f, -22f), new Vector2(460f, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
        var track = UiKit.Panel("BeautyTrack", entrant, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(40f, -58f), new Vector2(460f, 16f));
        beautyFill = UiKit.Panel("BeautyFill", track, UiKit.Gold, Vector2.zero, new Vector2(0f, 16f));
        beautyFill.anchorMin = beautyFill.anchorMax = new Vector2(0f, 0.5f);
        beautyFill.pivot = new Vector2(0f, 0.5f);

        var pill = UiKit.Panel("FairStatus", body, UiKit.OkTint, new Vector2(-370f, 95f), new Vector2(660f, 60f));
        fairPill = pill.GetComponent<Image>();
        fairText = UiKit.Label(pill, 26, "", Vector2.zero, new Vector2(640f, 56f), UiKit.OkGreen);
        fairText.fontStyle = FontStyle.Bold;

        Prize(body, new Vector2(-590f, -40f), "ที่ 1", "1,000 ฿", UiKit.Gold, UiKit.GoldInk);
        Prize(body, new Vector2(-370f, -40f), "ที่ 2", "500 ฿", Silver, UiKit.Ink);
        Prize(body, new Vector2(-150f, -40f), "ที่ 3", "250 ฿", Bronze, Color.white);

        enterButton = UiKit.RoundButton(body, "ส่งไก่เข้าประกวด (พลังงาน 5)", new Vector2(-370f, -190f), new Vector2(660f, 100f), UiKit.Gold, UiKit.GoldInk, Enter, 34);
        UiKit.Label(body, 22, "เคล็ดลับ: ผสมพันธุ์ไก่ให้ลูกสวยกว่าพ่อแม่ แล้วส่งตัวที่สวยที่สุด", new Vector2(-370f, -270f), new Vector2(660f, 34f), UiKit.Muted);

        // ── Right column: results board ──
        var boardPanel = UiKit.Panel("Board", body, Color.white, new Vector2(370f, -20f), new Vector2(660f, 660f), shadow: true);
        boardPanel.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(boardPanel, 30, "ผลการประกวด", new Vector2(0f, 290f), new Vector2(600f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
        board = UiKit.Container(boardPanel, new Vector2(0f, 200f));

        toastText = UiKit.ToastBar(body, new Vector2(0f, -330f), 800f);
        backdrop.SetActive(false);
        ShowBoardHint();
    }

    private static void Prize(RectTransform body, Vector2 position, string place, string amount, Color color, Color ink)
    {
        var card = UiKit.Panel("Prize", body, color, position, new Vector2(200f, 120f), shadow: true);
        UiKit.Label(card, 26, place, new Vector2(0f, 24f), new Vector2(180f, 36f), ink).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 32, amount, new Vector2(0f, -20f), new Vector2(180f, 44f), ink).fontStyle = FontStyle.Bold;
    }

    private void ShowBoardHint()
    {
        UiKit.Clear(board);
        UiKit.Label(board, 26, "ส่งไก่เข้าประกวดแล้ว\nผลจะแสดงที่นี่", new Vector2(0f, -180f), new Vector2(560f, 100f), UiKit.Muted);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (backdrop.activeSelf && Time.time >= nextRender) Render();
    }

    private void Render()
    {
        nextRender = Time.time + 1f;
        var coop = CoopClient.Instance;
        if (coop == null || !coop.IsOnline)
        {
            entrantName.text = "ออฟไลน์";
            entrantInfo.text = "ต้องออนไลน์ก่อนถึงจะประกวดได้";
            enterButton.interactable = false;
            return;
        }

        CoopClient.Chicken best = null;
        if (coop.Current.chickens != null)
            foreach (var c in coop.Current.chickens)
                if (c.adult && (best == null || c.beauty > best.beauty)) best = c;

        if (best == null)
        {
            entrantName.text = "ยังไม่มีไก่โตเต็มวัย";
            entrantInfo.text = "เลี้ยงไก่ที่เล้าข้างบ้านก่อน";
            entrantDisc.color = new Color32(0xD3, 0xD1, 0xC7, 0xFF);
            beautyFill.sizeDelta = new Vector2(0f, 16f);
        }
        else
        {
            entrantName.text = best.name;
            entrantInfo.text = $"สี{best.color} · ความสวย {best.beauty}/100";
            entrantDisc.color = CoopClient.FeatherColor(best.color);
            beautyFill.sizeDelta = new Vector2(460f * Mathf.Clamp01(best.beauty / 100f), 16f);
        }

        var s = coop.Current;
        bool canEnter = s.fair_open && !s.entered_this_fair;
        fairText.text = s.fair_open
            ? (s.entered_this_fair ? "งานวัดนี้ส่งประกวดไปแล้ว — รองานวัดครั้งหน้า" : $"งานวัดเปิดอยู่! เหลือเวลาอีก {CoopClient.Countdown(s.fair_ends_in_s - coop.Age)}")
            : $"ยังไม่ถึงงานวัด — งานครั้งถัดไปในอีก {CoopClient.Countdown(s.next_fair_in_s - coop.Age)}";
        fairPill.color = canEnter ? UiKit.OkTint : (Color)new Color32(0xF1, 0xEF, 0xE8, 0xFF);
        fairText.color = canEnter ? UiKit.OkGreen : UiKit.Muted;
        enterButton.interactable = !busy && best != null && canEnter;
    }

    private async void Enter()
    {
        var coop = CoopClient.Instance;
        if (busy || coop == null) return;
        busy = true;
        try
        {
            var r = await coop.EnterContestAsync();
            ShowResults(r);
            if (r.prize > 0)
            {
                Notify($"ยินดีด้วย! ได้ที่ {r.place} รับรางวัล {r.prize:N0} บาท", UiKit.PayGreen);
                UiKit.FloatAbovePlayer($"ที่ {r.place}! +{r.prize} ฿", new Color(1f, 0.85f, 0.2f));
            }
            else Notify($"ได้ที่ {r.place} — ผสมพันธุ์ให้ไก่สวยขึ้นแล้วมาใหม่งานหน้า", UiKit.Price);
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("energy") ? "พลังงานไม่พอ — กินอะไรหน่อย หรือไปนอนพัก"
                : e.Message.Contains("closed") ? "งานวัดยังไม่เปิด"
                : e.Message.Contains("already entered") ? "งานวัดนี้ส่งประกวดไปแล้ว"
                : e.Message.Contains("no adult") ? "ยังไม่มีไก่โตเต็มวัย" : "ส่งประกวดไม่สำเร็จ", UiKit.Danger);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Contest] {e.Message}");
        }
        busy = false;
        Render();
    }

    private void ShowResults(CoopClient.State r)
    {
        UiKit.Clear(board);
        if (r.contest == null) return;
        for (int i = 0; i < r.contest.Length; i++)
        {
            var e = r.contest[i];
            var row = UiKit.Panel("Rank", board, e.mine ? (Color)new Color32(0xFF, 0xF6, 0xDF, 0xFF) : (Color)new Color32(0xFB, 0xF8, 0xF1, 0xFF),
                new Vector2(0f, -i * 100f), new Vector2(600f, 86f));
            if (e.mine) row.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;

            Color medal = i == 0 ? UiKit.Gold : i == 1 ? Silver : i == 2 ? Bronze : (Color)new Color32(0xD3, 0xD1, 0xC7, 0xFF);
            var badge = UiKit.Rect("Place", row, medal);
            badge.GetComponent<Image>().sprite = UiKit.CircleSprite;
            badge.anchoredPosition = new Vector2(-250f, 0f);
            badge.sizeDelta = new Vector2(60f, 60f);
            UiKit.Label(badge, 30, (i + 1).ToString(), Vector2.zero, new Vector2(56f, 56f), i < 2 ? UiKit.GoldInk : Color.white).fontStyle = FontStyle.Bold;

            var name = UiKit.Label(row, 28, e.name, new Vector2(-30f, 0f), new Vector2(360f, 60f), UiKit.Ink, TextAnchor.MiddleLeft);
            if (e.mine) name.fontStyle = FontStyle.Bold;
            UiKit.Label(row, 28, $"{e.score} คะแนน", new Vector2(220f, 0f), new Vector2(150f, 60f), UiKit.Price, TextAnchor.MiddleRight).fontStyle = FontStyle.Bold;
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
