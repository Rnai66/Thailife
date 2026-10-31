using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// โยนห่วง: rows of soda bottles on a stepped stand. Each of the five rings takes two taps: the first
// stops the arrow swinging left and right (aim), the second stops the power bar (how far it flies).
// A ring that drops over a bottle neck counts.
public class RingTossUI : SkillGameUI
{
    private static RingTossUI instance;

    private static readonly float[] RowY = { 175f, 25f, -125f };   // far, middle, near
    private static readonly float[] RowScale = { 0.72f, 0.86f, 1f };
    private const int PerRow = 5;
    private const float AimRange = 440f, AimPeriod = 2.4f, PowerPeriod = 1.8f;
    private const float NearY = -205f, FarY = 255f; // where power 0 and power 1 land

    private enum Phase { Idle, Aim, Power, Flying }

    private readonly List<Vector2> necks = new List<Vector2>();
    private readonly List<int> rows = new List<int>();
    private RectTransform bottles, arrow, bar, fill, rings;
    private Phase phase = Phase.Idle;
    private float phaseStart, aimX, power, aimOffset;

    protected override string GameId => "rings";
    protected override string Title => "โยนห่วง";
    protected override string Subtitle => "แตะครั้งแรกเล็งซ้ายขวา แตะครั้งที่สองกำหนดแรงโยน";
    protected override string ShotWord => "ห่วง";

    public static void Show()
    {
        if (instance == null) instance = new GameObject("RingTossUI").AddComponent<RingTossUI>();
        instance.Open();
    }

    protected override void BuildArena(RectTransform a)
    {
        a.GetComponent<Image>().color = new Color(0.16f, 0.2f, 0.32f);
        // Stepped stand: three wooden steps, the far one highest
        for (int r = 0; r < 3; r++)
        {
            var step = UiKit.Rect("Step", a, Color.Lerp(new Color(0.5f, 0.32f, 0.16f), new Color(0.68f, 0.46f, 0.26f), r / 2f));
            step.anchoredPosition = new Vector2(0f, RowY[r] - 70f * RowScale[r]);
            step.sizeDelta = new Vector2(1000f * (0.8f + 0.1f * r), 26f);
        }
        bottles = UiKit.Container(a, Vector2.zero);
        rings = UiKit.Container(a, Vector2.zero);

        // Aim arrow along the bottom, power bar on the right
        arrow = UiKit.Rect("Arrow", a, new Color(1f, 0.85f, 0.25f));
        arrow.sizeDelta = new Vector2(16f, 70f);
        arrow.anchoredPosition = new Vector2(0f, -280f);
        var head = UiKit.Rect("Head", arrow, new Color(1f, 0.85f, 0.25f));
        head.anchoredPosition = new Vector2(0f, 38f);
        head.sizeDelta = new Vector2(32f, 32f);
        head.localRotation = Quaternion.Euler(0f, 0f, 45f);
        bar = UiKit.Panel("PowerBar", a, new Color(0f, 0f, 0f, 0.5f), new Vector2(500f, 20f), new Vector2(34f, 460f));
        fill = UiKit.Rect("Fill", bar, new Color(0.95f, 0.45f, 0.25f));
        fill.pivot = new Vector2(0.5f, 0f);
        fill.anchoredPosition = new Vector2(0f, -226f);
        fill.sizeDelta = new Vector2(26f, 0f);
        UiKit.Label(a, 20, "แรง", new Vector2(500f, -232f), new Vector2(80f, 30f), Color.white);
        Lay(0f);
    }

    // Bottles, the far row smallest; offset shifts the rows sideways a little each round
    private void Lay(float offset)
    {
        UiKit.Clear(bottles);
        necks.Clear();
        rows.Clear();
        Color[] glass = { new Color(0.3f, 0.7f, 0.4f, 0.95f), new Color(0.55f, 0.35f, 0.2f, 0.95f), new Color(0.35f, 0.6f, 0.9f, 0.95f) };
        for (int r = 0; r < 3; r++)
        {
            float s = RowScale[r];
            float gap = 190f * s;
            for (int i = 0; i < PerRow; i++)
            {
                float x = (i - (PerRow - 1) * 0.5f) * gap + (r == 1 ? offset : -offset) * s;
                var body = UiKit.Panel("Bottle", bottles, glass[(r + i) % 3], new Vector2(x, RowY[r] - 30f * s), new Vector2(46f * s, 80f * s));
                var neck = UiKit.Rect("Neck", body, glass[(r + i) % 3]);
                neck.anchoredPosition = new Vector2(0f, 56f * s);
                neck.sizeDelta = new Vector2(18f * s, 34f * s);
                var cap = UiKit.Rect("Cap", neck, new Color(0.9f, 0.85f, 0.2f));
                cap.anchoredPosition = new Vector2(0f, 19f * s);
                cap.sizeDelta = new Vector2(22f * s, 8f * s);
                necks.Add(new Vector2(x, RowY[r] + 30f * s));
                rows.Add(r);
            }
        }
    }

