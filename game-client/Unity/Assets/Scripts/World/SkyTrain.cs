using UnityEngine;

// รถไฟฟ้า: runs along the elevated track over the main road, slows into the station in the
// middle, then carries on and loops round (it disappears past the edge of town).
public class SkyTrain : MonoBehaviour
{
    [SerializeField] private float from = -70f, to = 70f;  // local X along the track
    [SerializeField] private float speed = 14f;
    [SerializeField] private float stationX = 0f;

    private float x, dwell;

    private void Start() => x = from;

    private void Update()
    {
        if (dwell > 0f) { dwell -= Time.deltaTime; return; }
        float before = x;
        float slow = Mathf.Clamp01(Mathf.Abs(x - stationX) / 18f);           // brake near the station
        x += Mathf.Max(2f, speed * slow) * Time.deltaTime;
        if (before < stationX && x >= stationX) { x = stationX; dwell = 6f; } // stop for passengers
        if (x > to) x = from;
        var p = transform.localPosition;
        transform.localPosition = new Vector3(x, p.y, p.z);
    }
}
