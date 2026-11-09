using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Music engraved in the UI for the card game (MusicUI): a five-line staff (บรรทัด 5 เส้น) with its
// clef — treble, bass or the two bars of a drum staff — and notes on it: oval heads tilted as printed,
// stems up below the middle line and down above it, ledger lines, sharps and flats, whole notes for
// chords and scales, crosses for cymbals. The clefs and accidentals are pictures in
// Resources/Branding/Music, drawn in staff spaces (their frames are set out in the comments below).
// Positions: 0 = the bottom line, 1 = the space above it ... 8 = the top line; each step half a space.
public class StaffView
{
    private readonly RectTransform root;
    private readonly float sp;          // one space between lines, in canvas units
    private readonly float width;
    private readonly Color ink, paper;
    public float NoteLeft { get; private set; }   // where the first note can go (after the clef)
    public float NoteRight => width * 0.5f - sp * 1.2f;

    public StaffView(RectTransform parent, Vector2 centre, float lineSpace, float staffWidth, Color inkColor, Color paperColor)
    {
        root = new GameObject("Staff", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchoredPosition = centre;
        sp = lineSpace;
        width = staffWidth;
        ink = inkColor;
        paper = paperColor;
    }

    public RectTransform Root => root;
    public float Space => sp;

    public static float Y(int pos, float space) => (pos - 4) * space * 0.5f;

    public void Clear()
    {
        foreach (Transform c in root) Object.Destroy(c.gameObject);
    }

    // The lines and the clef. clef: "treble", "bass" or "drums"
    public void Draw(string clef)
    {
        Clear();
        for (int i = 0; i < 5; i++)
            Bar(new Vector2(0f, (i - 2) * sp), new Vector2(width, Mathf.Max(2f, sp * 0.08f)), ink);
        // barlines at both ends; a double bar at the end
        Bar(new Vector2(-width * 0.5f, 0f), new Vector2(Mathf.Max(2f, sp * 0.1f), sp * 4f), ink);
        Bar(new Vector2(width * 0.5f - sp * 0.35f, 0f), new Vector2(Mathf.Max(2f, sp * 0.1f), sp * 4f), ink);
        Bar(new Vector2(width * 0.5f - sp * 0.08f, 0f), new Vector2(sp * 0.22f, sp * 4f), ink);
        float left = -width * 0.5f;
        switch (clef)
        {
            case "treble": // frame x -0.3..3.2, y -2.5..6.5 spaces (lines at 0..4): centred on the middle line
                Glyph("Music/clef_treble", new Vector2(left + sp * 0.3f + sp * 1.75f, 0f), new Vector2(sp * 3.5f, sp * 9f));
                NoteLeft = left + sp * 4.6f;
                break;
            case "bass": // frame x -0.3..3.3, y -1..5
                Glyph("Music/clef_bass", new Vector2(left + sp * 0.3f + sp * 1.8f, 0f), new Vector2(sp * 3.6f, sp * 6f));
                NoteLeft = left + sp * 4.8f;
                break;
            default: // percussion clef: two thick bars across the middle
                foreach (float x in new[] { 1.2f, 1.75f })
                    Bar(new Vector2(left + sp * x, 0f), new Vector2(sp * 0.32f, sp * 2f), ink);
                NoteLeft = left + sp * 3.4f;
                break;
        }
    }

    // A note at x on position pos. whole: an open whole note, no stem; cross: a cymbal's x head
    // hollow: a half note's open head; flags: an eighth's (1) or a sixteenth's (2) เขบ็ด on the stem
    public RectTransform Note(float x, int pos, int acc, bool whole, Color color, bool cross = false, bool stem = true, bool hollow = false, int flags = 0)
    {
        var g = new GameObject("Note", typeof(RectTransform)).GetComponent<RectTransform>();
        g.SetParent(root, false);
        g.anchoredPosition = new Vector2(x, 0f);
        float y = Y(pos, sp);
        // ledger lines above and below the staff, through or under the note
        for (int p = -2; p >= pos; p -= 2) Bar(new Vector2(0f, Y(p, sp)), new Vector2(sp * (whole ? 2.3f : 2f), Mathf.Max(2f, sp * 0.08f)), ink, g);
        for (int p = 10; p <= pos; p += 2) Bar(new Vector2(0f, Y(p, sp)), new Vector2(sp * (whole ? 2.3f : 2f), Mathf.Max(2f, sp * 0.08f)), ink, g);

        if (cross)
        {
            foreach (float a in new[] { 45f, -45f })
            {
                var r = Bar(new Vector2(0f, y), new Vector2(sp * 1.3f, sp * 0.16f), color, g);
                r.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }
        else if (whole)
        {
            var outer = Oval(g, new Vector2(0f, y), new Vector2(sp * 1.62f, sp * 1.08f), -12f, color);
            Oval(outer, Vector2.zero, new Vector2(sp * 0.82f, sp * 0.62f), 50f, paper).name = "Paper"; // the hollow, turned the other way
        }
        else
        {
            var head = Oval(g, new Vector2(0f, y), new Vector2(sp * 1.32f, sp * 0.98f), 20f, color);
            if (hollow) Oval(head, Vector2.zero, new Vector2(sp * 1.0f, sp * 0.42f), 12f, paper).name = "Paper"; // a slim slot, as printed
        }

        if (!whole && stem)
        {
            bool up = pos < 4;
            float sx = up ? sp * 0.6f : -sp * 0.6f;
            float len = sp * (flags > 1 ? 3.9f : 3.4f);
            Bar(new Vector2(sx, y + (up ? len : -len) * 0.5f), new Vector2(Mathf.Max(2f, sp * 0.12f), len), color, g);
            // the flags hang off the stem's end to the right, curling back towards the head
            float end = y + (up ? len : -len), dir = up ? -1f : 1f;
            for (int f = 0; f < flags; f++)
            {
                var o = new Vector2(sx, end + dir * f * sp * 0.85f);
                Stroke(g, color, o, dir, new[] { 0.06f, 0.05f, 0.14f, 0.4f, 0.4f, 0.78f, 0.7f, 1.12f, 0.88f, 1.5f, 0.88f, 1.95f, 0.7f, 2.35f },
                    new[] { 0.24f, 0.22f, 0.19f, 0.16f, 0.13f, 0.11f });
            }
        }
        if (acc != 0) Accidental(g, new Vector2(-sp * 1.55f, y), acc, color);
        return g;
    }

    // A note value or a rest where it is printed: notes in the second space (stem up), rests in the middle
    public RectTransform Value(float x, int index, Color color)
    {
        var v = MusicTheory.Values[index];
        if (!v.rest) return Note(x, 3, 0, v.hollow && !v.stem, color, stem: v.stem, hollow: v.hollow && v.stem, flags: v.flags);
        var g = new GameObject("Rest", typeof(RectTransform)).GetComponent<RectTransform>();
        g.SetParent(root, false);
        g.anchoredPosition = new Vector2(x, 0f);
        switch (index)
        {
            case 5: // whole rest: a block hanging from the fourth line
                Bar(new Vector2(0f, sp * 0.76f), new Vector2(sp * 1.25f, sp * 0.52f), color, g).name = "Block";
                break;
            case 6: // half rest: a block sitting on the middle line
                Bar(new Vector2(0f, sp * 0.26f), new Vector2(sp * 1.25f, sp * 0.52f), color, g).name = "Block";
                break;
            case 7: // quarter rest: the zigzag, its middle stroke heavy, a hook below
                Stroke(g, color, Vector2.zero, 1f, new[] { -0.2f, 1.5f, 0.35f, 0.85f, -0.22f, 0.12f, 0.3f, -0.5f }, new[] { 0.16f, 0.42f, 0.16f });
                Stroke(g, color, Vector2.zero, 1f, new[] { 0.3f, -0.5f, -0.1f, -0.62f, -0.28f, -0.9f, -0.05f, -1.3f }, new[] { 0.2f, 0.17f, 0.14f });
                break;
            default: // eighth rest: a dot with a tail to a slanted stem
                Dot(g, color, new Vector2(-0.22f, 0.58f) * sp, 0.56f * sp);
                Stroke(g, color, Vector2.zero, 1f, new[] { -0.12f, 0.36f, 0.12f, 0.42f, 0.42f, 0.66f, -0.06f, -1.0f }, new[] { 0.13f, 0.13f, 0.14f });
                break;
        }
        return g;
    }

    // A pen stroke through points (x, y pairs in spaces, y flipped by dir) with a width per segment
    private void Stroke(Transform parent, Color color, Vector2 origin, float dir, float[] xy, float[] widths)
    {
        for (int i = 0; i + 3 < xy.Length; i += 2)
        {
            var a = origin + new Vector2(xy[i], xy[i + 1] * dir) * sp;
            var b = origin + new Vector2(xy[i + 2], xy[i + 3] * dir) * sp;
            float w = widths[Mathf.Min(i / 2, widths.Length - 1)] * sp;
            var d = b - a;
            var r = Bar((a + b) * 0.5f, new Vector2(d.magnitude + w * 0.3f, w), color, parent);
            r.name = "Pen";
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            Dot(parent, color, b, w); // round joins
        }
    }

    private static void Dot(Transform parent, Color color, Vector2 at, float size)
    {
        var r = Oval(parent, at, Vector2.one * size, 0f, color);
        r.name = "Pen";
    }

    // A box with a ? where a note is missing
    public RectTransform Missing(float x, Color color)
    {
        var g = new GameObject("Missing", typeof(RectTransform)).GetComponent<RectTransform>();
        g.SetParent(root, false);
        g.anchoredPosition = new Vector2(x, 0f);
        var box = UiKit.Panel("Box", g, color, Vector2.zero, new Vector2(sp * 1.9f, sp * 5.2f));
        box.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.22f);
        box.gameObject.AddComponent<Outline>().effectColor = color;
        var q = UiKit.Label(g, Mathf.RoundToInt(sp * 2.2f), "?", Vector2.zero, new Vector2(sp * 2f, sp * 2.6f), color);
        q.fontStyle = FontStyle.Bold;
        return g;
    }

    private void Accidental(Transform parent, Vector2 at, int acc, Color color)
    {
        // sharp: frame 1.5 x 4 spaces centred on the note; flat: 1.25 x 4, its bowl on the note
        if (acc > 0) Glyph("Music/sharp", at, new Vector2(sp * 1.5f, sp * 4f), parent, color);
        else Glyph("Music/flat", at + new Vector2(sp * 0.12f, 0f), new Vector2(sp * 1.25f, sp * 4f), parent, color);
    }

    private RectTransform Glyph(string art, Vector2 at, Vector2 size, Transform parent = null, Color? color = null)
    {
        var r = UiKit.Rect("Glyph", parent != null ? parent : root, color ?? ink);
        r.anchoredPosition = at;
        r.sizeDelta = size;
        var img = r.GetComponent<Image>();
        img.sprite = UiKit.Art(art);
        img.raycastTarget = false;
        if (img.sprite == null) img.enabled = false;
        return r;
    }

    private RectTransform Bar(Vector2 at, Vector2 size, Color color, Transform parent = null)
    {
        var r = UiKit.Rect("Line", parent != null ? parent : root, color);
        r.anchoredPosition = at;
        r.sizeDelta = size;
        r.GetComponent<Image>().raycastTarget = false;
        return r;
    }

    private static RectTransform Oval(Transform parent, Vector2 at, Vector2 size, float tilt, Color color)
    {
        var r = UiKit.Rect("Head", parent, color);
        r.anchoredPosition = at;
        r.sizeDelta = size;
        r.localRotation = Quaternion.Euler(0f, 0f, tilt);
        var img = r.GetComponent<Image>();
        img.sprite = UiKit.CircleSprite;
        img.raycastTarget = false;
        return r;
    }

    // ── A guitar chord box: six strings, the frets, where the fingers go, x and o over the nut ─────
    public static void ChordBox(RectTransform parent, Vector2 centre, float h, string shape, string name, Color ink)
    {
        var g = new GameObject("ChordBox", typeof(RectTransform)).GetComponent<RectTransform>();
        g.SetParent(parent, false);
        g.anchoredPosition = centre;
        float w = h * 0.72f, gap = w / 5f, fret = h / 4.6f;
        float top = h * 0.5f - h * 0.12f, left = -w * 0.5f;
        var name1 = UiKit.Label(g, Mathf.RoundToInt(h * 0.16f), name, new Vector2(0f, h * 0.5f + h * 0.08f), new Vector2(w * 1.6f, h * 0.2f), ink);
        name1.fontStyle = FontStyle.Bold;
        for (int s = 0; s < 6; s++) Line(g, new Vector2(left + s * gap, top - fret * 2f), new Vector2(Mathf.Max(2f, h * 0.012f), fret * 4f), ink);
        for (int f = 0; f <= 4; f++) Line(g, new Vector2(0f, top - f * fret), new Vector2(w, f == 0 ? h * 0.04f : Mathf.Max(2f, h * 0.012f)), ink); // the nut is thick
        for (int s = 0; s < 6 && s < shape.Length; s++)
        {
            char c = shape[s];
            float x = left + s * gap;
            if (c == 'x' || c == '0')
            {
                UiKit.Label(g, Mathf.RoundToInt(h * 0.1f), c == 'x' ? "x" : "o", new Vector2(x, top + h * 0.065f), new Vector2(gap, h * 0.11f), ink);
                continue;
            }
            int n = c - '0';
            var dot = UiKit.Rect("Finger", g, ink);
            dot.anchoredPosition = new Vector2(x, top - (n - 0.5f) * fret);
            dot.sizeDelta = Vector2.one * gap * 0.78f;
            dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
            dot.GetComponent<Image>().raycastTarget = false;
        }
    }

    private static void Line(Transform p, Vector2 at, Vector2 size, Color c)
    {
        var r = UiKit.Rect("Line", p, c);
        r.anchoredPosition = at;
        r.sizeDelta = size;
        r.GetComponent<Image>().raycastTarget = false;
    }
}
