using UnityEngine;

// The door of a hotel room: walk up to it to go back down to the lobby.
[RequireComponent(typeof(Collider))]
public class HotelRoomExit : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ContextButton.Show(this, "ออกจากห้อง", "กลับไปที่ล็อบบี้โรงแรม", () => { ContextButton.Hide(this); HotelRoom.Leave(); });
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }
}
