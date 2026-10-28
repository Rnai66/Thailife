using UnityEngine;

// Automatic sliding glass doors (ประตูกระจกอัตโนมัติ): the two panels glide apart when the player walks
// up to the entrance and close again after they leave. Built by Entrance.Modern in the Editor.
[RequireComponent(typeof(Collider))]
public class AutoDoor : MonoBehaviour
{
    [SerializeField] private Transform left, right;
    [SerializeField] private float slide = 1f;

    private Vector3 leftShut, rightShut;
    private int inside;
    private float open; // 0 shut .. 1 open

    public void Configure(Transform leftPanel, Transform rightPanel, float distance)
    {
        left = leftPanel;
        right = rightPanel;
        slide = distance;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake()
    {
        if (left != null) leftShut = left.localPosition;
        if (right != null) rightShut = right.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) inside++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) inside = Mathf.Max(0, inside - 1);
    }

    private void Update()
    {
        float goal = inside > 0 ? 1f : 0f;
        if (Mathf.Approximately(open, goal)) return;
        open = Mathf.MoveTowards(open, goal, Time.deltaTime * 2.5f);
        float k = Mathf.SmoothStep(0f, 1f, open);
        if (left != null) left.localPosition = leftShut + Vector3.left * slide * k;
        if (right != null) right.localPosition = rightShut + Vector3.right * slide * k;
    }
}
