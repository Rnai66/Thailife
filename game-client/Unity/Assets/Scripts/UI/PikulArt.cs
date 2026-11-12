using UnityEngine;

// ดอกพิกุล — the game's yellow five-petal flower (UiKit.FlowerSprite) — as the loading mark
// (PikulSpinner) and the confetti of the level-up burst (LevelUpFX); with the soft glow, the sun rays
// and the sparkle those effects use.
public static class PikulArt
{
    private static Sprite glow, rays, spark;

    public static Sprite Pikul => UiKit.FlowerSprite; // the game's own flower (the one on the old loading screen)
    public static Sprite Glow => glow != null ? glow : glow = MakeGlow();
    public static Sprite Rays => rays != null ? rays : rays = MakeRays();
    public static Sprite Spark => spark != null ? spark : spark = MakeSpark();

    // 0 between petals .. 1 down a petal's middle, for `count` petals turned by `turn` (radians)
    private static float Petal(float a, int count, float turn)
    {
        float f = Mathf.Repeat((a + turn) * count / (Mathf.PI * 2f), 1f);
        return 1f - Mathf.Abs(f * 2f - 1f);
    }

    private static Sprite MakeGlow()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - r);
            px[y * n + x] = new Color(1f, 1f, 1f, a * a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeRays()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
            float ray = Mathf.SmoothStep(0.35f, 0.65f, Petal(a, 14, 0f));
            float fade = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f);
            px[y * n + x] = new Color(1f, 1f, 1f, ray * fade * fade);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // A four-pointed twinkle
    private static Sprite MakeSpark()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
            float star = Mathf.Clamp01(1f - (Mathf.Sqrt(dx) + Mathf.Sqrt(dy)) * 0.9f);
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3f);
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(star * 2f + core));
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
