using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// เลเวลอัป!: a golden moment over everything. A flash, sun rays turning behind a big "LEVEL UP!" that
// bounces in, the new level in a gold medallion, the new title under it, a burst of ดอกพิกุล and
// twinkles flying out and drifting down; round the player a ring of light at the feet and flowers
// spiralling up. A bright run of notes. Taps go through; gone by itself after a few seconds.
public class LevelUpFX : MonoBehaviour
{
    private const float Duration = 3.4f;

    private class Bit
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public float spin, life, age, size;
    }

    private readonly List<Bit> bits = new List<Bit>();
    private RectTransform rays, rays2, banner, medal, flash;
    private CanvasGroup group;
    private Text title;
    private float age;

    // level: the new level; caption: what levelled up (e.g. a career's title, "นักดนตรี")
    private static float lastShown = -10f;

    public static void Show(int level, string caption = null)
    {
        if (Time.unscaledTime - lastShown < 2.5f) return; // two systems reporting the same level-up
        lastShown = Time.unscaledTime;
        var go = new GameObject("LevelUpFX");
        DontDestroyOnLoad(go);
        go.AddComponent<LevelUpFX>().Build(level, caption);
        GameAudio.Play("level_up", null, 0.9f);
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) LevelUpAura.Burst(player.transform);
    }

    private void Build(int level, string caption)
    {
        var canvasGo = new GameObject("LevelUpCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiKit.Portrait ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var root = canvasGo.transform;
        float y = UiKit.Portrait ? 260f : 120f;

        flash = Img(root, "Flash", PikulArt.Glow, new Color(1f, 0.95f, 0.75f, 0.9f), new Vector2(0f, y), 2600f);
        rays = Img(root, "Rays", PikulArt.Rays, new Color(1f, 0.85f, 0.35f, 0.55f), new Vector2(0f, y), 1100f);
        rays2 = Img(root, "Rays2", PikulArt.Rays, new Color(1f, 0.97f, 0.8f, 0.35f), new Vector2(0f, y), 800f);
        Img(root, "Glow", PikulArt.Glow, new Color(1f, 0.75f, 0.25f, 0.7f), new Vector2(0f, y), 700f);

        // the medallion with the new level
        medal = Img(root, "Medal", UiKit.CircleSprite, new Color(0.62f, 0.38f, 0.08f), new Vector2(0f, y + 30f), 250f);
        var face = Img(medal, "Face", UiKit.CircleSprite, new Color(1f, 0.8f, 0.25f), Vector2.zero, 226f);
        Img(face, "Shine", PikulArt.Glow, new Color(1f, 1f, 0.85f, 0.7f), new Vector2(-30f, 40f), 150f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            var f = Img(medal, "Pikul", PikulArt.Pikul, Color.white, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 128f, 46f);
            f.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
        }
        UiKit.Label(face, 30, "เลเวล", new Vector2(0f, 52f), new Vector2(200f, 40f), new Color(0.45f, 0.25f, 0.05f)).font = ThaiFont.Display;
        UiKit.Cartoon(UiKit.Label(face, 110, level.ToString(), new Vector2(0f, -14f), new Vector2(220f, 130f), Color.white), 4f);

        // LEVEL UP! on a ribbon below it
        banner = UiKit.Rect("Banner", root, new Color(0f, 0f, 0f, 0f));
        banner.anchoredPosition = new Vector2(0f, y - 150f);
        banner.sizeDelta = new Vector2(760f, 120f);
        var ribbon = UiKit.Panel("Ribbon", banner, new Color(0.75f, 0.12f, 0.1f), Vector2.zero, new Vector2(700f, 104f), shadow: true);
        ribbon.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.82f, 0.3f);
        foreach (float s in new[] { -1f, 1f })
        {
            var tail = UiKit.Panel("Tail", banner, new Color(0.55f, 0.08f, 0.07f), new Vector2(s * 380f, -18f), new Vector2(110f, 80f));
            tail.SetAsFirstSibling();
        }
        var head = UiKit.Cartoon(UiKit.Label(ribbon, 76, "LEVEL UP!", new Vector2(0f, 2f), new Vector2(680f, 100f), new Color(1f, 0.9f, 0.45f)), 4f);
        head.GetComponent<Outline>().effectColor = new Color(0.35f, 0.05f, 0.03f);
        title = UiKit.Cartoon(UiKit.Label(root, 40, string.IsNullOrEmpty(caption) ? "เก่งขึ้นอีกขั้นแล้ว!" : caption, new Vector2(0f, y - 250f), new Vector2(1000f, 60f), Color.white), 3f);

        // the burst: flowers and twinkles out from the medallion
        for (int i = 0; i < 46; i++)
        {
            bool flower = i % 3 != 0;
            float a = Random.Range(0f, Mathf.PI * 2f), v = Random.Range(380f, 1100f);
            var size = flower ? Random.Range(34f, 70f) : Random.Range(22f, 46f);
            var color = flower ? Color.white : (i % 2 == 0 ? new Color(1f, 0.92f, 0.5f) : new Color(1f, 1f, 1f));
            var r = Img(root, "Bit", flower ? PikulArt.Pikul : PikulArt.Spark, color, new Vector2(0f, y + 30f), size);
            bits.Add(new Bit
            {
                rect = r, image = r.GetComponent<Image>(), size = size,
                velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.85f + 0.35f) * v,
                spin = Random.Range(-260f, 260f), life = Random.Range(1.6f, 2.8f),
            });
        }
        Animate(0f);
    }

    private static RectTransform Img(Transform parent, string name, Sprite sprite, Color color, Vector2 at, float size)
    {
        var r = UiKit.Rect(name, parent, color);
        r.anchoredPosition = at;
        r.sizeDelta = new Vector2(size, size);
        var img = r.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return r;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.9f, c3 = c1 + 1f;
        x = Mathf.Clamp01(x) - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;
        Animate(Time.unscaledDeltaTime);
        if (age >= Duration) Destroy(gameObject);
    }

    private void Animate(float dt)
    {
        float t = age;
        flash.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.2f, t / 0.35f);
        flash.GetComponent<Image>().color = new Color(1f, 0.95f, 0.75f, 0.9f * Mathf.Clamp01(1f - t / 0.45f));
        rays.localRotation = Quaternion.Euler(0f, 0f, -t * 22f);
        rays2.localRotation = Quaternion.Euler(0f, 0f, t * 30f);
        float pop = EaseOutBack(t / 0.5f);
        rays.localScale = rays2.localScale = Vector3.one * Mathf.Max(0.01f, pop);
        medal.localScale = Vector3.one * Mathf.Max(0.01f, EaseOutBack((t - 0.05f) / 0.55f));
        medal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 4f);
        float b = EaseOutBack((t - 0.25f) / 0.45f);
        banner.localScale = new Vector3(Mathf.Max(0.01f, b), Mathf.Max(0.01f, b), 1f);
        title.color = new Color(1f, 1f, 1f, Mathf.Clamp01((t - 0.6f) / 0.3f));
        group.alpha = Mathf.Clamp01((Duration - t) / 0.5f);

        foreach (var bit in bits)
        {
            bit.age += dt;
            bit.velocity *= Mathf.Pow(0.12f, dt);            // air drag: they burst out then float
            bit.velocity += Vector2.down * 380f * dt;          // and drift down
            bit.rect.anchoredPosition += bit.velocity * dt + new Vector2(Mathf.Sin((bit.age + bit.size) * 3f) * 40f * dt, 0f);
            bit.rect.Rotate(0f, 0f, bit.spin * dt);
            var c = bit.image.color;
            c.a = Mathf.Clamp01((bit.life - bit.age) / 0.6f);
            bit.image.color = c;
        }
    }
}
