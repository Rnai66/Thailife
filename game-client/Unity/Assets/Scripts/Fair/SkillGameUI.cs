using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shared frame for the fair's skill games (ยิงปืนจุก, โยนห่วง, ปาโป่ง): the play area on the left, a side
// panel with the shots left, what each score pays and the start button. A round costs one coupon
// (fair_start); the player's own hits are sent back at the end (fair_finish) and paid in points.
public abstract class SkillGameUI : MonoBehaviour
{
    protected abstract string GameId { get; }
    protected abstract string Title { get; }
    protected abstract string Subtitle { get; }
    protected abstract string ShotWord { get; } // "จุก", "ห่วง", "ดอก"

    // Lay out the play area once; Begin is called with the round's seed when a coupon is spent;
    // Tap gets every press in the play area (local units, centre = 0,0; 1060 x 640)
    protected abstract void BuildArena(RectTransform arena);
    protected abstract void Begin(System.Random rng);
    protected abstract void Tap(Vector2 at);

    protected RectTransform arena;
    protected bool Playing => round != null && left > 0;
    protected int Left => left;

    private GameObject backdrop, prizeLayer;
    private RectTransform body;
    private Text title, subtitle, strip, shotsText, table, toast, scoreText;
    private Button startButton;
    private Coroutine toastRun;
    private string round;
    private int shots, left, hits;
    private bool busy;

    protected void Open()
    {
        backdrop.SetActive(true);
        Load();
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (prizeLayer != null) Destroy(prizeLayer);
    }

    protected virtual void Awake()
    {
        body = UiKit.PremiumWindow(transform, GameId + "Canvas", UiKit.WindowSize(new Vector2(1540f, 900f), 1460f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        title.text = Title;
        subtitle.text = Subtitle;

        var frame = UiKit.Panel("Arena", body, new Color(0.12f, 0.1f, 0.16f), new Vector2(-170f, 18f), new Vector2(1060f, 640f), shadow: true);
        frame.gameObject.AddComponent<RectMask2D>();
        frame.gameObject.AddComponent<Outline>().effectColor = UiKit.WoodEdge;
        arena = frame;
        BuildArena(arena);
        frame.gameObject.AddComponent<TapArea>().Tapped += at => { if (Playing) Tap(at); };

        var side = UiKit.Panel("Side", body, Color.white, new Vector2(565f, 18f), new Vector2(340f, 640f), shadow: true);
        side.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        scoreText = UiKit.Cartoon(UiKit.Label(side, 44, "", new Vector2(0f, 262f), new Vector2(310f, 60f), UiKit.GoldText), 3f);
        shotsText = UiKit.Label(side, 24, "", new Vector2(0f, 205f), new Vector2(310f, 40f), UiKit.Ink);
        UiKit.Label(side, 24, "แต้มที่ได้", new Vector2(0f, 155f), new Vector2(300f, 36f), UiKit.Price).fontStyle = FontStyle.Bold;
        table = UiKit.Label(side, 21, "", new Vector2(0f, -20f), new Vector2(300f, 310f), UiKit.Ink);
        table.supportRichText = true;
        startButton = UiKit.RoundButton(side, "", new Vector2(0f, -262f), new Vector2(300f, 76f), UiKit.Gold, UiKit.GoldInk, StartRound, 24);

        strip = FairHud.Strip(body, new Vector2(-170f, -333f), 900f);
        toast = UiKit.ToastBar(body, new Vector2(-170f, -250f), 900f);
        if (UiKit.Portrait) // upright: the play area on top, the side panel underneath
        {
            UiKit.Stack(frame, new Vector2(0f, 352f), 0.66f);
            UiKit.Stack(side, new Vector2(0f, -250f), 1.1f);
            UiKit.Stack((RectTransform)strip.transform.parent, new Vector2(0f, 110f), 0.75f);
            UiKit.Stack((RectTransform)toast.transform.parent, new Vector2(0f, 200f));
        }
        backdrop.SetActive(false);
    }

    private async void Load()
    {
        var f = FairClient.Instance;
        if (f == null) return;
        Render();
        try { await f.RefreshAsync(); }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
        Render();
    }

    private void Render()
    {
        var s = FairClient.Instance?.Current;
        strip.text = FairHud.Format(s);
        var g = FairClient.Instance?.FindGame(GameId);
        if (g != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int h = g.shots; h >= 1; h--)
            {
                if (g.points[h] == 0) continue;
                string line = $"โดน {h} · {g.points[h]} แต้ม";
                sb.AppendLine(h == g.shots ? $"<b><color=#BA7517>{line} (ครบ!)</color></b>" : line);
            }
            if (g.best > 0) sb.Append($"\nสถิติของคุณ: {g.best}/{g.shots}");
            table.text = sb.ToString();
        }
        scoreText.text = round != null ? $"{hits}/{shots}" : "";
        shotsText.text = round != null ? $"เหลือ {left} {ShotWord}" : g != null ? $"รอบละ 1 คูปอง · {g.shots} {ShotWord}" : "";
        startButton.GetComponentInChildren<Text>().text = s != null && !s.has_pass ? "ซื้อบัตรที่ซุ้มขายบัตรก่อน" : "เล่น · 1 คูปอง";
        startButton.interactable = round == null && !busy && s != null && s.has_pass;
    }

    private async void StartRound()
    {
        var f = FairClient.Instance;
        if (busy || f == null || round != null) return;
        busy = true;
        Render();
        try
        {
            var s = await f.StartAsync(GameId);
            round = s.round;
            shots = left = Mathf.Max(1, f.FindGame(GameId)?.shots ?? 5);
            hits = 0;
            if (prizeLayer != null) Destroy(prizeLayer);
            Begin(new System.Random(s.seed));
            GameAudio.Play("coins", null, 0.4f);
        }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
        busy = false;
        Render();
    }

    // A shot was taken: hit or miss. The last one ends the round after a short pause.
    protected void Shot(bool hit)
    {
        if (!Playing) return;
        left--;
        if (hit) hits++;
        Render();
        if (left == 0) StartCoroutine(Finish());
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        FinishRound();
    }

    private async void FinishRound()
    {
        var f = FairClient.Instance;
        string id = round;
        try
        {
            var s = await f.FinishAsync(id, hits);
            string head = s.hits == s.shots ? "ครบทุกเป้า!" : s.hits > 0 ? $"โดน {s.hits} จาก {s.shots}" : "ไม่โดนเลย";
            string line = s.earned > 0 ? $"ได้ {s.earned} แต้ม" + (s.record ? "\nสถิติใหม่!" : "") : "ลองใหม่อีกรอบนะ";
            prizeLayer = FairPrize.Show(this, body, head, null, line, s.earned > 0, s.hits == s.shots);
        }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
        round = null;
        Render();
        Ended();
    }

    // Round over (after the points came back): stop moving things
    protected virtual void Ended() { }

    // A word popping up over the play area ("ป๊อป!", "พลาด!")
    protected void Float(string text, Vector2 at, Color color)
    {
        var label = UiKit.Cartoon(UiKit.Label(arena, 40, text, at, new Vector2(240f, 60f), color), 3f);
        StartCoroutine(Rise(label.rectTransform));
    }

    private static IEnumerator Rise(RectTransform r)
    {
        Vector2 from = r.anchoredPosition;
        for (float t = 0f; t < 0.8f && r != null; t += Time.unscaledDeltaTime)
        {
            r.anchoredPosition = from + Vector2.up * t * 90f;
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);
}
