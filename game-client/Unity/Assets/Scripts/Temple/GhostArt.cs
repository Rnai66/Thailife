using System.Collections.Generic;
using UnityEngine;

// Cartoon portraits of the Thai ghosts for the exorcism window (ExorciseUI), painted in code like
// AmuletArt: a pale glowing figure with its tell-tale features — กระสือ a floating head trailing its
// insides, กระหัง with winnowing-basket wings, ปอบ wild-eyed with a long tongue, ตานี in banana-leaf
// green, ตะเคียน in a red sabai, พราย in white, เปรต tall and thin with a pin-hole mouth, แม่นาก with
// her long hair and baby.
public static class GhostArt
{
    private const int Size = 256;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    private static Color[] px;

    public static Sprite Get(string id)
    {
        if (cache.TryGetValue(id ?? "", out var s) && s != null) return s;
        px = new Color[Size * Size];
        Paint(id);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        cache[id ?? ""] = s;
        px = null;
        return s;
    }

    private static readonly Color Ink = new Color(0.08f, 0.05f, 0.08f);
    private static readonly Color Pale = new Color(0.86f, 0.9f, 0.84f);
    private static readonly Color Hair = new Color(0.07f, 0.07f, 0.09f);
    private static readonly Color Blood = new Color(0.75f, 0.08f, 0.1f);
    private static readonly Color Eye = new Color(1f, 0.25f, 0.2f);

