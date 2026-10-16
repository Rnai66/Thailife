using UnityEngine;
using UnityEngine.UI;

// Local player's health bar (top-left) and a "KO!" banner. Builds its own canvas at runtime.
[RequireComponent(typeof(PlayerCombat))]
public class HealthHud : MonoBehaviour
{
    private PlayerCombat combat;
    private RectTransform fill;
    private Image fillImage;
    private Text label;
    private GameObject koBanner;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        Build();
    }

    private void OnEnable() => combat.HealthChanged += Refresh;
    private void OnDisable() => combat.HealthChanged -= Refresh;
    private void Start() => Refresh(combat.Health);

    private void Refresh(int health)
    {
        float ratio = combat.MaxHealth > 0 ? (float)health / combat.MaxHealth : 0f;
        fill.anchorMax = new Vector2(ratio, 1f);
        fillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.35f), ratio);
        label.text = $"HP {health}/{combat.MaxHealth}";
        koBanner.SetActive(combat.IsKnockedOut || health <= 0);
    }

    private void Build()
    {
        var canvasGo = new GameObject("HealthHud", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Bar background, inset from the top-left corner (clear of the notch in landscape)
        var background = CreateRect("HealthBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        background.anchorMin = background.anchorMax = new Vector2(0f, 1f);
        background.pivot = new Vector2(0f, 1f);
        background.anchoredPosition = new Vector2(120f, -50f);
        background.sizeDelta = new Vector2(520f, 44f);

        fill = CreateRect("Fill", background, Color.green);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        fillImage = fill.GetComponent<Image>();

        label = CreateText("Label", background, font, 28, TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        var ko = CreateText("KO", canvasGo.transform, font, 160, TextAnchor.MiddleCenter);
        ko.text = "KO!";
        ko.color = new Color(1f, 0.85f, 0.2f);
        ko.rectTransform.anchorMin = ko.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
        ko.rectTransform.sizeDelta = new Vector2(800f, 240f);
        koBanner = ko.gameObject;
        koBanner.SetActive(false);
    }

    private static RectTransform CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
