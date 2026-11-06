using UnityEngine;

// รถบั๊ม: the little cars drive round the floor on their own, steering about and bouncing off the
// rails and off each other (with a spin). The first car carries the player's seat on a ride.
public class BumperCars : MonoBehaviour
{
    [SerializeField] private Transform[] cars;
    [SerializeField] private Vector2 half = new Vector2(4.5f, 3.5f); // floor half-size, local X and Z

    private const float Speed = 2.6f, Radius = 0.85f;

    private Vector2[] pos;
    private float[] heading, turn, spin, jolt; // jolt: how hard it was just hit (rocks the car, decays)

    public void Configure(Transform[] bumperCars, Vector2 floorHalfSize)
    {
        cars = bumperCars;
        half = floorHalfSize;
    }

    private void Awake()
    {
        int n = cars != null ? cars.Length : 0;
        pos = new Vector2[n];
        heading = new float[n];
        turn = new float[n];
        spin = new float[n];
        jolt = new float[n];
        for (int i = 0; i < n; i++)
        {
            pos[i] = new Vector2(cars[i].localPosition.x, cars[i].localPosition.z);
            heading[i] = i * 77f;
            turn[i] = Random.Range(-60f, 60f);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < pos.Length; i++)
        {
            if (Random.value < dt * 0.6f) turn[i] = Random.Range(-90f, 90f);
            heading[i] += (turn[i] + spin[i]) * dt;
            spin[i] = Mathf.MoveTowards(spin[i], 0f, 500f * dt);
            float rad = heading[i] * Mathf.Deg2Rad;
            pos[i] += new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * Speed * dt;
            // Rails round the floor: turn back in
            if (Mathf.Abs(pos[i].x) > half.x - Radius) { pos[i].x = Mathf.Sign(pos[i].x) * (half.x - Radius); heading[i] = -heading[i]; spin[i] = 260f; jolt[i] = 0.7f; }
            if (Mathf.Abs(pos[i].y) > half.y - Radius) { pos[i].y = Mathf.Sign(pos[i].y) * (half.y - Radius); heading[i] = 180f - heading[i]; spin[i] = -260f; jolt[i] = 0.7f; }
        }
        // Bumps: push apart and spin both cars
        for (int i = 0; i < pos.Length; i++)
            for (int j = i + 1; j < pos.Length; j++)
            {
                var d = pos[j] - pos[i];
                float m = d.magnitude;
                if (m >= Radius * 2f || m < 0.0001f) continue;
                var push = d / m * (Radius * 2f - m) * 0.5f;
                pos[i] -= push;
                pos[j] += push;
                heading[i] += 140f;
                heading[j] += 140f;
                spin[i] = 320f;
                spin[j] = -320f;
                jolt[i] = jolt[j] = 1f;
            }
        for (int i = 0; i < pos.Length; i++)
        {
            cars[i].localPosition = new Vector3(pos[i].x, cars[i].localPosition.y, pos[i].y);
            // Rocks after a bump, and a little buzz from the motor all the time
            jolt[i] = Mathf.MoveTowards(jolt[i], 0f, dt * 1.6f);
            float t = Time.time * 24f + i * 3.1f;
            float rock = jolt[i] * jolt[i] * 9f, buzz = 0.5f;
            cars[i].localRotation = Quaternion.Euler(0f, heading[i], 0f) *
                Quaternion.Euler(Mathf.Sin(t) * rock + Mathf.Sin(t * 3.7f) * buzz, 0f, Mathf.Cos(t * 0.9f) * rock * 0.8f + Mathf.Sin(t * 4.3f) * buzz);
        }
    }
}
