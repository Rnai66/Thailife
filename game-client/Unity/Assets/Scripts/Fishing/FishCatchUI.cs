using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Full-screen catch card (ได้ปลา!): the screen dims, a burst of light spins behind a big picture of
// the fish (Resources/Branding/Icons/sell_<fish id>), which pops in with its name and weight.
// Closes on a tap or after a few seconds.
public class FishCatchUI : MonoBehaviour
{
    private const float Duration = 3.2f;

    private static FishCatchUI current;
    private static Sprite rays;

    private CanvasGroup group;
    private RectTransform burst, fish, title, info;
    private float start;
    private bool closing;

    public static void Show(string fishId, string fishName, float kg)
    {
        if (current != null) Destroy(current.gameObject);
        current = new GameObject("FishCatchUI").AddComponent<FishCatchUI>();
        current.Build(fishId, fishName, kg);
    }

    private void Build(string fishId, string fishName, float kg)
    {
        start = Time.unscaledTime;
        var canvasGo = new GameObject("CatchCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();

        var dim = UiKit.Rect("Dim", canvasGo.transform, new Color(0.02f, 0.05f, 0.12f, 0.72f));
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        var tap = dim.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => { if (Time.unscaledTime - start > 0.4f) Close(); });

        // Spinning light burst
        burst = UiKit.Rect("Burst", dim, new Color(1f, 0.85f, 0.4f, 0.55f));
        burst.anchoredPosition = new Vector2(0f, 40f);
        burst.sizeDelta = new Vector2(1000f, 1000f);
        var burstImage = burst.GetComponent<Image>();
        burstImage.sprite = rays != null ? rays : rays = MakeRays();
        burstImage.raycastTarget = false;

        // The fish picture (or a big letter if there is no icon for it)
        fish = UiKit.IconTile(dim, "sell_" + fishId, new Vector2(0f, 60f), new Vector2(460f, 460f));
        if (UiKit.Art("Icons/sell_" + fishId) == null)
            UiKit.Label(fish, 200, fishName.Length > 3 ? fishName.Substring(3, 1) : "ป", Vector2.zero, new Vector2(440f, 440f), UiKit.Ink);
        fish.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        fish.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -14f);
        fish.localScale = Vector3.zero;

        title = UiKit.Rect("Title", dim, new Color(0f, 0f, 0f, 0f));
        title.anchoredPosition = new Vector2(0f, 360f);
        title.sizeDelta = new Vector2(1200f, 140f);
        var t = UiKit.Cartoon(UiKit.Label(title, 110, "ได้ปลา!", Vector2.zero, new Vector2(1200f, 140f), UiKit.GoldText), 6f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;

        info = UiKit.WoodPlate(dim, new Vector2(0f, -270f), new Vector2(900f, 150f));
        UiKit.Cartoon(UiKit.Label(info, 64, fishName, new Vector2(0f, 22f), new Vector2(860f, 80f), UiKit.WoodInk), 4f);
        UiKit.Cartoon(UiKit.Label(info, 44, $"หนัก {kg:0.00} กิโลกรัม", new Vector2(0f, -38f), new Vector2(860f, 60f), UiKit.GoldText), 3f);
        UiKit.Label(dim, 26, "แตะเพื่อเก็บใส่ข้อง", new Vector2(0f, -390f), new Vector2(800f, 40f), new Color(1f, 1f, 1f, 0.75f));

        GameAudio.Play("bell", null, 0.7f);
    }

    private void Update()
    {
        float k = Time.unscaledTime - start;
        if (!closing)
        {
            float pop = EaseOutBack(Mathf.Clamp01(k / 0.55f));
            fish.localScale = Vector3.one * pop * (1f + Mathf.Sin(k * 3f) * 0.025f);
            fish.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 2.2f) * 4f);
            title.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01((k - 0.15f) / 0.5f));
            info.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01((k - 0.3f) / 0.5f));
            group.alpha = Mathf.Clamp01(k / 0.2f);
            if (k > Duration) Close();
        }
        burst.localRotation = Quaternion.Euler(0f, 0f, -k * 25f);
        burst.localScale = Vector3.one * (0.9f + Mathf.Sin(k * 2f) * 0.06f);
    }

    private void Close()
    {
        if (closing) return;
        closing = true;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.3f;
            fish.localScale = Vector3.one * (1f - t / 0.3f * 0.6f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // Soft sun-ray burst: 16 rays fading out from the middle
    private static Sprite MakeRays()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n * 0.5f) / (n * 0.5f), dy = (y - n * 0.5f) / (n * 0.5f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 8f)), 6f);
                float fade = Mathf.Clamp01(1f - r);
                float glow = Mathf.Clamp01(1f - r * 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ray * fade * 0.9f + glow * glow)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
