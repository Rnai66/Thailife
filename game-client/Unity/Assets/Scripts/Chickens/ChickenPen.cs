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
        public float wait;
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
                birds[c.id] = new Bird { root = Build(c), target = RandomPoint(), wait = Random.value * 2f };
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
            if (bird.wait > 0f) { bird.wait -= Time.deltaTime; continue; }

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
        }
    }

    private Vector3 RandomPoint() =>
        new Vector3(Random.Range(-size.x * 0.4f, size.x * 0.4f), 0f, Random.Range(-size.y * 0.4f, size.y * 0.4f));

    private Transform Build(CoopClient.Chicken c)
    {
        var root = new GameObject($"Chicken_{c.id}").transform;
        root.SetParent(transform, false);
        root.localPosition = RandomPoint();
        root.localScale = Vector3.one * (c.adult ? (c.sex == "rooster" ? 1.15f : 1f) : 0.55f);

        Color feather = c.adult ? CoopClient.FeatherColor(c.color) : new Color(1f, 0.9f, 0.4f);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.3f, 0f), new Vector3(0.35f, 0.32f, 0.45f), feather);   // body
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.55f, 0.2f), new Vector3(0.2f, 0.22f, 0.2f), feather);  // head
        Part(PrimitiveType.Cube, root, new Vector3(0f, 0.55f, 0.33f), new Vector3(0.06f, 0.05f, 0.1f), new Color(1f, 0.75f, 0.2f)); // beak
        if (c.adult)
        {
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.7f, 0.2f), new Vector3(0.04f, c.sex == "rooster" ? 0.12f : 0.06f, 0.14f), new Color(0.85f, 0.1f, 0.1f)); // comb
            Color tail = c.color.EndsWith("หางขาว") ? Color.white : c.color == "ขาวล้วน" ? feather : new Color(0.08f, 0.08f, 0.1f);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.45f, -0.25f), new Vector3(0.08f, c.sex == "rooster" ? 0.35f : 0.18f, 0.18f), tail, new Vector3(-30f, 0f, 0f)); // tail
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
