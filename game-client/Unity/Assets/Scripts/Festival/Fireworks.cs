using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// พลุ over the big fair after dark: a rocket climbs with a spark trail, bursts into a ball of glowing
// sparks that fall and fade, now and then a finale of several at once. The sparks are pooled spheres
// with the glow materials the scene builder hands over.
public class Fireworks : MonoBehaviour
{
    [SerializeField] private Material[] colors;
    [SerializeField] private Vector2 area = new Vector2(30f, 20f); // launch spread, local X and Z
    [SerializeField] private float height = 26f;

    private const int Sparks = 28, Pool = 4;

    private readonly List<Transform[]> bursts = new List<Transform[]>();
    private readonly Queue<int> free = new Queue<int>();
    private float next;

    public void Configure(Material[] glowColors, Vector2 launchArea, float burstHeight)
    {
        colors = glowColors;
        area = launchArea;
        height = burstHeight;
    }

    private void Awake()
    {
        if (colors == null || colors.Length == 0) { enabled = false; return; }
        for (int b = 0; b < Pool; b++)
        {
            var sparks = new Transform[Sparks + 1]; // the last one is the rocket
            for (int i = 0; i <= Sparks; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(s.GetComponent<Collider>());
                s.name = "Spark";
                s.transform.SetParent(transform, false);
                var r = s.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                s.SetActive(false);
                sparks[i] = s.transform;
            }
            bursts.Add(sparks);
            free.Enqueue(b);
        }
    }

    // The fair closing mid-burst: put every spark away and free the whole pool
    private void OnDisable()
    {
        StopAllCoroutines();
        free.Clear();
        for (int b = 0; b < bursts.Count; b++)
        {
            foreach (var s in bursts[b]) if (s != null) s.gameObject.SetActive(false);
            free.Enqueue(b);
        }
    }

    private void Update()
    {
        if (Time.time < next || !DayNightCycle.Dark) return; // after dark only
        bool finale = Random.value < 0.15f;
        next = Time.time + (finale ? 6f : Random.Range(1.6f, 3.6f));
        int count = finale ? Pool : 1;
        for (int k = 0; k < count && free.Count > 0; k++) StartCoroutine(Launch(free.Dequeue(), k * 0.25f));
    }

    private IEnumerator Launch(int index, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var sparks = bursts[index];
        var mat = colors[Random.Range(0, colors.Length)];
        foreach (var s in sparks) s.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var from = new Vector3(Random.Range(-area.x, area.x) * 0.5f, 0f, Random.Range(-area.y, area.y) * 0.5f);
        var top = from + Vector3.up * (height + Random.Range(-4f, 4f));

        // Rocket climbing
        var rocket = sparks[Sparks];
        rocket.gameObject.SetActive(true);
        rocket.localScale = Vector3.one * 0.35f;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            rocket.localPosition = Vector3.Lerp(from, top, 1f - (1f - t / 1.2f) * (1f - t / 1.2f));
            yield return null;
        }
        rocket.gameObject.SetActive(false);

        // Burst: sparks fly out on a sphere, slow down, droop and shrink away
        var velocity = new Vector3[Sparks];
        float power = Random.Range(7f, 11f);
        for (int i = 0; i < Sparks; i++)
        {
            velocity[i] = Random.onUnitSphere * power;
            sparks[i].localPosition = top;
            sparks[i].gameObject.SetActive(true);
        }
        const float Life = 2.2f;
        for (float t = 0f; t < Life; t += Time.deltaTime)
        {
            float k = t / Life;
            for (int i = 0; i < Sparks; i++)
            {
                velocity[i] = velocity[i] * (1f - 1.8f * Time.deltaTime) + Vector3.down * 4f * Time.deltaTime;
                sparks[i].localPosition += velocity[i] * Time.deltaTime;
                sparks[i].localScale = Vector3.one * Mathf.Lerp(0.45f, 0.05f, k);
            }
            yield return null;
        }
        for (int i = 0; i < Sparks; i++) sparks[i].gameObject.SetActive(false);
        free.Enqueue(index);
    }
}
