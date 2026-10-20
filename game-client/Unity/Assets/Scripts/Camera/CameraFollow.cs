using UnityEngine;

// Third-person camera that follows the player. Orbit left/right (yaw), tilt up/down (pitch) and
// zoom in/out (distance) — driven by CameraLookZone (drag / pinch on the right half of the screen)
// and the mouse wheel in the Editor.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f); // starting view; sets distance and pitch
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookHeight = 1.2f;

    public const float MinDistance = 3f, MaxDistance = 16f;
    public const float MinPitch = 5f, MaxPitch = 75f;

    private float yaw, pitch, distance;
    private bool ready;

    public void Rotate(float deltaDegrees)
    {
        yaw = Mathf.Repeat(yaw + deltaDegrees, 360f);
    }

    public void Tilt(float deltaDegrees)
    {
        Init();
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
        distance = Mathf.Clamp(offset.magnitude, MinDistance, MaxDistance);
        pitch = Mathf.Clamp(Mathf.Atan2(offset.y, -offset.z) * Mathf.Rad2Deg, MinPitch, MaxPitch);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Init();

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll > 0f ? 0.9f : 1.1f);
        }
#endif

        Vector3 focus = target.position + Vector3.up * lookHeight;
        Vector3 desiredPosition = focus + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(focus);
    }
}
