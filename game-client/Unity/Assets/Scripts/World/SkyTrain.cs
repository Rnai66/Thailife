using UnityEngine;

// รถไฟฟ้า: runs along the elevated track over the main road, slows into the station in the
// middle, opens its doors (SlideDoor) while it waits, then carries on and loops round (it
// disappears past the edge of town).
public class SkyTrain : MonoBehaviour
{
    [SerializeField] private float from = -70f, to = 70f;  // local X along the track
    [SerializeField] private float speed = 14f;
    [SerializeField] private float stationX = 9.3f; // lead car here: the three cars centred on the platform

    private const float Dwell = 6f;

    private float x, dwell;
    private SlideDoor[] doors;
    private bool open;

    private void Start()
    {
        x = from;
        doors = GetComponentsInChildren<SlideDoor>();
    }

    private void Update()
    {
        if (dwell > 0f)
        {
            dwell -= Time.deltaTime;
            SetDoors(dwell > 1.2f && dwell < Dwell - 0.6f); // open after stopping, closed before leaving
            return;
        }
        float before = x;
        float slow = Mathf.Clamp01(Mathf.Abs(x - stationX) / 18f);           // brake near the station
        x += Mathf.Max(2f, speed * slow) * Time.deltaTime;
        if (before < stationX && x >= stationX) { x = stationX; dwell = Dwell; } // stop for passengers
        if (x > to) x = from;
        var p = transform.localPosition;
        transform.localPosition = new Vector3(x, p.y, p.z);
    }

    private void SetDoors(bool value)
    {
        if (open == value || doors == null) return;
        open = value;
        foreach (var d in doors) if (d != null) d.Open = value;
    }
}
