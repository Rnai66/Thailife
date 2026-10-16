using UnityEngine;

// Songthaew stop (ป้ายรถสองแถว): walking into the trigger opens the travel map.
[RequireComponent(typeof(Collider))]
public class TravelStop : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        // Only the local player (remote avatars are triggers without a PlayerController)
        if (other.GetComponent<PlayerController>() != null)
            TravelMenu.Show();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            TravelMenu.Hide();
    }
}
