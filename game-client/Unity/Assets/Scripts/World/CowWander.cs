using UnityEngine;

// A cow (วัว) grazing inside its pen: ambles to a spot, stops to graze (head down), moves on.
public class CowWander : MonoBehaviour
{
    [SerializeField] private Vector2 pen = new Vector2(12f, 8f); // half-size around the start point
    [SerializeField] private Transform head;

    private Vector3 home, target;
    private float graze, phase;
    private Transform[] legs; // hip pivots in front-left, front-right, back-left, back-right order

    public void Configure(Vector2 halfSize, Transform headPart)
    {
        pen = halfSize;
        head = headPart;
    }

    private void Start()
    {
        home = transform.localPosition;
        var found = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform) if (child.name.StartsWith("LegPivot")) found.Add(child);
        legs = found.ToArray();
        Pick();
    }

    private void Pick()
    {
        target = home + new Vector3(Random.Range(-pen.x, pen.x), 0f, Random.Range(-pen.y, pen.y));
        graze = 0f;
    }

    private void Update()
    {
        if (graze > 0f)
        {
            graze -= Time.deltaTime;
            if (head != null) head.localRotation = Quaternion.Euler(Mathf.Lerp(head.localEulerAngles.x > 180f ? head.localEulerAngles.x - 360f : head.localEulerAngles.x, 35f, Time.deltaTime * 2f), 0f, 0f);
            if (graze <= 0f) Pick();
            Walk(false);
            return;
        }
        if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.identity, Time.deltaTime * 3f);
        Vector3 to = target - transform.localPosition;
        to.y = 0f;
        if (to.magnitude < 0.3f) { graze = Random.Range(4f, 10f); return; }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to), Time.deltaTime * 1.5f);
        transform.localPosition += to.normalized * 0.45f * Time.deltaTime;
        Walk(true);
    }

    // Four-beat walk: diagonal legs move together (front-left with back-right), hooves plant and lift
    private void Walk(bool walking)
    {
        if (legs == null) return;
        if (walking) phase += Time.deltaTime * 6f;
        for (int i = 0; i < legs.Length; i++)
        {
            bool diagonal = i == 0 || i == 3;
            float swing = walking ? Mathf.Sin(phase + (diagonal ? 0f : Mathf.PI)) * 22f : 0f;
            var target = Quaternion.Euler(swing, 0f, 0f);
            legs[i].localRotation = walking ? target : Quaternion.Slerp(legs[i].localRotation, target, Time.deltaTime * 4f);
        }
    }
}
