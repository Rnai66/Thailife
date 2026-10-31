using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Reports every press inside a UI rect (or any child without its own handler) in the rect's local
// units: the skill games hit-test their moving targets themselves.
public class TapArea : MonoBehaviour, IPointerDownHandler
{
    public event Action<Vector2> Tapped;

    public void OnPointerDown(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local))
            Tapped?.Invoke(local);
    }
}
