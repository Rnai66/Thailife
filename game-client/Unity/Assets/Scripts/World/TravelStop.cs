using UnityEngine;

// Songthaew stop (ป้ายรถสองแถว), boat pier or long-distance terminal (บขส. / station / airport):
// walking into the trigger opens the travel map.
[RequireComponent(typeof(Collider))]
public class TravelStop : MonoBehaviour
{
    [SerializeField] private string title; // empty = ป้ายรถสองแถว
    [SerializeField] private bool boat;    // boat pier: sea trips only (ทะเล ⇄ เกาะ)
    [SerializeField] private string intercity; // bus / train / air: a long-distance terminal (TravelMode)

    public void Configure(string menuTitle, bool boatPier = false, string longDistance = null)
    {
        title = menuTitle;
        boat = boatPier;
        intercity = longDistance;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        // Only the local player (remote avatars are triggers without a PlayerController). Arriving by
        // songthaew puts you right under the stop: don't pop the map up again as you get off.
        if (other.GetComponent<PlayerController>() != null && Time.timeSinceLevelLoad > 3f)
            TravelMenu.Show(title, boat, intercity);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            TravelMenu.Hide();
    }
}