    protected override void Begin(System.Random rng)
    {
        aimOffset = (float)rng.NextDouble();
        Lay(((float)rng.NextDouble() - 0.5f) * 60f);
        UiKit.Clear(rings);
        StartAim();
    }

    private void StartAim()
    {
        phase = Phase.Aim;
        phaseStart = Time.unscaledTime;
        fill.sizeDelta = new Vector2(26f, 0f);
    }

    private void Update()
    {
        float t = Time.unscaledTime - phaseStart;
        if (phase == Phase.Aim)
        {
            aimX = Mathf.Sin((t / AimPeriod + aimOffset) * Mathf.PI * 2f) * AimRange;
            arrow.anchoredPosition = new Vector2(aimX, -280f);
        }
        else if (phase == Phase.Power)
        {
            power = Mathf.PingPong(t / PowerPeriod * 2f, 1f);
            fill.sizeDelta = new Vector2(26f, power * 452f);
        }
    }

    protected override void Tap(Vector2 at)
    {
        if (phase == Phase.Aim)
        {
            phase = Phase.Power;
            phaseStart = Time.unscaledTime;
            GameAudio.Play("whoosh", null, 0.3f);
        }
        else if (phase == Phase.Power)
        {
            phase = Phase.Flying;
            StartCoroutine(Throw(aimX, power));
        }
    }

    private IEnumerator Throw(float x, float p)
    {
        Vector2 land = new Vector2(x, Mathf.Lerp(NearY, FarY, p));
        // The ring narrows to the row it lands on, so far throws look further away
        float depth = Mathf.InverseLerp(NearY, FarY, land.y);
        float scale = Mathf.Lerp(1.05f, 0.7f, depth);
        // A ring counts when it drops over a neck: close enough sideways and in that row's depth
        int hit = -1;
        for (int i = 0; i < necks.Count; i++)
        {
            float s = RowScale[rows[i]];
            if (Mathf.Abs(land.x - necks[i].x) <= 34f * s && Mathf.Abs(land.y - necks[i].y) <= 38f * s) { hit = i; break; }
        }
        var ring = UiKit.Rect("Ring", rings, new Color(0.95f, 0.3f, 0.5f));
        ring.GetComponent<Image>().sprite = RingSprite();
        ring.GetComponent<Image>().raycastTarget = false;
        Vector2 from = new Vector2(x * 0.3f, -300f);
        GameAudio.Play("whoosh", null, 0.6f);
        for (float t = 0f; t < 0.55f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.55f;
            ring.anchoredPosition = Vector2.Lerp(from, land, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 160f;
            ring.sizeDelta = new Vector2(110f, 44f) * Mathf.Lerp(1.3f, scale, k);
            ring.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 20f) * 12f);
            yield return null;
        }
        ring.localRotation = Quaternion.identity;
        if (hit >= 0)
        {
            ring.anchoredPosition = necks[hit] + Vector2.down * 30f * RowScale[rows[hit]];
            Float("ลง!", necks[hit] + Vector2.up * 60f, new Color(1f, 0.95f, 0.5f));
            GameAudio.Play("bell", null, 0.8f);
        }
        else
        {
            Float("ไม่ลง", land + Vector2.up * 40f, new Color(1f, 0.85f, 0.8f));
            GameAudio.Play("punch", null, 0.3f);
            StartCoroutine(Bounce(ring));
        }
        Shot(hit >= 0);
        yield return new WaitForSecondsRealtime(0.25f);
        if (Playing) StartAim();
        else phase = Phase.Idle;
    }

    private static IEnumerator Bounce(RectTransform ring)
    {
        Vector2 from = ring.anchoredPosition;
        float dir = from.x >= 0f ? 1f : -1f;
        for (float t = 0f; t < 0.5f && ring != null; t += Time.unscaledDeltaTime)
        {
            ring.anchoredPosition = from + new Vector2(dir * t * 160f, Mathf.Sin(t / 0.5f * Mathf.PI) * 40f - t * 80f);
            ring.GetComponent<Image>().color = new Color(0.95f, 0.3f, 0.5f, 1f - t / 0.5f);
            yield return null;
        }
        if (ring != null) Destroy(ring.gameObject);
    }

    protected override void Ended()
    {
        phase = Phase.Idle;
        UiKit.Clear(rings);
    }

    // A flat hoop seen from above at an angle: an ellipse outline
    private static Sprite ringSprite;
    private static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int w = 128, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f * 1.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return ringSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
