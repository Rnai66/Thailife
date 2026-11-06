using System.Collections.Generic;
using UnityEngine;

// Makes a vehicle move like a real one on its springs: the body squats when it pulls away, dives when it
// brakes, rolls out of a turn and rocks back and forth before it settles (a damped spring), bounces over
// the bumps in the road and shivers with the engine when it idles. Bicycles and motorbikes lean into
// the turn instead. It watches the vehicle's own movement, so it works whatever drives it (the player in
// DrivableCar, TrafficCar on its route). Everything but the wheels goes under a "Sway" pivot (bikes lean
// whole, wheels and all); the wheels spin with the distance covered unless something else spins them.
[DefaultExecutionOrder(100)] // after whatever moved the vehicle this frame
public class CarSway : MonoBehaviour
{
    [SerializeField] private bool lean;               // a bike: leans into turns, pivots at the ground
    [SerializeField] private bool spinWheels = true;
    [SerializeField] private float pivotHeight = 0.5f; // roll/pitch centre above the ground (cars)

    private Transform sway;
    private Transform[] wheels;
    private float wheelRadius = 0.35f;
    private Vector3 lastPos;
    private float lastYaw, speed, accel, yawRate;
    private float pitch, pitchVel, roll, rollVel, seed;
    private bool started;

    public void Configure(bool isBike, bool spin, float pivot = 0.5f)
    {
        lean = isBike;
        spinWheels = spin;
        pivotHeight = isBike ? 0f : pivot;
    }

    private void Start()
    {
        sway = new GameObject("Sway").transform;
        sway.SetParent(transform, false);
        var move = new List<Transform>();
        foreach (Transform c in transform)
            if (c != sway && (lean || c.name != "Wheel")) move.Add(c);
        foreach (var c in move) c.SetParent(sway, true);

        var list = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name == "Wheel")
            {
                list.Add(t);
                if (t.childCount > 0) wheelRadius = Mathf.Max(0.1f, t.GetChild(0).lossyScale.x * 0.5f);
            }
        wheels = list.ToArray();
        seed = Random.value * 100f;
        lastPos = transform.position;
        lastYaw = transform.eulerAngles.y;
        started = true;
    }

    private void LateUpdate()
    {
        if (!started) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // How the vehicle itself moved this frame
        Vector3 d = transform.position - lastPos;
        d.y = 0f;
        float v = d.magnitude > 5f ? 0f : Vector3.Dot(d, transform.forward) / dt; // a teleport isn't driving
        float yaw = Mathf.DeltaAngle(lastYaw, transform.eulerAngles.y) / dt;
        lastPos = transform.position;
        lastYaw = transform.eulerAngles.y;
        float a = (v - speed) / dt;
        speed = v;
        accel = Mathf.Lerp(accel, Mathf.Clamp(a, -20f, 20f), 1f - Mathf.Exp(-dt * 6f));
        yawRate = Mathf.Lerp(yawRate, Mathf.Clamp(yaw, -180f, 180f), 1f - Mathf.Exp(-dt * 8f));
        float lateral = speed * yawRate * Mathf.Deg2Rad; // m/s², + = the turn is to the right
        float moving = Mathf.Clamp01(Mathf.Abs(speed) / 8f);

        // Where the springs want the body to sit
        float pitchTarget, rollTarget;
        if (lean)
        {
            pitchTarget = Mathf.Clamp(-accel * 0.25f, -2f, 2f);
            rollTarget = Mathf.Clamp(-lateral * 2.2f, -24f, 24f); // into the turn
        }
        else
        {
            pitchTarget = Mathf.Clamp(-accel * 0.7f, -4.5f, 4.5f); // + = nose down (braking)
            rollTarget = Mathf.Clamp(lateral * 0.9f, -5.5f, 5.5f);  // out of the turn
        }
        // Road: rough bits rock it more the faster it goes
        float t = Time.time;
        rollTarget += (Mathf.PerlinNoise(seed, t * 1.7f) - 0.5f) * 2.4f * moving;
        pitchTarget += (Mathf.PerlinNoise(t * 1.3f, seed) - 0.5f) * 1.6f * moving;

        // A soft, under-damped spring: it overshoots and rocks a couple of times before settling
        float k = lean ? 30f : 55f, c = lean ? 9f : 5.5f;
        pitchVel += (k * (pitchTarget - pitch) - c * pitchVel) * dt;
        rollVel += (k * (rollTarget - roll) - c * rollVel) * dt;
        pitch += pitchVel * dt;
        roll += rollVel * dt;

        // Bumps and the engine's shiver
        float bounce = (Mathf.PerlinNoise(seed + 7f, t * (2f + moving * 6f)) - 0.5f) * 0.05f * moving;
        float idle = lean ? 0f : Mathf.Sin(t * 70f + seed) * 0.004f * (1f - moving);

        var rot = Quaternion.Euler(pitch, 0f, roll);
        var pivot = new Vector3(0f, pivotHeight, 0f);
        sway.localRotation = rot;
        sway.localPosition = pivot - rot * pivot + new Vector3(0f, bounce + idle, 0f);

        if (spinWheels && wheels != null)
        {
            float spin = speed * dt / wheelRadius * Mathf.Rad2Deg;
            foreach (var w in wheels) if (w != null) w.Rotate(0f, spin, 0f, Space.Self);
        }
    }
}
