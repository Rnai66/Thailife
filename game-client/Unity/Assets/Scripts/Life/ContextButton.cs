using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One big on-screen action button (+ key E) offered by whatever the player is standing at:
// a rice plot, the sleeping mat, … The latest owner wins; Hide(owner) only hides your own prompt.
public class ContextButton : MonoBehaviour
{
    private static ContextButton instance;

    private object owner;
    private Action onPress;
    private GameObject root;
    private Button button;
    private Image buttonImage;
    private Text buttonLabel;
    private Text status;

    public static void Show(object who, string label, string message, Action action, bool enabled = true, bool highlight = false)
    {
        if (instance == null)
            instance = new GameObject("ContextButton").AddComponent<ContextButton>();
        instance.owner = who;
        instance.onPress = action;
        instance.buttonLabel.text = label;
        instance.status.text = message ?? "";
        instance.status.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(message));
        instance.button.interactable = enabled;
        instance.buttonImage.color = highlight ? new Color(1f, 0.55f, 0.2f, 0.95f) : new Color(0.25f, 0.65f, 0.35f, 0.9f);
        instance.root.SetActive(true);
    }

    public static void Hide(object who)
    {
        if (instance == null || instance.owner != who) return;
        instance.owner = null;
        instance.onPress = null;
        instance.root.SetActive(false);
    }

    public static bool IsShownBy(object who) => instance != null && instance.owner == who && instance.root.activeSelf;

    private void Awake() => Build();

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (owner == null || !button.interactable) return;
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        bool pressed = Input.GetKeyDown(KeyCode.E);
#else
        bool pressed = false;
#endif
        if (pressed) onPress?.Invoke();
    }

    private void Build()
    {
        var canvasGo = new GameObject("ContextCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Same spot as the fishing button: above the A/B/C/D diamond
        var buttonGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)buttonGo.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-300f, 600f);
        rt.sizeDelta = new Vector2(300f, 160f);
        buttonImage = buttonGo.GetComponent<Image>();
        button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => onPress?.Invoke());
        buttonLabel = MakeText(rt, font, 40, new Vector2(280f, 150f));

        var statusBg = new GameObject("Status", typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)statusBg.transform;
        srt.SetParent(canvasGo.transform, false);
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -170f);
        srt.sizeDelta = new Vector2(1000f, 76f);
        var bg = statusBg.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;
        status = MakeText(srt, font, 34, new Vector2(980f, 76f));

        root.SetActive(false);
    }

    private static Text MakeText(Transform parent, Font font, int size, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = box;
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
        return text;
    }
}
