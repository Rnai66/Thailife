using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Animated title screen (ThaiLife9) shown once when the game starts:
//   - the real 3D village behind, the camera flying slowly around it
//   - the ThaiLife9 logo pops in with a bounce, then floats, with a light sweep across it
//   - sparkles twinkle round the logo, frangipani flowers drift down
//   - the tagline fades in, the 15 place icons scroll along the bottom
//   - "แตะเพื่อเริ่มเล่น" pulses on a wood sign; tap and it all zooms away and gives the camera back
// Art: Resources/Branding/LogoFull (logo with transparent background) and Icons/*.
public class TitleScreen : MonoBehaviour
{
    private static bool shown;
    private static readonly string[] IconNames =
    {
        "fair", "school", "sao", "hotel", "koknongna", "boxing", "temple", "hospital",
        "farm", "market", "city", "fishing", "food", "travel", "community",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => shown = false; // domain reload is off: show again every Play

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ShowOnLaunch()
    {
        if (shown || UiKit.Art("LogoFull") == null) return;
        shown = true;
        var go = new GameObject("TitleScreen");
        DontDestroyOnLoad(go);
        go.AddComponent<TitleScreen>();
    }

    private class Sparkle { public RectTransform Rect; public Image Image; public float Phase, Speed; }
    private class Petal { public RectTransform Rect; public float Speed, Sway, Spin, Phase; }

    private CanvasGroup group;
    private RectTransform root, logo, shine, tagline, prompt, strip;
    private Image logoImage;
    private CanvasGroup taglineGroup, promptGroup, stripGroup;
    private readonly List<Sparkle> sparkles = new List<Sparkle>();
    private readonly List<Petal> petals = new List<Petal>();
    private readonly List<RectTransform> icons = new List<RectTransform>();
    private float start, stripOffset;
    private bool leaving;

    // Camera fly-around
    private Camera cam;
    private CameraFollow follow;
    private Vector3 orbitCenter;

    private const float IconSize = 132f, IconGap = 156f;
    // Laid out from the bottom up each frame (icon row, sign, tagline, then the logo in what's left)
    private float LogoY = 125f, TaglineY = -205f, PromptY = -298f, logoAspect = 1.4f;
    private RectTransform sparkleRoot;

    // The game's HUD and other windows stay hidden while the title is up
    private readonly List<Canvas> hidden = new List<Canvas>();
    private Canvas own;

    private void Awake()
    {
        start = Time.unscaledTime;
        var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        own = canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();

        // Full-screen tap catcher with a soft vignette over the 3D world
        root = UiKit.Rect("Root", canvasGo.transform, Color.white);
        Stretch(root);
        var vignette = root.GetComponent<Image>();
        vignette.sprite = MakeVignette();
        vignette.color = new Color(0.05f, 0.03f, 0.1f, 0.85f);
        var tap = root.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => { if (!leaving && Time.unscaledTime - start > 0.8f) StartCoroutine(Leave()); });

        BuildPetals();

        // Logo (masked so the light sweep only shows on the artwork)
        var art = UiKit.Art("LogoFull");
        logo = UiKit.Rect("Logo", root, Color.white);
        logoAspect = art.rect.width / art.rect.height;
        logo.anchoredPosition = new Vector2(0f, LogoY);
        logo.sizeDelta = new Vector2(560f * logoAspect, 560f);
        logoImage = logo.GetComponent<Image>();
        logoImage.sprite = art;
        logoImage.raycastTarget = false;
        logo.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        shine = UiKit.Rect("Shine", logo, new Color(1f, 1f, 0.9f, 0.45f));
        shine.sizeDelta = new Vector2(110f, 1400f);
        shine.localRotation = Quaternion.Euler(0f, 0f, -24f);
        shine.GetComponent<Image>().raycastTarget = false;
        logo.localScale = Vector3.zero;

        BuildSparkles();

        // Tagline under the logo
        tagline = UiKit.Rect("Tagline", root, new Color(0f, 0f, 0f, 0f));
        tagline.anchoredPosition = new Vector2(0f, TaglineY);
        tagline.sizeDelta = new Vector2(1200f, 70f);
        taglineGroup = tagline.gameObject.AddComponent<CanvasGroup>();
        var tag = UiKit.Cartoon(UiKit.Label(tagline, 44, "สร้างชีวิต • สร้างอนาคต • สไตล์ไทย", Vector2.zero, new Vector2(1200f, 70f), UiKit.GoldText), 3f);
        tag.horizontalOverflow = HorizontalWrapMode.Overflow;
        taglineGroup.alpha = 0f;

        // "แตะเพื่อเริ่มเล่น" on a wood sign
        prompt = UiKit.WoodPlate(root, new Vector2(0f, PromptY), new Vector2(620f, 100f));
        promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
        UiKit.Cartoon(UiKit.Label(prompt, 44, "แตะเพื่อเริ่มเล่น", Vector2.zero, new Vector2(600f, 96f), UiKit.WoodInk), 3f);
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;

        // Place icons scrolling along the bottom
        strip = UiKit.Rect("IconStrip", root, new Color(0f, 0f, 0f, 0f));
        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(1f, 0f);
        strip.pivot = new Vector2(0.5f, 0f);
        strip.sizeDelta = new Vector2(0f, IconSize + 30f);
        strip.anchoredPosition = new Vector2(0f, -IconSize - 40f);
        stripGroup = strip.gameObject.AddComponent<CanvasGroup>();
        stripGroup.blocksRaycasts = false;
        int copies = 2; // two rounds so the row never runs out while it scrolls
        for (int c = 0; c < copies; c++)
            foreach (var name in IconNames)
            {
                if (UiKit.Art("Icons/" + name) == null) continue;
                var tile = UiKit.IconTile(strip, name, Vector2.zero, new Vector2(IconSize, IconSize));
                tile.anchorMin = tile.anchorMax = new Vector2(0f, 0.5f);
                tile.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
                icons.Add(tile);
            }

        // Take the camera for the fly-around
        cam = Camera.main;
        if (cam != null)
        {
            follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;
            var player = FindAnyObjectByType<PlayerController>();
            orbitCenter = player != null ? player.transform.position : cam.transform.position + cam.transform.forward * 12f;
        }
        StartCoroutine(PopSound());
    }

