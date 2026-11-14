using UnityEngine;
using UnityEngine.UI;

// A fish card (การ์ดปลา): gold frame, deep water behind, the painted fish (FishArt), 1–3 stars for
// how big it is for its kind, a "หายาก" tag for the rare sea fish, and a ribbon with the name and the
// weight. Used by the full-screen catch (FishCatchUI) and the auto-quest result card (AutoCardUI).
public static class FishCard
{
    private static Sprite water, star;

    public static RectTransform Make(Transform parent, string id, string name, float kg, Vector2 position, Vector2 size)
    {
        float k = size.y / 520f; // everything scales with the card
        var frame = UiKit.Panel("FishCard", parent, UiKit.Gold, position, size, shadow: true);
        frame.GetComponent<Image>().raycastTarget = false;
        var inner = UiKit.Panel("Water", frame, Color.white, Vector2.zero, size - Vector2.one * 20f * k);
        var innerImage = inner.GetComponent<Image>();
        innerImage.sprite = water != null ? water : water = MakeWater();
        innerImage.type = Image.Type.Simple;
        innerImage.raycastTarget = false;
        inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        var spec = FishArt.Find(id);
        var art = FishArt.Get(id);
        var fish = UiKit.Rect("Fish", inner, Color.white);
        fish.anchoredPosition = new Vector2(0f, 30f * k);
        fish.sizeDelta = new Vector2(size.x * 0.98f, size.x * 0.49f);
        var fishImage = fish.GetComponent<Image>();
        fishImage.raycastTarget = false;
        if (art != null) { fishImage.sprite = art; fishImage.preserveAspect = true; }
        else fishImage.color = new Color(1f, 1f, 1f, 0f);
        var drop = fish.gameObject.AddComponent<Shadow>();
        drop.effectColor = new Color(0f, 0.05f, 0.1f, 0.35f);
        drop.effectDistance = new Vector2(6f * k, -10f * k);

        // stars, top left
        int stars = FishArt.Stars(id, kg);
        for (int i = 0; i < 3; i++)
        {
            var s = UiKit.Rect("Star", inner, i < stars ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 1f, 1f, 0.25f));
            s.anchorMin = s.anchorMax = new Vector2(0f, 1f);
            s.anchoredPosition = new Vector2((44f + i * 56f) * k, -42f * k);
            s.sizeDelta = Vector2.one * 52f * k;
            var img = s.GetComponent<Image>();
            img.sprite = star != null ? star : star = MakeStar();
            img.raycastTarget = false;
        }
        if (spec != null && spec.rare)
        {
            var tag = UiKit.Panel("Rare", inner, new Color(0.62f, 0.16f, 0.5f), Vector2.zero, new Vector2(150f, 50f) * k);
            tag.anchorMin = tag.anchorMax = new Vector2(1f, 1f);
            tag.anchoredPosition = new Vector2(-95f * k, -42f * k);
            tag.GetComponent<Image>().raycastTarget = false;
            UiKit.Label(tag, Mathf.RoundToInt(30 * k), "หายาก", Vector2.zero, new Vector2(150f, 50f) * k, Color.white).raycastTarget = false;
        }

        // name + weight ribbon along the bottom
        var ribbon = UiKit.Rect("Ribbon", inner, new Color(0.05f, 0.1f, 0.16f, 0.72f));
        ribbon.anchorMin = new Vector2(0f, 0f);
        ribbon.anchorMax = new Vector2(1f, 0f);
        ribbon.pivot = new Vector2(0.5f, 0f);
        ribbon.anchoredPosition = Vector2.zero;
        ribbon.sizeDelta = new Vector2(0f, 110f * k);
        ribbon.GetComponent<Image>().raycastTarget = false;
        var n = UiKit.Cartoon(UiKit.Label(ribbon, Mathf.RoundToInt(50 * k), name, new Vector2(-size.x * 0.17f, 0f), new Vector2(size.x * 0.6f, 100f * k), UiKit.WoodInk, TextAnchor.MiddleLeft), 3f * k);
        n.raycastTarget = false;
        var w = UiKit.Cartoon(UiKit.Label(ribbon, Mathf.RoundToInt(42 * k), $"{kg:0.00} กก.", new Vector2(size.x * 0.27f, 0f), new Vector2(size.x * 0.4f, 100f * k), UiKit.GoldText, TextAnchor.MiddleRight), 3f * k);
        w.raycastTarget = false;
        return frame;
    }

    // Deep water: light from the surface at the top, dark below, a few soft light shafts
    private static Sprite MakeWater()
    {
        const int w = 128, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var top = new Color(0.36f, 0.7f, 0.82f);
        var bottom = new Color(0.04f, 0.2f, 0.32f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = y / (float)(h - 1);
                var c = Color.Lerp(bottom, top, t * t);
                float shaft = Mathf.Pow(Mathf.Abs(Mathf.Sin((x + y * 0.35f) * 0.07f)), 12f) * t * 0.18f;
                c += new Color(shaft, shaft, shaft * 0.8f, 0f);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // Five-pointed star with soft edges
    private static Sprite MakeStar()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dx, dy); // 0 = straight up
                float seg = Mathf.Repeat(a, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f); // 0..1 between two points
                float edge = Mathf.Lerp(0.95f, 0.42f, 1f - Mathf.Abs(seg - 0.5f) * 2f);
                float inside = Mathf.Clamp01((edge - r) * n * 0.5f);
                float rim = Mathf.Clamp01((edge - r) * n * 0.5f - 2.5f);
                var c = Color.Lerp(new Color(0.55f, 0.32f, 0.05f), Color.white, rim);
                c.a = inside;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
