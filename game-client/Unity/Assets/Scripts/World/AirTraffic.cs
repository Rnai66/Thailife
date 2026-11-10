using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// เครื่องบินขึ้นลง: the airport's runway is busy. Every minute or so an airliner of one of the airlines
// (the inactive templates the Editor built, Aviation) comes down the glide slope out of the distance,
// touches down with a puff of smoke, brakes, turns off and taxis away to the hangar; in between one
// taxis out from the hangar, lines up, rolls, lifts its nose, climbs away and tucks its wheels up.
// Everything is in the runway's own space: it runs along local +Z, centre at the origin, the taxiway
// along local +X (`taxiX`), the hangar beyond it.
public class AirTraffic : MonoBehaviour
{
    [SerializeField] private float runwayLength = 420f;
    [SerializeField] private float taxiX = 40f, hangarX = 78f;
    [SerializeField] private Transform templates;
    [SerializeField] private float firstIn = 6f, gapMin = 35f, gapMax = 60f;

    private const float Approach = 46f, Touchdown = 38f, Rotate = 44f, Taxi = 9f;
    private const float Glide = 6.5f; // degrees: steeper than real, so you see it come down
    // where the taxiway meets the hangar door (local z): the landing's way in (Land) comes out here
    private float HangarZ => runwayLength * 0.5f - 40f - 25f + 25f - 14f - 120f - 14f;

    private readonly List<GameObject> spare = new List<GameObject>();
    private bool landingNext = true;

    public void Configure(Transform templateRoot, float length, float taxiwayX, float hangarAt)
    {
        templates = templateRoot;
        runwayLength = length;
        taxiX = taxiwayX;
        hangarX = hangarAt;
    }

    private IEnumerator Start()
    {
        if (templates == null || templates.childCount == 0) yield break;
        templates.gameObject.SetActive(false);
        yield return new WaitForSeconds(firstIn);
        while (true)
        {
            StartCoroutine(landingNext ? Land(Pick()) : TakeOff(Pick()));
            landingNext = !landingNext;
            yield return new WaitForSeconds(Random.Range(gapMin, gapMax));
        }
    }

    private Transform Pick()
    {
        var t = templates.GetChild(Random.Range(0, templates.childCount));
        var go = Instantiate(t.gameObject, transform);
        go.name = t.name;
        go.SetActive(true);
        return go.transform;
    }

    private static void Gear(Transform plane, bool down)
    {
        var g = plane.Find("Gear");
        if (g != null) g.gameObject.SetActive(down);
    }

    // Local runway point -> world, plane faces local heading (yaw in runway space), nose pitched up
    private void Put(Transform plane, Vector3 local, float yaw, float pitch)
    {
        plane.position = transform.TransformPoint(local);
        plane.rotation = transform.rotation * Quaternion.Euler(-pitch, yaw, 0f);
    }

    private IEnumerator Land(Transform plane)
    {
        float start = -runwayLength * 0.5f, touch = start + 45f;
        float far = 950f, height = far * Mathf.Tan(Glide * Mathf.Deg2Rad);
        Gear(plane, true);
        // down the glide slope, nose a little up, levelling off (the flare) just above the runway
        for (float z = touch - far; z < touch;)
        {
            float d = touch - z;
            float y = d * Mathf.Tan(Glide * Mathf.Deg2Rad);
            if (d < 60f) y = Mathf.Lerp(0f, 60f * Mathf.Tan(Glide * Mathf.Deg2Rad), Mathf.Pow(d / 60f, 1.6f));
            float speed = Mathf.Lerp(Touchdown, Approach, Mathf.Clamp01(d / 300f));
            Put(plane, new Vector3(0f, y, z), 0f, d < 60f ? 6f : 3f);
            z += speed * Time.deltaTime;
            yield return null;
        }
        Smoke(plane);
        GameAudio.Play("jet", plane.position, 1f, 260f);
        // roll out, braking, then off to the side and along the taxiway to the hangar
        float v = Touchdown, zz = touch, pitch = 6f;
        float exit = runwayLength * 0.5f - 40f;
        while (zz < exit - 25f)
        {
            float left = exit - 25f - zz;
            v = Mathf.Max(Taxi, Mathf.Min(v - 7f * Time.deltaTime, Mathf.Sqrt(Taxi * Taxi + 2f * 4f * left)));
            pitch = Mathf.MoveTowards(pitch, 0f, 4f * Time.deltaTime);
            zz += v * Time.deltaTime;
            Put(plane, new Vector3(0f, 0f, zz), 0f, pitch);
            yield return null;
        }
        yield return Turn(plane, new Vector3(0f, 0f, zz), 0f, 90f, 25f);
        yield return Roll(plane, new Vector3(25f, 0f, zz + 25f), new Vector3(taxiX, 0f, zz + 25f), 90f);
        yield return Turn(plane, new Vector3(taxiX, 0f, zz + 25f), 90f, 180f, 14f);
        float z0 = zz + 25f - 14f;
        yield return Roll(plane, new Vector3(taxiX + 14f, 0f, z0), new Vector3(taxiX + 14f, 0f, z0 - 120f), 180f);
        yield return Turn(plane, new Vector3(taxiX + 14f, 0f, z0 - 120f), 180f, 90f, 14f);
        yield return Roll(plane, new Vector3(taxiX + 28f, 0f, z0 - 134f), new Vector3(hangarX + 20f, 0f, z0 - 134f), 90f);
        Destroy(plane.gameObject);
    }

