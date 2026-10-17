using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Small helpers for the runtime-built windows (coop, contest, …): canvas, panels, labels, buttons.
public static class UiKit
{
    private static Font font;
    public static Font Font => font != null ? font : font = ThaiFont.Font;

    public static readonly Color Paper = new Color(0.98f, 0.95f, 0.86f);
    public static readonly Color Brown = new Color(0.3f, 0.15f, 0.05f);
    public static readonly Color Green = new Color(0.1f, 0.45f, 0.2f);

    // Premium palette (teak, gold and cream) shared by every window
    public static readonly Color Teak = new Color32(0x3B, 0x24, 0x16, 0xFF);
    public static readonly Color TeakDark = new Color32(0x24, 0x15, 0x0D, 0xFF);
    public static readonly Color Gold = new Color32(0xE8, 0xB6, 0x4C, 0xFF);
    public static readonly Color GoldInk = new Color32(0x41, 0x24, 0x02, 0xFF);
    public static readonly Color GoldText = new Color32(0xFA, 0xC7, 0x75, 0xFF);
    public static readonly Color HeaderText = new Color32(0xFA, 0xEE, 0xDA, 0xFF);
    public static readonly Color HeaderSub = new Color32(0xC9, 0xA9, 0x7A, 0xFF);
    public static readonly Color Cream = new Color32(0xFB, 0xF4, 0xE6, 0xFF);
    public static readonly Color CardBorder = new Color32(0xEA, 0xDB, 0xC2, 0xFF);
    public static readonly Color Ink = new Color32(0x3B, 0x24, 0x16, 0xFF);
    public static readonly Color Muted = new Color32(0x88, 0x87, 0x80, 0xFF);
    public static readonly Color Price = new Color32(0xBA, 0x75, 0x17, 0xFF);
    public static readonly Color PayGreen = new Color32(0x2E, 0x8B, 0x57, 0xFF);
    public static readonly Color OkGreen = new Color32(0x3B, 0x6D, 0x11, 0xFF);
    public static readonly Color OkTint = new Color32(0xEA, 0xF3, 0xDE, 0xFF);
    public static readonly Color Danger = new Color32(0x99, 0x3C, 0x1D, 0xFF);
    public static readonly Color DangerTint = new Color32(0xFA, 0xEC, 0xE7, 0xFF);
    public static readonly Color TabText = new Color32(0xD9, 0xC2, 0xA0, 0xFF);
    public static readonly Color Pill = new Color32(0xFA, 0xEE, 0xDA, 0xFF);

