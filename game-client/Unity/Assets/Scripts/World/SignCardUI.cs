using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Detail card for a signpost (ป้ายบอกทาง): pops up at the bottom of the screen while you stand by the
// sign — a vermilion header with the place name in gold, flowers at the corners, and the sign's text
// on cream underneath, big enough to read on a phone. Display only (no touches), so the joystick and
// buttons keep working. While a place's own window is open (e.g. the chicken contest, which already
// shows the same name and details) the card steps aside, so only one popup is on screen. One shared instance.
public class SignCardUI : MonoBehaviour
{
    private static SignCardUI instance;
    private static readonly Color Vermilion = new Color(0.72f, 0.12f, 0.08f);

    private CanvasGroup group;
    private RectTransform card, header;
    private Text title, body;
    private object owner;
    private Coroutine anim;
    private bool suppressed; // hidden because a window is open

    public static void Show(object who, string heading, string text)
    {
        if (instance == null) instance = new GameObject("SignCardUI").AddComponent<SignCardUI>();
        instance.Open(who, heading, text);
    }

    public static void Hide(object who)
    {
        if (instance != null && instance.owner == who) instance.Close();
    }

    private void Awake()
    {
        var canvasGo = new GameObject("SignCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        card = UiKit.Rect("SignCard", canvasGo.transform, UiKit.Cream);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0f);
        card.anchoredPosition = new Vector2(0f, 60f);
        Style(card.GetComponent<Image>());
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        card.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -10f);

        header = UiKit.Rect("Header", card, Vermilion);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.offsetMin = new Vector2(10f, -96f);
        header.offsetMax = new Vector2(-10f, -10f);
        Style(header.GetComponent<Image>());
        var line = UiKit.Rect("GoldLine", header, UiKit.Gold);
        line.anchorMin = new Vector2(0f, 0f);
        line.anchorMax = new Vector2(1f, 0f);
        line.offsetMin = new Vector2(0f, -3f);
        line.offsetMax = new Vector2(0f, 3f);
        foreach (float side in new[] { -1f, 1f })
        {
            var flower = UiKit.Rect("Flower", header, Color.white);
            flower.anchorMin = flower.anchorMax = new Vector2(side < 0 ? 0f : 1f, 0.5f);
            flower.anchoredPosition = new Vector2(side * -46f, 0f);
            flower.sizeDelta = new Vector2(44f, 44f);
            flower.GetComponent<Image>().sprite = UiKit.FlowerSprite;
        }

        title = UiKit.Cartoon(UiKit.Label(header, 48, "", Vector2.zero, Vector2.zero, UiKit.GoldText), 3f);
        Stretch(title.rectTransform, 90f, 0f);
        body = UiKit.Label(card, 34, "", Vector2.zero, Vector2.zero, UiKit.Ink);
        body.font = ThaiFont.Display;
        body.lineSpacing = 1.1f;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.rectTransform.anchorMin = Vector2.zero;
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(40f, 22f);
        body.rectTransform.offsetMax = new Vector2(-40f, -110f);
        foreach (var g in canvasGo.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        group.alpha = 0f;
        card.gameObject.SetActive(false);
    }

    private static void Style(Image image)
    {
        image.sprite = UiKit.RoundedSprite;
        image.type = Image.Type.Sliced;
    }

    private static void Stretch(RectTransform rt, float sideInset, float vertical)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(sideInset, vertical);
        rt.offsetMax = new Vector2(-sideInset, -vertical);
    }

    private void Update()
    {
        if (owner == null) return;
        bool window = UiKit.AnyWindowOpen;
        if (window && !suppressed)
        {
            suppressed = true;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Pop(false));
        }
        else if (!window && suppressed)
        {
            suppressed = false;
            card.gameObject.SetActive(true);
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Pop(true));
        }
    }

    private void Open(object who, string heading, string text)
    {
        owner = who;
        suppressed = UiKit.AnyWindowOpen;
        title.text = heading;
        body.text = text;
        bool hasBody = !string.IsNullOrEmpty(text);
        body.gameObject.SetActive(hasBody);
        int lines = hasBody ? text.Split('\n').Length : 0;
        float width = Mathf.Clamp(Mathf.Max(heading.Length * 34f, LongestLine(text) * 22f) + 220f, 640f, 1100f);
        card.sizeDelta = new Vector2(width, 116f + (hasBody ? lines * 48f + 36f : 0f));
        if (suppressed) return;
        card.gameObject.SetActive(true);
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Pop(true));
        GameAudio.Play("whoosh", null, 0.25f);
    }

    private void Close()
    {
        owner = null;
        suppressed = false;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Pop(false));
    }

    private IEnumerator Pop(bool show)
    {
        float from = group.alpha, to = show ? 1f : 0f;
        float time = show ? 0.35f : 0.18f;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            group.alpha = Mathf.Lerp(from, to, k);
            card.localScale = Vector3.one * (show ? EaseOutBack(k) * 0.3f + 0.7f : 1f - k * 0.1f);
            yield return null;
        }
        group.alpha = to;
        card.localScale = Vector3.one;
        if (!show) card.gameObject.SetActive(false);
        anim = null;
    }

    private static int LongestLine(string text)
    {
        int best = 0;
        if (string.IsNullOrEmpty(text)) return 0;
        foreach (var l in text.Split('\n')) best = Mathf.Max(best, l.Length);
        return best;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
