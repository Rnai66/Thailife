using UnityEngine;

// Third-person camera that follows the player. Orbit left/right (yaw), tilt up/down (pitch) and
// zoom in/out (distance) — driven by CameraLookZone (drag / pinch on the right half of the screen)
// and the mouse wheel in the Editor.
// It starts wide and high (the whole scene round you, DefaultDistance / DefaultPitch, a wide lens) and,
// while you walk or ride and haven't touched the camera for a moment, swings round gently behind the
// way you're going, so you don't have to turn it by hand (walking back towards it doesn't flip it).
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f); // starting view; sets distance and pitch
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookHeight = 1.2f;

    public const float MinDistance = 3f, MaxDistance = 16f;
    public const float MinPitch = 5f, MaxPitch = 75f;
    public const float DefaultDistance = 10.5f, DefaultPitch = 30f;
    private const float MinFov = 66f;                  // a wide lens, landscape
    private const float FollowAfter = 1.5f;            // seconds after you last turned the camera yourself
    private const float SwingSpeed = 75f;              // degrees a second at a run

    private float lastManual = -100f;
    private Vector3 lastTargetPos;

    private float yaw, pitch, distance;
    private bool ready;
    private Camera cam;
    private float landscapeFov;

    // Upright the view is narrow: open the lens and step back a little so the player still sees around them
    private const float PortraitFov = 76f, PortraitPullBack = 1.1f;

    public void Rotate(float deltaDegrees)
    {
        lastManual = Time.unscaledTime;
        yaw = Mathf.Repeat(yaw + deltaDegrees, 360f);
    }

    public void Tilt(float deltaDegrees)
    {
        Init();
        lastManual = Time.unscaledTime;
        pitch = Mathf.Clamp(pitch + deltaDegrees, MinPitch, MaxPitch);
    }

    // factor < 1 zooms in, > 1 zooms out
    public void Zoom(float factor)
    {
        Init();
        distance = Mathf.Clamp(distance * factor, MinDistance, MaxDistance);
    }

    private void Init()
    {
        if (ready) return;
        ready = true;
        // (the scenes' saved offset is the old close view: start from the wide one)
        distance = Mathf.Clamp(Mathf.Max(offset.magnitude, DefaultDistance), MinDistance, MaxDistance);
        pitch = Mathf.Clamp(DefaultPitch, MinPitch, MaxPitch);
        if (target != null) { yaw = target.eulerAngles.y; lastTargetPos = target.position; }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Init();
        bool portrait = ScreenFit.Portrait;
        if (cam == null && TryGetComponent(out cam)) landscapeFov = Mathf.Max(cam.fieldOfView, MinFov);
        if (cam != null) cam.fieldOfView = portrait ? Mathf.Max(landscapeFov, PortraitFov) : landscapeFov;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll > 0f ? 0.9f : 1.1f);
        }
#endif

        FollowHeading();
        Vector3 focus = target.position + Vector3.up * lookHeight;
        Vector3 desiredPosition = focus + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance * (portrait ? PortraitPullBack : 1f));

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(focus);
    }

    // Swing round behind the way the player is going (not while you turn it yourself, nor when they
    // walk back towards the camera)
    private void FollowHeading()
    {
        float dt = Time.deltaTime;
        Vector3 moved = target.position - lastTargetPos;
        lastTargetPos = target.position;
        moved.y = 0f;
        if (dt <= 0f || moved.sqrMagnitude > 25f) return; // a teleport
        float speed = moved.magnitude / dt;
        if (speed < 0.8f || Time.unscaledTime - lastManual < FollowAfter) return;
        float heading = Mathf.Atan2(moved.x, moved.z) * Mathf.Rad2Deg;
        float diff = Mathf.Abs(Mathf.DeltaAngle(yaw, heading));
        if (diff > 135f) return;
        yaw = Mathf.MoveTowardsAngle(yaw, heading, SwingSpeed * Mathf.Clamp01(speed / 5f) * Mathf.Clamp01(diff / 20f + 0.2f) * dt);
    }
}
