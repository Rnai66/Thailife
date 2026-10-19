using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Title screen (โลโก้ Thai Life Simulator) shown once when the game starts: the logo banner
// over the temple fair, "แตะเพื่อเริ่มเล่น", then it fades away. Art: Resources/Branding.
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
    private RectTransform banner, screenRect;
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

        // Banner fills the screen (cropped at the sides if needed), logo stays in view
        var banner = UiKit.Rect("Banner", bg, Color.white);
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 0.5f);
        var image = banner.GetComponent<Image>();
        image.sprite = UiKit.Art("TitleBanner");
        image.raycastTarget = false;
        this.banner = banner;
        screenRect = bg;
        aspect = image.sprite.rect.width / image.sprite.rect.height;

        var bar = UiKit.Panel("PromptBar", bg, new Color(0.24f, 0.14f, 0.08f, 0.85f), Vector2.zero, new Vector2(760f, 100f), shadow: true);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 110f);
        bar.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        prompt = UiKit.Label(bar, 40, "แตะเพื่อเริ่มเล่น", Vector2.zero, new Vector2(720f, 90f), UiKit.GoldText);
        prompt.fontStyle = FontStyle.Bold;

        var tap = bg.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => { if (!leaving) StartCoroutine(FadeOut()); });
    }

    // Cover the whole screen with the banner (cropping a little top/bottom or left/right)
    private void LateUpdate()
    {
        if (banner == null || screenRect == null) return;
        Vector2 p = screenRect.rect.size;
        if (p.x <= 0f || p.y <= 0f) return;
        banner.sizeDelta = p.x / p.y > aspect ? new Vector2(p.x, p.x / aspect) : new Vector2(p.y * aspect, p.y);
    }

    private void Update()
    {
        if (prompt != null && !leaving) prompt.color = Color.Lerp(UiKit.GoldText, Color.white, Mathf.PingPong(Time.unscaledTime, 1f));
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
