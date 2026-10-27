using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Touch zone on the right half of the screen for the camera:
//   one finger — drag left/right to orbit, up/down to tilt
//   two fingers — pinch to zoom in/out
public class CameraLookZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private CameraFollow cameraFollow;

    [Tooltip("Degrees of rotation for a drag across the full screen width")]
    [SerializeField] private float degreesPerScreenWidth = 270f;

    [Tooltip("Degrees of tilt for a drag across the full screen height")]
    [SerializeField] private float degreesPerScreenHeight = 120f;

    private readonly Dictionary<int, Vector2> fingers = new Dictionary<int, Vector2>();
    private float pinchStart;

    public void OnPointerDown(PointerEventData eventData)
    {
        fingers[eventData.pointerId] = eventData.position;
        if (fingers.Count == 2) pinchStart = PinchDistance();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        fingers.Remove(eventData.pointerId);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (cameraFollow == null || Screen.width <= 0) return;
        fingers[eventData.pointerId] = eventData.position;

        if (fingers.Count >= 2)
        {
            float now = PinchDistance();
            if (pinchStart > 1f && now > 1f) cameraFollow.Zoom(pinchStart / now); // fingers apart = zoom in
            pinchStart = now;
            return;
        }

        // Measured against the long and short sides so a drag turns the same amount upright or sideways
        float wide = Mathf.Max(Screen.width, Screen.height), narrow = Mathf.Min(Screen.width, Screen.height);
        cameraFollow.Rotate(eventData.delta.x / wide * degreesPerScreenWidth);
        cameraFollow.Tilt(-eventData.delta.y / narrow * degreesPerScreenHeight); // drag up = look from lower
    }

    private float PinchDistance()
    {
        Vector2 a = default, b = default;
        int i = 0;
        foreach (var p in fingers.Values)
        {
            if (i == 0) a = p; else if (i == 1) b = p;
            if (++i == 2) break;
        }
        return Vector2.Distance(a, b);
    }

    private void OnDisable() => fingers.Clear();
}
