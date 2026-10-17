using UnityEngine;

// A place on the canal bank where the player can fish. Put it on a trigger collider;
// the bobber lands at bobberPoint (over the water).
[RequireComponent(typeof(Collider))]
public class FishingSpot : MonoBehaviour
{
    [SerializeField] private Transform bobberPoint;

    public Vector3 BobberPosition => bobberPoint != null ? bobberPoint.position : transform.position + transform.forward * 2f;

    public void Configure(Transform bobber) => bobberPoint = bobber;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            FishingUI.Enter(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            FishingUI.Exit(this);
    }
}
