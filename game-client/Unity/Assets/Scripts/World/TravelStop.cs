using UnityEngine;

// Songthaew stop (ป้ายรถสองแถว): walking into the trigger opens the travel map.
[RequireComponent(typeof(Collider))]
public class TravelStop : MonoBehaviour
{
    [SerializeField] private string title; // empty = ป้ายรถสองแถว
    [SerializeField] private bool boat;    // boat pier: sea trips only (ทะเล ⇄ เกาะ)
    [SerializeField] private bool train;   // skytrain platform: trips to other provinces

    public void Configure(string menuTitle, bool boatPier = false, bool trainPlatform = false)
    {
        title = menuTitle;
        boat = boatPier;
        train = trainPlatform;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        // Only the local player (remote avatars are triggers without a PlayerController)
        if (other.GetComponent<PlayerController>() != null)
            TravelMenu.Show(title, boat, train);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            TravelMenu.Hide();
    }
}
