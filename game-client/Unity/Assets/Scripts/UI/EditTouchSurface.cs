using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// An invisible touch layer for the edit modes (โหมดก่อสร้าง BuildUI, จัดร้าน CafeUI): put a finger on a
// piece and drag to move it, no walking needed. Grab is asked first: true when there is something under
// the finger to take; then Move follows the finger and Drop lets go (moved: whether it went anywhere).
// A quick touch that took nothing is a Tap. Anything else turns the camera the way CameraLookZone does
// (one finger orbits and tilts, two fingers pinch to zoom).
public class EditTouchSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Func<Vector2, bool> Grab;
    public Action<Vector2> Move;
    public Action<Vector2, bool> Drop;
    public Action<Vector2> Tap;

    private const float DragPixels = 14f, TapSeconds = 0.4f;
    // How far a finger on a piece has to go before the piece follows it (a steady hand shouldn't nudge it)
    public float HoldPixels = 14f;
    private bool holdMoved;
    private const float DegreesPerWidth = 270f, DegreesPerHeight = 120f;

    private readonly Dictionary<int, Vector2> fingers = new Dictionary<int, Vector2>();
    private int holdId = int.MinValue;
    private Vector2 downAt;
    private float downTime;
    private bool moved;
    private float pinchStart;
    private CameraFollow cameraFollow;

    public void OnPointerDown(PointerEventData e)
    {
        fingers[e.pointerId] = e.position;
        if (fingers.Count == 1)
        {
            downAt = e.position;
            downTime = Time.unscaledTime;
            moved = holdMoved = false;
            if (Grab != null && Grab(e.position)) holdId = e.pointerId;
        }
        else if (fingers.Count == 2 && holdId == int.MinValue) pinchStart = PinchDistance();
    }

    public void OnDrag(PointerEventData e)
    {
        fingers[e.pointerId] = e.position;
        if ((e.position - downAt).sqrMagnitude > DragPixels * DragPixels) moved = true;
        if (e.pointerId == holdId)
        {
            if ((e.position - downAt).sqrMagnitude > HoldPixels * HoldPixels) holdMoved = true;
            if (holdMoved) Move?.Invoke(e.position);
            return;
        }
        if (holdId != int.MinValue) return; // a second finger while carrying something: ignore it
        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (cameraFollow == null) return;
        if (fingers.Count >= 2)
        {
            float now = PinchDistance();
            if (pinchStart > 1f && now > 1f) cameraFollow.Zoom(pinchStart / now);
            pinchStart = now;
            return;
        }
        float wide = Mathf.Max(Screen.width, Screen.height), narrow = Mathf.Min(Screen.width, Screen.height);
        if (wide <= 0f) return;
        cameraFollow.Rotate(e.delta.x / wide * DegreesPerWidth);
        cameraFollow.Tilt(-e.delta.y / narrow * DegreesPerHeight);
    }

    public void OnPointerUp(PointerEventData e)
    {
        fingers.Remove(e.pointerId);
        if (e.pointerId == holdId)
        {
            holdId = int.MinValue;
            Drop?.Invoke(e.position, holdMoved);
            return;
        }
        if (fingers.Count == 0 && !moved && Time.unscaledTime - downTime < TapSeconds) Tap?.Invoke(e.position);
    }

    // Lost the finger (the screen closed under it): let go where it was
    private void OnDisable()
    {
        if (holdId != int.MinValue && fingers.TryGetValue(holdId, out var at)) Drop?.Invoke(at, holdMoved);
        holdId = int.MinValue;
        fingers.Clear();
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

    // Where a screen point lands on the flat ground at height y (false: it points at the sky)
    public static bool GroundPoint(Vector2 screen, float y, out Vector3 world)
    {
        world = default;
        var cam = Camera.main;
        if (cam == null) return false;
        var ray = cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
        if (!plane.Raycast(ray, out float d) || d > 200f) return false;
        world = ray.GetPoint(d);
        return true;
    }

    // Lay the touch layer over a canvas, leaving the joystick's corner (bottom left) free so you can still walk
    public static EditTouchSurface Make(Transform canvas)
    {
        var root = new GameObject("EditTouch", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        root.transform.SetAsFirstSibling(); // under the panel and its buttons
        var rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var surface = root.AddComponent<EditTouchSurface>();
        // two catchers: everything right of the joystick, and the strip above it
        Catcher(root.transform, new Vector2(0.3f, 0f), new Vector2(1f, 1f));
        Catcher(root.transform, new Vector2(0f, 0.5f), new Vector2(0.3f, 1f));
        return surface;
    }

    private static void Catcher(Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Catch", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f); // invisible, still takes touches
    }
}
