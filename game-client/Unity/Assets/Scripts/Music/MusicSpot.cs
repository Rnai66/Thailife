using UnityEngine;

// A stage where the player can perform (village hall, temple fair, tambon/city event stage, pub).
[RequireComponent(typeof(Collider))]
public class MusicSpot : MonoBehaviour
{
    [SerializeField] private string venueId = "village_hall";

    public void Configure(string venue) => venueId = venue;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            MusicUI.Show(venueId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            MusicUI.Hide();
    }
}
