using UnityEngine;

// The longtail that ferries people across the river, going to and fro between the two piers and
// waiting a while at each; it turns round for the way back. Just for looks: the crossing itself is
// FerryCrossing. While the royal barges go by it waits at the pier.
public class FerryShuttle : MonoBehaviour
{
    [SerializeField] private float halfRun = 7f, speed = 2.2f, wait = 6f;

    private float clock;

    private void Update()
    {
        // One leg = crossing + waiting at the far pier
        float cross = halfRun * 2f / speed, leg = cross + wait;
        float t = clock % (leg * 2f);
        bool back = t >= leg;
        // about to cast off while the royal barges are passing: keep waiting at the pier
        float intoLeg = back ? t - leg : t;
        bool waiting = intoLeg >= cross || t < 0.05f;
        bool hold = waiting && intoLeg > leg - 0.5f && BargeProcession.Near(transform.position, 14f);
        if (!hold) clock += Time.deltaTime;
        float k = back ? 1f - Mathf.Clamp01((t - leg) / cross) : Mathf.Clamp01(t / cross);
        k = Mathf.SmoothStep(0f, 1f, k);
        var p = transform.localPosition;
        p.z = Mathf.Lerp(-halfRun, halfRun, k);
        p.y = Mathf.Sin(Time.time * 1.7f) * 0.04f; // bobbing
        transform.localPosition = p;
        transform.localRotation = Quaternion.Euler(0f, back ? 180f : 0f, 0f);
    }
}
