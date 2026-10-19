using UnityEngine;

// A cow (วัว) grazing inside its pen: ambles to a spot, stops to graze (head down), moves on.
public class CowWander : MonoBehaviour
{
    [SerializeField] private Vector2 pen = new Vector2(12f, 8f); // half-size around the start point
    [SerializeField] private Transform head;

    private Vector3 home, target;
    private float graze;

    public void Configure(Vector2 halfSize, Transform headPart)
    {
        pen = halfSize;
        head = headPart;
    }

    private void Start()
    {
        home = transform.localPosition;
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
            return;
        }
        if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.identity, Time.deltaTime * 3f);
        Vector3 to = target - transform.localPosition;
        to.y = 0f;
        if (to.magnitude < 0.3f) { graze = Random.Range(4f, 10f); return; }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to), Time.deltaTime * 1.5f);
        transform.localPosition += to.normalized * 0.45f * Time.deltaTime;
    }
}
