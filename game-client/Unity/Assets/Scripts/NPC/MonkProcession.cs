using UnityEngine;

// พระบิณฑบาต: in the early morning (06:00-08:30 game time) a line of monks walks barefoot from
// the temple along the road with their alms bowls, then back. Monks are built from simple
// shapes: saffron robe, shaven head, black alms bowl.
public class MonkProcession : MonoBehaviour
{
    [SerializeField] private Vector3 from, to;   // world points along the road
    [SerializeField] private int monks = 3;

    private Transform[] line;
    private float t;
    private float nextCheck;

    public void Configure(Vector3 start, Vector3 end, int count)
    {
        from = start;
        to = end;
        monks = count;
    }

    private static bool Morning => GameClock.Hour == 6 || GameClock.Hour == 7 || (GameClock.Hour == 8 && GameClock.Minute < 30);

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 2f;
            if (Morning && line == null) Spawn();
            else if (!Morning && line != null) Clear();
        }
        if (line == null) return;

        // Out and back across the morning (150 game minutes = 150 real seconds)
        float minutes = (GameClock.Hour - 6) * 60f + GameClock.Minute + GameClock.MinuteOfDay % 1f;
        float u = Mathf.Clamp01(minutes / 150f);
        float along = u < 0.5f ? u * 2f : (1f - u) * 2f;
        Vector3 dir = (to - from).normalized;
        for (int i = 0; i < line.Length; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, Mathf.Clamp01(along - i * 0.025f));
            var m = line[i];
            m.position = p + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 4f + i)) * 0.04f; // gentle walking bob
            m.rotation = Quaternion.LookRotation(u < 0.5f ? dir : -dir);
        }
    }

    private void Spawn()
    {
        line = new Transform[monks];
        var robe = Mat(new Color(0.95f, 0.5f, 0.1f));
        var skin = Mat(new Color(0.8f, 0.6f, 0.45f));
        var bowl = Mat(new Color(0.08f, 0.08f, 0.08f));
        for (int i = 0; i < monks; i++)
        {
            var m = new GameObject("Monk_พระ").transform;
            m.SetParent(transform, false);
            Part(PrimitiveType.Cylinder, m, new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.75f, 0.45f), robe);   // robe
            Part(PrimitiveType.Sphere, m, new Vector3(0f, 1.48f, 0f), new Vector3(0.52f, 0.5f, 0.45f), robe);      // shoulders
            Part(PrimitiveType.Sphere, m, new Vector3(0f, 1.78f, 0f), new Vector3(0.26f, 0.3f, 0.26f), skin);      // shaven head
            Part(PrimitiveType.Sphere, m, new Vector3(0f, 1.05f, 0.3f), new Vector3(0.3f, 0.22f, 0.3f), bowl);     // alms bowl
            line[i] = m;
        }
    }

    private void Clear()
    {
        foreach (var m in line) if (m != null) Destroy(m.gameObject);
        line = null;
    }

    private static Material Mat(Color c) => new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };

    private static void Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
