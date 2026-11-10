using UnityEngine;
using UnityEngine.EventSystems;

// A button you hold down (the throttle and brake when driving, DrivableCar): Held while a finger or the
// mouse is on it; it shrinks a little while held so you can feel it's pressed.
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool Held { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => Set(true);
    public void OnPointerUp(PointerEventData eventData) => Set(false);
    public void OnPointerExit(PointerEventData eventData) { if (!eventData.dragging) Set(false); }

    private void OnDisable() => Set(false);

    private void Set(bool held)
    {
        Held = held;
        transform.localScale = held ? Vector3.one * 0.92f : Vector3.one;
    }
}
