using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// มหาลัย ก.ไก่: your paper card with 4 Thai letters on the left, the blackboard on the right.
// The letters the host drew from the drum (already decided by the server) are chalked up one
// by one; matching ones light up on your card until it's complete.
public class KokaiUI : MonoBehaviour
{
    private const float CallInterval = 0.35f;

    private static KokaiUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, resultText;
    private RectTransform cardRoot, board;
    private readonly List<Image> cardCells = new List<Image>();
    private Coroutine running;

    public static void Show(FestivalClient.State r)
    {
        if (instance == null)
            instance = new GameObject("KokaiUI").AddComponent<KokaiUI>();
        instance.backdrop.SetActive(true);
        if (instance.running != null) instance.StopCoroutine(instance.running);
        instance.running = instance.StartCoroutine(instance.Play(r));
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "KokaiCanvas", new Vector2(1400f, 860f), 58, "icon:fair",
            out backdrop, out titleText, out subtitleText, Close, out _);
        titleText.text = "มหาลัย ก.ไก่";
        subtitleText.text = "อักษรบนกระดาษครบ 4 ตัวเร็ว ได้รางวัลใหญ่";

        var paper = UiKit.Panel("Paper", body, new Color(0.99f, 0.98f, 0.94f), new Vector2(-450f, 40f), new Vector2(400f, 480f), shadow: true);
        paper.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(paper, 28, "กระดาษของคุณ", new Vector2(0f, 195f), new Vector2(360f, 44f), UiKit.Ink).fontStyle = FontStyle.Bold;
        cardRoot = UiKit.Container(paper, new Vector2(0f, -20f));

        var slate = UiKit.Panel("Blackboard", body, new Color(0.13f, 0.25f, 0.18f), new Vector2(230f, 40f), new Vector2(820f, 560f), shadow: true);
        slate.gameObject.AddComponent<Outline>().effectColor = new Color(0.45f, 0.3f, 0.15f);
        UiKit.Label(slate, 26, "กระดานดำ", new Vector2(0f, 245f), new Vector2(400f, 40f), new Color(0.9f, 0.95f, 0.85f));
        board = UiKit.Container(slate, new Vector2(0f, 0f));

        resultText = UiKit.Label(body, 34, "", new Vector2(0f, -280f), new Vector2(1200f, 60f), UiKit.Ink);
        resultText.fontStyle = FontStyle.Bold;
        UiKit.RoundButton(body, "ปิด", new Vector2(560f, -280f), new Vector2(160f, 64f), UiKit.Pill, UiKit.Price, Close, 28);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private IEnumerator Play(FestivalClient.State r)
    {
        UiKit.Clear(cardRoot);
        UiKit.Clear(board);
        cardCells.Clear();
        resultText.text = "พิธีกรกำลังหมุนกลอง...";
        resultText.color = UiKit.Muted;

        for (int i = 0; i < r.card.Length; i++)
        {
            var cell = UiKit.Panel("Letter", cardRoot, Color.white, new Vector2(i % 2 == 0 ? -85f : 85f, i < 2 ? 85f : -85f), new Vector2(150f, 150f));
            cell.gameObject.AddComponent<Outline>().effectColor = UiKit.Ink;
            UiKit.Label(cell, 90, r.card[i], Vector2.zero, new Vector2(140f, 140f), UiKit.Ink).fontStyle = FontStyle.Bold;
            cardCells.Add(cell.GetComponent<Image>());
        }

        var wanted = new List<string>(r.card);
        yield return new WaitForSeconds(0.6f);
        for (int i = 0; i < r.draws.Length; i++)
        {
            string letter = r.draws[i];
            int col = i % 11, row = i / 11;
            var chalk = UiKit.Label(board, 46, letter, new Vector2(-325f + col * 65f, 170f - row * 105f), new Vector2(60f, 90f), new Color(0.95f, 0.97f, 0.92f));
            int hit = wanted.IndexOf(letter);
            if (hit >= 0)
            {
                chalk.color = new Color(1f, 0.9f, 0.4f);
                cardCells[System.Array.IndexOf(r.card, letter)].color = new Color(0.55f, 0.85f, 0.5f);
                wanted[hit] = null;
            }
            resultText.text = $"เรียกตัวที่ {i + 1}: {letter}";
            GameAudio.Play("bell", null, hit >= 0 ? 0.9f : 0.5f);
            yield return new WaitForSeconds(CallInterval);
        }

        resultText.text = r.prize;
        resultText.color = r.won ? UiKit.OkGreen : UiKit.Danger;
        if (r.won) UiKit.FloatAbovePlayer(r.prize, new Color(1f, 0.85f, 0.3f));
        running = null;
    }
}
