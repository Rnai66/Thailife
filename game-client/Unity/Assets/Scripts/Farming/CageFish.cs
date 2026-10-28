using System.Collections.Generic;
using UnityEngine;

// The fish in one floating cage (กระชัง): rounded little fish of the stocked species circle inside the
// net, wag their tails and now and then leap out of the water with a splash. They grow with the cage's
// progress (fingerlings → market size) and older, well-fed fish jump higher and more often.
// Prawns (กุ้งก้ามกราม) don't leap; they flick backwards instead. FishPond passes in the cage state.
public class CageFish : MonoBehaviour
{
    [SerializeField] private float radius = 0.75f;
    [SerializeField] private int count = 5;

    private class Swimmer
    {
        public Transform body, tail, splash;
        public float angle, speed, lane, phase, nextJump, jump = -1f, dir = 1f;
    }

    private readonly List<Swimmer> swimmers = new List<Swimmer>();
    private PondClient.Cage cage;
    private string species;
    private float shownAt;
    private Material template;

    private const float Water = 0.04f;

    public void Configure(float cageRadius, int fishCount)
    {
        radius = cageRadius;
        count = fishCount;
    }

    public void Show(PondClient.Cage c)
    {
        cage = c;
        shownAt = Time.time;
        string s = c != null && c.stage != "empty" ? (string.IsNullOrEmpty(c.species) ? "tilapia" : c.species) : null;
        if (s != species) Rebuild(s);
    }

    // 0 = just stocked, 1 = ready to harvest (counted on between server updates)
    private float Growth()
    {
        if (cage == null) return 0f;
        if (cage.stage == "grown") return 1f;
        float left = Mathf.Max(1f, cage.ready_in_s);
        float total = left / Mathf.Max(0.02f, 1f - cage.progress);
        return Mathf.Clamp01(cage.progress + (Time.time - shownAt) / total);
    }

    private void Rebuild(string id)
    {
        foreach (var s in swimmers) if (s.body != null) Destroy(s.body.gameObject);
        foreach (var s in swimmers) if (s.splash != null) Destroy(s.splash.gameObject);
        swimmers.Clear();
        species = id;
        if (id == null) return;
        if (template == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            template = player != null ? player.GetComponent<Renderer>()?.sharedMaterial : null;
        }
        for (int i = 0; i < count; i++)
        {
            var s = new Swimmer
            {
                angle = i * Mathf.PI * 2f / count + Random.value * 0.5f,
                speed = Random.Range(0.35f, 0.6f),
                lane = Random.Range(0.35f, 1f),
                phase = Random.value * 10f,
                nextJump = Time.time + Random.Range(2f, 9f),
                dir = i % 2 == 0 ? 1f : -1f,
            };
            s.body = Model(id, out s.tail);
            s.splash = Splash();
            swimmers.Add(s);
        }
    }

    private void Update()
    {
        if (species == null) return;
        float g = Growth();
        bool fed = cage != null && (cage.fed_today || cage.stage == "grown");
        float size = Mathf.Lerp(0.4f, 1f, g);
        bool prawn = species == "prawn";

        foreach (var s in swimmers)
        {
            s.body.localScale = Vector3.one * size;
            float r = radius * s.lane;
            float wag = Mathf.Sin(Time.time * (s.jump >= 0f ? 22f : 9f) + s.phase);

            if (s.jump < 0f && Time.time >= s.nextJump && g > 0.08f)
                s.jump = 0f; // start a leap (or a prawn's flick)

            if (s.jump >= 0f)
            {
                float dur = prawn ? 0.35f : Mathf.Lerp(0.6f, 0.95f, g);
                s.jump += Time.deltaTime / dur;
                float k = Mathf.Clamp01(s.jump);
                if (prawn)
                {
                    // Tail flick: a quick dart backwards along the circle
                    s.angle -= s.dir * Time.deltaTime * 3f / Mathf.Max(0.2f, r);
                    Place(s, r, Water + Mathf.Sin(k * Mathf.PI) * 0.04f, 0f);
                }
                else
                {
                    float height = Mathf.Lerp(0.18f, 0.6f, g) * (fed ? 1f : 0.6f);
                    s.angle += s.dir * Time.deltaTime * 1.6f / Mathf.Max(0.2f, r);
                    Place(s, r, Water + Mathf.Sin(k * Mathf.PI) * height, Mathf.Lerp(-55f, 55f, k));
                    if (k > 0.97f) Splash(s, size);
                }
                if (s.jump >= 1f)
                {
                    s.jump = -1f;
                    // Grown, well-fed fish jump the most; fingerlings rarely
                    float rate = (0.25f + g) * (fed ? 1.3f : 0.7f);
                    s.nextJump = Time.time + Random.Range(3f, 9f) / rate;
                    if (!prawn) Splash(s, size);
                }
            }
            else
            {
                s.angle += s.dir * Time.deltaTime * s.speed / Mathf.Max(0.2f, r);
                Place(s, r, Water + Mathf.Sin(Time.time * 2f + s.phase) * 0.01f, 0f);
            }
            if (s.tail != null) s.tail.localRotation = Quaternion.Euler(0f, wag * (prawn ? 8f : 28f), 0f);
            FadeSplash(s);
        }
    }

