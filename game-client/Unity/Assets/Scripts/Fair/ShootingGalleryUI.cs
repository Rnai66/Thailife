using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ยิงปืนจุก: three shelves of tin targets sliding past a striped curtain, the back rows smaller and
// faster. Ten corks a round: tap a target to shoot it off the shelf. Nothing is random but the speeds
// the round starts with.
public class ShootingGalleryUI : SkillGameUI
{
    private static ShootingGalleryUI instance;

    private class Target
    {
        public RectTransform rect;
        public int row;
        public float x;
        public bool down;
    }

    private static readonly float[] RowY = { 175f, 15f, -145f };
    private static readonly float[] RowSize = { 70f, 86f, 104f };
    private static readonly Color[] Tins =
    {
        new Color(0.95f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 0.95f), new Color(1f, 0.8f, 0.25f),
        new Color(0.4f, 0.8f, 0.45f), new Color(0.85f, 0.45f, 0.9f),
    };
    private const float Span = 1180f; // targets wrap round past the frame edges
    private const int PerRow = 5;

    private readonly List<Target> targets = new List<Target>();
    private readonly float[] speed = { 210f, -160f, 120f };
    private RectTransform gun, effects;

    protected override string GameId => "shooting";
    protected override string Title => "ยิงปืนจุก";
    protected override string Subtitle => "แตะเป้าที่วิ่งอยู่เพื่อยิงให้ล้ม แถวหลังเล็กและเร็วกว่า";
    protected override string ShotWord => "จุก";

    public static void Show()
    {
        if (instance == null) instance = new GameObject("ShootingGalleryUI").AddComponent<ShootingGalleryUI>();
        instance.Open();
    }

    protected override void BuildArena(RectTransform a)
    {
        // Red and white curtain stripes behind the shelves
        for (int i = 0; i < 14; i++)
        {
            var stripe = UiKit.Rect("Stripe", a, i % 2 == 0 ? new Color(0.78f, 0.16f, 0.18f) : new Color(0.96f, 0.92f, 0.86f));
            stripe.anchoredPosition = new Vector2(-530f + 38f + i * 76f, 40f);
            stripe.sizeDelta = new Vector2(76f, 560f);
        }
        var valance = UiKit.Rect("Valance", a, new Color(0.95f, 0.75f, 0.2f));
        valance.anchoredPosition = new Vector2(0f, 300f);
        valance.sizeDelta = new Vector2(1060f, 40f);
        for (int r = 0; r < 3; r++)
        {
            var shelf = UiKit.Rect("Shelf", a, new Color(0.55f, 0.35f, 0.18f));
            shelf.anchoredPosition = new Vector2(0f, RowY[r] - RowSize[r] * 0.5f - 8f);
            shelf.sizeDelta = new Vector2(1060f, 16f);
            for (int i = 0; i < PerRow; i++)
            {
                var t = new Target { row = r, x = -Span * 0.5f + (i + 0.5f) * Span / PerRow };
                t.rect = Tin(a, RowSize[r], Tins[(r * 2 + i) % Tins.Length]);
                targets.Add(t);
            }
        }
        // Counter and the cork gun
        var counter = UiKit.Rect("Counter", a, new Color(0.45f, 0.28f, 0.14f));
        counter.anchoredPosition = new Vector2(0f, -285f);
        counter.sizeDelta = new Vector2(1060f, 70f);
        gun = UiKit.Rect("Gun", a, new Color(0.35f, 0.22f, 0.12f));
        gun.pivot = new Vector2(0.5f, 0f);
        gun.anchoredPosition = new Vector2(0f, -320f);
        gun.sizeDelta = new Vector2(34f, 170f);
        var barrel = UiKit.Rect("Barrel", gun, new Color(0.25f, 0.25f, 0.28f));
        barrel.anchoredPosition = new Vector2(0f, 70f);
        barrel.sizeDelta = new Vector2(20f, 60f);
        var cork = UiKit.Rect("Cork", gun, new Color(0.85f, 0.65f, 0.4f));
        cork.anchoredPosition = new Vector2(0f, 104f);
        cork.sizeDelta = new Vector2(16f, 14f);
        effects = UiKit.Container(a, Vector2.zero);
        Place();
    }

