using UnityEngine;

// A fish shadow drifting back and forth in the canal (ambience only; catches come from the server).
public class FishSwim : MonoBehaviour
{
    [SerializeField] private float range = 6f;
    [SerializeField] private float speed = 0.6f;
    [SerializeField] private float wobble = 0.6f;

    private Vector3 origin;
    private float phase;

    private void Start()
    {
        origin = transform.localPosition;
        phase = Random.value * 100f;
        speed *= Random.Range(0.6f, 1.4f);
    }

    private void Update()
    {
        float t = Time.time * speed + phase;
        float x = Mathf.Sin(t) * range;
        float z = Mathf.Sin(t * 2.3f) * wobble;
        transform.localPosition = origin + new Vector3(x, 0f, z);

        // Face the swimming direction
        float dx = Mathf.Cos(t);
        transform.localRotation = Quaternion.Euler(0f, dx >= 0f ? 90f : -90f, 0f);
    }
}
