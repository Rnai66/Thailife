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
    // ── Wood-sign style (like the game art): plank texture, cream text with a brown outline ──
    public static readonly Color WoodInk = new Color32(0xFF, 0xF3, 0xD6, 0xFF);   // cream lettering
    public static readonly Color WoodEdge = new Color32(0x3E, 0x20, 0x0C, 0xFF);  // dark outline
    private static Sprite wood, flower;

    // A wooden plaque: warm planks with grain, a dark rim and a light top edge (9-sliced)
    public static Sprite WoodSprite => wood != null ? wood : wood = MakeWood();

    private static Sprite MakeWood()
    {
        const int w = 160, h = 80, r = 22, rim = 6;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        var light = new Color(0.78f, 0.5f, 0.26f);
        var dark = new Color(0.52f, 0.29f, 0.12f);
        var edge = (Color)WoodEdge;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, r, w - r), cy = Mathf.Clamp(y + 0.5f, r, h - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)); // distance from the rounded core
            float alpha = Mathf.Clamp01(r + 0.5f - d);
            float v = y / (float)(h - 1);
            // Grain: wavy streaks along the plank
            float grain = Mathf.Sin(y * 0.55f + Mathf.Sin(x * 0.05f) * 2.2f) * 0.5f + 0.5f;
            grain = grain * 0.6f + Mathf.PerlinNoise(x * 0.04f, y * 0.35f) * 0.4f;
            Color c = Color.Lerp(dark, light, 0.35f + v * 0.45f) * (0.85f + grain * 0.22f);
            if (d > r - rim) c = Color.Lerp(c, edge, Mathf.Clamp01((d - (r - rim)) / 2f));       // dark rim
            else if (y > h - rim - 4 && d < r - rim) c = Color.Lerp(c, new Color(1f, 0.85f, 0.6f), 0.35f); // top highlight
            c.a = alpha;
            px[y * w + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
    }

    // Little yellow frangipani (ลีลาวดี) used as an ornament on plaques
    public static Sprite FlowerSprite => flower != null ? flower : flower = MakeFlower();

    private static Sprite MakeFlower()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
            float petal = 0.55f + 0.4f * Mathf.Abs(Mathf.Cos(a * 2.5f));   // five petals
            float inside = Mathf.Clamp01((petal - r) * 18f);
            Color c = Color.Lerp(new Color(1f, 0.72f, 0.15f), new Color(1f, 0.95f, 0.6f), Mathf.Clamp01(r * 1.6f));
            if (r > petal - 0.08f) c = Color.Lerp(c, new Color(0.6f, 0.35f, 0.05f), 0.6f); // outline
            c.a = inside;
            px[y * n + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // Cartoon lettering: bold with a thick brown outline
    public static Text Cartoon(Text t, float outline = 2.5f)
    {
        t.font = ThaiFont.Display;     // Mitr: rounded cartoon lettering
        t.fontStyle = FontStyle.Normal;
        t.verticalOverflow = VerticalWrapMode.Overflow; // Mitr's lines are taller than Sarabun's: never let uGUI drop the line
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = WoodEdge;
        o.effectDistance = new Vector2(outline, -outline);
        return t;
    }

    public static RectTransform WoodPlate(Transform parent, Vector2 position, Vector2 size)
    {
        var rt = Rect("WoodPlate", parent, Color.white);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var img = rt.GetComponent<Image>();
        img.sprite = WoodSprite;
        img.type = Image.Type.Sliced;
        rt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -5f);
        return rt;
    }

    // Wooden sign button "✿ ข้อความ ✿" like the labels in the game art
    public static Button WoodButton(Transform parent, string text, Vector2 position, Vector2 size, UnityAction onClick, int fontSize = 30, bool flowers = true)
    {
        var rt = WoodPlate(parent, position, size);
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.7f);
        button.colors = colors;
        rt.gameObject.AddComponent<PressScale>();
        bool roomy = flowers && size.x >= 220f;
        float pad = roomy ? size.y * 0.9f : 12f;
        Cartoon(Label(rt, fontSize, text, Vector2.zero, new Vector2(size.x - pad * 2f, size.y - 8f), WoodInk));
        if (roomy)
            foreach (float side in new[] { -1f, 1f })
            {
                var f = Rect("Flower", rt, Color.white);
                f.GetComponent<Image>().sprite = FlowerSprite;
                f.GetComponent<Image>().raycastTarget = false;
                f.sizeDelta = Vector2.one * size.y * 0.5f;
                f.anchoredPosition = new Vector2(side * (size.x * 0.5f - size.y * 0.45f), 0f);
            }
        return button;
    }

    // ── Artwork (Resources/Branding) ───────────────────────────────────────────
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites = new System.Collections.Generic.Dictionary<string, Sprite>();

    // A picture from Resources/Branding (e.g. "Icons/temple", "Logo"); null if it isn't there
    public static Sprite Art(string path)
    {
        if (sprites.TryGetValue(path, out var s) && s != null) return s;
        var tex = Resources.Load<Texture2D>("Branding/" + path);
        if (tex == null) return null;
        s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprites[path] = s;
        return s;
    }

    // A place icon (Resources/Branding/Icons/<name>) clipped to a rounded tile
    public static RectTransform IconTile(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var tile = Panel("IconTile", parent, Color.white, position, size);
        tile.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var img = Rect("Icon", tile, Color.white);
        img.anchorMin = Vector2.zero;
        img.anchorMax = Vector2.one;
        img.offsetMin = new Vector2(-size.x * 0.04f, -size.y * 0.04f); // trim the artwork's own rounded edge
        img.offsetMax = new Vector2(size.x * 0.04f, size.y * 0.04f);
        var image = img.GetComponent<Image>();
        image.sprite = Art("Icons/" + name);
        image.raycastTarget = false;
        return tile;
    }

    // Window badge: "icon:temple" shows a place icon, anything else a letter
    public static void Badge(RectTransform badge, string logo)
    {
        if (logo != null && logo.StartsWith("icon:") && Art("Icons/" + logo.Substring(5)) != null)
            IconTile(badge, logo.Substring(5), Vector2.zero, badge.sizeDelta - new Vector2(6f, 6f));
        else
            Label(badge, 40, logo, Vector2.zero, badge.sizeDelta - new Vector2(6f, 6f), GoldInk).fontStyle = FontStyle.Bold;
    }

    public static RectTransform PremiumWindow(Transform owner, string name, Vector2 size, int sortingOrder,
        string logo, out GameObject backdrop, out Text title, out Text subtitle, UnityAction onClose, out RectTransform header)
    {
        var card = Window(owner, name, size, sortingOrder, out backdrop);
        var cardImage = card.GetComponent<Image>();
        cardImage.sprite = RoundedSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = TeakDark;

        float top = size.y * 0.5f;
        header = Panel("Header", card, Color.white, new Vector2(0f, top - 58f), new Vector2(size.x, 116f));
        header.GetComponent<Image>().sprite = WoodSprite; // wooden sign header
        var line = Rect("GoldLine", card, Gold);
        line.anchoredPosition = new Vector2(0f, top - 117f);
        line.sizeDelta = new Vector2(size.x, 4f);

        float left = -size.x * 0.5f;
        var badge = Panel("Logo", header, Gold, new Vector2(left + 70f, 0f), new Vector2(76f, 76f));
        Badge(badge, logo);
        title = Cartoon(Label(header, 40, "", new Vector2(left + 360f, 14f), new Vector2(520f, 54f), WoodInk, TextAnchor.MiddleLeft), 3f);
        subtitle = Label(header, 24, "", new Vector2(left + 360f, -26f), new Vector2(520f, 34f), new Color32(0xFF, 0xE2, 0xA8, 0xFF), TextAnchor.MiddleLeft);
        subtitle.gameObject.AddComponent<Outline>().effectColor = WoodEdge;
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
        label.font = ThaiFont.Display; // rounded Mitr on every button
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return button;
    }

    public static void FloatAbovePlayer(string text, Color color)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) FloatingText.Spawn(player.transform.position + Vector3.up * 2f, text, color);
    }
}
