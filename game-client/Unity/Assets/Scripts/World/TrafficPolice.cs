using System.Collections.Generic;
using UnityEngine;

// ตำรวจจราจร at a roundabout or a crossroads: lets the traffic in one way at a time. North–south
// gets to go, then a short all-stop while the junction clears, then east–west, and so on; the
// officer turns to the side that may go, waves it on and blows the whistle at each change.
// TrafficCar asks StopRoom how far it may still drive before the line where it has to wait. Cars
// already in the junction (on the ring of a roundabout) are never held, so it always empties.
public class TrafficPolice : MonoBehaviour
{
    [SerializeField] private Vector3 centre;          // the middle of the junction (world)
    [SerializeField] private float ring = 8.6f;       // a roundabout's outer edge (m); a crossroads: half the road width
    [SerializeField] private Vector3 stand;           // where the officer stands (world)
    [SerializeField] private float green = 11f, clear = 3f;

    private static readonly List<TrafficPolice> all = new List<TrafficPolice>();

    private PoliceOfficer officer;
    private int phase;          // 0 north–south may go, 1 all stop, 2 east–west may go, 3 all stop
    private float phaseEnds, nextWave;

    public void Configure(Vector3 junction, float radius, Vector3 officerAt)
    {
        centre = junction;
        ring = radius;
        stand = officerAt;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Start()
    {
        officer = PoliceOfficer.Make(transform, stand, 0f, transform.position.GetHashCode());
        phase = 0;
        phaseEnds = Time.time + green;
        Point();
    }

    private void Update()
    {
        if (Time.time >= phaseEnds)
        {
            phase = (phase + 1) % 4;
            phaseEnds = Time.time + (phase % 2 == 0 ? green : clear);
            Point();
            if (officer != null && Near(30f)) officer.Whistle(phase % 2 == 1);
        }
        if (officer != null && phase % 2 == 0 && Time.time >= nextWave)
        {
            officer.Wave();
            nextWave = Time.time + 2.2f;
        }
    }

    // Face across the way that may go (its cars come from the sides of the officer's view), or the middle
    private void Point()
    {
        if (officer == null) return;
        if (phase == 0) officer.Face(90f);      // north–south: arms out to them, facing east
        else if (phase == 2) officer.Face(0f);  // east–west: facing north
        else officer.FaceTowards(centre);
    }

    private bool Near(float metres)
    {
        var cam = Camera.main;
        return cam != null && (cam.transform.position - stand).sqrMagnitude < metres * metres * 4f;
    }

    // Is a car heading this way allowed into the junction now?
    private bool MayGo(Vector3 forward)
    {
        if (phase % 2 == 1) return false;
        bool northSouth = Mathf.Abs(forward.z) >= Mathf.Abs(forward.x);
        return northSouth == (phase == 0);
    }

    // How far (m) a car at `position` heading `forward` may still drive before it must wait for the
    // police, or float.MaxValue. `stopping`: the distance it needs to stop (v²/2a) — much closer than
    // that to the line when told to wait, it carries on (like an amber light).
    public static float StopRoom(Vector3 position, Vector3 forward, float halfLength, float stopping)
    {
        float best = float.MaxValue;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return best;
        forward.Normalize();
        foreach (var p in all)
        {
            var to = p.centre - position;
            to.y = 0f;
            float d = to.magnitude;
            float line = p.ring + 1.2f;                 // the give-way line, just outside the junction
            if (d <= line + 0.5f || d > line + 30f) continue;
            if (Vector3.Dot(forward, to / d) < 0.8f) continue; // not driving into it
            if (p.MayGo(forward)) continue;
            float room = d - line - halfLength;
            if (room < stopping * 0.6f) continue; // too late to stop in comfort: through it goes
            best = Mathf.Min(best, Mathf.Max(0f, room));
        }
        return best;
    }
}
