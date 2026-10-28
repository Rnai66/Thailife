using System.Collections.Generic;
using UnityEngine;

// Shows the player's chickens (from CoopClient) pecking around inside the pen.
// Each chicken is a few primitives coloured by its feather colour; chicks are small and yellow.
public class ChickenPen : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(5f, 4f);

    private class Bird
    {
        public Transform root;
        public Vector3 target;
        public float wait, phase;
        public Transform[] legs; // hip pivots: swing while walking so the feet step instead of sliding
    }

    private readonly Dictionary<int, Bird> birds = new Dictionary<int, Bird>();
    private Material template;

    public void Configure(Vector2 penSize) => size = penSize;

    private void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        template = player != null ? player.GetComponent<Renderer>()?.sharedMaterial : null;
        if (CoopClient.Instance != null) CoopClient.Instance.Changed += Sync;
        if (CoopClient.Instance?.Current != null) Sync(CoopClient.Instance.Current);
    }

    private void OnDestroy()
    {
        if (CoopClient.Instance != null) CoopClient.Instance.Changed -= Sync;
    }

    private void Sync(CoopClient.State state)
    {
        var alive = new HashSet<int>();
        if (state.chickens != null)
        {
            foreach (var c in state.chickens)
            {
                alive.Add(c.id);
                if (birds.TryGetValue(c.id, out var bird))
                {
                    // Chick grew up: rebuild with the adult look
                    bool wasChick = bird.root.localScale.x < 0.9f;
                    if (wasChick && c.adult) { Destroy(bird.root.gameObject); birds.Remove(c.id); }
                    else continue;
                }
                var built = Build(c);
                var legs = new List<Transform>();
                foreach (Transform part in built) if (part.name == "LegPivot") legs.Add(part);
                birds[c.id] = new Bird { root = built, target = RandomPoint(), wait = Random.value * 2f, legs = legs.ToArray() };
            }
        }

        var gone = new List<int>();
        foreach (var id in birds.Keys) if (!alive.Contains(id)) gone.Add(id);
        foreach (var id in gone) { Destroy(birds[id].root.gameObject); birds.Remove(id); }
    }

    private void Update()
    {
        foreach (var bird in birds.Values)
        {
            if (bird.wait > 0f) { bird.wait -= Time.deltaTime; Step(bird, false); continue; }

            var pos = bird.root.localPosition;
            var to = bird.target - pos;
            to.y = 0f;
            if (to.magnitude < 0.1f)
            {
                bird.target = RandomPoint();
                bird.wait = Random.Range(0.5f, 3f); // stop and peck
                continue;
            }
            bird.root.localPosition = pos + to.normalized * (0.8f * Time.deltaTime);
            bird.root.localRotation = Quaternion.LookRotation(to.normalized);
            Step(bird, true);
        }
    }

    // Quick alternating steps with a little side-to-side waddle; legs settle when the bird stops
    private static void Step(Bird bird, bool walking)
    {
        if (walking) bird.phase += Time.deltaTime * 14f;
        float swing = walking ? Mathf.Sin(bird.phase) * 35f : 0f;
        for (int i = 0; i < bird.legs.Length; i++)
        {
            var target = Quaternion.Euler(i % 2 == 0 ? swing : -swing, 0f, 0f);
            bird.legs[i].localRotation = walking ? target : Quaternion.Slerp(bird.legs[i].localRotation, target, Time.deltaTime * 10f);
        }
        if (walking)
            bird.root.localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(bird.phase) * 5f);
    }

    private Vector3 RandomPoint() =>
        new Vector3(Random.Range(-size.x * 0.4f, size.x * 0.4f), 0f, Random.Range(-size.y * 0.4f, size.y * 0.4f));

    private Transform Build(CoopClient.Chicken c)
    {
        var root = new GameObject($"Chicken_{c.id}").transform;
        root.SetParent(transform, false);
        root.localPosition = RandomPoint();
        root.localScale = Vector3.one * (c.adult ? (c.sex == "rooster" ? 1.15f : 1f) : 0.55f);

        // A plump, rounded bird: round body and chest, wings, big round head with eyes and a little beak
        Color feather = c.adult ? CoopClient.FeatherColor(c.color) : new Color(1f, 0.9f, 0.4f);
        Color red = new Color(0.9f, 0.15f, 0.15f), orange = new Color(1f, 0.7f, 0.2f), ink = new Color(0.08f, 0.07f, 0.07f);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.32f, -0.02f), new Vector3(0.42f, 0.38f, 0.48f), feather);   // body
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.36f, 0.13f), new Vector3(0.32f, 0.32f, 0.28f), feather);    // chest
        foreach (float x in new[] { -0.19f, 0.19f })
        {
            Part(PrimitiveType.Sphere, root, new Vector3(x, 0.34f, -0.03f), new Vector3(0.08f, 0.22f, 0.3f), Color.Lerp(feather, ink, 0.15f)); // wing
            var hip = new GameObject("LegPivot").transform; // the leg swings from the hip
            hip.SetParent(root, false);
            hip.localPosition = new Vector3(x * 0.45f, 0.15f, 0.02f);
            Part(PrimitiveType.Cylinder, hip, new Vector3(0f, -0.075f, 0f), new Vector3(0.035f, 0.075f, 0.035f), orange); // leg
            Part(PrimitiveType.Sphere, hip, new Vector3(0f, -0.14f, 0.04f), new Vector3(0.08f, 0.025f, 0.1f), orange);  // foot
        }
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.6f, 0.2f), new Vector3(0.26f, 0.26f, 0.26f), feather);      // head
        foreach (float x in new[] { -0.075f, 0.075f })
        {
            Part(PrimitiveType.Sphere, root, new Vector3(x, 0.63f, 0.31f), new Vector3(0.05f, 0.06f, 0.03f), ink);          // eye
            Part(PrimitiveType.Sphere, root, new Vector3(x + 0.012f, 0.645f, 0.322f), new Vector3(0.018f, 0.018f, 0.01f), Color.white); // shine
            Part(PrimitiveType.Sphere, root, new Vector3(x * 1.35f, 0.57f, 0.285f), new Vector3(0.05f, 0.03f, 0.02f), new Color(1f, 0.6f, 0.65f)); // blush
        }
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.585f, 0.345f), new Vector3(0.07f, 0.05f, 0.09f), orange);   // beak
        if (c.adult)
        {
            bool rooster = c.sex == "rooster";
            for (int i = 0; i < 3; i++) // comb: three round bumps
                Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.73f + (i == 1 ? 0.02f : 0f), 0.13f + i * 0.06f),
                    Vector3.one * (rooster ? 0.085f : 0.06f), red);
            Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.52f, 0.33f), new Vector3(0.045f, rooster ? 0.09f : 0.06f, 0.045f), red); // wattle
            Color tail = c.color.EndsWith("หางขาว") ? Color.white : c.color == "ขาวล้วน" ? feather : new Color(0.08f, 0.08f, 0.1f);
            if (rooster) // a fan of curved tail feathers
                for (int i = -1; i <= 1; i++)
                    Part(PrimitiveType.Capsule, root, new Vector3(i * 0.06f, 0.52f, -0.26f), new Vector3(0.06f, 0.2f, 0.06f), tail, new Vector3(-35f, 0f, i * 18f));
            else
                Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.48f, -0.24f), new Vector3(0.12f, 0.24f, 0.16f), tail, new Vector3(-30f, 0f, 0f));
        }
        return root;
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
