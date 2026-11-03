using UnityEngine;

// A pendulum swing about local X (เรือไวกิ้ง): builds up to the full angle, swings a while, settles,
// then starts again
public class Swinger : MonoBehaviour
{
    [SerializeField] private float angle = 70f;
    [SerializeField] private float period = 4.2f;

    public void Configure(float maxAngle, float swingPeriod)
    {
        angle = maxAngle;
        period = swingPeriod;
    }

    private void Update()
    {
        const float Cycle = 40f;
        float t = Time.time % Cycle;
        float amp = t < 8f ? t / 8f : t < 30f ? 1f : Mathf.Max(0f, 1f - (t - 30f) / 10f);
        transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * Mathf.PI * 2f / period) * angle * amp, 0f, 0f);
    }
}