    private void Place(Swimmer s, float r, float y, float pitch)
    {
        var pos = new Vector3(Mathf.Cos(s.angle) * r, y, Mathf.Sin(s.angle) * r);
        // Heading: along the circle in the swimming direction
        var heading = new Vector3(-Mathf.Sin(s.angle), 0f, Mathf.Cos(s.angle)) * s.dir;
        s.body.localPosition = pos;
        s.body.localRotation = Quaternion.LookRotation(heading) * Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Splash(Swimmer s, float size)
    {
        if (s.splash == null) return;
        var p = s.body.localPosition;
        s.splash.localPosition = new Vector3(p.x, Water + 0.01f, p.z);
        s.splash.localScale = new Vector3(0.15f, 0.02f, 0.15f) * size;
        s.splash.gameObject.SetActive(true);
    }

    private static void FadeSplash(Swimmer s)
    {
        if (s.splash == null || !s.splash.gameObject.activeSelf) return;
        var sc = s.splash.localScale;
        sc.x += Time.deltaTime * 1.2f;
        sc.z += Time.deltaTime * 1.2f;
        sc.y = Mathf.Max(0.002f, sc.y - Time.deltaTime * 0.04f);
        s.splash.localScale = sc;
        if (sc.x > 0.6f) s.splash.gameObject.SetActive(false);
    }

    // ── Models (forward +Z, about 0.4 m long at market size) ──
    private Transform Model(string id, out Transform tail)
    {
        var root = new GameObject("CageFish_" + id).transform;
        root.SetParent(transform, false);
        if (id == "prawn") return Prawn(root, out tail);

        Color body, belly, fin;
        switch (id)
        {
            case "duk": body = new Color(0.28f, 0.27f, 0.24f); belly = new Color(0.75f, 0.72f, 0.62f); fin = new Color(0.22f, 0.2f, 0.18f); break;
            case "tapien": body = new Color(0.86f, 0.8f, 0.55f); belly = new Color(0.97f, 0.95f, 0.88f); fin = new Color(0.9f, 0.35f, 0.25f); break;
            default: body = new Color(0.42f, 0.5f, 0.56f); belly = new Color(0.85f, 0.88f, 0.9f); fin = new Color(0.55f, 0.35f, 0.4f); break; // ปลานิล
        }
        bool catfish = id == "duk";
        float tall = catfish ? 0.11f : (id == "tapien" ? 0.19f : 0.16f);

        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0f, 0f), new Vector3(0.13f, tall, 0.4f), body);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, -tall * 0.2f, 0.02f), new Vector3(0.11f, tall * 0.7f, 0.33f), belly);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, tall * 0.45f, -0.02f), new Vector3(0.025f, tall * 0.55f, 0.18f), fin);   // dorsal fin
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Sphere, root, new Vector3(sx * 0.055f, -0.02f, 0.06f), new Vector3(0.012f, 0.04f, 0.07f), fin, new Vector3(0f, sx * 30f, sx * 40f)); // pectoral fin
            Part(PrimitiveType.Sphere, root, new Vector3(sx * 0.05f, tall * 0.15f, 0.14f), Vector3.one * 0.035f, Color.white);         // eye
            Part(PrimitiveType.Sphere, root, new Vector3(sx * 0.064f, tall * 0.15f, 0.148f), Vector3.one * 0.02f, new Color(0.05f, 0.05f, 0.06f));
            if (catfish) // whiskers
                Part(PrimitiveType.Cylinder, root, new Vector3(sx * 0.05f, -0.01f, 0.22f), new Vector3(0.006f, 0.06f, 0.006f), body, new Vector3(70f, 0f, sx * -40f));
        }
        if (id == "tilapia") // dark vertical bars of ปลานิล
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.01f, -0.08f + i * 0.08f), new Vector3(0.132f, tall * 0.75f, 0.025f), body * 0.7f);

        tail = new GameObject("TailPivot").transform;
        tail.SetParent(root, false);
        tail.localPosition = new Vector3(0f, 0f, -0.18f);
        Part(PrimitiveType.Sphere, tail, new Vector3(0f, 0.04f, -0.06f), new Vector3(0.02f, 0.1f, 0.1f), fin, new Vector3(35f, 0f, 0f));
        Part(PrimitiveType.Sphere, tail, new Vector3(0f, -0.04f, -0.06f), new Vector3(0.02f, 0.1f, 0.1f), fin, new Vector3(-35f, 0f, 0f));
        return root;
    }

    private Transform Prawn(Transform root, out Transform tail)
    {
        var shell = new Color(0.45f, 0.55f, 0.6f);
        var claw = new Color(0.15f, 0.35f, 0.75f);
        var orange = new Color(0.95f, 0.55f, 0.2f);
        Part(PrimitiveType.Capsule, root, new Vector3(0f, 0f, 0.06f), new Vector3(0.1f, 0.1f, 0.12f), shell, new Vector3(90f, 0f, 0f)); // head shell
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cylinder, root, new Vector3(sx * 0.08f, 0f, 0.25f), new Vector3(0.025f, 0.13f, 0.025f), claw, new Vector3(80f, sx * 15f, 0f));       // long blue claw arm
            Part(PrimitiveType.Sphere, root, new Vector3(sx * 0.12f, 0f, 0.38f), new Vector3(0.04f, 0.035f, 0.09f), claw);
            Part(PrimitiveType.Sphere, root, new Vector3(sx * 0.035f, 0.04f, 0.15f), Vector3.one * 0.025f, new Color(0.05f, 0.05f, 0.06f));                         // eye
            Part(PrimitiveType.Cylinder, root, new Vector3(sx * 0.03f, 0.03f, 0.3f), new Vector3(0.004f, 0.12f, 0.004f), orange, new Vector3(75f, sx * 12f, 0f));    // feeler
        }
        tail = new GameObject("TailPivot").transform;
        tail.SetParent(root, false);
        tail.localPosition = new Vector3(0f, 0f, -0.04f);
        for (int i = 0; i < 4; i++) // curled tail segments
            Part(PrimitiveType.Sphere, tail, new Vector3(0f, -i * 0.012f, -0.04f - i * 0.045f), new Vector3(0.09f - i * 0.012f, 0.07f - i * 0.008f, 0.06f), shell);
        Part(PrimitiveType.Sphere, tail, new Vector3(0f, -0.05f, -0.24f), new Vector3(0.1f, 0.015f, 0.06f), orange);
        return root;
    }

    private Transform Splash()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Splash";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        var r = go.GetComponent<Renderer>();
        var m = template != null ? new Material(template) : r.material;
        m.color = new Color(0.85f, 0.95f, 1f);
        r.sharedMaterial = m;
        go.SetActive(false);
        return go.transform;
    }

    private void Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        var material = template != null ? new Material(template) : renderer.material;
        material.color = color;
        renderer.sharedMaterial = material;
    }
}
