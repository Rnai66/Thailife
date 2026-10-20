using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ปาโป่ง: a wooden board full of balloons. Buy three darts, then tap the balloons you aim at; each
// dart flies to the board and pops it — or misses and sticks in the wood beside it. The server
// decided beforehand which throws hit; how many pop picks the prize (1 ลูกชิ้น · 2 ชานม · 3 ตุ๊กตา).
public class DartsUI : MonoBehaviour
{
    private static DartsUI instance;
    private const int Cols = 6, Rows = 4;

    private static readonly Color[] Colors =
    {
        new Color(0.95f, 0.25f, 0.3f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.6f, 1f),
        new Color(0.4f, 0.85f, 0.4f), new Color(0.9f, 0.4f, 0.9f), new Color(1f, 0.55f, 0.2f),
    };

    private GameObject backdrop, prizeLayer;
    private RectTransform body, board, stuck;
    private Text title, subtitle, dartsText, toast;
    private Button buyButton;
    private readonly List<RectTransform> balloons = new List<RectTransform>();
    private readonly List<Image> dartIcons = new List<Image>();
    private Coroutine toastRun;
    private FestivalClient.State round;
    private int next;            // index of the next dart in round.throws
    private int price = 10;
    private bool busy, flying;

    public static void Show(string boothName, int ticketPrice)
    {
        if (instance == null) instance = new GameObject("DartsUI").AddComponent<DartsUI>();
        instance.price = ticketPrice;
        instance.title.text = boothName;
        instance.buyButton.GetComponentInChildren<Text>().text = $"ซื้อลูกดอก 3 ดอก · {ticketPrice} ฿";
        instance.backdrop.SetActive(true);
        instance.Refresh();
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (prizeLayer != null) Destroy(prizeLayer);
    }

