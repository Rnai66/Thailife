using UnityEngine;

// Classroom (or library) spot inside the school: standing on it opens the classroom window
// (SchoolUI), in library mode for the library's reading tables.
[RequireComponent(typeof(Collider))]
public class SchoolDesk : MonoBehaviour
{
    [SerializeField] private bool library;

    public void Configure(bool isLibrary) => library = isLibrary;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SchoolUI.Show(library);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SchoolUI.Hide();
    }
}