    private static void Paint(string id)
    {
        switch (id)
        {
            case "krasue":
                Glow(new Color(0.4f, 1f, 0.5f), 128, 150, 95);
                // dangling insides: heart, lungs and a coil of gut
                for (int i = 0; i < 9; i++) Ellipse(128 + Mathf.Sin(i * 1.3f) * 14, 108 - i * 10, 9, 8, new Color(0.85f, 0.25f, 0.35f), outline: true);
                Ellipse(112, 96, 14, 18, new Color(0.7f, 0.15f, 0.2f), outline: true);
                Ellipse(146, 96, 14, 18, new Color(0.7f, 0.15f, 0.2f), outline: true);
                Ellipse(128, 70, 12, 13, Blood, outline: true);
                Head(128, 162, Pale, longHair: true, mouth: Blood, fangs: true);
                break;
            case "krahang":
                Glow(new Color(0.9f, 0.6f, 0.3f), 128, 130, 110);
                Ellipse(58, 140, 52, 40, new Color(0.72f, 0.55f, 0.3f), outline: true);   // กระด้ง wings
                Ellipse(198, 140, 52, 40, new Color(0.72f, 0.55f, 0.3f), outline: true);
                for (int r = 12; r < 52; r += 10) { Ring(58, 140, r, r * 0.77f, new Color(0.55f, 0.4f, 0.2f)); Ring(198, 140, r, r * 0.77f, new Color(0.55f, 0.4f, 0.2f)); }
                Body(128, 92, 34, 58, new Color(0.35f, 0.2f, 0.12f), new Color(0.6f, 0.45f, 0.35f));
                Box(150, 30, 160, 120, new Color(0.5f, 0.35f, 0.2f));                         // the pestle (สาก) as a tail
                Head(128, 168, new Color(0.62f, 0.45f, 0.36f), longHair: false, mouth: Ink, fangs: true);
                break;
            case "pop":
                Glow(new Color(0.75f, 0.4f, 0.9f), 128, 135, 110);
                Body(128, 80, 50, 78, new Color(0.3f, 0.28f, 0.32f), new Color(0.72f, 0.66f, 0.6f));
                Head(128, 160, new Color(0.74f, 0.68f, 0.62f), longHair: true, hairColor: new Color(0.75f, 0.75f, 0.78f), mouth: Blood, fangs: true, wild: true);
                Box(124, 96, 132, 138, Blood);                                                 // long tongue
                Ellipse(128, 96, 6, 6, Blood);
                break;
            case "tani":
                Glow(new Color(0.4f, 0.95f, 0.45f), 128, 130, 110);
                Leaf(60, 120); Leaf(196, 120);
                Body(128, 78, 52, 84, new Color(0.2f, 0.55f, 0.25f), Pale);
                Head(128, 160, new Color(0.8f, 0.9f, 0.78f), longHair: true, mouth: new Color(0.4f, 0.1f, 0.15f));
                break;
            case "takhian":
                Glow(new Color(1f, 0.45f, 0.35f), 128, 130, 110);
                Box(20, 0, 50, 256, new Color(0.35f, 0.22f, 0.12f));                         // the ตะเคียน trunk behind
                Body(128, 78, 52, 84, new Color(0.65f, 0.12f, 0.12f), Pale);
                Box(96, 120, 160, 132, new Color(0.95f, 0.75f, 0.25f));                        // golden sabai sash
                Head(128, 160, Pale, longHair: true, mouth: Blood);
                break;
            case "phrai":
                Glow(new Color(0.6f, 0.85f, 1f), 128, 130, 115);
                Body(128, 78, 56, 90, new Color(0.93f, 0.95f, 0.97f), Pale);
                Head(128, 160, new Color(0.88f, 0.92f, 0.95f), longHair: true, mouth: new Color(0.3f, 0.3f, 0.4f));
                break;
            case "pret":
                Glow(new Color(0.75f, 0.75f, 0.85f), 128, 128, 120);
                Body(128, 60, 30, 110, new Color(0.5f, 0.48f, 0.5f), new Color(0.55f, 0.53f, 0.55f));
                Box(124, 160, 132, 210, new Color(0.55f, 0.53f, 0.55f));                       // the long neck
                Ellipse(128, 222, 22, 22, new Color(0.55f, 0.53f, 0.55f), outline: true);
                Ellipse(120, 226, 4, 4, Eye); Ellipse(136, 226, 4, 4, Eye);
                Ellipse(128, 212, 2, 2, Ink);                                                  // a pin-hole mouth
                break;
            case "nak":
                Glow(new Color(0.75f, 0.55f, 1f), 128, 130, 115);
                Body(128, 78, 54, 86, new Color(0.45f, 0.25f, 0.55f), Pale);
                Ellipse(168, 112, 18, 14, new Color(0.95f, 0.95f, 0.95f), outline: true);       // the baby in her arm
                Ellipse(178, 120, 8, 8, Pale, outline: true);
                Head(128, 160, Pale, longHair: true, mouth: Blood, longest: true);
                break;
            default:
                Glow(new Color(0.8f, 0.9f, 1f), 128, 130, 110);
                Body(128, 80, 52, 84, new Color(0.9f, 0.92f, 0.95f), Pale);
                Head(128, 160, Pale, longHair: true, mouth: Ink);
                break;
        }
    }

    // ── Parts ─────────────────────────────────────────────────────────────────────────────
    private static void Head(float x, float y, Color skin, bool longHair, Color mouth, bool fangs = false, bool wild = false,
        Color? hairColor = null, bool longest = false)
    {
        var hair = hairColor ?? Hair;
        if (longHair) Box(x - 40, y - (longest ? 130 : 90), x + 40, y + 20, hair);
        Ellipse(x, y + 10, 42, 40, hair);
        Ellipse(x, y, 34, 38, skin, outline: true);
        Box(x - 34, y + 8, x + 34, y + 40, hair);                                // fringe
        if (wild) for (int i = -3; i <= 3; i++) Ellipse(x + i * 12, y + 44, 8, 12, hair);
        Ellipse(x - 13, y - 2, wild ? 10 : 8, wild ? 10 : 6, Color.white, outline: true);
        Ellipse(x + 13, y - 2, wild ? 10 : 8, wild ? 10 : 6, Color.white, outline: true);
        Ellipse(x - 13, y - 2, 4, 4, Eye);
        Ellipse(x + 13, y - 2, 4, 4, Eye);
        Ellipse(x - 13, y - 9, 7, 2, new Color(0.3f, 0.3f, 0.35f, 0.6f));        // dark rings under the eyes
        Ellipse(x + 13, y - 9, 7, 2, new Color(0.3f, 0.3f, 0.35f, 0.6f));
        Ellipse(x, y - 22, 10, 4, mouth);
        if (fangs) { Box(x - 7, y - 30, x - 4, y - 22, Color.white); Box(x + 4, y - 30, x + 7, y - 22, Color.white); }
    }

