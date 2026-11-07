using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ข้อความแจ้งผล (สำเร็จ / เลเวลอัป / ออโต้กำลังทำอะไร): a calm, readable pill in the upper middle of the
// screen — clear text on a dark backing, gently in, held long enough to read (longer for longer text),
// gently out. Up to three stack; a newer one pushes the older ones up. FloatingText sends its words
// here and keeps only short numbers (+50 ฿, damage) for over the head.
public class ScreenToast : MonoBehaviour
{
    private const int MaxShown = 3;
    private const float Gap = 82f;

    private static ScreenToast instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private class Item
    {
        public RectTransform rect;
        public CanvasGroup group;
        public float born, life;
        public string text;
    }

    private readonly List<Item> items = new List<Item>();
    private RectTransform root;

    public static void Show(string message, Color color)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (instance == null) instance = new GameObject("ScreenToast").AddComponent<ScreenToast>();
        instance.Add(message, color);
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        var canvasGo = new GameObject("ToastCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = new GameObject("Toasts", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvasGo.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.68f);
    }

    private void Add(string message, Color color)
    {
        // the same words again while still showing: just keep that one up longer
        foreach (var it in items)
            if (it.text == message) { it.born = Time.unscaledTime - 0.3f; return; }

        float width = Mathf.Min(1100f, 140f + message.Length * 22f);
        var bar = UiKit.Panel("Toast", root, new Color(0.1f, 0.07f, 0.05f, 0.82f), Vector2.zero, new Vector2(width, 70f));
        bar.gameObject.AddComponent<Outline>().effectColor = new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.8f);
        bar.GetComponent<Image>().raycastTarget = false;
        // light colours stay as they are; dark ones would vanish on the backing, so lift them
        var ink = color.grayscale < 0.55f ? Color.Lerp(color, Color.white, 0.5f) : color;
        ink.a = 1f;
        var label = UiKit.Label(bar, 34, message, Vector2.zero, new Vector2(width - 40f, 62f), ink);
        label.fontStyle = FontStyle.Bold;
        var group = bar.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        items.Add(new Item { rect = bar, group = group, born = Time.unscaledTime, life = Mathf.Clamp(2.2f + message.Length * 0.06f, 2.6f, 5f), text = message });
        while (items.Count > MaxShown) { Destroy(items[0].rect.gameObject); items.RemoveAt(0); }
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            float age = now - it.born;
            if (age >= it.life) { Destroy(it.rect.gameObject); items.RemoveAt(i); continue; }
            float fadeIn = Mathf.Clamp01(age / 0.25f), fadeOut = Mathf.Clamp01((it.life - age) / 0.5f);
            it.group.alpha = Mathf.Min(fadeIn, fadeOut);
            // newest at the bottom, older ones above it; a little rise as it appears
            float slot = (items.Count - 1 - i) * Gap;
            float y = slot + (1f - Mathf.SmoothStep(0f, 1f, fadeIn)) * -20f;
            it.rect.anchoredPosition = Vector2.Lerp(it.rect.anchoredPosition, new Vector2(0f, y), 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            float s = Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, fadeIn));
            it.rect.localScale = new Vector3(s, s, 1f);
        }
    }
}
