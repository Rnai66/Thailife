using System.Collections.Generic;
using UnityEngine;

// A farm machine at work in the tambon fields (TambonFarm): the tractor ploughing, the walking tractor
// in the flooded paddy, the combine harvester going up and down the rice — back and forth in lanes over
// its plot, turning at the dikes, its wheels and reel/tiller ("Spin") turning. A machine with no plot
// (the threshing machine) works where it stands: its "Shake" part judders with the engine, the straw
// goes out of its "Chute" in an arc onto the "Pile", and chaff dust puffs out of the "Dust" point.
// They work round the clock (lamps on at night, as at harvest), and wait if you stand in front of them.
public class FarmWork : MonoBehaviour
{
    [SerializeField] private Vector3 centre;        // the plot, in parent space
    [SerializeField] private Vector2 size;          // zero: works standing still
    [SerializeField] private float lane = 3f;
    [SerializeField] private float speed = 1.4f;
    [SerializeField] private float spinRate = 300f; // degrees a second for the "Spin" parts
    [SerializeField] private bool straw;

    private readonly List<Vector3> points = new List<Vector3>();
    private int target, step = 1;
    private float current;
    private Transform[] wheels, spins;
    private float wheelRadius = 0.4f;
    private Transform chute;
    private PlayerController player;
    private float lookFor, nextStraw;
    private readonly List<(Transform t, Vector3 v, float until)> bits = new List<(Transform, Vector3, float)>();
    private static Material strawMat;

    public void Configure(Vector3 plotCentre, Vector2 plotSize, float laneWidth, float metresPerSecond, bool blowsStraw = false)
    {
        centre = plotCentre;
        size = plotSize;
        lane = laneWidth;
        speed = metresPerSecond;
        straw = blowsStraw;
    }

    private static bool WorkingHours => true;
    private Transform shake, pile, dustAt;
    private Vector3 shakeHome;
    private float nextDust;
    private readonly List<(Transform t, Vector3 v, float born)> puffs = new List<(Transform, Vector3, float)>();
    private static Material dustMat;

    private void Start()
    {
        var w = new List<Transform>();
        var s = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            if (t.name == "Wheel") { w.Add(t); if (t.childCount > 0) wheelRadius = t.GetChild(0).lossyScale.x * 0.5f; }
            else if (t.name == "Spin") s.Add(t);
            else if (t.name == "Chute") chute = t;
            else if (t.name == "Shake" && shake == null) { shake = t; shakeHome = t.localPosition; }
            else if (t.name == "Pile" && pile == null) pile = t;
            else if (t.name == "Dust" && dustAt == null) dustAt = t;
        }
        wheels = w.ToArray();
        spins = s.ToArray();

