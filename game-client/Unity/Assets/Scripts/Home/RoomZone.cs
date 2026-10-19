using UnityEngine;

// Trigger inside the room: tells HomeInterior when the local player walks in or out.
public class RoomZone : MonoBehaviour
{
    public HomeInterior Interior;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) Interior.SetInside(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) Interior.SetInside(false);
    }
}
