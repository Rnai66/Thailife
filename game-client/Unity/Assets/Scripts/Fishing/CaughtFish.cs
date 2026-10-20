using UnityEngine;

// The fish you just caught (โชว์ปลาที่ตกได้): it leaps out of the water at the bobber, arcs up to
// the player, flaps above their head for a moment, then is put in the basket. Built from primitives
// in the species' colours, sized by its weight.
public class CaughtFish : MonoBehaviour
{
    private Vector3 from;
    private Transform player;
    private float start;
    private const float Flight = 0.9f, Show = 1.8f;

    public static void Play(Vector3 bobber, Transform player, string fishId, float kg)
    {
        var go = new GameObject("CaughtFish");
        go.transform.position = bobber;
        Build(go.transform, fishId, kg);
        var c = go.AddComponent<CaughtFish>();
        c.from = bobber;
        c.player = player;
        c.start = Time.time;
        GameAudio.Play("whoosh", bobber, 0.6f);
    }

    private void Update()
    {
        float t = Time.time - start;
        Vector3 target = player != null ? player.position + Vector3.up * 2.3f : from + Vector3.up * 2f;
        if (t < Flight)
        {
            // Arc from the water to above the player's head, spinning
            float k = t / Flight;
            transform.position = Vector3.Lerp(from, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;
            transform.rotation = Quaternion.Euler(k * 540f, Time.time * 200f, 0f);
        }
        else if (t < Flight + Show)
        {
            // Held up for show: wiggle side to side
            float w = t - Flight;
            transform.position = target + Vector3.up * Mathf.Sin(w * 3f) * 0.05f;
            var face = player != null ? Quaternion.LookRotation(Vector3.Cross(Vector3.up, player.forward)) : Quaternion.identity;
            transform.rotation = face * Quaternion.Euler(0f, Mathf.Sin(w * 14f) * 25f, Mathf.Sin(w * 7f) * 8f);
        }
        else
        {
            // Into the basket
            float k = (t - Flight - Show) / 0.3f;
            transform.localScale = Vector3.one * Mathf.Max(0f, 1f - k);
            if (k >= 1f) Destroy(gameObject);
        }
    }

    // ── Model ───────────────────────────────────────────────────────────────────
    private static void Build(Transform root, string id, float kg)
    {
        float len = Mathf.Clamp(0.28f + kg * 0.11f, 0.28f, 0.95f); // bigger catch, bigger fish
        if (id == "squid") { BuildSquid(root, len); return; }

        Color body, belly, fin;
        float slim = 1f;
        switch (id)
        {
            case "tilapia":   body = new Color(0.55f, 0.56f, 0.62f); belly = new Color(0.88f, 0.75f, 0.75f); fin = new Color(0.78f, 0.47f, 0.5f); break;
            case "barb":      body = new Color(0.78f, 0.8f, 0.82f);  belly = new Color(0.96f, 0.96f, 0.94f); fin = new Color(0.95f, 0.7f, 0.25f); break;
            case "snakehead": body = new Color(0.3f, 0.32f, 0.26f);  belly = new Color(0.72f, 0.72f, 0.65f); fin = body; slim = 0.7f; break;
            case "catfish":   body = new Color(0.42f, 0.4f, 0.38f);  belly = new Color(0.78f, 0.76f, 0.72f); fin = body; break;
            case "mackerel":  body = new Color(0.27f, 0.5f, 0.58f);  belly = new Color(0.9f, 0.92f, 0.94f); fin = new Color(0.23f, 0.43f, 0.51f); break;
            case "seabass":   body = new Color(0.66f, 0.72f, 0.76f); belly = new Color(0.94f, 0.96f, 0.97f); fin = new Color(0.5f, 0.57f, 0.63f); break;
            case "grouper":   body = new Color(0.58f, 0.38f, 0.27f); belly = new Color(0.84f, 0.7f, 0.58f); fin = body; slim = 1.2f; break;
            case "kingfish":  body = new Color(0.31f, 0.47f, 0.66f); belly = new Color(0.9f, 0.92f, 0.95f); fin = new Color(0.27f, 0.39f, 0.58f); slim = 0.7f; break;
            default:          body = new Color(0.6f, 0.62f, 0.66f);  belly = new Color(0.92f, 0.92f, 0.9f); fin = body; break;
        }
        Part(root, PrimitiveType.Sphere, Vector3.zero, new Vector3(len * 0.28f, len * 0.36f * slim, len), body);
        Part(root, PrimitiveType.Sphere, new Vector3(0f, -len * 0.07f * slim, -len * 0.05f), new Vector3(len * 0.24f, len * 0.22f * slim, len * 0.8f), belly);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -len * 0.58f), new Vector3(0.015f, len * 0.36f * slim, len * 0.28f), fin, new Vector3(45f, 0f, 0f));
        Part(root, PrimitiveType.Cube, new Vector3(0f, len * 0.2f * slim, 0f), new Vector3(0.012f, len * 0.14f, len * 0.35f), fin, new Vector3(-15f, 0f, 0f));
        foreach (float x in new[] { -1f, 1f })
        {
            Part(root, PrimitiveType.Sphere, new Vector3(x * len * 0.12f, len * 0.05f, len * 0.36f), Vector3.one * len * 0.07f, Color.white);
            Part(root, PrimitiveType.Sphere, new Vector3(x * len * 0.145f, len * 0.05f, len * 0.375f), Vector3.one * len * 0.035f, Color.black);
            if (id == "catfish") Part(root, PrimitiveType.Cube, new Vector3(x * len * 0.12f, -len * 0.04f, len * 0.55f), new Vector3(0.008f, 0.008f, len * 0.3f), Color.black, new Vector3(0f, x * 25f, 0f));
        }
    }

    private static void BuildSquid(Transform root, float len)
    {
        var pink = new Color(0.94f, 0.6f, 0.62f);
        Part(root, PrimitiveType.Capsule, Vector3.zero, new Vector3(len * 0.3f, len * 0.5f, len * 0.3f), pink, new Vector3(90f, 0f, 0f));
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            Part(root, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * len * 0.08f, Mathf.Sin(a) * len * 0.08f, -len * 0.62f), new Vector3(0.025f, len * 0.22f, 0.025f), pink, new Vector3(90f, 0f, 0f));
        }
        foreach (float x in new[] { -1f, 1f })
            Part(root, PrimitiveType.Sphere, new Vector3(x * len * 0.13f, 0f, -len * 0.3f), Vector3.one * len * 0.08f, Color.black);
    }

    private static Material baseMaterial;

    private static void Part(Transform root, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        if (baseMaterial == null) baseMaterial = r.sharedMaterial;
        var m = new Material(baseMaterial) { color = color };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.75f); // wet and shiny
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void OnDestroy()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) Destroy(r.sharedMaterial);
    }
}
