using System.Collections;
using UnityEngine;

// Skytrain station lift (ลิฟต์ขึ้นสถานีรถไฟฟ้า): one at street level and one on the platform. Press
// the button: the cabin comes to your floor, the landing and cabin doors (SlideDoor) open, you step
// in, they close, the cabin carries you up (or down) the glass shaft and opens at the other end.
// Two of these share one cabin.
[RequireComponent(typeof(Collider))]
public class SkyLift : MonoBehaviour
{
    private const float RideSeconds = 3f;

    [SerializeField] private string label = "ขึ้นลิฟต์";
    [SerializeField] private Transform cabin, rideFrom, rideTo, exitPoint;
    [SerializeField] private SlideDoor[] fromDoors, toDoors; // landing + cabin doors on each floor

    private static bool riding;

    public void Configure(string buttonLabel, Transform liftCabin, Transform from, Transform to, Transform exit,
        SlideDoor[] doorsHere, SlideDoor[] doorsThere)
    {
        fromDoors = doorsHere;
        toDoors = doorsThere;
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
        var facing = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        // Call the cabin down (or up) to this floor if it waits at the other one
        if (cabin != null && (cabin.position - rideFrom.position).sqrMagnitude > 0.01f)
        {
            var start = cabin.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / RideSeconds)
            {
                cabin.position = Vector3.Lerp(start, rideFrom.position, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            cabin.position = rideFrom.position;
        }
        GameAudio.Play("door_chime", rideFrom.position, 0.4f);
        yield return Doors(fromDoors, true);
        player.Teleport(rideFrom.position + Vector3.up * 1.05f, facing);
        yield return new WaitForSeconds(0.2f);
        yield return Doors(fromDoors, false);
        GameAudio.Play("whoosh", null, 0.5f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / RideSeconds)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            var at = Vector3.Lerp(rideFrom.position, rideTo.position, k);
            if (cabin != null) cabin.position = at;
            player.Teleport(at + Vector3.up * 1.05f, facing);
            yield return null;
        }
        if (cabin != null) cabin.position = rideTo.position;
        GameAudio.Play("door_chime", rideTo.position, 0.4f);
        yield return Doors(toDoors, true);
        player.Teleport(exitPoint.position + Vector3.up * 1.05f, exitPoint.rotation);
        player.InputEnabled = true;
        yield return new WaitForSeconds(0.8f);
        yield return Doors(toDoors, false);
        riding = false;
    }

    private static IEnumerator Doors(SlideDoor[] doors, bool open)
    {
        if (doors == null || doors.Length == 0) yield break;
        foreach (var d in doors) if (d != null) d.Open = open;
        yield return new WaitForSeconds(0.7f);
    }

    private void OnDisable()
    {
        riding = false;
    }
}
