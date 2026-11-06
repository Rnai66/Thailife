using UnityEngine;

// Classroom (or library) spot inside the school: standing on it opens the classroom window
// (SchoolUI), in library mode for the library's reading tables. During a class the player sits on a
// chair of the room (ClassSeat).
[RequireComponent(typeof(Collider))]
public class SchoolDesk : MonoBehaviour
{
    [SerializeField] private bool library;

    public static SchoolDesk Current { get; private set; } // the one the player last stepped on

    public void Configure(bool isLibrary) => library = isLibrary;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null || ClassSeat.JustClosed) return;
        Current = this;
        SchoolUI.Show(library);
    }

    private void OnTriggerExit(Collider other)
    {
        // sitting down on a chair in the room takes the player off the zone: the class goes on
        if (other.GetComponent<PlayerController>() != null && !ClassSeat.Seated) SchoolUI.Hide();
    }
}