    // A tin can target: body, top rim and a painted bullseye
    private static RectTransform Tin(RectTransform parent, float size, Color color)
    {
        var t = UiKit.Panel("Tin", parent, color, Vector2.zero, new Vector2(size * 0.75f, size));
        t.pivot = new Vector2(0.5f, 0f);
        var rim = UiKit.Rect("Rim", t, color * 0.75f);
        rim.anchoredPosition = new Vector2(0f, size * 0.46f);
        rim.sizeDelta = new Vector2(size * 0.78f, 8f);
        var ring = UiKit.Rect("Ring", t, Color.white);
        ring.GetComponent<Image>().sprite = UiKit.CircleSprite;
        ring.sizeDelta = Vector2.one * size * 0.46f;
        var dot = UiKit.Rect("Dot", ring, new Color(0.85f, 0.15f, 0.15f));
        dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
        dot.sizeDelta = Vector2.one * size * 0.2f;
        return t;
    }

    protected override void Begin(System.Random rng)
    {
        for (int r = 0; r < 3; r++)
        {
            float s = 110f + (2 - r) * 55f + (float)rng.NextDouble() * 60f; // back (top) row fastest
            speed[r] = rng.Next(2) == 0 ? s : -s;
        }
        foreach (var t in targets)
        {
            t.down = false;
            t.rect.localRotation = Quaternion.identity;
            t.rect.gameObject.SetActive(true);
        }
        UiKit.Clear(effects);
    }

    private void Update()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            t.x += speed[t.row] * Time.unscaledDeltaTime;
            if (t.x > Span * 0.5f) t.x -= Span;
            if (t.x < -Span * 0.5f) t.x += Span;
        }
        Place();
    }

    private void Place()
    {
        foreach (var t in targets)
            t.rect.anchoredPosition = new Vector2(t.x, RowY[t.row] - RowSize[t.row] * 0.5f);
    }

    protected override void Tap(Vector2 at)
    {
        if (at.y < -250f) return; // the counter, not a shot
        AimGun(at);
        GameAudio.Play("punch", null, 0.5f);
        Target hit = null;
        foreach (var t in targets)
        {
            if (t.down) continue;
            float size = RowSize[t.row];
            var p = t.rect.anchoredPosition;
            if (Mathf.Abs(at.x - p.x) <= size * 0.42f && at.y >= p.y - 6f && at.y <= p.y + size + 6f) { hit = t; break; }
        }
        if (hit != null)
        {
            hit.down = true;
            StartCoroutine(Fall(hit));
            Float("โดน!", at + Vector2.up * 40f, new Color(1f, 0.95f, 0.5f));
            GameAudio.Play("punch_heavy", null, 0.8f);
        }
        else
        {
            Float("พลาด", at + Vector2.up * 30f, new Color(1f, 0.85f, 0.8f));
        }
        StartCoroutine(Puff(at));
        Shot(hit != null);
    }

    private void AimGun(Vector2 at)
    {
        Vector2 d = at - gun.anchoredPosition;
        gun.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
    }

    private IEnumerator Fall(Target t)
    {
        for (float k = 0f; k < 0.3f && t.rect != null; k += Time.unscaledDeltaTime)
        {
            t.rect.localRotation = Quaternion.Euler(0f, 0f, (speed[t.row] > 0 ? -1f : 1f) * 90f * k / 0.3f);
            yield return null;
        }
        if (t.rect != null && t.down) t.rect.gameObject.SetActive(false);
    }

    private IEnumerator Puff(Vector2 at)
    {
        var puff = UiKit.Rect("Puff", effects, new Color(1f, 1f, 1f, 0.8f));
        puff.GetComponent<Image>().sprite = UiKit.CircleSprite;
        puff.GetComponent<Image>().raycastTarget = false;
        puff.anchoredPosition = at;
        for (float k = 0f; k < 0.3f && puff != null; k += Time.unscaledDeltaTime)
        {
            puff.sizeDelta = Vector2.one * Mathf.Lerp(10f, 60f, k / 0.3f);
            puff.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.8f * (1f - k / 0.3f));
            yield return null;
        }
        if (puff != null) Destroy(puff.gameObject);
    }

    // Round over: stand everything back up for the next one
    protected override void Ended()
    {
        foreach (var t in targets)
        {
            t.down = false;
            t.rect.localRotation = Quaternion.identity;
            t.rect.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
