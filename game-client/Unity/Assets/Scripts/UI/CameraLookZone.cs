using UnityEngine;
using UnityEngine.EventSystems;

// Drag on this UI zone (right half of the screen) to orbit the camera around the player.
public class CameraLookZone : MonoBehaviour, IDragHandler
{
    [SerializeField] private CameraFollow cameraFollow;

    [Tooltip("Degrees of rotation for a drag across the full screen width")]
    [SerializeField] private float degreesPerScreenWidth = 270f;

    public void OnDrag(PointerEventData eventData)
    {
        if (cameraFollow == null || Screen.width <= 0) return;
        cameraFollow.Rotate(eventData.delta.x / Screen.width * degreesPerScreenWidth);
    }
}
