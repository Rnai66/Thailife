using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Prize reveal shared by the fair game screens (สอยดาว, ปาโป่ง, บิงโก): a card pops up over the game
// with a spinning light burst, the prize picture and its name. Big prizes get gold and a bigger burst.
public static class FairPrize
{
    private static Sprite rays;

    public static GameObject Show(MonoBehaviour host, RectTransform parent, string heading, string icon, string prize, bool won, bool big)
    {
        var layer = UiKit.Rect("PrizeLayer", parent, new Color(0f, 0f, 0f, 0.45f));
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;

        var burst = UiKit.Rect("Burst", layer, big ? new Color(1f, 0.82f, 0.3f, 0.75f) : new Color(1f, 0.95f, 0.75f, won ? 0.5f : 0f));
        burst.sizeDelta = Vector2.one * (big ? 760f : 600f);
        burst.anchoredPosition = new Vector2(0f, 30f);
        var img = burst.GetComponent<Image>();
        img.sprite = rays != null ? rays : rays = MakeRays();
        img.raycastTarget = false;

        var card = UiKit.Panel("PrizeCard", layer, big ? new Color32(0xFF, 0xF4, 0xD0, 0xFF) : UiKit.Cream, new Vector2(0f, 20f), new Vector2(560f, 470f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = big ? UiKit.Gold : UiKit.CardBorder;
        var head = UiKit.Cartoon(UiKit.Label(card, big ? 46 : 38, heading, new Vector2(0f, 190f), new Vector2(540f, 60f), big ? UiKit.GoldText : UiKit.WoodInk), 3f);
        head.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (!string.IsNullOrEmpty(icon) && UiKit.Art("Icons/" + icon) != null)
            UiKit.IconTile(card, icon, new Vector2(0f, 30f), new Vector2(230f, 230f));
        else
            UiKit.Label(card, 120, won ? "★" : "—", new Vector2(0f, 30f), new Vector2(230f, 230f), won ? UiKit.Gold : UiKit.Muted);
        var name = UiKit.Label(card, 34, prize, new Vector2(0f, -140f), new Vector2(520f, 90f), won ? UiKit.Ink : UiKit.Muted);
        name.font = ThaiFont.Display;
        UiKit.Label(card, 20, "แตะเพื่อปิด", new Vector2(0f, -205f), new Vector2(400f, 30f), UiKit.Muted);

        var tap = layer.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => Object.Destroy(layer.gameObject));

        host.StartCoroutine(Animate(burst, card, big));
        GameAudio.Play(won ? "bell" : "whoosh", null, big ? 1f : 0.7f);
        if (won) GameAudio.Play("coins", null, big ? 1f : 0.6f);
        return layer.gameObject;
    }

    private static IEnumerator Animate(RectTransform burst, RectTransform card, bool big)
    {
        float start = Time.unscaledTime;
        while (card != null)
        {
            float k = Time.unscaledTime - start;
            float pop = Mathf.Clamp01(k / 0.45f);
            card.localScale = Vector3.one * (1f + 2.70158f * Mathf.Pow(pop - 1f, 3f) + 1.70158f * Mathf.Pow(pop - 1f, 2f));
            if (burst != null)
            {
                burst.localRotation = Quaternion.Euler(0f, 0f, -k * (big ? 40f : 22f));
                burst.localScale = Vector3.one * (0.92f + Mathf.Sin(k * 2.4f) * 0.06f);
            }
            yield return null;
        }
    }

    private static Sprite MakeRays()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n * 0.5f) / (n * 0.5f), dy = (y - n * 0.5f) / (n * 0.5f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(dy, dx) * 9f)), 6f);
                float glow = Mathf.Clamp01(1f - r * 1.7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ray * Mathf.Clamp01(1f - r) * 0.9f + glow * glow)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
