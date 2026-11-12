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
    private RectTransform buttonRect, statusRect;

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
        instance.buttonImage.color = highlight ? new Color(1f, 0.78f, 0.45f) : Color.white; // wooden sign, golden when urgent
        instance.root.SetActive(true);
    }

    public static void Hide(object who)
    {
        if (instance == null || instance.owner != who) return;
        instance.owner = null;
        instance.onPress = null;
        instance.root.SetActive(false);
    }

    // Someone else's button is up (don't take it over)
    public static bool TakenByOther(object who) => instance != null && instance.root.activeSelf && instance.owner != null && instance.owner != who;

    public static bool IsShownBy(object who) => instance != null && instance.owner == who && instance.root.activeSelf;

    private void Awake() => Build();

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        Layout();
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

        var font = ThaiFont.Font;

        // Same spot as the fishing button: above the action cluster. The slot is what ScreenFit pins to
        // the corner; the button moves inside it (out of the pedals' way while driving, Layout)
        var slot = new GameObject("ActionSlot", typeof(RectTransform));
        var slotRt = (RectTransform)slot.transform;
        slotRt.SetParent(canvasGo.transform, false);
        slotRt.anchorMin = slotRt.anchorMax = new Vector2(1f, 0f);
        slotRt.anchoredPosition = new Vector2(-300f, 650f); // above the action cluster
        slotRt.sizeDelta = new Vector2(300f, 160f);
        var buttonGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)buttonGo.transform;
        rt.SetParent(slotRt, false);
        rt.sizeDelta = new Vector2(300f, 160f);
        buttonRect = rt;
        buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.sprite = UiKit.WoodSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonGo.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
        buttonGo.AddComponent<PressScale>();
        button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => onPress?.Invoke());
        buttonLabel = UiKit.Cartoon(MakeText(rt, font, 40, new Vector2(270f, 140f)), 3f);
        buttonLabel.color = UiKit.WoodInk;

        var statusSlot = new GameObject("StatusSlot", typeof(RectTransform));
        var ssRt = (RectTransform)statusSlot.transform;
        ssRt.SetParent(canvasGo.transform, false);
        ssRt.anchorMin = ssRt.anchorMax = new Vector2(0.5f, 1f);
        ssRt.anchoredPosition = new Vector2(0f, -170f);
        ssRt.sizeDelta = new Vector2(1000f, 76f);
        var statusBg = new GameObject("Status", typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)statusBg.transform;
        srt.SetParent(ssRt, false);
        srt.sizeDelta = new Vector2(1000f, 76f);
        statusRect = srt;
        var bg = statusBg.GetComponent<Image>();
        bg.sprite = UiKit.WoodSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.95f);
        bg.raycastTarget = false;
        status = UiKit.Cartoon(MakeText(srt, font, 32, new Vector2(960f, 70f)), 2f);
        status.color = UiKit.WoodInk;

        root.SetActive(false);
    }

    // Driving: the button steps left of the pedals (DrivableCar's คันเร่ง/เบรก are where it usually is).
    // Upright phone: the message line is narrower and lower, clear of the menu column on the left and
    // the map and clock on the right.
    private void Layout()
    {
        if (buttonRect == null) return;
        bool driving = DrivableCar.Current != null;
        buttonRect.anchoredPosition = driving ? new Vector2(-400f, -10f) : Vector2.zero;
        bool portrait = ScreenFit.Portrait;
        statusRect.anchoredPosition = portrait ? new Vector2(0f, -150f) : Vector2.zero;
        statusRect.sizeDelta = new Vector2(portrait ? 820f : 1000f, 76f);
        status.rectTransform.sizeDelta = new Vector2(portrait ? 790f : 960f, 70f);
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
