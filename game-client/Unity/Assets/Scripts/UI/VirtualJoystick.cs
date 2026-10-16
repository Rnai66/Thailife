using UnityEngine;
using UnityEngine.EventSystems;

// Floating joystick: put this on a full-height UI zone (left half of the screen).
// Touching anywhere in the zone moves the joystick there; dragging steers the player.
// Works with mouse in the Editor because uGUI treats the mouse as a pointer.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private PlayerController player;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float radius = 110f;
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
    [SerializeField] private float idleAlpha = 0.35f;

    private RectTransform zone;
    private CanvasGroup backgroundGroup;
    private Vector2 restPosition;
    private int activePointer = int.MinValue;

    public Vector2 Value { get; private set; }

    private void Awake()
    {
        zone = (RectTransform)transform;
        restPosition = background.anchoredPosition;

        backgroundGroup = background.GetComponent<CanvasGroup>();
        if (backgroundGroup == null) backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();
        backgroundGroup.blocksRaycasts = false;
        backgroundGroup.alpha = idleAlpha;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != int.MinValue) return; // already steered by another finger

        activePointer = eventData.pointerId;
        if (TryGetLocalPoint(eventData, out Vector2 local))
            background.anchoredPosition = local;

        backgroundGroup.alpha = 1f;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer || !TryGetLocalPoint(eventData, out Vector2 local)) return;

        Vector2 offset = Vector2.ClampMagnitude(local - background.anchoredPosition, radius);
        handle.anchoredPosition = offset;

        Vector2 value = offset / radius;
        SetValue(value.magnitude < deadZone ? Vector2.zero : value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        Release();
    }

    private void OnDisable() => Release();

    private void Release()
    {
        activePointer = int.MinValue;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (background != null) background.anchoredPosition = restPosition;
        if (backgroundGroup != null) backgroundGroup.alpha = idleAlpha;
        SetValue(Vector2.zero);
    }

    private void SetValue(Vector2 value)
    {
        Value = value;
        if (player != null) player.ExternalMoveInput = value;
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 local)
    {
        // Zone pivot is bottom-left, so the local point equals the child's anchoredPosition
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            zone, eventData.position, eventData.pressEventCamera, out local);
    }
}