    private IEnumerator TakeOff(Transform plane)
    {
        float start = -runwayLength * 0.5f + 25f;
        Gear(plane, true);
        // out of the hangar, down the taxiway to the end of the runway, onto it
        float door = HangarZ;
        yield return Roll(plane, new Vector3(hangarX + 20f, 0f, door), new Vector3(taxiX + 14f, 0f, door), -90f);
        yield return Turn(plane, new Vector3(taxiX + 14f, 0f, door), -90f, 180f, 14f);
        yield return Roll(plane, new Vector3(taxiX, 0f, door - 14f), new Vector3(taxiX, 0f, start + 14f), 180f);
        yield return Turn(plane, new Vector3(taxiX, 0f, start + 14f), 180f, -90f, 14f);
        yield return Roll(plane, new Vector3(taxiX - 14f, 0f, start), new Vector3(14f, 0f, start), -90f);
        yield return Turn(plane, new Vector3(14f, 0f, start), -90f, 0f, 14f);
        yield return new WaitForSeconds(2f);
        GameAudio.Play("jet", plane.position, 1f, 260f);
        // the roll: faster and faster, nose up, off
        float v = 3f, z = start + 14f, y = 0f, pitch = 0f;
        bool airborne = false;
        float upFor = 0f;
        while (z < runwayLength * 0.5f + 1800f)
        {
            if (!airborne)
            {
                v += 5.5f * Time.deltaTime;
                if (v > Rotate) pitch = Mathf.MoveTowards(pitch, 10f, 4f * Time.deltaTime);
                if (pitch > 6f) airborne = true;
            }
            else
            {
                v = Mathf.Min(v + 2f * Time.deltaTime, 70f);
                y += v * Mathf.Sin(pitch * Mathf.Deg2Rad * 0.9f) * Time.deltaTime;
                pitch = Mathf.MoveTowards(pitch, 12f, 2f * Time.deltaTime);
                upFor += Time.deltaTime;
                if (upFor > 4f) Gear(plane, false);
            }
            z += v * Time.deltaTime;
            Put(plane, new Vector3(0f, y, z), 0f, pitch);
            yield return null;
        }
        Destroy(plane.gameObject);
    }

    // Taxi in a straight line at taxi speed, facing `yaw`
    private IEnumerator Roll(Transform plane, Vector3 from, Vector3 to, float yaw)
    {
        float len = Vector3.Distance(from, to);
        for (float d = 0f; d < len; d += Taxi * Time.deltaTime)
        {
            Put(plane, Vector3.Lerp(from, to, d / len), yaw, 0f);
            yield return null;
        }
        Put(plane, to, yaw, 0f);
    }

    // A quarter turn (or any) on the ground round a corner of radius r, starting at `at` facing yaw0
    private IEnumerator Turn(Transform plane, Vector3 at, float yaw0, float yaw1, float r)
    {
        float sweep = Mathf.DeltaAngle(yaw0, yaw1);
        if (Mathf.Abs(sweep) < 1f || r < 0.5f) { Put(plane, at, yaw1, 0f); yield break; }
        float side = Mathf.Sign(sweep); // +1 turning right
        var fwd = Quaternion.Euler(0f, yaw0, 0f) * Vector3.forward;
        var right = Quaternion.Euler(0f, yaw0 + 90f * side, 0f) * Vector3.forward;
        var centre = at + right * r;
        float arc = Mathf.Abs(sweep) * Mathf.Deg2Rad * r;
        for (float d = 0f; d < arc; d += Taxi * 0.8f * Time.deltaTime)
        {
            float a = d / arc * sweep;
            var p = centre + Quaternion.Euler(0f, a, 0f) * (at - centre);
            Put(plane, p, yaw0 + a, 0f);
            yield return null;
        }
        Put(plane, centre + Quaternion.Euler(0f, sweep, 0f) * (at - centre), yaw1, 0f);
        _ = fwd;
    }

    // Tyre smoke where the main wheels touch
    private static void Smoke(Transform plane)
    {
        var go = new GameObject("TouchdownSmoke");
        go.transform.position = plane.position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 1.6f;
        main.startSpeed = 1.5f;
        main.startSize = 2.2f;
        main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 40f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = SmokeMaterial;
        ps.Play();
        Destroy(go, 3f);
    }

    private static Material smoke;
    private static Material SmokeMaterial
    {
        get
        {
            if (smoke != null) return smoke;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            smoke = new Material(shader) { color = new Color(0.9f, 0.9f, 0.9f, 0.5f) };
            return smoke;
        }
    }
}
