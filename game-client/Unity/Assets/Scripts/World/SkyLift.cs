using System.Collections;
using UnityEngine;

// Skytrain station lift (ลิฟต์ขึ้นสถานีรถไฟฟ้า): one at street level and one on the platform. Step in
// and press the button: the cabin carries you up (or down) the glass shaft and lets you out at the
// other end. Two of these share one cabin.
[RequireComponent(typeof(Collider))]
public class SkyLift : MonoBehaviour
{
    private const float RideSeconds = 3f;

    [SerializeField] private string label = "ขึ้นลิฟต์";
    [SerializeField] private Transform cabin, rideFrom, rideTo, exitPoint;

    private static bool riding;

    public void Configure(string buttonLabel, Transform liftCabin, Transform from, Transform to, Transform exit)
    {
        label = buttonLabel;
        cabin = liftCabin;
        rideFrom = from;
        rideTo = to;
        exitPoint = exit;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (riding || other.GetComponent<PlayerController>() == null) return;
        ContextButton.Show(this, label, "ลิฟต์สถานีรถไฟฟ้า · ชานชาลาอยู่ชั้นบน", () => StartCoroutine(Ride(other.GetComponent<PlayerController>())));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }

    private IEnumerator Ride(PlayerController player)
    {
        if (riding || player == null) yield break;
        riding = true;
        ContextButton.Hide(this);
        player.InputEnabled = false;
        GameAudio.Play("whoosh", null, 0.5f);
        var facing = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / RideSeconds)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            var at = Vector3.Lerp(rideFrom.position, rideTo.position, k);
            if (cabin != null) cabin.position = at;
            player.Teleport(at + Vector3.up * 1.05f, facing);
            yield return null;
        }
        if (cabin != null) cabin.position = rideTo.position;
        player.Teleport(exitPoint.position + Vector3.up * 1.05f, exitPoint.rotation);
        player.InputEnabled = true;
        riding = false;
    }

    private void OnDisable()
    {
        riding = false;
    }
}