    // Premium window frame: teak header (logo letter, title, subtitle, close X) with a gold
    // underline over a cream rounded body. Returns the body; header texts come back via out.
    public static RectTransform PremiumWindow(Transform owner, string name, Vector2 size, int sortingOrder,
        string logo, out GameObject backdrop, out Text title, out Text subtitle, UnityAction onClose, out RectTransform header)
    {
        var card = Window(owner, name, size, sortingOrder, out backdrop);
        var cardImage = card.GetComponent<Image>();
        cardImage.sprite = RoundedSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = TeakDark;

        float top = size.y * 0.5f;
        header = Panel("Header", card, Teak, new Vector2(0f, top - 58f), new Vector2(size.x, 116f));
        var line = Rect("GoldLine", card, Gold);
        line.anchoredPosition = new Vector2(0f, top - 117f);
        line.sizeDelta = new Vector2(size.x, 4f);

        float left = -size.x * 0.5f;
        var badge = Panel("Logo", header, Gold, new Vector2(left + 70f, 0f), new Vector2(76f, 76f));
        Label(badge, 40, logo, Vector2.zero, new Vector2(70f, 70f), GoldInk).fontStyle = FontStyle.Bold;
        title = Label(header, 40, "", new Vector2(left + 360f, 14f), new Vector2(520f, 54f), HeaderText, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        subtitle = Label(header, 24, "", new Vector2(left + 360f, -26f), new Vector2(520f, 34f), HeaderSub, TextAnchor.MiddleLeft);
        if (onClose != null)
            RoundButton(header, "X", new Vector2(-left - 60f, 0f), new Vector2(70f, 70f), new Color32(0x5A, 0x3A, 0x26, 0xFF), HeaderText, onClose, 32, circle: true);

        return Panel("Body", card, Cream, new Vector2(0f, -58f), new Vector2(size.x - 40f, size.y - 156f));
    }

    // Small coloured rounded pill with text (badges, statuses)
    public static Text PillLabel(Transform parent, string text, Vector2 position, Vector2 size, Color fill, Color ink, int fontSize = 24)
    {
        var pill = Panel("Pill", parent, fill, position, size);
        var label = Label(pill, fontSize, text, Vector2.zero, size - new Vector2(16f, 4f), ink);
        return label;
    }

    // Pop-in message bar used by every window (create once, call Toast to show)
    public static Text ToastBar(Transform parent, Vector2 position, float width)
    {
        var bar = Panel("Toast", parent, PayGreen, position, new Vector2(width, 70f), shadow: true);
        var text = Label(bar, 28, "", Vector2.zero, new Vector2(width - 20f, 64f), Color.white);
        text.fontStyle = FontStyle.Bold;
        bar.gameObject.SetActive(false);
        return text;
    }

    public static Coroutine Toast(MonoBehaviour host, Text toast, string message, Color color, Coroutine running)
    {
        if (running != null) host.StopCoroutine(running);
        return host.StartCoroutine(ToastRoutine(toast, message, color));
    }

    private static System.Collections.IEnumerator ToastRoutine(Text toast, string message, Color color)
    {
        var bar = (RectTransform)toast.transform.parent;
        toast.text = message;
        bar.GetComponent<Image>().color = color;
        bar.gameObject.SetActive(true);
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            bar.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.05f, t / 0.15f);
            yield return null;
        }
        bar.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(2.4f);
        bar.gameObject.SetActive(false);
    }

    // Full-screen overlay canvas with a dimmed backdrop and a centred card; returns the card
    public static RectTransform Window(Transform owner, string name, Vector2 cardSize, int sortingOrder, out GameObject backdrop)
    {
        var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(owner, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = Rect("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f));
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        backdrop = dim.gameObject;

        var card = Rect("Card", dim, Paper);
        card.sizeDelta = cardSize;
        return card;
    }

    public static RectTransform Rect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static Text Label(Transform parent, int size, string text, Vector2 position, Vector2 box, Color color,
        TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var label = go.GetComponent<Text>();
        label.font = Font;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = size;
        return label;
    }

    public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, Color color, UnityAction onClick, int fontSize = 34)
    {
        var rt = Rect("Button", parent, color);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(onClick);
        Label(rt, fontSize, text, Vector2.zero, size - new Vector2(12f, 6f), new Color(0.2f, 0.1f, 0.05f));
        return button;
    }

    public static void Clear(Transform container)
    {
        foreach (Transform child in container) Object.Destroy(child.gameObject);
    }

    public static RectTransform Container(Transform parent, Vector2 position)
    {
        var rt = new GameObject("Container", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        return rt;
    }

    // ── Premium look: rounded panels, circles, soft shadows, press feedback ───────
    private static Sprite rounded, circle;

    // 9-sliced rounded rectangle (corner radius ~20 canvas units), generated once in code
    public static Sprite RoundedSprite => rounded != null ? rounded : rounded = MakeRounded(64, 20);
    public static Sprite CircleSprite => circle != null ? circle : circle = MakeRounded(64, 32, sliced: false);

    private static Sprite MakeRounded(int size, int radius, bool sliced = true)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // distance outside the rounded corner, anti-aliased over one pixel
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
            byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        var border = sliced ? new Vector4(radius + 2, radius + 2, radius + 2, radius + 2) : Vector4.zero;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    public static RectTransform Panel(string name, Transform parent, Color color, Vector2 position, Vector2 size, bool shadow = false)
    {
        var rt = Rect(name, parent, color);
        var image = rt.GetComponent<Image>();
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        if (shadow)
        {
            var s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.18f);
            s.effectDistance = new Vector2(0f, -4f);
        }
        return rt;
    }

    public static Button RoundButton(Transform parent, string text, Vector2 position, Vector2 size, Color color, Color textColor, UnityAction onClick, int fontSize = 32, bool circle = false)
    {
        var rt = circle ? Rect("Button", parent, color) : Panel("Button", parent, color, position, size);
        if (circle)
        {
            rt.GetComponent<Image>().sprite = CircleSprite;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        button.colors = colors;
        rt.gameObject.AddComponent<PressScale>();
        var label = Label(rt, fontSize, text, Vector2.zero, size - new Vector2(12f, 6f), textColor);
        label.fontStyle = FontStyle.Bold;
        return button;
    }

    public static void FloatAbovePlayer(string text, Color color)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) FloatingText.Spawn(player.transform.position + Vector3.up * 2f, text, color);
    }
}
