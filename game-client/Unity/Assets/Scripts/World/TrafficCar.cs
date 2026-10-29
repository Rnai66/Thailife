using UnityEngine;

// A vehicle in city traffic (รถเมล์ แท็กซี่ ตุ๊กตุ๊ก รถหรู ...): drives along a path of points in its
// parent's space, keeps its lane, stops at red lights (TrafficLight) on its stop lines, waits at bus
// stops, and brakes for whatever is in front — the car ahead, the player, the player's car. At the
// end of the path it carries on from the start (the ends are out past the edge of town).
[RequireComponent(typeof(Rigidbody))]
public class TrafficCar : MonoBehaviour
{
    private const float Accel = 3.5f, Brake = 6f;

    [SerializeField] private Vector3[] path;            // parent-space points, in driving order
    [SerializeField] private bool loop;                 // drive from the last point back to the first (else jump)
    [SerializeField] private float topSpeed = 10f;
    [SerializeField] private float halfLength = 2.2f;
    [SerializeField] private float startAt;             // metres along the path at the start
    [SerializeField] private TrafficLight lights;
    [SerializeField] private float[] stopAt;            // stop lines: metres along the path...
    [SerializeField] private int[] stopFor;             // ...and the light approach each obeys
    [SerializeField] private float[] dwellAt;           // bus stops: metres along the path

    private float[] dist;
    private float length, s, speed, dwell, lastDwell = -1f;
    private Transform[] wheels;
    private float wheelRadius = 0.35f;

    public void Configure(Vector3[] points, bool closed, float maxSpeed, float halfLen, float start, TrafficLight light,
        float[] stops, int[] approaches, float[] busStops = null)
    {
        path = points;
        loop = closed;
        topSpeed = maxSpeed;
        halfLength = halfLen;
        startAt = start;
        lights = light;
        stopAt = stops;
        stopFor = approaches;
        dwellAt = busStops;
    }

    // Distance along a path to the point nearest p (for placing stop lines by position)
    public static float DistanceTo(Vector3[] points, Vector3 p)
    {
        float best = float.MaxValue, at = 0f, run = 0f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            var a = points[i]; var b = points[i + 1];
            float seg = Vector3.Distance(a, b);
            float t = seg > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, b - a) / (seg * seg)) : 0f;
            float d = Vector3.Distance(Vector3.Lerp(a, b, t), p);
            if (d < best) { best = d; at = run + t * seg; }
            run += seg;
        }
        return at;
    }

    private void Awake()
    {
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        if (path == null || path.Length < 2) { enabled = false; return; }
        int n = path.Length + (loop ? 1 : 0);
        dist = new float[n];
        for (int i = 1; i < n; i++) dist[i] = dist[i - 1] + Vector3.Distance(path[(i - 1) % path.Length], path[i % path.Length]);
        length = dist[n - 1];
        s = Mathf.Repeat(startAt, length);
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name == "Wheel") // a group turned on its side: the tyre and a hub that shows it turning
            {
                list.Add(t);
                if (t.childCount > 0) wheelRadius = t.GetChild(0).lossyScale.x * 0.5f;
            }
        wheels = list.ToArray();
        speed = topSpeed * 0.6f;
        Place();
    }

    private Vector3 At(float d)
    {
        d = Mathf.Repeat(d, length);
        int i = 1;
        while (i < dist.Length - 1 && dist[i] < d) i++;
        float seg = dist[i] - dist[i - 1];
        float t = seg > 0f ? (d - dist[i - 1]) / seg : 0f;
        var local = Vector3.Lerp(path[(i - 1) % path.Length], path[i % path.Length], t);
        return transform.parent != null ? transform.parent.TransformPoint(local) : local;
    }

    private void Place()
    {
        var p = At(s);
        var ahead = At(s + 1.5f) - At(s - 1.5f); // look along the curve: smooth turns
        ahead.y = 0f;
        var rot = ahead.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(ahead) : transform.rotation;
        bool jump = (p - transform.position).sqrMagnitude > 25f;
        if (jump) transform.SetPositionAndRotation(p, rot); // wrapped round to the start
        else
        {
            transform.position = p;
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.deltaTime);
        }
    }

    // Free road ahead (metres) before something we must not hit
    private float Clearance()
    {
        float look = 4f + speed * 1.2f;
        var origin = transform.position + Vector3.up * 0.9f;
        float best = look;
        foreach (var hit in Physics.SphereCastAll(origin, 0.8f, transform.forward, look + halfLength, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            bool blocks = hit.collider.GetComponentInParent<TrafficCar>() != null
                       || hit.collider.GetComponentInParent<PlayerController>() != null
                       || hit.collider.GetComponentInParent<DrivableCar>() != null
                       || hit.collider is CharacterController;
            if (blocks) best = Mathf.Min(best, Mathf.Max(0f, hit.distance - halfLength));
        }
        return best;
    }

    private float ToStopLine()
    {
        float best = float.MaxValue;
        if (stopAt == null || lights == null) return best;
        for (int i = 0; i < stopAt.Length; i++)
        {
            float ahead = Mathf.Repeat(stopAt[i] - (s + halfLength), length);
            if (ahead > 40f) continue;
            var lamp = lights.State(stopFor[i]);
            if (lamp == TrafficLight.Lamp.Green) continue;
            // On yellow, go on if too close to stop in comfort
            if (lamp == TrafficLight.Lamp.Yellow && ahead < speed * speed / (2f * Brake) + 0.5f) continue;
            best = Mathf.Min(best, ahead);
        }
        return best;
    }

    // Top speed, eased off for a bend in the next few metres
    private float Cruise()
    {
        var a = At(s + 1f) - At(s);
        var b = At(s + 9f) - At(s + 8f);
        a.y = b.y = 0f;
        return Vector3.Angle(a, b) > 20f ? Mathf.Min(topSpeed, 4.5f) : topSpeed;
    }

    private void Update()
    {
        if (dwell > 0f) { dwell -= Time.deltaTime; return; }

        // Bus stops: pull in and wait
        if (dwellAt != null)
            foreach (float d in dwellAt)
            {
                float ahead = Mathf.Repeat(d - s, length);
                if (ahead < 0.3f && lastDwell != d) { dwell = 6f; lastDwell = d; speed = 0f; return; }
                if (ahead > 0.3f && ahead < 30f && lastDwell == d) lastDwell = -1f;
            }

        float room = Mathf.Min(Clearance() - 1.5f, ToStopLine() - 0.3f);
        if (dwellAt != null)
            foreach (float d in dwellAt)
            {
                float ahead = Mathf.Repeat(d - s, length);
                if (ahead < 25f && lastDwell != d) room = Mathf.Min(room, ahead + 0.5f);
            }
        float want = room <= 0f ? 0f : Mathf.Min(Cruise(), Mathf.Sqrt(2f * Brake * room));
        speed = want < speed ? Mathf.Max(want, speed - Brake * 2f * Time.deltaTime) : Mathf.Min(want, speed + Accel * Time.deltaTime);
        if (room <= 0f) speed = 0f;
        s = Mathf.Repeat(s + speed * Time.deltaTime, length);
        Place();

        float spin = speed * Time.deltaTime / Mathf.Max(0.1f, wheelRadius) * Mathf.Rad2Deg;
        foreach (var w in wheels) w.Rotate(0f, spin, 0f, Space.Self);
    }
}
