using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One on-screen action button in the PUBG-style cluster (ต่อย / เตะ / กระโดด / ป้องกัน): a round
// glass button with a white icon and a small label under it. Press = action; Block is held while
// the finger is down. The look is built at runtime from the icon name the scene stores.
public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private PlayerActions actions;
    [SerializeField] private ActionInput input;
    [SerializeField] private string icon;              // Resources/Branding/Icons/<icon>
    [SerializeField] private string label;
    [SerializeField] private Color accent = Color.white;
    [SerializeField] private float pressedScale = 0.88f;

    private static readonly Color Rest = new Color(0.08f, 0.06f, 0.05f, 0.42f);

    private Vector3 restScale;
    private Image face;
    private int activePointer = int.MinValue;

    private void Awake()
    {
        restScale = transform.localScale;
        face = GetComponent<Image>();
        Dress();
    }

    private void Dress()
    {
        if (face == null || string.IsNullOrEmpty(icon)) return;
        float size = ((RectTransform)transform).sizeDelta.x;
        face.sprite = UiKit.CircleSprite;
        face.color = Rest;
        var ring = face.gameObject.AddComponent<Outline>();
        ring.effectColor = new Color(accent.r, accent.g, accent.b, 0.85f);
        ring.effectDistance = new Vector2(3f, -3f);

        foreach (Transform old in transform) Destroy(old.gameObject); // labels from older scene builds

        var art = UiKit.Art("Icons/" + icon);
        if (art != null)
        {
            var glyph = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)glyph.transform;
            rt.SetParent(transform, false);
            rt.sizeDelta = Vector2.one * size * 0.58f;
            var image = glyph.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0.95f);
        }

        if (!string.IsNullOrEmpty(label))
        {
            var text = UiKit.Label(transform, 24, label, new Vector2(0f, -size * 0.5f - 18f), new Vector2(160f, 34f), Color.white);
            text.font = ThaiFont.Display;
            UiKit.Cartoon(text, 2f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != int.MinValue) return;
        activePointer = eventData.pointerId;

        transform.localScale = restScale * pressedScale;
        if (face != null && !string.IsNullOrEmpty(icon)) face.color = new Color(accent.r, accent.g, accent.b, 0.7f);
        if (actions != null) actions.Press(input);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        Release();
    }

    private void OnDisable() => Release();

    private void Release()
    {
        if (activePointer == int.MinValue) return;
        activePointer = int.MinValue;

        transform.localScale = restScale;
        if (face != null && !string.IsNullOrEmpty(icon)) face.color = Rest;
        if (actions != null) actions.Release(input);
    }
}
