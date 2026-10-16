using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookHeight = 1.2f;

    // Orbit angle around the target, changed by touch drag (CameraLookZone)
    private float yaw;

    public void Rotate(float deltaDegrees)
    {
        yaw = Mathf.Repeat(yaw + deltaDegrees, 360f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
