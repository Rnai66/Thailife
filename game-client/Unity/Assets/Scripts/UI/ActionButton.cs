using UnityEngine;
using UnityEngine.EventSystems;

// One on-screen action button (A/B/C/D). Press = action; Block is held while the finger is down.
public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private PlayerActions actions;
    [SerializeField] private ActionInput input;
    [SerializeField] private float pressedScale = 0.88f;

    private Vector3 restScale;
    private int activePointer = int.MinValue;

    private void Awake()
    {
        restScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != int.MinValue) return;
        activePointer = eventData.pointerId;

        transform.localScale = restScale * pressedScale;
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
        if (actions != null) actions.Release(input);
    }
}
