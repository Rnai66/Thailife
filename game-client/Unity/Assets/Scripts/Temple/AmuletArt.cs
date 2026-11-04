using System;
using System.Collections.Generic;
using UnityEngine;

// Cartoon pictures of the temple's amulets (พระเครื่อง), yantra cloths (ผ้ายันต์), the temple dog and
// the กุมารทอง, painted once in code from layered shapes: a gold or silver casing, the clay or metal
// face, and the seated Buddha (or monk) raised on it, with a soft highlight — like the amulet posters.
public static class AmuletArt
{
    private const int Size = 128, Samples = 2;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    private struct Layer
    {
        public Func<float, float, bool> Inside;
        public Color Color;
        public Layer(Func<float, float, bool> inside, Color color) { Inside = inside; Color = color; }
    }

    public static Sprite Get(string id)
    {
        if (cache.TryGetValue(id, out var s)) return s;
        var layers = Layers(id);
        if (layers == null) return null;
        s = Paint(layers);
        cache[id] = s;
        return s;
    }

    private static Sprite Paint(List<Layer> layers)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            Color sum = Color.clear;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
            {
                float x = (px + (sx + 0.5f) / Samples) / Size * 2f - 1f;
                float y = (py + (sy + 0.5f) / Samples) / Size * 2f - 1f;
                Color c = Color.clear;
                foreach (var l in layers) if (l.Inside(x, y)) c = l.Color.a >= 1f ? l.Color : Color.Lerp(c, new Color(l.Color.r, l.Color.g, l.Color.b, Mathf.Max(c.a, l.Color.a)), l.Color.a);
                sum += c;
            }
            pixels[py * Size + px] = sum / (Samples * Samples);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ── Shapes in -1..1, +y up ───────────────────────────────────────────────────────────────
    private static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry) => (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) < 1f;

    private static bool Box(float x, float y, float cx, float cy, float hw, float hh, float r)
    {
        float dx = Mathf.Max(Mathf.Abs(x - cx) - (hw - r), 0f), dy = Mathf.Max(Mathf.Abs(y - cy) - (hh - r), 0f);
        return dx * dx + dy * dy < r * r;
    }

    // A pointed arch (กลีบบัว / ซุ้มเรือนแก้ว): a box whose top narrows to a point
    private static bool Arch(float x, float y, float hw, float bottom, float shoulder, float top)
    {
        if (y < bottom || y > top || Mathf.Abs(x) > hw) return false;
        if (y <= shoulder) return true;
        float t = (y - shoulder) / (top - shoulder);
        float w = hw * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
        return Mathf.Abs(x) < w;
    }

    private static bool Tri(float x, float y, float hw, float bottom, float top) =>
        y > bottom && y < top && Mathf.Abs(x) < hw * (top - y) / (top - bottom);

    private static bool Line(float x, float y, float ax, float ay, float bx, float by, float w)
    {
        float vx = bx - ax, vy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy));
        float dx = x - (ax + vx * t), dy = y - (ay + vy * t);
        return dx * dx + dy * dy < w * w * 0.25f;
    }

    // The seated Buddha raised on the face: lotus base, crossed legs, body, head and ushnisha
    private static Func<float, float, bool> Buddha(float cx, float cy, float s) => (x, y) =>
    {
        x = (x - cx) / s; y = (y - cy) / s;
        return Box(x, y, 0f, -0.62f, 0.62f, 0.1f, 0.05f)        // base
               || Ellipse(x, y, 0f, -0.42f, 0.55f, 0.16f)       // crossed legs
               || Ellipse(x, y, 0f, -0.08f, 0.3f, 0.36f)        // body
               || Ellipse(x, y, 0f, 0.38f, 0.17f, 0.19f)        // head
               || Tri(x, y, 0.07f, 0.52f, 0.72f);               // ushnisha
    };

    // A monk's bust (หลวงพ่อ): shaven head and robed shoulders
    private static Func<float, float, bool> Monk(float cx, float cy, float s) => (x, y) =>
    {
        x = (x - cx) / s; y = (y - cy) / s;
        return Ellipse(x, y, 0f, 0.28f, 0.26f, 0.3f) || (Ellipse(x, y, 0f, -0.55f, 0.62f, 0.5f) && y < -0.12f);
    };

    private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    private static readonly Color Gold = new Color(0.98f, 0.8f, 0.3f), GoldDark = new Color(0.72f, 0.5f, 0.12f);
    private static readonly Color Silver = new Color(0.86f, 0.88f, 0.9f), SilverDark = new Color(0.55f, 0.57f, 0.62f);
    private static readonly Color Shine = new Color(1f, 1f, 1f, 0.35f);

    // A cased amulet: casing, face, the figure in relief (shadow + body) and a shine on the casing
    private static List<Layer> Cased(Func<float, float, bool> outer, Func<float, float, bool> inner, Color face, Func<float, float, bool> figure,
        bool silver = false, Func<float, float, bool> loop = null)
    {
        var rim = silver ? Silver : Gold;
        var rimDark = silver ? SilverDark : GoldDark;
        var list = new List<Layer>();
        if (loop != null) list.Add(new Layer(loop, rimDark));
        list.Add(new Layer((x, y) => outer(x + 0.03f, y + 0.04f), new Color(0f, 0f, 0f, 0.35f))); // drop shadow
        list.Add(new Layer(outer, rimDark));
        list.Add(new Layer((x, y) => outer(x * 1.04f, y * 1.04f), rim));
        list.Add(new Layer(inner, face));
        list.Add(new Layer((x, y) => figure(x + 0.025f, y - 0.025f), Shade(face, 1.35f)));    // lit edge
        list.Add(new Layer(figure, Shade(face, 0.72f)));
        list.Add(new Layer((x, y) => outer(x, y) && !inner(x, y) && x - y < -0.6f, Shine));
        return list;
    }

    private static Func<float, float, bool> Loop => (x, y) => Ellipse(x, y, 0f, 0.86f, 0.13f, 0.12f) && !Ellipse(x, y, 0f, 0.86f, 0.06f, 0.055f);

    private static List<Layer> Layers(string id)
    {
        switch (id)
        {
            case "somdej": // พระสมเด็จ: a white-powder rectangle, the Buddha under an arch on a three-tier base
                return Cased((x, y) => Box(x, y, 0f, -0.05f, 0.6f, 0.8f, 0.08f), (x, y) => Box(x, y, 0f, -0.05f, 0.5f, 0.7f, 0.05f), new Color(0.93f, 0.89f, 0.78f),
                    (x, y) => Buddha(0f, 0.02f, 0.5f)(x, y) || Box(x, y, 0f, -0.48f, 0.4f, 0.04f, 0.02f) || Box(x, y, 0f, -0.58f, 0.32f, 0.04f, 0.02f)
                              || (Arch(x, y, 0.42f, -0.5f, 0.1f, 0.62f) && !Arch(x, y, 0.37f, -0.5f, 0.1f, 0.56f)), loop: Loop);
            case "nangphaya": // พระนางพญา: a terracotta triangle
                return Cased((x, y) => Tri(x, y, 0.82f, -0.82f, 0.85f), (x, y) => Tri(x, y, 0.68f, -0.72f, 0.66f), new Color(0.72f, 0.4f, 0.24f),
                    Buddha(0f, -0.25f, 0.55f), loop: Loop);
            case "khunphaen": // พระขุนแผน: a tall pointed arch, red clay
                return Cased((x, y) => Arch(x, y, 0.62f, -0.85f, 0.1f, 0.85f), (x, y) => Arch(x, y, 0.52f, -0.76f, 0.1f, 0.7f), new Color(0.66f, 0.3f, 0.2f),
                    Buddha(0f, -0.12f, 0.6f), loop: Loop);
            case "phra_rod": // พระรอด: a small dark-green arch on a high base
                return Cased((x, y) => Arch(x, y, 0.55f, -0.8f, 0.0f, 0.8f), (x, y) => Arch(x, y, 0.45f, -0.7f, 0.0f, 0.66f), new Color(0.28f, 0.36f, 0.28f),
                    (x, y) => Buddha(0f, -0.08f, 0.5f)(x, y) || Box(x, y, 0f, -0.55f, 0.36f, 0.06f, 0.02f), loop: Loop);
            case "phra_pidta": // พระปิดตา: a dark round figure covering its eyes
                return Cased((x, y) => Ellipse(x, y, 0f, -0.05f, 0.66f, 0.78f), (x, y) => Ellipse(x, y, 0f, -0.05f, 0.56f, 0.68f), new Color(0.22f, 0.2f, 0.18f),
                    (x, y) => Ellipse(x, y, 0f, -0.15f, 0.36f, 0.42f) || Ellipse(x, y, 0f, 0.3f, 0.22f, 0.2f), loop: Loop);
            case "thuat": // หลวงพ่อทวด: an oval with the monk seated
                return Cased((x, y) => Ellipse(x, y, 0f, -0.05f, 0.64f, 0.8f), (x, y) => Ellipse(x, y, 0f, -0.05f, 0.54f, 0.7f), new Color(0.75f, 0.55f, 0.3f),
                    (x, y) => Monk(0f, 0.05f, 0.5f)(x, y) || Ellipse(x, y, 0f, -0.42f, 0.42f, 0.14f), loop: Loop);
            case "rian_raek": // เหรียญรุ่นแรก: a copper coin, the monk's bust, a beaded rim
                return Cased((x, y) => Ellipse(x, y, 0f, -0.04f, 0.8f, 0.8f), (x, y) => Ellipse(x, y, 0f, -0.04f, 0.68f, 0.68f), new Color(0.78f, 0.45f, 0.25f),
                    (x, y) => Monk(0f, -0.08f, 0.55f)(x, y) || Bead(x, y), silver: true, loop: Loop);
            case "phra_kring": // พระกริ่ง: a little bronze statue, no casing
                return new List<Layer>
                {
                    new Layer((x, y) => Buddha(0.04f, -0.08f, 0.95f)(x, y), new Color(0f, 0f, 0f, 0.35f)),
                    new Layer(Buddha(0f, -0.04f, 0.95f), new Color(0.45f, 0.3f, 0.16f)),
                    new Layer((x, y) => Buddha(0f, -0.04f, 0.95f)(x, y) && x < -0.08f, new Color(0.62f, 0.44f, 0.24f)),
                    new Layer((x, y) => Ellipse(x, y, -0.06f, 0.32f, 0.05f, 0.08f), new Color(1f, 0.85f, 0.55f, 0.6f)),
                };
            case "takrut": // ตะกรุดโทน: a rolled metal tube on a red cord
                return new List<Layer>
                {
                    new Layer((x, y) => Line(x, y, -0.9f, 0.55f, -0.45f, 0.05f, 0.08f) || Line(x, y, 0.9f, 0.55f, 0.45f, 0.05f, 0.08f), new Color(0.8f, 0.12f, 0.1f)),
                    new Layer((x, y) => Box(x, y, 0.03f, -0.04f, 0.62f, 0.2f, 0.18f), new Color(0f, 0f, 0f, 0.35f)),
                    new Layer((x, y) => Box(x, y, 0f, 0f, 0.62f, 0.2f, 0.18f), GoldDark),
                    new Layer((x, y) => Box(x, y, 0f, 0.05f, 0.6f, 0.12f, 0.1f), Gold),
                    new Layer((x, y) => Line(x, y, -0.3f, -0.18f, -0.3f, 0.18f, 0.03f) || Line(x, y, 0f, -0.18f, 0f, 0.18f, 0.03f) || Line(x, y, 0.3f, -0.18f, 0.3f, 0.18f, 0.03f), GoldDark),
                    new Layer((x, y) => Box(x, y, -0.62f, 0f, 0.07f, 0.22f, 0.06f) || Box(x, y, 0.62f, 0f, 0.07f, 0.22f, 0.06f), new Color(0.8f, 0.12f, 0.1f)),
                    new Layer((x, y) => Box(x, y, 0f, 0.1f, 0.5f, 0.03f, 0.02f), Shine),
                };
            case "yant_5": return Yantra(new Color(0.96f, 0.94f, 0.86f), new Color(0.75f, 0.1f, 0.1f), 5);
            case "yant_8": return Yantra(new Color(0.95f, 0.85f, 0.3f), new Color(0.6f, 0.05f, 0.05f), 8);
            case "yant_kraphet": return Yantra(new Color(0.8f, 0.12f, 0.1f), new Color(0.98f, 0.85f, 0.35f), 9);
            case "yant_wessuwan": return Yantra(new Color(0.96f, 0.94f, 0.86f), new Color(0.12f, 0.1f, 0.1f), 1);
            case "dog": // หมาวัด: a friendly brown dog's face
                return new List<Layer>
                {
                    new Layer((x, y) => Ellipse(x, y, -0.55f, 0.35f, 0.22f, 0.4f) || Ellipse(x, y, 0.55f, 0.35f, 0.22f, 0.4f), new Color(0.45f, 0.28f, 0.15f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, 0f, 0.62f, 0.62f), new Color(0.78f, 0.55f, 0.32f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, -0.28f, 0.36f, 0.28f), new Color(0.97f, 0.9f, 0.78f)),
                    new Layer((x, y) => Ellipse(x, y, -0.24f, 0.12f, 0.08f, 0.1f) || Ellipse(x, y, 0.24f, 0.12f, 0.08f, 0.1f), new Color(0.1f, 0.08f, 0.06f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, -0.15f, 0.13f, 0.09f), new Color(0.12f, 0.08f, 0.06f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, -0.42f, 0.09f, 0.1f) && y < -0.38f, new Color(0.9f, 0.4f, 0.45f)),
                };
            case "kuman": // กุมารทอง: a gold child with a topknot (จุก), hands together
                return new List<Layer>
                {
                    new Layer((x, y) => Ellipse(x, y + 0.05f, 0.04f, -0.04f, 0.5f, 0.85f), new Color(0f, 0f, 0f, 0.3f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, -0.45f, 0.42f, 0.4f) || Ellipse(x, y, 0f, 0.22f, 0.33f, 0.33f) || Ellipse(x, y, 0f, 0.62f, 0.1f, 0.1f), GoldDark),
                    new Layer((x, y) => Ellipse(x, y, -0.02f, -0.43f, 0.38f, 0.36f) || Ellipse(x, y, -0.02f, 0.24f, 0.29f, 0.29f) || Ellipse(x, y, -0.02f, 0.63f, 0.08f, 0.08f), Gold),
                    new Layer((x, y) => Box(x, y, 0f, -0.42f, 0.42f, 0.08f, 0.05f), new Color(0.8f, 0.12f, 0.1f)),
                    new Layer((x, y) => Ellipse(x, y, -0.1f, 0.24f, 0.04f, 0.05f) || Ellipse(x, y, 0.1f, 0.24f, 0.04f, 0.05f), new Color(0.25f, 0.15f, 0.05f)),
                    new Layer((x, y) => Ellipse(x, y, 0f, -0.18f, 0.08f, 0.14f), GoldDark),
                    new Layer((x, y) => Ellipse(x, y, -0.12f, 0.34f, 0.08f, 0.06f), Shine),
                };
        }
        return null;
    }

    private static bool Bead(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + (y + 0.04f) * (y + 0.04f));
        if (r < 0.58f || r > 0.66f) return false;
        float a = Mathf.Atan2(y + 0.04f, x);
        return Mathf.Cos(a * 18f) > 0.2f;
    }

    // A yantra cloth: a square of cloth with an inked border, the yantra's frame and its rows of script
    private static List<Layer> Yantra(Color cloth, Color ink, int rows)
    {
        var list = new List<Layer>
        {
            new Layer((x, y) => Box(x + 0.04f, y + 0.04f, 0f, 0f, 0.82f, 0.82f, 0.03f), new Color(0f, 0f, 0f, 0.3f)),
            new Layer((x, y) => Box(x, y, 0f, 0f, 0.82f, 0.82f, 0.03f), cloth),
            new Layer((x, y) => Box(x, y, 0f, 0f, 0.74f, 0.74f, 0.02f) && !Box(x, y, 0f, 0f, 0.7f, 0.7f, 0.02f), ink),
        };
        if (rows == 1) // ท้าวเวสสุวรรณ: the guardian giant with his club
        {
            list.Add(new Layer((x, y) => Ellipse(x, y, 0f, 0.38f, 0.16f, 0.17f) || Tri(x, y, 0.1f, 0.5f, 0.68f)
                                         || (Ellipse(x, y, 0f, -0.15f, 0.36f, 0.42f)) || Box(x, y, -0.18f, -0.58f, 0.08f, 0.1f, 0.03f) || Box(x, y, 0.18f, -0.58f, 0.08f, 0.1f, 0.03f)
                                         || Line(x, y, 0.36f, -0.55f, 0.5f, 0.45f, 0.1f), ink));
            list.Add(new Layer((x, y) => Ellipse(x, y, -0.06f, 0.4f, 0.03f, 0.03f) || Ellipse(x, y, 0.06f, 0.4f, 0.03f, 0.03f), cloth));
            return list;
        }
        // A square-and-circle frame with the script as short strokes in rows
        list.Add(new Layer((x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.42f) < 0.025f || Tri(x, y, 0.36f, -0.2f, 0.4f) && !Tri(x, y, 0.3f, -0.16f, 0.32f), ink));
        int n = Mathf.Clamp(rows, 3, 9);
        list.Add(new Layer((x, y) =>
        {
            if (Mathf.Abs(x) > 0.62f || Mathf.Abs(y) > 0.62f || (x * x + y * y < 0.2f)) return false;
            float row = (y + 0.62f) / 1.24f * n;
            float frac = row - Mathf.Floor(row);
            if (frac < 0.35f || frac > 0.6f) return false;
            float col = (x + 0.62f) * 9f + Mathf.Floor(row) * 0.37f;
            return col - Mathf.Floor(col) < 0.6f;
        }, ink));
        return list;
    }
}