    private void Awake()
    {
        body = UiKit.PremiumWindow(transform, "DartsCanvas", new Vector2(1540f, 900f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        subtitle.text = "ปาลูกดอกให้ลูกโป่งแตก แตกมากได้รางวัลใหญ่";

        board = UiKit.Panel("Board", body, Color.white, new Vector2(-170f, 18f), new Vector2(1060f, 640f), shadow: true);
        board.GetComponent<Image>().sprite = UiKit.WoodSprite;
        board.GetComponent<Image>().type = Image.Type.Sliced;
        board.gameObject.AddComponent<Outline>().effectColor = UiKit.WoodEdge;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                var b = UiKit.Rect("Balloon", board, Colors[(r * 2 + c) % Colors.Length]);
                b.GetComponent<Image>().sprite = UiKit.CircleSprite;
                b.anchoredPosition = new Vector2(-400f + c * 160f, 210f - r * 140f);
                b.sizeDelta = new Vector2(100f, 120f);
                var shine = UiKit.Rect("Shine", b, new Color(1f, 1f, 1f, 0.45f));
                shine.GetComponent<Image>().sprite = UiKit.CircleSprite;
                shine.anchoredPosition = new Vector2(-20f, 28f);
                shine.sizeDelta = new Vector2(24f, 34f);
                var knot = UiKit.Rect("Knot", b, b.GetComponent<Image>().color * 0.8f);
                knot.anchoredPosition = new Vector2(0f, -62f);
                knot.sizeDelta = new Vector2(14f, 14f);
                knot.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var target = b;
                b.gameObject.AddComponent<Button>().onClick.AddListener(() => Throw(target));
                balloons.Add(b);
            }
        stuck = UiKit.Container(board, Vector2.zero);

        var side = UiKit.Panel("Side", body, Color.white, new Vector2(565f, 18f), new Vector2(340f, 640f), shadow: true);
        side.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(side, 28, "ลูกดอก", new Vector2(0f, 270f), new Vector2(300f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
        for (int i = 0; i < 3; i++)
        {
            var d = Dart(side, new Vector2(-80f + i * 80f, 180f));
            dartIcons.Add(d.GetComponent<Image>());
        }
        dartsText = UiKit.Label(side, 22, "", new Vector2(0f, 105f), new Vector2(310f, 40f), UiKit.Muted);
        UiKit.Label(side, 24, "รางวัล", new Vector2(0f, 50f), new Vector2(300f, 36f), UiKit.Price).fontStyle = FontStyle.Bold;
        string[] icons = { "food_look_chin", "food_bubble_tea", "sell_goods_teddy" };
        string[] names = { "แตก 1 ลูก · ลูกชิ้นปิ้ง", "แตก 2 ลูก · ชานมไข่มุก", "แตก 3 ลูก · ตุ๊กตาหมี" };
        for (int i = 0; i < 3; i++)
        {
            UiKit.IconTile(side, icons[i], new Vector2(-110f, -10f - i * 90f), new Vector2(74f, 74f));
            UiKit.Label(side, 20, names[i], new Vector2(40f, -10f - i * 90f), new Vector2(220f, 70f), UiKit.Ink, TextAnchor.MiddleLeft);
        }
        buyButton = UiKit.RoundButton(side, "", new Vector2(0f, -260f), new Vector2(300f, 76f), UiKit.Gold, UiKit.GoldInk, Buy, 24);

        toast = UiKit.ToastBar(body, new Vector2(0f, -320f), 900f);
        backdrop.SetActive(false);
    }

    // A dart: red fletching, wooden shaft, steel tip pointing up
    private static RectTransform Dart(RectTransform parent, Vector2 at)
    {
        var d = UiKit.Rect("Dart", parent, new Color(0.7f, 0.5f, 0.3f));
        d.anchoredPosition = at;
        d.sizeDelta = new Vector2(8f, 80f);
        var tip = UiKit.Rect("Tip", d, new Color(0.75f, 0.78f, 0.82f));
        tip.anchoredPosition = new Vector2(0f, 46f);
        tip.sizeDelta = new Vector2(5f, 16f);
        var fl = UiKit.Rect("Fletch", d, new Color(0.9f, 0.2f, 0.2f));
        fl.anchoredPosition = new Vector2(0f, -32f);
        fl.sizeDelta = new Vector2(26f, 22f);
        fl.localRotation = Quaternion.Euler(0f, 0f, 45f);
        foreach (var g in d.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        return d;
    }

    private int Left => round != null && round.throws != null ? round.throws.Length - next : 0;

    private void Refresh()
    {
        int left = Left;
        for (int i = 0; i < dartIcons.Count; i++) dartIcons[i].color = i < left ? new Color(0.7f, 0.5f, 0.3f) : new Color(0.85f, 0.85f, 0.85f);
        dartsText.text = left > 0 ? $"เหลือ {left} ดอก · แตะลูกโป่งเพื่อปา" : "ซื้อลูกดอกก่อนเล่น";
        buyButton.interactable = left == 0 && !busy && !flying;
    }

    private async void Buy()
    {
        var f = FestivalClient.Instance;
        if (busy || f == null || Left > 0) return;
        busy = true;
        Refresh();
        try
        {
            round = await f.PlayAsync("darts");
            next = 0;
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            // Fresh balloons, darts pulled out of the board
            foreach (var b in balloons) { b.gameObject.SetActive(true); b.localScale = Vector3.one; }
            UiKit.Clear(stuck);
            if (prizeLayer != null) Destroy(prizeLayer);
            GameAudio.Play("coins", null, 0.5f);
        }
        catch (Exception e) { Say(FestivalBooth.Explain(e, price), UiKit.Danger); }
        busy = false;
        Refresh();
    }

    private void Throw(RectTransform target)
    {
        if (flying) return;
        if (Left <= 0) { Say("ซื้อลูกดอกก่อนนะ", UiKit.Danger); return; }
        bool hit = round.throws[next++];
        StartCoroutine(Fly(target, hit));
    }

    private IEnumerator Fly(RectTransform target, bool hit)
    {
        flying = true;
        Refresh();
        var dart = Dart(stuck, new Vector2(target.anchoredPosition.x * 0.3f, -380f));
        Vector2 aim = target.anchoredPosition;
        if (!hit) aim += new Vector2(UnityEngine.Random.value < 0.5f ? -68f : 68f, UnityEngine.Random.Range(-40f, 40f)); // into the wood between
        Vector2 from = dart.anchoredPosition;
        GameAudio.Play("whoosh", null, 0.6f);
        for (float t = 0f; t < 0.32f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.32f;
            dart.anchoredPosition = Vector2.Lerp(from, aim, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 60f;
            dart.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.8f, k);
            yield return null;
        }
        dart.anchoredPosition = aim;
        if (hit)
        {
            GameAudio.Play("punch", null, 0.9f);
            Pop(target);
            Destroy(dart.gameObject);
            Float("ป๊อป!", aim, new Color(1f, 0.95f, 0.5f));
        }
        else
        {
            GameAudio.Play("punch_heavy", null, 0.4f);
            Float("พลาด!", aim + Vector2.up * 70f, new Color(1f, 0.85f, 0.8f));
            StartCoroutine(Wobble(target));
        }
        yield return new WaitForSecondsRealtime(0.35f);
        flying = false;
        Refresh();
        if (Left == 0 && round != null)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            prizeLayer = FairPrize.Show(this, body, round.pops > 0 ? $"แตก {round.pops} ลูก!" : "ไม่แตกเลย", round.icon, round.prize, round.won, round.pops == 3);
            if (round.won) LifeClient.Instance?.Refresh();
            round = null;
            Refresh();
        }
    }

    private void Pop(RectTransform balloon)
    {
        var color = balloon.GetComponent<Image>().color;
        for (int i = 0; i < 8; i++) StartCoroutine(Shard(balloon.anchoredPosition, color, i));
        balloon.gameObject.SetActive(false);
    }

    private IEnumerator Shard(Vector2 at, Color color, int i)
    {
        var s = UiKit.Rect("Shard", stuck, color);
        s.sizeDelta = new Vector2(16f, 10f);
        Vector2 v = Quaternion.Euler(0f, 0f, i * 45f + 10f) * Vector2.up * 380f;
        for (float t = 0f; t < 0.45f && s != null; t += Time.unscaledDeltaTime)
        {
            s.anchoredPosition = at + v * t + Vector2.down * 600f * t * t;
            s.localRotation = Quaternion.Euler(0f, 0f, t * 720f + i * 30f);
            s.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 1f - t / 0.45f);
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }

    private IEnumerator Wobble(RectTransform b)
    {
        for (float t = 0f; t < 0.5f && b != null; t += Time.unscaledDeltaTime)
        {
            b.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 8f * (1f - t / 0.5f));
            yield return null;
        }
        if (b != null) b.localRotation = Quaternion.identity;
    }

    private void Float(string text, Vector2 at, Color color)
    {
        var label = UiKit.Cartoon(UiKit.Label(stuck, 40, text, at, new Vector2(240f, 60f), color), 3f);
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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
