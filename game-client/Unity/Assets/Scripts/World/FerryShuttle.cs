using UnityEngine;

// The longtail that ferries people across the river, going to and fro between the two piers and
// waiting a while at each; it turns round for the way back. Just for looks: the crossing itself is
// FerryCrossing.
public class FerryShuttle : MonoBehaviour
{
    [SerializeField] private float halfRun = 7f, speed = 2.2f, wait = 6f;

    private float start;

    private void Update()
    {
        // One leg = crossing + waiting at the far pier
        float cross = halfRun * 2f / speed, leg = cross + wait;
        float t = (Time.time - start) % (leg * 2f);
        bool back = t >= leg;
        float k = back ? 1f - Mathf.Clamp01((t - leg) / cross) : Mathf.Clamp01(t / cross);
        k = Mathf.SmoothStep(0f, 1f, k);
        var p = transform.localPosition;
        p.z = Mathf.Lerp(-halfRun, halfRun, k);
        p.y = Mathf.Sin(Time.time * 1.7f) * 0.04f; // bobbing
        transform.localPosition = p;
        transform.localRotation = Quaternion.Euler(0f, back ? 180f : 0f, 0f);
    }
}
