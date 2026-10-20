using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Title screen (ThaiLife9) shown once when the game starts: the whole banner fitted to the screen
// width over a darkened, screen-filling copy of itself, "แตะเพื่อเริ่มเล่น" on a wood sign in the
// space below, then it fades away. Art: Resources/Branding/TitleBanner.
public class TitleScreen : MonoBehaviour
{
    private static bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => shown = false; // domain reload is off: show again every Play

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ShowOnLaunch()
    {
        if (shown || UiKit.Art("TitleBanner") == null) return;
        shown = true;
        var go = new GameObject("TitleScreen");
        DontDestroyOnLoad(go);
        go.AddComponent<TitleScreen>();
    }

    private CanvasGroup group;
    private RectTransform banner, backdrop, promptBar, screenRect;
    private float aspect = 1.72f;
    private Text prompt;
    private bool leaving;

    private void Awake()
    {
        var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();

        var bg = UiKit.Rect("Background", canvasGo.transform, new Color(0.98f, 0.96f, 0.9f));
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;

        var art = UiKit.Art("TitleBanner");
        aspect = art.rect.width / art.rect.height;
        screenRect = bg;

        // Behind: the banner enlarged to cover the screen and darkened, so there are no empty bands
        backdrop = UiKit.Rect("Backdrop", bg, new Color(0.45f, 0.42f, 0.4f));
        backdrop.anchorMin = backdrop.anchorMax = new Vector2(0.5f, 0.5f);
        var back = backdrop.GetComponent<Image>();
        back.sprite = art;
        back.raycastTarget = false;

        // In front: the whole banner, fitted to the width so the logo and its tagline are never cut
        banner = UiKit.Rect("Banner", bg, Color.white);
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
        banner.pivot = new Vector2(0.5f, 1f);
        var image = banner.GetComponent<Image>();
        image.sprite = art;
        image.raycastTarget = false;
        banner.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -8f);

        // "แตะเพื่อเริ่มเล่น" on a wood sign in the space under the banner
        promptBar = UiKit.WoodPlate(bg, Vector2.zero, new Vector2(700f, 110f));
        promptBar.anchorMin = promptBar.anchorMax = new Vector2(0.5f, 0f);
        prompt = UiKit.Cartoon(UiKit.Label(promptBar, 46, "แตะเพื่อเริ่มเล่น", Vector2.zero, new Vector2(680f, 100f), UiKit.WoodInk), 3f);

        var tap = bg.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => { if (!leaving) StartCoroutine(FadeOut()); });
    }

    // Lay out for this screen: backdrop covers it, banner fits (top-aligned), prompt centred in what's left
    private void LateUpdate()
    {
        if (banner == null || screenRect == null) return;
        Vector2 p = screenRect.rect.size;
        if (p.x <= 0f || p.y <= 0f) return;
        backdrop.sizeDelta = p.x / p.y > aspect ? new Vector2(p.x, p.x / aspect) : new Vector2(p.y * aspect, p.y);

        const float promptSpace = 150f; // always leave room for the sign under the banner
        float w = Mathf.Min(p.x, (p.y - promptSpace) * aspect);
        banner.sizeDelta = new Vector2(w, w / aspect);
        float free = p.y - w / aspect;
        banner.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, free - promptSpace) * 0.25f);
        float bannerBottom = p.y + banner.anchoredPosition.y - w / aspect;
        promptBar.anchoredPosition = new Vector2(0f, Mathf.Max(70f, bannerBottom * 0.5f));
    }

    private void Update()
    {
        if (prompt != null && !leaving) prompt.color = Color.Lerp(UiKit.WoodInk, UiKit.GoldText, Mathf.PingPong(Time.unscaledTime, 1f));
    }

    private IEnumerator FadeOut()
    {
        leaving = true;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.5f;
            yield return null;
        }
        Destroy(gameObject);
    }
}
