using UnityEngine;

// รถแข่งวนสนาม: a race car going round and round a closed circuit (local points of its parent),
// braking into the tight corners and flat out down the straights. Not part of the road traffic
// (TrafficNet), so taxis never try to drive the track.
public class RaceLap : MonoBehaviour
{
    [SerializeField] private Vector3[] points;
    [SerializeField] private float top = 26f, corner = 11f;
    [SerializeField] private float startAt;

    private float[] at;
    private float length, s, speed;

    public void Configure(Vector3[] loop, float topSpeed, float cornerSpeed, float start)
    {
        points = loop;
        top = topSpeed;
        corner = cornerSpeed;
        startAt = start;
    }

    private void Start()
    {
        if (points == null || points.Length < 3) { enabled = false; return; }
        at = new float[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            at[i + 1] = at[i] + Vector3.Distance(points[i], points[(i + 1) % points.Length]);
        length = at[points.Length];
        s = Mathf.Repeat(startAt, length);
        speed = corner;
    }

    private void Update()
    {
        // how sharp the road bends a little ahead: slow for it
        Vector3 here = Point(s, out var f0), ahead = Point(s + 18f, out var f1);
        float bend = Vector3.Angle(f0, f1);
        float want = Mathf.Lerp(top, corner, Mathf.Clamp01(bend / 70f));
        speed = Mathf.MoveTowards(speed, want, (want < speed ? 14f : 6f) * Time.deltaTime);
        s = Mathf.Repeat(s + speed * Time.deltaTime, length);
        var p = Point(s, out var f);
        var parent = transform.parent;
        transform.position = parent != null ? parent.TransformPoint(p) : p;
        if (f.sqrMagnitude > 0.0001f)
        {
            var dir = parent != null ? parent.TransformDirection(f) : f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
        _ = here; _ = ahead;
    }

    private Vector3 Point(float d, out Vector3 forward)
    {
        d = Mathf.Repeat(d, length);
        int i = 0;
        while (i < points.Length - 1 && at[i + 1] < d) i++;
        var a = points[i];
        var b = points[(i + 1) % points.Length];
        float seg = at[i + 1] - at[i];
        forward = (b - a).normalized;
        return seg < 0.001f ? a : Vector3.Lerp(a, b, (d - at[i]) / seg);
    }
}
