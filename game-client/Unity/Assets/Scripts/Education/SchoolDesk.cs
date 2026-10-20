using UnityEngine;

// Classroom door at the school: walking in opens the classroom window (SchoolUI).
[RequireComponent(typeof(Collider))]
public class SchoolDesk : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SchoolUI.Show();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SchoolUI.Hide();
    }
}
