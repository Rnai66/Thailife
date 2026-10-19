using UnityEngine;

// Sign text that is never mirrored: Unity's 3D text draws through solid objects, so a second
// copy on the back of a board would show through. Instead the one label turns round (180°)
// whenever the camera is on the board's other side.
public class ReadableText : MonoBehaviour
{
    private Quaternion front;

    private void Awake() => front = transform.localRotation;

    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        // The text reads correctly when the camera is on the side its forward (+Z) points away from
        Quaternion baseWorld = transform.parent != null ? transform.parent.rotation * front : front;
        bool behind = Vector3.Dot(baseWorld * Vector3.forward, cam.transform.position - transform.position) > 0f;
        transform.localRotation = behind ? front * Quaternion.Euler(0f, 180f, 0f) : front;
    }
}
