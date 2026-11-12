using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// หน้าโหลด: going to another map (village → town, the songthaew, the hospital...) shows a loading
// screen over everything. The new scene loads in the background, then the screen stays until that
// map is really ready — connected to the server with the player's life loaded and the world's objects
// set up — so you never land in a half-loaded town. If the server can't be reached it lets you in
// after a while anyway. Lives across scenes.
public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;

    private CanvasGroup group;
    private Text heading, tip, percent;
    private RectTransform fill, spinner;
    private CanvasScaler scaler;
    private int shape = -1; // 0 landscape, 1 portrait: the layout it was last fitted to
    private bool loading;

    private const float BarWidth = 900f;
    private const float MaxWaitOnline = 12f; // seconds to wait for the server after the map is up

    private static readonly string[] Tips =
    {
        "ทำภารกิจรายวันครบ รับโบนัสพิเศษ",
        "นอนพักที่บ้านเพื่อฟื้นพลังงาน",
        "ข้าวที่รดน้ำได้ผลผลิตเกือบสองเท่า",
        "พระเครื่องที่ห้อยอยู่ช่วยเสริมพลังได้ถึง 3 องค์",
        "กลางคืนระวังผี! หมอผีปราบผีได้ค่าครู",
        "ขึ้นรถสองแถวที่ป้ายเพื่อเดินทางไปที่อื่น",
        "เรียนที่โรงเรียนเพื่อเปิดอาชีพใหม่",
        "กดปุ่มออโต้ ให้ตัวละครทำภารกิจเอง",
    };

    public static bool Loading => instance != null && instance.loading;
    public static bool Entering => instance != null && instance.entering;

    private bool entering;
    private string pendingScene, pendingName; // a map asked for while entering the game: loaded behind the same screen

    // Leaving the title screen: the loading screen stays up until you're online, your life is loaded and
    // the game has decided where you start (HomeClient); if that's another map it loads behind the same screen
    public static void Enter()
    {
        if (instance == null)
        {
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
        }
        if (instance.loading) return;
        instance.StartCoroutine(instance.EnterRun());
    }

    private IEnumerator EnterRun()
    {
        loading = entering = true;
        pendingScene = null;
        group.gameObject.SetActive(true);
        heading.text = "กำลังเข้าสู่เกม…";
        tip.text = "เคล็ดลับ: " + Tips[Random.Range(0, Tips.Length)];
        Show(0f, null);
        float started = Time.unscaledTime;
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.2f; yield return null; }
        group.alpha = 1f;

        // online, life loaded and the start decided (0-60%)
        float waitFrom = Time.unscaledTime;
        while (Time.unscaledTime - waitFrom < MaxWaitOnline)
        {
            var life = LifeClient.Instance;
            bool online = life != null && life.IsOnline;
            if (online && (HomeClient.StartDecided || pendingScene != null)) break;
            float k = (Time.unscaledTime - waitFrom) / MaxWaitOnline;
            Show(0.6f * (1f - Mathf.Exp(-4f * k)), online ? "กำลังเตรียมบ้านของคุณ…" : "กำลังเชื่อมต่อ…");
            yield return null;
        }
        if (pendingScene != null)
        {
            heading.text = string.IsNullOrEmpty(pendingName) ? "กำลังโหลด…" : $"กำลังเดินทางไป{pendingName}…";
            var op = SceneManager.LoadSceneAsync(pendingScene);
            pendingScene = null;
            while (op != null && !op.isDone) { Show(0.6f + Mathf.Clamp01(op.progress / 0.9f) * 0.35f, null); yield return null; }
            for (int i = 0; i < 3; i++) yield return null;
            float w = Time.unscaledTime;
            while (Time.unscaledTime - w < MaxWaitOnline && !(LifeClient.Instance != null && LifeClient.Instance.IsOnline)) yield return null;
        }
        Show(1f, null);
        for (int i = 0; i < 3; i++) yield return null;
        while (Time.unscaledTime - started < 1.2f) yield return null;
        entering = false;
        yield return Hide();
    }

    // Use instead of SceneManager.LoadScene
    public static void Load(string scene, string placeName = null)
    {
        if (instance == null)
        {
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
        }
        if (instance.loading)
        {
            if (instance.entering) { instance.pendingScene = scene; instance.pendingName = placeName; }
            return;
        }
        instance.StartCoroutine(instance.Run(scene, placeName));
    }

    private void Awake()
    {
        var canvasGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // over everything, the title screen too
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 0.5f;
        this.scaler = scaler;
        scaler.referenceResolution = Screen.height > Screen.width ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
        group = canvasGo.GetComponent<CanvasGroup>();

        var back = UiKit.Rect("Back", canvasGo.transform, new Color(0.06f, 0.05f, 0.09f, 1f));
        back.anchorMin = Vector2.zero;
        back.anchorMax = Vector2.one;
        back.sizeDelta = Vector2.zero; // catches every tap while loading

        var art = UiKit.Art("LogoFull");
        if (art != null)
        {
            var logo = UiKit.Rect("Logo", back, Color.white);
            float hgt = 320f;
            logo.sizeDelta = new Vector2(hgt * art.rect.width / art.rect.height, hgt);
            logo.anchoredPosition = new Vector2(0f, 150f);
            var img = logo.GetComponent<Image>();
            img.sprite = art;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        heading = UiKit.Cartoon(UiKit.Label(back, 40, "", new Vector2(0f, -70f), new Vector2(1400f, 60f), UiKit.GoldText), 3f);
        var bar = UiKit.Panel("Bar", back, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, -140f), new Vector2(BarWidth + 8f, 26f));
        fill = UiKit.Panel("Fill", bar, UiKit.Gold, Vector2.zero, new Vector2(BarWidth, 18f));
        fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = new Vector2(4f, 0f);
        percent = UiKit.Label(back, 26, "", new Vector2(0f, -180f), new Vector2(400f, 40f), new Color(0.85f, 0.8f, 0.7f));
        tip = UiKit.Label(back, 28, "", new Vector2(0f, -250f), new Vector2(1400f, 50f), new Color(0.75f, 0.75f, 0.8f));

        // the loading mark: the turning flower, big, in the middle just above the logo
        spinner = PikulSpinner.Make(back, new Vector2(0f, 420f), 140f);

        group.alpha = 0f;
        canvasGo.SetActive(false);
    }

    // Phone held upright or on its side: fit the screen to it (the title screen may be the other way)
    private void Update()
    {
        if (scaler == null || !group.gameObject.activeSelf) return;
        int now = Screen.height > Screen.width ? 1 : 0;
        if (now == shape) return;
        shape = now;
        scaler.referenceResolution = now == 1 ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
        float w = now == 1 ? 1000f : 1400f;
        heading.rectTransform.sizeDelta = new Vector2(w, 60f);
        tip.rectTransform.sizeDelta = new Vector2(w, 90f);
        tip.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private void Show(float progress, string label)
    {
        fill.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(progress), 18f);
        percent.text = label ?? $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
    }

    private IEnumerator Run(string scene, string placeName)
    {
        loading = true;
        var canvas = group.gameObject;
        canvas.SetActive(true);
        heading.text = string.IsNullOrEmpty(placeName) ? "กำลังโหลด…" : $"กำลังเดินทางไป{placeName}…";
        tip.text = "เคล็ดลับ: " + Tips[Random.Range(0, Tips.Length)];
        Show(0f, null);
        float started = Time.unscaledTime;

        // fade in over the old map
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / 0.25f;
            yield return null;
        }
        group.alpha = 1f;

        // the map itself (0-70%)
        var op = SceneManager.LoadSceneAsync(scene);
        if (op == null) { yield return Hide(); yield break; }
        while (!op.isDone)
        {
            Show(Mathf.Clamp01(op.progress / 0.9f) * 0.7f, null);
            yield return null;
        }

        // let every object in it start up, then wait for the server and the player's life (70-100%)
        for (int i = 0; i < 3; i++) yield return null;
        float waitFrom = Time.unscaledTime;
        while (Time.unscaledTime - waitFrom < MaxWaitOnline)
        {
            var life = LifeClient.Instance;
            if (life != null && life.IsOnline) break;
            float k = (Time.unscaledTime - waitFrom) / MaxWaitOnline;
            Show(0.7f + 0.25f * (1f - Mathf.Exp(-3f * k)), "กำลังเชื่อมต่อ…");
            yield return null;
        }
        Show(1f, null);
        // a couple more frames so what the server sent is put on screen, and never a mere flash
        for (int i = 0; i < 3; i++) yield return null;
        while (Time.unscaledTime - started < 0.9f) yield return null;
        yield return Hide();
    }

    private IEnumerator Hide()
    {
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.35f;
            yield return null;
        }
        group.alpha = 0f;
        group.gameObject.SetActive(false);
        loading = false;
    }
}