    private IEnumerator PopSound()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        GameAudio.Play("bell", null, 0.5f);
    }

    private void Update()
    {
        float t = Time.unscaledTime - start;
        float dt = Time.unscaledDeltaTime;
        if (!leaving) HideOtherCanvases(); // the HUD and tutorial banner are made a moment after us
        Layout();

        // Logo: pop in (ease-out-back), then float and breathe
        if (!leaving)
        {
            float k = Mathf.Clamp01((t - 0.15f) / 0.75f);
            float pop = EaseOutBack(k);
            float breathe = 1f + Mathf.Sin(t * 2f) * 0.012f * k;
            logo.localScale = Vector3.one * pop * breathe;
            logo.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -10f + Mathf.Sin(t * 0.9f) * 0.8f * k);
            logo.anchoredPosition = new Vector2(0f, LogoY + Mathf.Sin(t * 1.4f) * 10f * k);

            taglineGroup.alpha = Mathf.Clamp01((t - 0.9f) / 0.5f);
            tagline.anchoredPosition = new Vector2(0f, TaglineY - (1f - taglineGroup.alpha) * 20f);
            promptGroup.alpha = Mathf.Clamp01((t - 1.3f) / 0.4f);
            prompt.anchoredPosition = new Vector2(0f, PromptY);
            prompt.localScale = Vector3.one * (1f + Mathf.Sin(t * 4f) * 0.04f * promptGroup.alpha);
            float slide = EaseOutCubic(Mathf.Clamp01((t - 1.0f) / 0.7f));
            strip.anchoredPosition = new Vector2(0f, Mathf.Lerp(-IconSize - 40f, 14f, slide));
        }

        // Light sweep across the logo every 3.5 s
        float sweep = Mathf.Repeat(t - 1.2f, 3.5f) / 1.1f;
        shine.anchoredPosition = new Vector2(Mathf.Lerp(-logo.sizeDelta.x * 0.7f, logo.sizeDelta.x * 0.7f, sweep), 0f);
        shine.gameObject.SetActive(t > 1.2f && sweep <= 1f);

        // Sparkles twinkle around the logo
        foreach (var s in sparkles)
        {
            float v = Mathf.Max(0f, Mathf.Sin(t * s.Speed + s.Phase));
            s.Rect.localScale = Vector3.one * v;
            s.Rect.localRotation = Quaternion.Euler(0f, 0f, t * 60f + s.Phase * 30f);
            s.Image.color = new Color(1f, 0.95f, 0.7f, v * Mathf.Clamp01(t - 0.8f));
        }

        // Flowers drift down and sway
        float h = root.rect.height, w = root.rect.width;
        foreach (var p in petals)
        {
            var pos = p.Rect.anchoredPosition;
            pos.y -= p.Speed * dt;
            pos.x += Mathf.Sin(t * p.Sway + p.Phase) * 30f * dt;
            if (pos.y < -h * 0.5f - 60f) pos = new Vector2(Random.Range(-w * 0.5f, w * 0.5f), h * 0.5f + 60f);
            p.Rect.anchoredPosition = pos;
            p.Rect.localRotation = Quaternion.Euler(0f, 0f, t * p.Spin + p.Phase * 50f);
        }

        // Icon row scrolls left forever
        stripOffset += 70f * dt;
        float loop = IconNames.Length * IconGap;
        for (int i = 0; i < icons.Count; i++)
        {
            float x = Mathf.Repeat(i * IconGap - stripOffset, loop * 2f) - IconGap;
            icons[i].anchoredPosition = new Vector2(x + IconSize * 0.5f, Mathf.Sin(t * 2f + i * 0.7f) * 5f);
        }

        // Camera flies slowly round the village
        if (cam != null && !leaving)
        {
            float a = t * 7f * Mathf.Deg2Rad + 0.6f;
            var pos = orbitCenter + new Vector3(Mathf.Sin(a) * 26f, 11f + Mathf.Sin(t * 0.3f) * 1.5f, Mathf.Cos(a) * 26f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, pos, Mathf.Clamp01(dt * 2f));
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(orbitCenter + Vector3.up * 2f - cam.transform.position), Mathf.Clamp01(dt * 3f));
        }
    }

    private IEnumerator Leave()
    {
        leaving = true;
        GameAudio.Play("coins", null, 0.4f);
        ShowOtherCanvases();
        if (follow != null) follow.enabled = true; // the follow camera glides back to the player
        Vector3 logoStart = logo.localScale;
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.6f;
            group.alpha = 1f - k;
            logo.localScale = logoStart * (1f + k * 0.25f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (follow != null) follow.enabled = true;
        ShowOtherCanvases();
    }

    private void HideOtherCanvases()
    {
        foreach (var c in FindObjectsByType<Canvas>())
            if (c != own && c.isRootCanvas && c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.enabled = false;
                hidden.Add(c);
            }
    }

    private void ShowOtherCanvases()
    {
        foreach (var c in hidden) if (c != null) c.enabled = true;
        hidden.Clear();
    }

    // ── Pieces ──────────────────────────────────────────────────────────────────
    // Positions for this screen height (canvas units, 0 = centre)
    private void Layout()
    {
        float h = root.rect.height;
        if (h <= 0f) return;
        float bottom = -h * 0.5f;
        float iconTop = 14f + (IconSize + 30f) * 0.5f + IconSize * 0.5f;
        PromptY = bottom + iconTop + 16f + 50f;
        TaglineY = PromptY + 50f + 14f + 35f;
        float space = h * 0.5f - 18f - (TaglineY + 35f + 6f);
        float logoH = Mathf.Clamp(space, 260f, 560f);
        LogoY = TaglineY + 35f + 6f + logoH * 0.5f;
        logo.sizeDelta = new Vector2(logoH * logoAspect, logoH);
        if (sparkleRoot != null)
        {
            sparkleRoot.anchoredPosition = new Vector2(0f, LogoY);
            sparkleRoot.localScale = Vector3.one * (logoH / 560f);
        }
    }

    private void BuildSparkles()
    {
        var star = MakeStar();
        sparkleRoot = UiKit.Rect("Sparkles", root, new Color(0f, 0f, 0f, 0f));
        sparkleRoot.GetComponent<Image>().raycastTarget = false;
        for (int i = 0; i < 16; i++)
        {
            var r = UiKit.Rect("Sparkle", sparkleRoot, Color.white);
            float ang = i / 16f * Mathf.PI * 2f;
            r.anchoredPosition = new Vector2(Mathf.Cos(ang) * Random.Range(380f, 520f), Mathf.Sin(ang) * Random.Range(200f, 300f));
            float size = Random.Range(28f, 60f);
            r.sizeDelta = new Vector2(size, size);
            var img = r.GetComponent<Image>();
            img.sprite = star;
            img.raycastTarget = false;
            sparkles.Add(new Sparkle { Rect = r, Image = img, Phase = Random.Range(0f, 6.3f), Speed = Random.Range(1.5f, 3f) });
        }
    }

    private void BuildPetals()
    {
        for (int i = 0; i < 18; i++)
        {
            var r = UiKit.Rect("Flower", root, i % 3 == 0 ? new Color(1f, 0.85f, 0.4f, 0.9f) : new Color(1f, 1f, 1f, 0.9f));
            r.anchoredPosition = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 700f));
            float size = Random.Range(26f, 52f);
            r.sizeDelta = new Vector2(size, size);
            var img = r.GetComponent<Image>();
            img.sprite = UiKit.FlowerSprite;
            img.raycastTarget = false;
            petals.Add(new Petal { Rect = r, Speed = Random.Range(40f, 90f), Sway = Random.Range(0.6f, 1.4f), Spin = Random.Range(-60f, 60f), Phase = Random.Range(0f, 6.3f) });
        }
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    // Dark edges, clear middle (so the 3D village shows through)
    private static Sprite MakeVignette()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n * 0.5f) / (n * 0.5f), dy = (y - n * 0.5f) / (n * 0.5f);
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.35f) / 0.85f)) * 0.9f + 0.12f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    // Four-point twinkle star with a soft glow
    private static Sprite MakeStar()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - n * 0.5f) / (n * 0.5f), dy = Mathf.Abs(y - n * 0.5f) / (n * 0.5f);
                float rays = Mathf.Max(Mathf.Clamp01(1f - dx * 6f) * Mathf.Clamp01(1f - dy), Mathf.Clamp01(1f - dy * 6f) * Mathf.Clamp01(1f - dx));
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(rays + glow * glow * 0.8f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
