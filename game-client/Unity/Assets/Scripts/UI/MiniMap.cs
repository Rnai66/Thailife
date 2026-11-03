using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Round minimap (แผนที่) in the HUD's top-right corner: an orthographic camera high above the player
// renders the world, north up, into a small texture a few times a second; the arrow in the middle
// turns with the player. Tap it to zoom out and back in.
public class MiniMap : MonoBehaviour, IPointerClickHandler
{
    private const int Pixels = 256;
    private const float Near = 28f, Far = 60f, Height = 90f, Interval = 0.15f;

    private Transform target;
    private Camera cam;
    private RenderTexture texture;
    private RectTransform arrow;
    private Text zoomLabel;
    private bool zoomedOut;
    private float nextRender;

    public void Configure(Transform player) => target = player;

    private void Start()
    {
        var rt = (RectTransform)transform;
        float size = rt.sizeDelta.x;

        texture = new RenderTexture(Pixels, Pixels, 16) { name = "MiniMap", antiAliasing = 1 };
        var camGo = new GameObject("MiniMapCamera");
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Near;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.3f, 0.45f, 0.22f);
        cam.cullingMask = ~(1 << 5); // everything but UI
        cam.nearClipPlane = 1f;
        cam.farClipPlane = Height + 20f;
        cam.targetTexture = texture;
        cam.depth = -10f;
        cam.enabled = false;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderShadows = false;
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;
        camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Gold ring, the round map inside it, the player arrow on top
        var ring = GetComponent<Image>();
        ring.sprite = UiKit.CircleSprite;
        ring.color = UiKit.Gold;
        var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
        var mask = (RectTransform)maskGo.transform;
        mask.SetParent(transform, false);
        mask.sizeDelta = Vector2.one * (size - 10f);
        maskGo.GetComponent<Image>().sprite = UiKit.CircleSprite;
        maskGo.GetComponent<Image>().raycastTarget = false;
        maskGo.GetComponent<Mask>().showMaskGraphic = false;
        var mapGo = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
        var map = (RectTransform)mapGo.transform;
        map.SetParent(mask, false);
        map.sizeDelta = mask.sizeDelta;
        var raw = mapGo.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;

        arrow = new GameObject("Player", typeof(RectTransform)).GetComponent<RectTransform>();
        arrow.SetParent(transform, false);
        var dot = Piece(arrow, new Vector2(0f, 0f), new Vector2(22f, 22f), new Color(1f, 0.85f, 0.2f), UiKit.CircleSprite);
        dot.gameObject.AddComponent<Outline>().effectColor = UiKit.WoodEdge;
        var nose = Piece(arrow, new Vector2(0f, 14f), new Vector2(12f, 12f), new Color(1f, 0.85f, 0.2f), null);
        nose.localRotation = Quaternion.Euler(0f, 0f, 45f);
        nose.gameObject.AddComponent<Outline>().effectColor = UiKit.WoodEdge;

        UiKit.Cartoon(UiKit.Label(transform, 26, "N", new Vector2(0f, size * 0.5f - 22f), new Vector2(40f, 34f), UiKit.WoodInk), 2f);
        zoomLabel = UiKit.Cartoon(UiKit.Label(transform, 18, "", new Vector2(0f, -size * 0.5f + 20f), new Vector2(120f, 26f), UiKit.WoodInk), 2f);
    }

    private static RectTransform Piece(Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject("Piece", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return rt;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        zoomedOut = !zoomedOut;
        if (cam != null) cam.orthographicSize = zoomedOut ? Far : Near;
        nextRender = 0f;
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        cam.enabled = false; // renders for one frame at a time, a few times a second
        if (target == null) return;
        arrow.localRotation = Quaternion.Euler(0f, 0f, -target.eulerAngles.y);
        if (Time.unscaledTime < nextRender) return;
        nextRender = Time.unscaledTime + Interval;
        cam.transform.position = target.position + Vector3.up * Height;
        cam.enabled = true;
    }

    private void OnDestroy()
    {
        if (cam != null) Destroy(cam.gameObject);
        if (texture != null) texture.Release();
    }
}
