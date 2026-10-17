using UnityEngine;
using UnityEngine.EventSystems;

// Buttons dip slightly while pressed — small touch feedback that makes the UI feel responsive.
public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressed = 0.94f;

    public void OnPointerDown(PointerEventData eventData) => transform.localScale = Vector3.one * pressed;
    public void OnPointerUp(PointerEventData eventData) => transform.localScale = Vector3.one;
    public void OnPointerExit(PointerEventData eventData) => transform.localScale = Vector3.one;
    private void OnDisable() => transform.localScale = Vector3.one;
}
