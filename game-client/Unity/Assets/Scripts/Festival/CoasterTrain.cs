using UnityEngine;

// รถไฟเหาะ: runs the cars round the closed track (a loop of points in this object's space). The chain
// lift drags the train up slowly; after that gravity sets the speed, fast at the bottom of each drop.
public class CoasterTrain : MonoBehaviour
{
    [SerializeField] private Vector3[] path;
    [SerializeField] private Transform[] cars;
    [SerializeField] private float liftFrom = 0.04f, liftTo = 0.3f; // fraction of the track that is the chain lift
    [SerializeField] private float spacing = 1.9f;

    private const float LiftSpeed = 3.2f, MinSpeed = 4f, MaxSpeed = 17f;

    private float[] at;   // distance along the track to each point
    private float length, distance, top;

    public void Configure(Vector3[] trackPath, Transform[] trainCars, float chainFrom, float chainTo)
    {
        path = trackPath;
        cars = trainCars;
        liftFrom = chainFrom;
        liftTo = chainTo;
    }

    private void Awake()
    {
        if (path == null || path.Length < 3) { enabled = false; return; }
        at = new float[path.Length + 1];
        top = float.MinValue;
        for (int i = 0; i < path.Length; i++)
        {
            at[i + 1] = at[i] + Vector3.Distance(path[i], path[(i + 1) % path.Length]);
            top = Mathf.Max(top, path[i].y);
        }
        length = at[path.Length];
    }

    private void Update()
    {
        float fraction = distance / length;
        float speed;
        if (fraction >= liftFrom && fraction <= liftTo) speed = LiftSpeed;
        else speed = Mathf.Clamp(Mathf.Sqrt(2f * 9.8f * Mathf.Max(0f, top - Sample(distance, out _).y)) * 0.85f, MinSpeed, MaxSpeed);
        distance = Mathf.Repeat(distance + speed * Time.deltaTime, length);
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] == null) continue;
            var p = Sample(distance - i * spacing, out var forward);
            cars[i].localPosition = p;
            if (forward.sqrMagnitude > 0.0001f) cars[i].localRotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    // Point and direction at a distance along the closed track
    private Vector3 Sample(float d, out Vector3 forward)
    {
        d = Mathf.Repeat(d, length);
        int lo = 0, hi = path.Length;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (at[mid] <= d) lo = mid; else hi = mid;
        }
        var a = path[lo];
        var b = path[(lo + 1) % path.Length];
        float seg = at[lo + 1] - at[lo];
        forward = b - a;
        return seg > 0f ? Vector3.Lerp(a, b, (d - at[lo]) / seg) : a;
    }
}
