using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// บิงโก: your 5x5 card (B I N G O, free middle) on the left, the caller's ball on the right. Buy a card
// and the balls are called one by one; numbers on your card get stamped, until a row, column or
// diagonal is complete. The fewer balls it takes, the bigger the prize. The server draws the balls.
public class BingoUI : MonoBehaviour
{
    private static BingoUI instance;
    private const float CallInterval = 0.5f;
    private const int MaxCalls = 34; // festival.lua BINGO_MAX_CALLS

    private static readonly string Letters = "BINGO";
    private static readonly Color[] LetterColors =
    {
        new Color(0.2f, 0.45f, 0.9f), new Color(0.9f, 0.25f, 0.3f), new Color(0.95f, 0.7f, 0.15f), new Color(0.25f, 0.65f, 0.35f), new Color(0.6f, 0.35f, 0.8f),
    };

    private GameObject backdrop, prizeLayer;
    private RectTransform body, history;
    private Text title, subtitle, ballText, countText, toast;
    private Image ball;
    private Button buyButton;
    private readonly Text[] numbers = new Text[25];
    private readonly Image[] cells = new Image[25];
    private readonly Image[] stamps = new Image[25];
    private Coroutine toastRun;
    private int price = 30;
    private bool busy, playing;

    public static void Show(string boothName, int ticketPrice)
    {
        if (instance == null) instance = new GameObject("BingoUI").AddComponent<BingoUI>();
        instance.price = ticketPrice;
        instance.title.text = boothName;
        instance.buyButton.GetComponentInChildren<Text>().text = $"ซื้อการ์ด · {ticketPrice} ฿";
        instance.backdrop.SetActive(true);
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (prizeLayer != null) Destroy(prizeLayer);
    }

    private static char LetterFor(int n) => Letters[Mathf.Clamp((n - 1) / 15, 0, 4)];

