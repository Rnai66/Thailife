using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ปาโป่ง: balloons bobbing and drifting across a wooden board. Five darts a round: tap where the
// dart should go. It takes a moment to fly, so aim a little ahead of a moving balloon; it pops the
// balloon it lands on, or sticks in the wood.
public class DartsUI : SkillGameUI
{
    private static DartsUI instance;
    private const int Cols = 7, Rows = 4;
    private const float Flight = 0.32f, Span = 1150f, Radius = 46f;

    private static readonly Color[] Colors =
    {
        new Color(0.95f, 0.25f, 0.3f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.6f, 1f),
        new Color(0.4f, 0.85f, 0.4f), new Color(0.9f, 0.4f, 0.9f), new Color(1f, 0.55f, 0.2f),
    };

    private class Balloon
    {
        public RectTransform rect;
        public int row;
        public float x, phase;
        public bool popped;
    }

    private readonly List<Balloon> balloons = new List<Balloon>();
    private readonly float[] drift = { 60f, -80f, 50f, -70f };
    private RectTransform stuck;
    private int flying;

    protected override string GameId => "darts";
    protected override string Title => "ปาโป่ง";
    protected override string Subtitle => "แตะลูกโป่งเพื่อปา ลูกดอกบินช้านิดหน่อย เล็งดักหน้าไว้";
    protected override string ShotWord => "ดอก";

    public static void Show()
    {
        if (instance == null) instance = new GameObject("DartsUI").AddComponent<DartsUI>();
        instance.Open();
    }

    protected override void BuildArena(RectTransform a)
    {
        var img = a.GetComponent<Image>();
        img.sprite = UiKit.WoodSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                var color = Colors[(r * 2 + c) % Colors.Length];
                var b = UiKit.Rect("Balloon", a, color);
                b.GetComponent<Image>().sprite = UiKit.CircleSprite;
                b.sizeDelta = new Vector2(Radius * 2f, Radius * 2.4f);
                var shine = UiKit.Rect("Shine", b, new Color(1f, 1f, 1f, 0.45f));
                shine.GetComponent<Image>().sprite = UiKit.CircleSprite;
                shine.anchoredPosition = new Vector2(-18f, 26f);
                shine.sizeDelta = new Vector2(22f, 32f);
                var knot = UiKit.Rect("Knot", b, color * 0.8f);
                knot.anchoredPosition = new Vector2(0f, -58f);
                knot.sizeDelta = new Vector2(14f, 14f);
                knot.localRotation = Quaternion.Euler(0f, 0f, 45f);
                balloons.Add(new Balloon { rect = b, row = r, x = -Span * 0.5f + (c + 0.5f + (r % 2) * 0.5f) * Span / Cols, phase = c * 1.3f + r });
            }
        stuck = UiKit.Container(a, Vector2.zero);
        Place();
    }

    private Vector2 Where(Balloon b, float time) =>
        new Vector2(b.x, 210f - b.row * 140f + Mathf.Sin(time * 2.2f + b.phase) * 14f);

    protected override void Begin(System.Random rng)
    {
        for (int r = 0; r < Rows; r++)
        {
            float s = 50f + (float)rng.NextDouble() * 70f;
            drift[r] = r % 2 == 0 ? s : -s;
        }
        foreach (var b in balloons)
        {
            b.popped = false;
            b.rect.gameObject.SetActive(true);
        }
        UiKit.Clear(stuck);
    }

    private void Update()
    {
        foreach (var b in balloons)
        {
            b.x += drift[b.row] * Time.unscaledDeltaTime;
            if (b.x > Span * 0.5f) b.x -= Span;
            if (b.x < -Span * 0.5f) b.x += Span;
        }
        Place();
    }

    private void Place()
    {
        float now = Time.unscaledTime;
        foreach (var b in balloons) b.rect.anchoredPosition = Where(b, now);
    }

    protected override void Tap(Vector2 at)
    {
        if (flying > 0 && Left <= flying) return; // every dart left is already in the air
        StartCoroutine(Fly(at));
    }

    private IEnumerator Fly(Vector2 aim)
    {
        flying++;
        var dart = Dart(stuck, new Vector2(aim.x * 0.3f, -380f));
        Vector2 from = dart.anchoredPosition;
        GameAudio.Play("whoosh", null, 0.6f);
        for (float t = 0f; t < Flight; t += Time.unscaledDeltaTime)
        {
            float k = t / Flight;
            dart.anchoredPosition = Vector2.Lerp(from, aim, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 60f;
            dart.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.8f, k);
            yield return null;
        }
        dart.anchoredPosition = aim;
        // Whatever balloon is under the tip now pops
        Balloon hit = null;
        foreach (var b in balloons)
        {
            if (b.popped) continue;
            Vector2 d = aim - b.rect.anchoredPosition;
            if (d.x * d.x / (Radius * Radius) + d.y * d.y / (Radius * 1.2f * Radius * 1.2f) <= 1f) { hit = b; break; }
        }
        if (hit != null)
        {
            hit.popped = true;
            GameAudio.Play("punch", null, 0.9f);
            Pop(hit.rect);
            Destroy(dart.gameObject);
            Float("ป๊อป!", aim, new Color(1f, 0.95f, 0.5f));
        }
        else
        {
            GameAudio.Play("punch_heavy", null, 0.4f);
            Float("พลาด!", aim + Vector2.up * 70f, new Color(1f, 0.85f, 0.8f));
        }
        flying--;
        Shot(hit != null);
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

    private void Pop(RectTransform balloon)
    {
        var color = balloon.GetComponent<Image>().color;
        for (int i = 0; i < 8; i++) StartCoroutine(Shard(balloon.anchoredPosition, color, i));
        balloon.gameObject.SetActive(false);
    }

    private IEnumerator Shard(Vector2 at, Color color, int i)
    {
        var s = UiKit.Rect("Shard", stuck, color);
        s.GetComponent<Image>().raycastTarget = false;
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

    // Round over: fresh balloons, darts pulled out of the board
    protected override void Ended()
    {
        foreach (var b in balloons)
        {
            b.popped = false;
            b.rect.gameObject.SetActive(true);
        }
        UiKit.Clear(stuck);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
