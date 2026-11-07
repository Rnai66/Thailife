using System.Collections.Generic;
using UnityEngine;

// ไฟหน้ารถ: from dusk to dawn every car and motorbike switches its headlights on — two bright lamps at
// the front corners (self-lit, so they show from far away) and, for the vehicles nearest the camera, a
// real spot light throwing a pool of light on the road ahead. Spot lights are limited (MaxBeams) so a
// busy street stays cheap on a phone. Added next to CarSway by TrafficCar and HomeCar.
public class CarHeadlights : MonoBehaviour
{
    private const int MaxBeams = 6;
    private const float BeamDistance = 40f;

    private static readonly List<CarHeadlights> all = new List<CarHeadlights>();
    private static Material lampMat, tailMat;
    private static float nextAssign;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { all.Clear(); lampMat = tailMat = null; nextAssign = 0f; }

    private bool bike;
    private GameObject lamps;
    private Light beam;
    private Vector3 front;   // local: middle of the front at lamp height
    private float halfWidth;
    private bool built;

    public void Configure(bool isBike) => bike = isBike;

    private void OnEnable() => all.Add(this);
    private void OnDisable()
    {
        all.Remove(this);
        if (beam != null) beam.enabled = false;
    }

    private void Build()
    {
        built = true;
        // the vehicle's size from its renderers, in its own space
        var b = new Bounds(Vector3.zero, Vector3.zero);
        bool any = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            var lb = r.localBounds; // the mesh box in its own space: right even when the car is turned
            for (int i = 0; i < 8; i++)
            {
                var corner = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        if (!any) b = new Bounds(new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 1.4f, 4f));
        halfWidth = bike ? 0f : Mathf.Max(0.2f, b.extents.x - 0.28f);
        float y = b.min.y + Mathf.Min(b.size.y * 0.42f, 0.85f);
        front = new Vector3(b.center.x, y, b.max.z + 0.02f);

        if (lampMat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            lampMat = new Material(shader) { name = "HeadlampOn", color = new Color(1f, 0.98f, 0.88f) };
            if (lampMat.HasProperty("_BaseColor")) lampMat.SetColor("_BaseColor", new Color(1f, 0.98f, 0.88f));
            tailMat = new Material(shader) { name = "TailOn", color = new Color(1f, 0.12f, 0.1f) };
            if (tailMat.HasProperty("_BaseColor")) tailMat.SetColor("_BaseColor", new Color(1f, 0.12f, 0.1f));
        }
        lamps = new GameObject("Headlights");
        lamps.transform.SetParent(SwayRoot(), false);
        float size = bike ? 0.16f : 0.2f;
        foreach (float side in bike ? new[] { 0f } : new[] { -1f, 1f })
        {
            Lamp(lampMat, new Vector3(front.x + side * halfWidth, front.y, front.z), size);
            Lamp(tailMat, new Vector3(front.x + side * halfWidth, front.y, b.min.z - 0.02f), size * 0.75f);
        }

        var beamGo = new GameObject("HeadlightBeam");
        beamGo.transform.SetParent(SwayRoot(), false);
        beamGo.transform.localPosition = front + Vector3.forward * 0.1f;
        beamGo.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        beam = beamGo.AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.range = 20f;
        beam.spotAngle = 70f;
        beam.innerSpotAngle = 35f;
        beam.intensity = 6f;
        beam.color = new Color(1f, 0.95f, 0.82f);
        beam.shadows = LightShadows.None;
        beam.renderMode = LightRenderMode.ForcePixel;
        beam.enabled = false;
        lamps.SetActive(false);
    }

    // Lamps rock with the body if CarSway has made its pivot
    private Transform SwayRoot()
    {
        var s = transform.Find("Sway");
        return s != null ? s : transform;
    }

    private void Lamp(Material m, Vector3 local, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.name = "Lamp";
        go.transform.SetParent(lamps.transform, false);
        go.transform.localPosition = local;
        go.transform.localScale = new Vector3(size, size * 0.7f, size * 0.4f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Update()
    {
        bool night = DayNightCycle.IsNight;
        if (!built)
        {
            if (!night) return;
            Build();
        }
        if (lamps.activeSelf != night) lamps.SetActive(night);
        if (!night) { beam.enabled = false; return; }
        // once in a while, the nearest few vehicles to the camera get the real light
        if (Time.time >= nextAssign) AssignBeams();
    }

    private static void AssignBeams()
    {
        nextAssign = Time.time + 0.5f;
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 c = cam.transform.position;
        all.RemoveAll(h => h == null);
        all.Sort((a, b) => (a.transform.position - c).sqrMagnitude.CompareTo((b.transform.position - c).sqrMagnitude));
        for (int i = 0; i < all.Count; i++)
        {
            var h = all[i];
            if (h.beam == null) continue;
            h.beam.enabled = i < MaxBeams && (h.transform.position - c).sqrMagnitude < BeamDistance * BeamDistance;
        }
    }
}
