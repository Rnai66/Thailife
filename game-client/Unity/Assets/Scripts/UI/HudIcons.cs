using System;
using System.Collections.Generic;
using UnityEngine;

// Plain white pictograms for the round HUD buttons and the bag's gear slots (bag, menu, person, star,
// …), drawn once in code from simple shapes so they stay crisp and clean on any background; tint
// them with the Image colour. The place icons in Resources carry their own captions and don't
// read at button size.
public static class HudIcons
{
    private const int Size = 96, Samples = 3;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // A pictogram by name; null for an unknown name
    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out var s)) return s;
        var shape = Shape(name);
        if (shape == null) return null;
        s = Draw(shape);
        cache[name] = s;
        return s;
    }

    private static Sprite Draw(Func<float, float, bool> inside)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[Size * Size];
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            int hits = 0;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
            {
                float x = (px + (sx + 0.5f) / Samples) / Size * 2f - 1f;
                float y = (py + (sy + 0.5f) / Samples) / Size * 2f - 1f;
                if (inside(x, y)) hits++;
            }
            pixels[py * Size + px] = new Color32(255, 255, 255, (byte)(hits * 255 / (Samples * Samples)));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ── Shapes, in a -1..1 square with +y up ──────────────────────────────────────────────────
    private static bool Circle(float x, float y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r;

    private static bool Box(float x, float y, float cx, float cy, float hw, float hh, float r)
    {
        float dx = Mathf.Max(Mathf.Abs(x - cx) - (hw - r), 0f), dy = Mathf.Max(Mathf.Abs(y - cy) - (hh - r), 0f);
        return dx * dx + dy * dy < r * r;
    }

    private static bool Frame(float x, float y, float cx, float cy, float hw, float hh, float r, float w) =>
        Box(x, y, cx, cy, hw, hh, r) && !Box(x, y, cx, cy, hw - w, hh - w, Mathf.Max(0.01f, r - w));

    // A stroke from a to b, w wide with round ends
    private static bool Line(float x, float y, float ax, float ay, float bx, float by, float w)
    {
        float vx = bx - ax, vy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy));
        float dx = x - (ax + vx * t), dy = y - (ay + vy * t);
        return dx * dx + dy * dy < w * w * 0.25f;
    }

    private static bool Poly(float x, float y, float[] p)
    {
        bool c = false;
        for (int i = 0, j = p.Length / 2 - 1; i < p.Length / 2; j = i++)
        {
            float xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
        }
        return c;
    }

    private static readonly float[] Shirt = { -0.25f, 0.75f, -0.78f, 0.45f, -0.56f, 0.06f, -0.38f, 0.16f, -0.38f, -0.8f, 0.38f, -0.8f, 0.38f, 0.16f, 0.56f, 0.06f, 0.78f, 0.45f, 0.25f, 0.75f, 0f, 0.55f };
    private static readonly float[] Pants = { -0.5f, 0.75f, 0.5f, 0.75f, 0.6f, -0.85f, 0.16f, -0.85f, 0f, 0.25f, -0.16f, -0.85f, -0.6f, -0.85f };
    private static readonly float[] Star = MakeStar();
    private static readonly float[] Bolt = { 0.15f, 0.9f, -0.55f, -0.1f, -0.05f, -0.1f, -0.2f, -0.9f, 0.55f, 0.15f, 0.05f, 0.15f };
    private static readonly float[] Tail = { -0.4f, -0.3f, -0.05f, -0.3f, -0.55f, -0.78f };

    private static float[] MakeStar()
    {
        var p = new float[20];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 0.92f : 0.4f;
            p[i * 2] = Mathf.Cos(a) * r;
            p[i * 2 + 1] = Mathf.Sin(a) * r - 0.05f;
        }
        return p;
    }

    private static Func<float, float, bool> Shape(string name)
    {
        switch (name)
        {
            case "bag": // backpack: body with a pocket, a handle on top
                return (x, y) => (Box(x, y, 0f, -0.12f, 0.62f, 0.66f, 0.2f) && !Line(x, y, -0.36f, -0.12f, 0.36f, -0.12f, 0.1f)
                                  && !Frame(x, y, 0f, -0.42f, 0.36f, 0.26f, 0.08f, 0.09f))
                                 || (Frame(x, y, 0f, 0.62f, 0.3f, 0.24f, 0.18f, 0.12f) && y > 0.5f);
            case "menu":
                return (x, y) => Line(x, y, -0.6f, 0.48f, 0.6f, 0.48f, 0.2f) || Line(x, y, -0.6f, 0f, 0.6f, 0f, 0.2f) || Line(x, y, -0.6f, -0.48f, 0.6f, -0.48f, 0.2f);
            case "close":
                return (x, y) => Line(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.2f) || Line(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.2f);
            case "person":
                return (x, y) => Circle(x, y, 0f, 0.4f, 0.32f) || (Circle(x, y, 0f, -0.75f, 0.72f) && y > -0.82f && !Circle(x, y, 0f, 0.4f, 0.42f));
            case "star":
                return (x, y) => Poly(x, y, Star);
            case "quest": // clipboard with lines
                return (x, y) => Frame(x, y, 0f, -0.08f, 0.6f, 0.78f, 0.14f, 0.13f) || Box(x, y, 0f, 0.7f, 0.28f, 0.14f, 0.07f)
                                 || Line(x, y, -0.28f, 0.3f, 0.3f, 0.3f, 0.12f) || Line(x, y, -0.28f, 0f, 0.3f, 0f, 0.12f) || Line(x, y, -0.28f, -0.3f, 0.12f, -0.3f, 0.12f);
            case "food": // bowl with chopsticks
                return (x, y) => (Circle(x, y, 0f, 0.02f, 0.78f) && y < 0.02f) || Box(x, y, 0f, -0.78f, 0.3f, 0.07f, 0.04f)
                                 || Line(x, y, -0.15f, 0.12f, 0.45f, 0.85f, 0.1f) || Line(x, y, 0.05f, 0.12f, 0.75f, 0.7f, 0.1f);
            case "chat": // speech bubble
                return (x, y) => (Box(x, y, 0f, 0.15f, 0.82f, 0.52f, 0.26f) || Poly(x, y, Tail))
                                 && !Circle(x, y, -0.36f, 0.15f, 0.1f) && !Circle(x, y, 0f, 0.15f, 0.1f) && !Circle(x, y, 0.36f, 0.15f, 0.1f);
            case "calendar":
                return (x, y) => (Frame(x, y, 0f, -0.1f, 0.74f, 0.66f, 0.12f, 0.12f) || (Box(x, y, 0f, -0.1f, 0.74f, 0.66f, 0.12f) && y > 0.26f))
                                 || Line(x, y, -0.36f, 0.5f, -0.36f, 0.82f, 0.13f) || Line(x, y, 0.36f, 0.5f, 0.36f, 0.82f, 0.13f)
                                 || Box(x, y, -0.3f, -0.05f, 0.1f, 0.1f, 0.03f) || Box(x, y, 0.05f, -0.05f, 0.1f, 0.1f, 0.03f) || Box(x, y, -0.3f, -0.38f, 0.1f, 0.1f, 0.03f);
            case "gear":
                return (x, y) =>
                {
                    float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                    float outer = Mathf.Cos(a * 8f) > 0.2f ? 0.84f : 0.64f;
                    return r < outer && r > 0.26f;
                };
            case "shirt":
                return (x, y) => Poly(x, y, Shirt);
            case "pants":
                return (x, y) => Poly(x, y, Pants);
            case "rod": // fishing rod with reel and line
                return (x, y) => Line(x, y, -0.72f, -0.8f, 0.62f, 0.82f, 0.1f) || Circle(x, y, -0.4f, -0.36f, 0.17f)
                                 || Line(x, y, 0.62f, 0.82f, 0.62f, 0.05f, 0.05f) || (Circle(x, y, 0.5f, 0.02f, 0.13f) && !Circle(x, y, 0.5f, 0.02f, 0.07f) && y < 0.05f);
            case "note": // musical note
                return (x, y) => Circle(x, y, -0.22f, -0.5f, 0.3f) || Line(x, y, 0.04f, -0.5f, 0.04f, 0.75f, 0.13f) || Line(x, y, 0.04f, 0.75f, 0.5f, 0.42f, 0.14f);
            case "car":
                return (x, y) => ((Box(x, y, 0f, -0.15f, 0.88f, 0.26f, 0.1f) || Box(x, y, -0.05f, 0.2f, 0.48f, 0.26f, 0.14f))
                                  && !Circle(x, y, -0.5f, -0.45f, 0.28f) && !Circle(x, y, 0.5f, -0.45f, 0.28f))
                                 || Circle(x, y, -0.5f, -0.45f, 0.2f) || Circle(x, y, 0.5f, -0.45f, 0.2f);
            case "coin":
                return (x, y) => Circle(x, y, 0f, 0f, 0.9f) && !(Circle(x, y, 0f, 0f, 0.72f) && !Circle(x, y, 0f, 0f, 0.62f));
            case "bolt": // energy
                return (x, y) => Poly(x, y, Bolt);
        }
        return null;
    }
}
