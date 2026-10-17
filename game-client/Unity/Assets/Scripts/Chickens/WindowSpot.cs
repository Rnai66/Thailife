using UnityEngine;

// Trigger zone that opens a window while the local player stands in it (coop, contest stage).
[RequireComponent(typeof(Collider))]
public class WindowSpot : MonoBehaviour
{
    public enum Window { Coop, Contest }

    [SerializeField] private Window window = Window.Coop;

    public void Configure(Window which) => window = which;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (window == Window.Coop) CoopUI.Show();
        else ContestUI.Show();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (window == Window.Coop) CoopUI.Hide();
        else ContestUI.Hide();
    }
}
