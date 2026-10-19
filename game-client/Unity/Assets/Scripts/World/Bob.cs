using UnityEngine;

// Gentle floating on the waves (boats, buoys): rises, falls and rocks a little.
public class Bob : MonoBehaviour
{
    [SerializeField] private float height = 0.12f;
    [SerializeField] private float rock = 3f;

    private Vector3 start;
    private Quaternion startRot;
    private float phase;

    private void Awake()
    {
        start = transform.localPosition;
        startRot = transform.localRotation;
        phase = Random.value * 10f;
    }

    private void Update()
    {
        float t = Time.time + phase;
        transform.localPosition = start + Vector3.up * Mathf.Sin(t * 1.3f) * height;
        transform.localRotation = startRot * Quaternion.Euler(Mathf.Sin(t * 0.9f) * rock, 0f, Mathf.Sin(t * 1.1f) * rock);
    }
}
