using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Small helpers for the runtime-built windows (coop, contest, …): canvas, panels, labels, buttons.
public static class UiKit
{
    private static Font font;
    public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static readonly Color Paper = new Color(0.98f, 0.95f, 0.86f);
    public static readonly Color Brown = new Color(0.3f, 0.15f, 0.05f);
    public static readonly Color Green = new Color(0.1f, 0.45f, 0.2f);

    // Full-screen overlay canvas with a dimmed backdrop and a centred card; returns the card
    public static RectTransform Window(Transform owner, string name, Vector2 cardSize, int sortingOrder, out GameObject backdrop)
    {
        var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(owner, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = Rect("Backdrop", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f));
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        backdrop = dim.gameObject;

        var card = Rect("Card", dim, Paper);
        card.sizeDelta = cardSize;
        return card;
    }

    public static RectTransform Rect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static Text Label(Transform parent, int size, string text, Vector2 position, Vector2 box, Color color,
        TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var label = go.GetComponent<Text>();
        label.font = Font;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = size;
        return label;
    }

    public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, Color color, UnityAction onClick, int fontSize = 34)
    {
        var rt = Rect("Button", parent, color);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(onClick);
        Label(rt, fontSize, text, Vector2.zero, size - new Vector2(12f, 6f), new Color(0.2f, 0.1f, 0.05f));
        return button;
    }

    public static void Clear(Transform container)
    {
        foreach (Transform child in container) Object.Destroy(child.gameObject);
    }

    public static RectTransform Container(Transform parent, Vector2 position)
    {
        var rt = new GameObject("Container", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        return rt;
    }

    public static void FloatAbovePlayer(string text, Color color)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) FloatingText.Spawn(player.transform.position + Vector3.up * 2f, text, color);
    }
}
