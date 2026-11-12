using System.Collections.Generic;
using UnityEngine;

// ด่านตรวจ: cones narrowing the road, a sign, officers on the side. Everyone slows right down through
// it — the traffic (TrafficCar asks SpeedCap) and you: drive your own vehicle through faster than
// 20 km/h and it's a ticket for running the checkpoint; slow down and the officer waves you on.
// The zone is a box (this transform's space, `size`) over the road. Placed by the Editor (Police.Checkpoint).
public class PoliceCheckpoint : MonoBehaviour
{
    [SerializeField] private Vector3 size = new Vector3(8f, 4f, 24f);
    [SerializeField] private Vector3[] officers = new Vector3[0]; // local spots by the road

    public const int Limit = 20;               // km/h through the checkpoint
    private const float CarCap = 20f / PlayerController.KmhPerMetre; // traffic: the same, in m/s

    private static readonly List<PoliceCheckpoint> all = new List<PoliceCheckpoint>();
    private readonly List<PoliceOfficer> crew = new List<PoliceOfficer>();
    private bool inside;
    private float passed = -1f, lastSide, quietUntil;

    public void Configure(Vector3 zone, Vector3[] officerSpots)
    {
        size = zone;
        officers = officerSpots;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Start()
    {
        int i = 0;
        foreach (var o in officers)
        {
            var p = transform.TransformPoint(o);
            // face across the road (towards the zone's middle line)
            var mid = transform.TransformPoint(new Vector3(0f, 0f, o.z));
            var officer = PoliceOfficer.Make(transform, p, Quaternion.LookRotation(mid - p).eulerAngles.y, transform.position.GetHashCode() + i++);
            crew.Add(officer);
        }
    }

    private bool Contains(Vector3 world, float margin = 0f)
    {
        var l = transform.InverseTransformPoint(world);
        return Mathf.Abs(l.x) <= size.x * 0.5f + margin && Mathf.Abs(l.z) <= size.z * 0.5f + margin && l.y > -2f && l.y < size.y;
    }

    // The most a traffic car at `world` may drive (m/s) — slowing down for a checkpoint just ahead too
    public static float SpeedCap(Vector3 world)
    {
        foreach (var c in all)
            if (c.Contains(world, 8f)) return CarCap;
        return float.MaxValue;
    }

    private void Update()
    {
        var car = DrivableCar.Current;
        bool now = car != null && Contains(car.transform.position);
        if (now)
        {
            foreach (var o in crew) o.FaceTowards(car.transform.position);
            // the speed it goes past the officers (the zone's middle line) is what counts
            float side = Mathf.Sign(transform.InverseTransformPoint(car.transform.position).z);
            if (inside && side != lastSide) passed = car.Kmh;
            lastSide = side;
        }
        if (now == inside) return;
        inside = now;
        if (now) { passed = -1f; return; }
        // out of the zone: judged if it went through past the officers
        float speed = passed;
        passed = -1f;
        if (speed < 0f || crew.Count == 0) return;
        var officer = crew[0];
        if (speed > Limit + 2 && Time.time >= quietUntil)
        {
            quietUntil = Time.time + 90f;
            officer.Whistle(stop: true);
            PoliceClient.Stop("checkpoint", Mathf.RoundToInt(speed), Limit);
        }
        else
        {
            officer.Wave(); // เชิญครับ
            if (DrivableCar.Current != null) UiKit.FloatAbovePlayer("ด่านตรวจ: เชิญผ่านครับ", UiKit.OkGreen);
        }
    }
}