        if (size.x <= 0f || size.y <= 0f) return;
        // lanes up and down the plot's length (z), side by side across it (x)
        int n = Mathf.Max(1, Mathf.FloorToInt(size.x / lane));
        float z0 = centre.z - size.y * 0.5f, z1 = centre.z + size.y * 0.5f;
        for (int k = 0; k < n; k++)
        {
            float x = centre.x - size.x * 0.5f + lane * (k + 0.5f);
            bool up = k % 2 == 0;
            points.Add(new Vector3(x, 0f, up ? z0 : z1));
            points.Add(new Vector3(x, 0f, up ? z1 : z0));
        }
        float best = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float d = (points[i] - transform.localPosition).sqrMagnitude;
            if (d < best) { best = d; target = i; }
        }
    }

    private void Update()
    {
        bool on = WorkingHours;
        if (on && spins != null)
            foreach (var t in spins) if (t != null) t.Rotate(Vector3.right, spinRate * Time.deltaTime, Space.Self);
        if (straw && chute != null) Straw(on);
        if (shake != null) // the engine shakes the whole machine
            shake.localPosition = on ? shakeHome + new Vector3(Mathf.Sin(Time.time * 61f) * 0.008f, Mathf.Sin(Time.time * 47f) * 0.012f, Mathf.Sin(Time.time * 53f) * 0.006f) : shakeHome;
        if (dustAt != null) Dust(on);
        if (points.Count < 2) return;

        Vector3 pos = transform.localPosition;
        Vector3 to = points[target] - pos;
        to.y = 0f;
        if (to.magnitude < 0.6f)
        {
            target += step;
            if (target >= points.Count || target < 0) { step = -step; target += 2 * step; } // done the plot: work it back the other way
            target = Mathf.Clamp(target, 0, points.Count - 1);
            to = points[target] - pos;
            to.y = 0f;
        }

        float want = on && !PlayerInFront() ? speed : 0f;
        if (to.sqrMagnitude > 0.01f)
        {
            var parent = transform.parent;
            Vector3 worldDir = parent != null ? parent.TransformDirection(to.normalized) : to.normalized;
            var look = Quaternion.LookRotation(worldDir, Vector3.up);
            if (want > 0f) transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 45f * Time.deltaTime);
            // slow right down to turn round at the end of a lane
            want *= Mathf.Clamp(Vector3.Dot(transform.forward, worldDir), 0.25f, 1f);
        }
        current = Mathf.MoveTowards(current, want, Time.deltaTime * 1.2f);
        transform.position += transform.forward * current * Time.deltaTime;

        float turn = current * Time.deltaTime / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;
        foreach (var w in wheels) if (w != null) w.Rotate(Vector3.up, -turn, Space.Self);
    }

    private bool PlayerInFront()
    {
        if (player == null)
        {
            if (Time.time < lookFor) return false;
            lookFor = Time.time + 3f;
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return false;
        }
        Vector3 d = player.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude < 5f && Vector3.Dot(d.normalized, transform.forward) > 0.3f;
    }

    // Chaff dust: soft puffs out of the drum's mouth that swell, drift up and shrink away
    private void Dust(bool on)
    {
        if (on && Time.time >= nextDust)
        {
            nextDust = Time.time + 0.25f;
            Transform puff = null;
            for (int i = 0; i < puffs.Count; i++)
                if (Time.time - puffs[i].born > 2f) { puff = puffs[i].t; puffs.RemoveAt(i); break; }
            if (puff == null && puffs.Count < 10)
            {
                puff = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                Destroy(puff.GetComponent<Collider>());
                if (dustMat == null)
                {
                    dustMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.78f, 0.7f, 0.52f) };
                    if (dustMat.HasProperty("_Smoothness")) dustMat.SetFloat("_Smoothness", 0f);
                }
                puff.GetComponent<Renderer>().sharedMaterial = dustMat;
                puff.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                puff.SetParent(transform, true);
            }
            if (puff != null)
            {
                puff.gameObject.SetActive(true);
                puff.position = dustAt.position;
                puffs.Add((puff, -dustAt.right * 0.6f + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f, Time.time));
            }
        }
        for (int i = 0; i < puffs.Count; i++)
        {
            var (t, v, born) = puffs[i];
            float age = Time.time - born;
            if (age > 2f) { t.gameObject.SetActive(false); continue; }
            t.position += v * Time.deltaTime;
            float k = Mathf.Sin(age / 2f * Mathf.PI); // swell then fade
            t.localScale = Vector3.one * (0.15f + 0.6f * k) / Mathf.Max(0.01f, transform.lossyScale.x);
        }
    }

    // Bits of straw blown out of the chute in an arc, falling onto the pile
    private void Straw(bool on)
    {
        if (on && Time.time >= nextStraw)
        {
            nextStraw = Time.time + 0.035f;
            Transform bit = null;
            for (int i = 0; i < bits.Count; i++)
                if (Time.time >= bits[i].until) { bit = bits[i].t; bits.RemoveAt(i); break; }
            if (bit == null && bits.Count < 60)
            {
                bit = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                Destroy(bit.GetComponent<Collider>());
                if (strawMat == null)
                    strawMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.86f, 0.74f, 0.4f) };
                bit.GetComponent<Renderer>().sharedMaterial = strawMat;
                bit.SetParent(transform, true);
                bit.localScale = new Vector3(0.03f, 0.03f, 0.22f);
            }
            if (bit != null)
            {
                bit.gameObject.SetActive(true);
                bit.position = chute.position;
                bit.rotation = Random.rotation;
                Vector3 v;
                if (pile != null) // aimed to come down on top of the stack in about a second
                {
                    float flight = Random.Range(0.9f, 1.25f);
                    var land = pile.position + Vector3.up * Random.Range(2f, 3.2f) + Random.insideUnitSphere * 1.1f;
                    v = (land - chute.position - 0.5f * new Vector3(0f, -4f, 0f) * flight * flight) / flight;
                    bits.Add((bit, v, Time.time + flight));
                }
                else
                {
                    v = chute.forward * Random.Range(2.5f, 3.6f) + Random.insideUnitSphere * 0.6f + Vector3.up * 0.8f;
                    bits.Add((bit, v, Time.time + 1.4f));
                }
            }
        }
        for (int i = 0; i < bits.Count; i++)
        {
            var (t, v, until) = bits[i];
            if (Time.time >= until) { t.gameObject.SetActive(false); continue; }
            v += Vector3.down * 4f * Time.deltaTime;
            t.position += v * Time.deltaTime;
            t.Rotate(200f * Time.deltaTime, 90f * Time.deltaTime, 0f, Space.Self);
            bits[i] = (t, v, until);
        }
    }
}