    private static void Body(float x, float y, float halfWidth, float height, Color cloth, Color skin)
    {
        // a flowing gown that fades out at the hem, like it floats
        for (int j = 0; j < (int)height; j++)
        {
            float t = j / height;
            float w = Mathf.Lerp(halfWidth * 0.55f, halfWidth, t);
            var c = cloth;
            c.a = Mathf.Clamp01(t * 1.6f);
            Box(x - w, y - height * 0.5f + (height - j), x + w, y - height * 0.5f + (height - j) + 1, c);
        }
        Ellipse(x - halfWidth * 0.75f, y + height * 0.05f, 7, 22, skin, outline: true); // arms reaching out
        Ellipse(x + halfWidth * 0.75f, y + height * 0.05f, 7, 22, skin, outline: true);
    }

    private static void Leaf(float x, float y)
    {
        Ellipse(x, y, 26, 70, new Color(0.25f, 0.6f, 0.2f), outline: true);
        Box(x - 1, y - 70, x + 1, y + 70, new Color(0.5f, 0.75f, 0.3f));
    }

    private static void Glow(Color c, float x, float y, float r)
    {
        for (int py = 0; py < Size; py++)
        for (int pxl = 0; pxl < Size; pxl++)
        {
            float d = Vector2.Distance(new Vector2(pxl, py), new Vector2(x, y)) / r;
            if (d >= 1.6f) continue;
            float a = Mathf.Clamp01(1f - d / 1.6f) * 0.55f;
            Blend(pxl, py, new Color(c.r, c.g, c.b, a * a * 1.6f));
        }
    }

    // ── Pixels ────────────────────────────────────────────────────────────────────────────
    private static void Ellipse(float cx, float cy, float rx, float ry, Color c, bool outline = false)
    {
        float o = outline ? 3f : 0f;
        int x0 = Mathf.Max(0, (int)(cx - rx - o - 1)), x1 = Mathf.Min(Size - 1, (int)(cx + rx + o + 1));
        int y0 = Mathf.Max(0, (int)(cy - ry - o - 1)), y1 = Mathf.Min(Size - 1, (int)(cy + ry + o + 1));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d <= 1f) Blend(x, y, c);
            else if (outline)
            {
                float ox = (x - cx) / (rx + o), oy = (y - cy) / (ry + o);
                if (ox * ox + oy * oy <= 1f) Blend(x, y, Ink);
            }
        }
    }

    private static void Ring(float cx, float cy, float rx, float ry, Color c)
    {
        for (int a = 0; a < 360; a += 2)
        {
            float r = a * Mathf.Deg2Rad;
            int x = (int)(cx + Mathf.Cos(r) * rx), y = (int)(cy + Mathf.Sin(r) * ry);
            if (x >= 0 && x < Size && y >= 0 && y < Size) Blend(x, y, c);
        }
    }

    private static void Box(float x0, float y0, float x1, float y1, Color c)
    {
        for (int y = Mathf.Max(0, (int)y0); y < Mathf.Min(Size, (int)y1); y++)
        for (int x = Mathf.Max(0, (int)x0); x < Mathf.Min(Size, (int)x1); x++)
            Blend(x, y, c);
    }

    private static void Blend(int x, int y, Color c)
    {
        int i = y * Size + x;
        var d = px[i];
        float a = c.a + d.a * (1f - c.a);
        if (a <= 0f) return;
        px[i] = new Color((c.r * c.a + d.r * d.a * (1f - c.a)) / a, (c.g * c.a + d.g * d.a * (1f - c.a)) / a, (c.b * c.a + d.b * d.a * (1f - c.a)) / a, a);
    }
}
