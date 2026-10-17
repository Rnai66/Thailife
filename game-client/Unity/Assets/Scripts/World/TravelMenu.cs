using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Travel map shown at a songthaew stop: destinations grouped by หมู่บ้าน / ตำบล / อำเภอ / จังหวัด.
// Built at runtime; one instance per scene.
public class TravelMenu : MonoBehaviour
{
    private static TravelMenu instance;

    private GameObject panel;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("TravelMenu").AddComponent<TravelMenu>();
        instance.panel.SetActive(true);
    }

    public static void Hide()
    {
        if (instance != null) instance.panel.SetActive(false);
    }

    private void Awake()
    {
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            Debug.LogWarning("[Travel] No EventSystem in the scene — run Thai Life > Repair Scene.");

        var canvasGo = new GameObject("TravelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the mobile controls
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var font = ThaiFont.Font;

        // Dim full-screen backdrop (also blocks the joystick while the map is open)
        panel = CreateImage("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f)).gameObject;
        var backdrop = (RectTransform)panel.transform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;

        var card = CreateImage("Card", panel.transform, new Color(0.98f, 0.95f, 0.86f));
        card.sizeDelta = new Vector2(980f, 820f);

        CreateText("Title", card, font, 54, "ป้ายรถสองแถว — ไปไหนดี?", new Vector2(0f, 340f), new Vector2(900f, 80f), new Color(0.3f, 0.15f, 0.05f));

        var current = Locations.Current;
        float y = 230f;
        foreach (var location in Locations.All)
        {
            bool here = current != null && location.Id == current.Id;
            string label = $"{Locations.LevelNameTh(location.Level)} · {location.NameTh}" + (here ? "  (อยู่ที่นี่)" : "");
            var target = location;
            CreateButton(card, font, label, new Vector2(0f, y), location.Available && !here,
                here ? new Color(0.75f, 0.85f, 0.75f) : location.Available ? new Color(1f, 0.8f, 0.3f) : new Color(0.8f, 0.8f, 0.8f),
                () => Locations.TravelTo(target));
            y -= 120f;
        }

        CreateButton(card, font, "ปิด", new Vector2(0f, -330f), true, new Color(0.9f, 0.45f, 0.4f), Hide, width: 300f);
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, string text, Vector2 position, Vector2 boxSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = boxSize;
        var label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true; // long place names shrink instead of clipping
        label.resizeTextMinSize = 20;
        label.resizeTextMaxSize = size;
        return label;
    }

    private static void CreateButton(RectTransform parent, Font font, string text, Vector2 position, bool interactable,
        Color color, UnityEngine.Events.UnityAction onClick, float width = 860f)
    {
        var rt = CreateImage("Button", parent, color);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 100f);

        var button = rt.gameObject.AddComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(onClick);

        CreateText("Label", rt, font, 40, text, Vector2.zero, new Vector2(width - 40f, 100f),
            interactable ? new Color(0.2f, 0.1f, 0.05f) : new Color(0.35f, 0.35f, 0.35f));
    }
}
