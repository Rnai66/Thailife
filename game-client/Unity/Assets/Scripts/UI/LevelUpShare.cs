using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// การ์ดเลเวลอัป to share: after the level-up burst, a card is painted (1080×1350, the shape Facebook
// and Instagram show best) — a shot of the player's world behind, "LEVEL UP!", the level in a gold
// medallion, the player's name and title, ดอกพิกุล, the game's name — and offered with a "แชร์"
// button. That opens the phone's own share sheet (Facebook, LINE, Messenger, Instagram, Save Image…,
// Plugins/iOS/ThaiShare.mm). Sharing it to an app (not just saving or copying) pays a reward once per
// level, a few times a day (server share_levelup): baht and XP.
public class LevelUpShare : MonoBehaviour
{
    private const int CardW = 1080, CardH = 1350;

    private static LevelUpShare instance;

    private int level;
    private string title;
    private Texture2D card;
    private string cardPath;
    private GameObject window;
    private Text status;
    private bool busy;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ThaiShare_Image(string path, string text, string receiver);
#endif

    public static int RewardBaht(int level) => 100 + 10 * level; // share.lua reward_for
    public static int RewardXp(int level) => 20 + level;

    // Called with the level-up burst; the card comes up once the burst has played
    public static void Offer(int newLevel, string newTitle)
    {
        if (newLevel < 2) return;
        if (instance == null)
        {
            var go = new GameObject("LevelUpShare"); // the name the native share sheet reports back to
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LevelUpShare>();
        }
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.ShowLater(newLevel, newTitle));
    }

    private IEnumerator ShowLater(int newLevel, string newTitle)
    {
        yield return new WaitForSecondsRealtime(3.6f);
        while (UiKit.AnyWindowOpen) yield return new WaitForSecondsRealtime(1f); // not over a shop or a class
        level = newLevel;
        title = newTitle;
        yield return new WaitForEndOfFrame();
        try { Paint(); }
        catch (Exception e) { Debug.LogWarning($"[LevelUpShare] card failed: {e.Message}"); yield break; }
        ShowWindow();
    }

    // ── The card picture ──────────────────────────────────────────────────────────────────────
    private void Paint()
    {
        // the world behind: the main camera rendered once into a portrait picture (no HUD)
        var world = new RenderTexture(CardW, CardH, 24, RenderTextureFormat.ARGB32);
        var cam = Camera.main;
        if (cam != null)
        {
            var before = cam.targetTexture;
            cam.targetTexture = world;
            cam.Render();
            cam.targetTexture = before;
        }

        var target = new RenderTexture(CardW, CardH, 24, RenderTextureFormat.ARGB32);
        var camGo = new GameObject("CardCamera");
        camGo.transform.position = new Vector3(0f, -5000f, 0f);
        var cardCam = camGo.AddComponent<Camera>();
        cardCam.clearFlags = CameraClearFlags.SolidColor;
        cardCam.backgroundColor = new Color(0.1f, 0.06f, 0.04f);
        cardCam.orthographic = true;
        cardCam.cullingMask = 1 << 5; // UI
        cardCam.targetTexture = target;
        cardCam.enabled = false;

        var canvasGo = new GameObject("CardCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.layer = 5;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cardCam;
        canvas.planeDistance = 1f;
        canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        BuildCard(canvasGo.transform);
        foreach (var t in canvasGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
        Canvas.ForceUpdateCanvases();

        // the world shot goes in as a RawImage under everything
        var bg = canvasGo.transform.Find("World");
        if (bg != null) bg.GetComponent<RawImage>().texture = world;
        Canvas.ForceUpdateCanvases();
        cardCam.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = target;
        if (card != null) Destroy(card);
        card = new Texture2D(CardW, CardH, TextureFormat.RGB24, false);
        card.ReadPixels(new Rect(0, 0, CardW, CardH), 0, 0);
        card.Apply();
        RenderTexture.active = previous;

        cardPath = Path.Combine(Application.persistentDataPath, $"ThaiLife9_Level{level}.png");
        File.WriteAllBytes(cardPath, card.EncodeToPNG());

        Destroy(canvasGo);
        Destroy(camGo);
        target.Release(); Destroy(target);
        world.Release(); Destroy(world);
    }

    private void BuildCard(Transform root)
    {
        var worldImg = new GameObject("World", typeof(RectTransform), typeof(RawImage));
        var w = (RectTransform)worldImg.transform;
        w.SetParent(root, false);
        Stretch(w);

        // darken top and bottom so the words read over any scene
        var shade = UiKit.Rect("Shade", root, Color.white);
        Stretch(shade);
        shade.GetComponent<Image>().sprite = ShadeSprite();

        // gold frame
        Frame(root, 0f, 22f, UiKit.Gold);
        Frame(root, 34f, 4f, new Color(1f, 0.9f, 0.6f, 0.9f));

        float cy = 120f;
        var glow = Img(root, PikulArt.Glow, new Color(1f, 0.85f, 0.45f, 0.75f), new Vector2(0f, cy), 900f);
        var rays = Img(root, PikulArt.Rays, new Color(1f, 0.9f, 0.55f, 0.55f), new Vector2(0f, cy), 1100f);
        rays.localRotation = Quaternion.Euler(0f, 0f, 12f);

        UiKit.Cartoon(UiKit.Label(root, 150, "LEVEL UP!", new Vector2(0f, 470f), new Vector2(1000f, 190f), UiKit.GoldText), 10f)
            .horizontalOverflow = HorizontalWrapMode.Overflow;

        // the medallion
        var outer = Img(root, UiKit.CircleSprite, new Color(0.95f, 0.72f, 0.25f), new Vector2(0f, cy), 440f);
        outer.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -12f);
        Img(root, UiKit.CircleSprite, new Color(0.62f, 0.38f, 0.08f), new Vector2(0f, cy), 392f);
        Img(root, UiKit.CircleSprite, new Color(0.98f, 0.82f, 0.38f), new Vector2(0f, cy), 372f);
        Img(root, UiKit.CircleSprite, new Color(0.45f, 0.12f, 0.08f), new Vector2(0f, cy), 330f);
        UiKit.Label(root, 70, "Lv", new Vector2(0f, cy + 95f), new Vector2(300f, 90f), UiKit.GoldText);
        UiKit.Cartoon(UiKit.Label(root, 190, level.ToString(), new Vector2(0f, cy - 25f), new Vector2(360f, 230f), Color.white), 6f)
            .horizontalOverflow = HorizontalWrapMode.Overflow;

        // ดอกพิกุล round the medallion
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f + 0.2f;
            float r = 300f + (i % 3) * 40f;
            var f = Img(root, PikulArt.Pikul, Color.white, new Vector2(Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r * 0.9f), 50f + (i % 4) * 16f);
            f.localRotation = Quaternion.Euler(0f, 0f, i * 37f);
        }

        string name = ProfileClient.Instance != null && !string.IsNullOrEmpty(ProfileClient.Instance.DisplayName) ? ProfileClient.Instance.DisplayName : "";
        if (name.Length > 0)
            UiKit.Cartoon(UiKit.Label(root, 76, name, new Vector2(0f, -240f), new Vector2(1000f, 100f), Color.white), 5f);
        if (!string.IsNullOrEmpty(title))
            UiKit.Cartoon(UiKit.Label(root, 54, title, new Vector2(0f, -330f), new Vector2(1000f, 80f), UiKit.GoldText), 4f);

        // footer
        var band = UiKit.Rect("Band", root, new Color(0.18f, 0.08f, 0.04f, 0.88f));
        band.anchorMin = new Vector2(0f, 0f);
        band.anchorMax = new Vector2(1f, 0f);
        band.pivot = new Vector2(0.5f, 0f);
        band.anchoredPosition = new Vector2(0f, 22f);
        band.sizeDelta = new Vector2(-44f, 170f);
        UiKit.Cartoon(UiKit.Label(band, 64, "ThaiLife9", new Vector2(0f, 30f), new Vector2(1000f, 80f), UiKit.GoldText), 4f);
        UiKit.Label(band, 34, "ใช้ชีวิตแบบไทยไปด้วยกัน · ดาวน์โหลดได้ที่ App Store", new Vector2(0f, -38f), new Vector2(1000f, 50f), UiKit.Cream);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Frame(Transform root, float inset, float width, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            var r = UiKit.Rect("Frame", root, color);
            bool horizontal = i < 2;
            r.anchorMin = horizontal ? new Vector2(0f, i == 0 ? 0f : 1f) : new Vector2(i == 2 ? 0f : 1f, 0f);
            r.anchorMax = horizontal ? new Vector2(1f, i == 0 ? 0f : 1f) : new Vector2(i == 2 ? 0f : 1f, 1f);
            r.pivot = horizontal ? new Vector2(0.5f, i == 0 ? 0f : 1f) : new Vector2(i == 2 ? 0f : 1f, 0.5f);
            r.anchoredPosition = horizontal ? new Vector2(0f, i == 0 ? inset : -inset) : new Vector2(i == 2 ? inset : -inset, 0f);
            r.sizeDelta = horizontal ? new Vector2(-2f * inset, width) : new Vector2(width, -2f * inset);
        }
    }

    private static RectTransform Img(Transform parent, Sprite sprite, Color color, Vector2 at, float size)
    {
        var r = UiKit.Rect("Img", parent, color);
        r.anchoredPosition = at;
        r.sizeDelta = Vector2.one * size;
        var image = r.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return r;
    }

    private static Sprite shade;
    private static Sprite ShadeSprite()
    {
        if (shade != null) return shade;
        const int h = 128;
        var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            float t = y / (h - 1f);
            float a = Mathf.Max(Mathf.Pow(1f - t, 2.2f) * 0.85f, Mathf.Pow(t, 2.5f) * 0.7f) + 0.15f;
            tex.SetPixel(0, y, new Color(0.08f, 0.03f, 0.02f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        return shade = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
    }

    // ── The window offering it ───────────────────────────────────────────────────────────────
    private void ShowWindow()
    {
        if (window != null) Destroy(window);
        var pane = UiKit.Window(transform, "LevelUpShareWindow", new Vector2(1180f, 860f), 300, out window);
        var preview = new GameObject("Card", typeof(RectTransform), typeof(RawImage));
        var p = (RectTransform)preview.transform;
        p.SetParent(pane, false);
        p.anchoredPosition = new Vector2(-300f, 0f);
        p.sizeDelta = new Vector2(540f, 675f);
        preview.GetComponent<RawImage>().texture = card;

        UiKit.Label(pane, 52, "การ์ดเลเวลอัป!", new Vector2(270f, 290f), new Vector2(560f, 80f), UiKit.Price);
        UiKit.Label(pane, 30, "แชร์ไปที่ Facebook, LINE หรือแอปอื่นๆ\nให้เพื่อนรู้ว่าคุณเก่งขึ้นแล้ว", new Vector2(270f, 180f), new Vector2(560f, 110f), UiKit.Ink);
        var reward = UiKit.Panel("Reward", pane, UiKit.OkTint, new Vector2(270f, 60f), new Vector2(540f, 100f));
        UiKit.Label(reward, 30, $"แชร์สำเร็จรับ ฿{RewardBaht(level)} + {RewardXp(level)} XP", Vector2.zero, new Vector2(520f, 90f), UiKit.OkGreen);
        UiKit.WoodButton(pane, "แชร์", new Vector2(270f, -70f), new Vector2(420f, 100f), Share, 38);
        UiKit.Button(pane, "ไว้ทีหลัง", new Vector2(270f, -200f), new Vector2(300f, 76f), new Color(0.55f, 0.5f, 0.45f), Close, 28);
        status = UiKit.Label(pane, 26, "", new Vector2(270f, -300f), new Vector2(560f, 70f), UiKit.Muted);
    }

    private void Close()
    {
        if (window != null) Destroy(window.transform.parent.gameObject); // the window's own canvas
        window = null;
    }

    private void Share()
    {
        if (busy || cardPath == null) return;
        string text = Lang.T($"เลเวลอัป! Lv {level} ใน ThaiLife9");
#if UNITY_IOS && !UNITY_EDITOR
        ThaiShare_Image(cardPath, text, gameObject.name);
#else
        Debug.Log($"[LevelUpShare] (editor) card saved at {cardPath}");
        OnShareDone("1|editor");
#endif
    }

    // From the native share sheet: "1|<activity type>" shared, "0|" closed
    public void OnShareDone(string message)
    {
        var parts = (message ?? "").Split('|');
        bool done = parts.Length > 0 && parts[0] == "1";
        string app = parts.Length > 1 ? parts[1] : "";
        if (!done) return;
        if (app.Contains("SaveToCameraRoll"))
        {
            if (status != null) status.text = "บันทึกรูปแล้ว · แชร์ไปยังแอปโซเชียลเพื่อรับรางวัล";
            return;
        }
        if (app.Contains("CopyToPasteboard") || app.Contains("Print") || app.Contains("AssignToContact")) return;
        Claim(app);
    }

    private async void Claim(string app)
    {
        busy = true;
        var conn = FindAnyObjectByType<NakamaConnection>();
        try
        {
            if (conn == null || conn.Session == null) throw new Exception("offline");
            var result = await conn.Client.RpcAsync(conn.Session, "share_levelup", JsonUtility.ToJson(new Request { level = level, app = app }));
            var r = JsonUtility.FromJson<Reward>(result.Payload);
            UiKit.FloatAbovePlayer($"แชร์สำเร็จ! +฿{r.baht} +{r.xp} XP", new Color(1f, 0.85f, 0.35f));
            GameAudio.Play("coins", null, 0.8f);
            CareerClient.Instance?.ApplyXp(r.level, r.xp_now, r.xp_to_next);
            Close();
        }
        catch (Exception e)
        {
            string m = e is ApiResponseException api ? api.Message : e.Message;
            string say = m.Contains("already") ? "รับรางวัลแชร์ของเลเวลนี้ไปแล้ว"
                : m.Contains("daily") ? "วันนี้รับรางวัลแชร์ครบแล้ว พรุ่งนี้มาใหม่นะ"
                : m.Contains("not your level") ? "การ์ดนี้เก่าแล้ว" : "รับรางวัลไม่สำเร็จ ลองใหม่อีกครั้ง";
            if (status != null) status.text = say;
            else UiKit.FloatAbovePlayer(say, new Color(1f, 0.7f, 0.5f));
        }
        busy = false;
    }

    [Serializable] private class Request { public int level; public string app; }
    [Serializable] private class Reward { public int baht; public int xp; public int level; public int xp_now; public int xp_to_next; }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (card != null) Destroy(card);
    }
}