    private void Awake()
    {
        body = UiKit.PremiumWindow(transform, "BingoCanvas", new Vector2(1540f, 900f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        subtitle.text = "ครบแถวเร็ว ได้รางวัลใหญ่ · แจ็กพอต 500 บาท";

        // The card
        var card = UiKit.Panel("Card", body, Color.white, new Vector2(-350f, 10f), new Vector2(660f, 650f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        for (int c = 0; c < 5; c++)
        {
            var head = UiKit.Rect("Letter", card, LetterColors[c]);
            head.GetComponent<Image>().sprite = UiKit.CircleSprite;
            head.anchoredPosition = new Vector2(-240f + c * 120f, 265f);
            head.sizeDelta = new Vector2(84f, 84f);
            UiKit.Cartoon(UiKit.Label(head, 50, Letters[c].ToString(), Vector2.zero, new Vector2(84f, 84f), Color.white), 2f);
        }
        for (int i = 0; i < 25; i++)
        {
            int r = i / 5, c = i % 5;
            var cell = UiKit.Panel("Cell", card, new Color(0.98f, 0.96f, 0.9f), new Vector2(-240f + c * 120f, 150f - r * 104f), new Vector2(112f, 96f));
            cell.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
            cells[i] = cell.GetComponent<Image>();
            var stamp = UiKit.Rect("Stamp", cell, new Color(0.9f, 0.15f, 0.2f, 0.55f));
            stamp.GetComponent<Image>().sprite = UiKit.CircleSprite;
            stamp.sizeDelta = new Vector2(80f, 80f);
            stamps[i] = stamp.GetComponent<Image>();
            numbers[i] = UiKit.Label(cell, 44, "", Vector2.zero, new Vector2(110f, 90f), UiKit.Ink);
            numbers[i].font = ThaiFont.Display;
        }

        // The caller
        var caller = UiKit.Panel("Caller", body, new Color(0.12f, 0.16f, 0.32f), new Vector2(400f, 10f), new Vector2(660f, 650f), shadow: true);
        caller.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(caller, 26, "พิธีกรเรียกเลข", new Vector2(0f, 285f), new Vector2(600f, 40f), new Color(1f, 0.9f, 0.6f));
        var ballRt = UiKit.Rect("Ball", caller, Color.white);
        ballRt.GetComponent<Image>().sprite = UiKit.CircleSprite;
        ballRt.anchoredPosition = new Vector2(0f, 150f);
        ballRt.sizeDelta = new Vector2(200f, 200f);
        ball = ballRt.GetComponent<Image>();
        ballText = UiKit.Label(ballRt, 66, "?", Vector2.zero, new Vector2(200f, 120f), UiKit.Ink);
        ballText.font = ThaiFont.Display;
        countText = UiKit.Label(caller, 24, "", new Vector2(0f, 25f), new Vector2(600f, 36f), new Color(0.9f, 0.9f, 1f));
        history = UiKit.Container(caller, new Vector2(0f, -110f));
        buyButton = UiKit.RoundButton(caller, "", new Vector2(0f, -270f), new Vector2(320f, 76f), UiKit.Gold, UiKit.GoldInk, Buy, 26);

        toast = UiKit.ToastBar(body, new Vector2(0f, -320f), 900f);
        Blank();
        backdrop.SetActive(false);
    }

    private void Blank()
    {
        for (int i = 0; i < 25; i++)
        {
            numbers[i].text = i == 12 ? "ฟรี" : "?";
            stamps[i].gameObject.SetActive(i == 12);
            cells[i].color = new Color(0.98f, 0.96f, 0.9f);
        }
        ballText.text = "?";
        ball.color = Color.white;
        countText.text = "ซื้อการ์ดแล้วรอฟังพิธีกรเรียกเลข";
        UiKit.Clear(history);
    }

    private async void Buy()
    {
        var f = FestivalClient.Instance;
        if (busy || playing || f == null) return;
        busy = true;
        buyButton.interactable = false;
        try
        {
            var r = await f.PlayAsync("bingo");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            if (prizeLayer != null) Destroy(prizeLayer);
            StartCoroutine(Run(r));
        }
        catch (Exception e)
        {
            Say(FestivalBooth.Explain(e, price), UiKit.Danger);
            buyButton.interactable = true;
        }
        busy = false;
    }

    private IEnumerator Run(FestivalClient.State r)
    {
        playing = true;
        Blank();
        var where = new Dictionary<int, int>();
        for (int i = 0; i < 25 && i < r.bingo_card.Length; i++)
        {
            int n = r.bingo_card[i];
            if (n > 0) { numbers[i].text = n.ToString(); where[n] = i; }
        }
        var marked = new bool[25];
        marked[12] = true;
        yield return new WaitForSecondsRealtime(0.6f);

        for (int k = 0; k < r.calls.Length; k++)
        {
            int n = r.calls[k];
            char letter = LetterFor(n);
            ballText.text = $"{letter}\n{n}";
            ballText.fontSize = 56;
            ball.color = Color.Lerp(Color.white, LetterColors[Letters.IndexOf(letter)], 0.35f);
            countText.text = $"ลูกที่ {k + 1} / {MaxCalls}";
            StartCoroutine(Bounce(ball.rectTransform));
            AddHistory(n, k);
            bool mine = where.TryGetValue(n, out int cell);
            if (mine)
            {
                marked[cell] = true;
                StartCoroutine(Stamp(stamps[cell]));
            }
            GameAudio.Play("bell", null, mine ? 0.9f : 0.4f);
            yield return new WaitForSecondsRealtime(CallInterval);
        }

        var line = Line(marked);
        if (line != null)
            foreach (int i in line) cells[i].color = new Color(1f, 0.85f, 0.35f);
        yield return new WaitForSecondsRealtime(0.6f);
        prizeLayer = FairPrize.Show(this, body, r.won ? "บิงโก!" : (line != null ? "บิงโก… ช้าไป" : "ยังไม่ครบแถว"), "", r.prize, r.won, r.won && r.calls.Length <= 22);
        if (r.won) UiKit.FloatAbovePlayer(r.prize, new Color(1f, 0.85f, 0.3f));
        playing = false;
        buyButton.interactable = true;
    }

    // The first complete row, column or diagonal
    private static int[] Line(bool[] m)
    {
        for (int r = 0; r < 5; r++)
            if (m[r * 5] && m[r * 5 + 1] && m[r * 5 + 2] && m[r * 5 + 3] && m[r * 5 + 4]) return new[] { r * 5, r * 5 + 1, r * 5 + 2, r * 5 + 3, r * 5 + 4 };
        for (int c = 0; c < 5; c++)
            if (m[c] && m[5 + c] && m[10 + c] && m[15 + c] && m[20 + c]) return new[] { c, 5 + c, 10 + c, 15 + c, 20 + c };
        if (m[0] && m[6] && m[12] && m[18] && m[24]) return new[] { 0, 6, 12, 18, 24 };
        if (m[4] && m[8] && m[12] && m[16] && m[20]) return new[] { 4, 8, 12, 16, 20 };
        return null;
    }

    private void AddHistory(int n, int k)
    {
        int col = k % 9, row = k / 9;
        var b = UiKit.Rect("Called", history, LetterColors[Letters.IndexOf(LetterFor(n))]);
        b.GetComponent<Image>().sprite = UiKit.CircleSprite;
        b.anchoredPosition = new Vector2(-256f + col * 64f, 60f - row * 58f);
        b.sizeDelta = new Vector2(54f, 54f);
        UiKit.Label(b, 24, n.ToString(), Vector2.zero, new Vector2(54f, 40f), Color.white).fontStyle = FontStyle.Bold;
    }

    private static IEnumerator Bounce(RectTransform r)
    {
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            r.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.12f);
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    private static IEnumerator Stamp(Image s)
    {
        s.gameObject.SetActive(true);
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            s.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, t / 0.2f);
            yield return null;
        }
        s.rectTransform.localScale = Vector3.one;
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
