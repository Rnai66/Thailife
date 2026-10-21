using System.Collections.Generic;
using UnityEngine;

// What the player holds and how they move while doing something: a fishing rod with its line running
// to the bobber, a hoe, a basket of seedlings, a watering can, a sickle, a bowl of chicken feed, a
// milk bucket or an instrument. The prop rides in the right hand (positioned from the hand bone and
// oriented with the body, so every character model works), the arm is lifted into a holding pose
// after the animator runs, and short motions (cast, chop, pour, sweep, scatter, strum) swing it.
//   ActivityPose.Hold("rod")           keep holding until Clear()
//   ActivityPose.Do("hoe", 1.4f)        a one-off action, then the prop is put away
public class ActivityPose : MonoBehaviour
{
    private static ActivityPose local;

    private Animator animator;
    private Transform prop, tip;
    private string held, motion;
    private float motionStart, motionLength, clearAt = -1f;
    private LineRenderer line;
    private Vector3? lineTarget;
    private readonly List<Transform> extras = new List<Transform>(); // grain, water drops

    private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { local = null; mats.Clear(); }

    private static ActivityPose Local
    {
        get
        {
            if (local != null) return local;
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return null;
            local = player.GetComponent<ActivityPose>() ?? player.gameObject.AddComponent<ActivityPose>();
            return local;
        }
    }

    // Keep a prop in hand (e.g. the rod while at a fishing spot)
    public static void Hold(string what, Vector3? face = null)
    {
        var p = Local;
        if (p == null) return;
        p.Equip(what);
        p.clearAt = -1f;
        if (face.HasValue) p.Face(face.Value);
    }

    // A one-off action with its prop; the prop goes away when it's done (unless something is held)
    public static void Do(string what, float seconds = 1.4f, Vector3? face = null)
    {
        var p = Local;
        if (p == null) return;
        bool keep = p.held == PropFor(what) && p.clearAt < 0f;
        p.Equip(PropFor(what));
        p.motion = what;
        p.motionStart = Time.time;
        p.motionLength = seconds;
        if (!keep) p.clearAt = Time.time + seconds;
        if (face.HasValue) p.Face(face.Value);
    }

    public static void Clear()
    {
        if (local != null) local.Unequip();
    }

    // Where the fishing line goes (the bobber); null = hangs from the tip
    public static void LineTo(Vector3? target)
    {
        if (local != null) local.lineTarget = target;
    }

    private static string PropFor(string motion) => motion switch
    {
        "cast" or "reel" => "rod",
        "chop" => "hoe",
        "plant" => "seedlings",
        "pour" => "can",
        "sweep" => "sickle",
        "scatter" => "bowl",
        "milk" => "bucket",
        "strum" => "instrument",
        _ => motion,
    };

    private void Face(Vector3 point)
    {
        var d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }

    private void Equip(string what)
    {
        if (held == what && prop != null) return;
        Unequip();
        held = what;
        prop = new GameObject("Held_" + what).transform;
        prop.SetParent(transform, false);
        Build(prop, what);
    }

    private void Unequip()
    {
        if (prop != null) Destroy(prop.gameObject);
        foreach (var e in extras) if (e != null) Destroy(e.gameObject);
        extras.Clear();
        prop = null;
        tip = null;
        held = null;
        motion = null;
        lineTarget = null;
        if (line != null) { Destroy(line.gameObject); line = null; }
    }

    private void Update()
    {
        if (clearAt > 0f && Time.time >= clearAt) { clearAt = -1f; Unequip(); }
        if (motion != null && Time.time > motionStart + motionLength) motion = null;
    }

    // ── Pose ─────────────────────────────────────────────────────────────────
    private void LateUpdate()
    {
        if (prop == null) return;
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        var root = animator.transform;
        float k = motion != null ? (Time.time - motionStart) / Mathf.Max(0.01f, motionLength) : 0f;
        float wave = Mathf.Sin(k * Mathf.PI * 2f * Cycles(motion));

        // Arm: forward and a little bent; actions swing it
        float lift = held switch { "rod" => -55f, "instrument" => -35f, "bucket" => -10f, "seedlings" => -25f, _ => -40f };
        float swing = motion switch
        {
            "cast" => k < 0.4f ? Mathf.Lerp(0f, 50f, k / 0.4f) : Mathf.Lerp(50f, -25f, (k - 0.4f) / 0.6f),
            "reel" => -15f * Mathf.Abs(wave),
            "chop" => 45f * wave,
            "plant" => 30f * Mathf.Abs(wave),
            "pour" => -10f,
            "sweep" => 0f,
            "scatter" => -25f * Mathf.Abs(wave),
            "milk" => 10f * wave,
            "strum" => 0f,
            _ => 0f,
        };
        Bend(HumanBodyBones.RightUpperArm, lift - swing, root.right);
        Bend(HumanBodyBones.RightLowerArm, -30f, root.right);
        if (motion == "sweep") Bend(HumanBodyBones.RightUpperArm, 40f * wave, root.up);
        if (held == "instrument")
        {
            Bend(HumanBodyBones.LeftUpperArm, -40f, root.right);
            Bend(HumanBodyBones.LeftLowerArm, -60f, root.right);
        }

        // Prop sits in the hand, turned with the body (plus the action's own tilt)
        var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null) return;
        Vector3 pos = hand.position;
        Quaternion rot = root.rotation;
        switch (held)
        {
            case "rod":
                float tilt = motion == "cast" ? Mathf.Lerp(-40f, 25f, Mathf.SmoothStep(0f, 1f, k)) : motion == "reel" ? -25f + 8f * wave : -10f;
                rot = root.rotation * Quaternion.Euler(-30f + tilt, 0f, 0f); // ~40° up, out over the water
                break;
            case "hoe":
                rot = root.rotation * Quaternion.Euler(-20f + 55f * wave, 0f, 0f);
                break;
            case "can":
                rot = root.rotation * Quaternion.Euler(motion == "pour" ? 35f : 0f, 0f, 0f);
                break;
            case "sickle":
                rot = root.rotation * Quaternion.Euler(10f, -50f * wave, 0f);
                break;
            case "instrument":
                pos = root.position + root.up * 1.15f + root.forward * 0.28f;
                rot = root.rotation * Quaternion.Euler(0f, 0f, 35f);
                break;
            case "bucket":
                pos = hand.position + Vector3.down * 0.05f;
                break;
        }
        prop.SetPositionAndRotation(pos, rot);

        UpdateLine();
        UpdateExtras(k);
    }

    private static float Cycles(string m) => m switch { "chop" => 3f, "plant" => 3f, "sweep" => 3f, "scatter" => 3f, "milk" => 5f, "reel" => 4f, "strum" => 6f, _ => 1f };

    private void Bend(HumanBodyBones bone, float angle, Vector3 axis)
    {
        var t = animator.GetBoneTransform(bone);
        if (t != null) t.rotation = Quaternion.AngleAxis(angle, axis) * t.rotation;
    }

    private void UpdateLine()
    {
        if (held != "rod" || tip == null) return;
        if (line == null)
        {
            line = new GameObject("FishingLine").AddComponent<LineRenderer>();
            line.positionCount = 12;
            line.startWidth = line.endWidth = 0.012f;
            line.material = Mat("Line", new Color(0.95f, 0.95f, 0.95f), unlit: true);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Vector3 a = tip.position;
        Vector3 b = lineTarget ?? a + Vector3.down * 1.2f;
        float sag = lineTarget.HasValue ? 0.25f + 0.1f * Vector3.Distance(a, b) * 0.1f : 0f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (line.positionCount - 1f);
            line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t));
        }
    }

    // Water pouring from the can, grain falling from the scattering hand
    private void UpdateExtras(float k)
    {
        bool pouring = motion == "pour" && held == "can";
        bool scattering = motion == "scatter" && held == "bowl";
        if (!pouring && !scattering)
        {
            foreach (var e in extras) if (e != null) e.gameObject.SetActive(false);
            return;
        }
        while (extras.Count < 10)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Destroy(d.GetComponent<Collider>());
            d.localScale = Vector3.one * 0.05f;
            extras.Add(d);
        }
        Vector3 from = tip != null ? tip.position : prop.position;
        for (int i = 0; i < extras.Count; i++)
        {
            var d = extras[i];
            d.gameObject.SetActive(true);
            d.GetComponent<Renderer>().sharedMaterial = pouring ? Mat("Water", new Color(0.55f, 0.8f, 1f)) : Mat("Grain", new Color(0.95f, 0.8f, 0.35f));
            float f = (Time.time * 2.2f + i / (float)extras.Count) % 1f;
            Vector3 spread = pouring ? transform.forward * 0.25f * f : Quaternion.Euler(0f, i * 36f, 0f) * transform.forward * 0.8f * f;
            d.position = from + spread + Vector3.down * (f * f * 1.4f);
        }
    }

    // ── Props (simple shapes, about real size; local +Z = forward, held at the origin) ──
    private void Build(Transform p, string what)
    {
        var wood = Mat("PropWood", new Color(0.55f, 0.38f, 0.2f));
        var bamboo = Mat("PropBamboo", new Color(0.78f, 0.68f, 0.4f));
        var steel = Mat("PropSteel", new Color(0.7f, 0.72f, 0.75f));
        switch (what)
        {
            case "rod":
                Part(p, PrimitiveType.Cylinder, bamboo, new Vector3(0f, 0f, 1.1f), new Vector3(0.035f, 1.15f, 0.035f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cylinder, Mat("PropReel", new Color(0.2f, 0.2f, 0.22f)), new Vector3(0f, -0.06f, 0.15f), new Vector3(0.09f, 0.02f, 0.09f), new Vector3(0f, 0f, 90f));
                tip = new GameObject("Tip").transform;
                tip.SetParent(p, false);
                tip.localPosition = new Vector3(0f, 0f, 2.25f);
                break;
            case "hoe":
                Part(p, PrimitiveType.Cylinder, wood, new Vector3(0f, 0f, 0.5f), new Vector3(0.04f, 0.65f, 0.04f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cube, steel, new Vector3(0f, -0.12f, 1.12f), new Vector3(0.22f, 0.26f, 0.03f), Vector3.zero);
                break;
            case "seedlings":
                Part(p, PrimitiveType.Cylinder, Mat("PropBasket", new Color(0.75f, 0.6f, 0.35f)), new Vector3(0f, -0.15f, 0.1f), new Vector3(0.3f, 0.1f, 0.3f), Vector3.zero);
                for (int i = 0; i < 6; i++)
                    Part(p, PrimitiveType.Cube, Mat("PropRice", new Color(0.35f, 0.7f, 0.25f)), new Vector3(-0.09f + (i % 3) * 0.09f, -0.02f, 0.05f + (i / 3) * 0.1f), new Vector3(0.02f, 0.22f, 0.02f), new Vector3(0f, 0f, (i - 3) * 6f));
                break;
            case "can":
                Part(p, PrimitiveType.Cylinder, Mat("PropCan", new Color(0.3f, 0.6f, 0.85f)), new Vector3(0f, -0.18f, 0.12f), new Vector3(0.2f, 0.12f, 0.2f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, Mat("PropCan", new Color(0.3f, 0.6f, 0.85f)), new Vector3(0f, -0.12f, 0.33f), new Vector3(0.03f, 0.14f, 0.03f), new Vector3(60f, 0f, 0f));
                tip = new GameObject("Spout").transform;
                tip.SetParent(p, false);
                tip.localPosition = new Vector3(0f, -0.05f, 0.45f);
                break;
            case "sickle":
                Part(p, PrimitiveType.Cylinder, wood, new Vector3(0f, 0f, 0.1f), new Vector3(0.035f, 0.12f, 0.035f), new Vector3(90f, 0f, 0f));
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 30f * Mathf.Deg2Rad;
                    Part(p, PrimitiveType.Cube, steel, new Vector3(Mathf.Sin(a) * 0.15f, 0f, 0.22f + (1f - Mathf.Cos(a)) * 0.15f + i * 0.02f), new Vector3(0.07f, 0.012f, 0.03f), new Vector3(0f, i * 30f, 0f));
                }
                break;
            case "bowl":
                Part(p, PrimitiveType.Sphere, Mat("PropBowl", new Color(0.9f, 0.3f, 0.25f)), new Vector3(0f, -0.08f, 0.1f), new Vector3(0.24f, 0.08f, 0.24f), Vector3.zero);
                Part(p, PrimitiveType.Sphere, Mat("Grain", new Color(0.95f, 0.8f, 0.35f)), new Vector3(0f, -0.04f, 0.1f), new Vector3(0.18f, 0.04f, 0.18f), Vector3.zero);
                tip = p;
                break;
            case "bucket":
                Part(p, PrimitiveType.Cylinder, steel, new Vector3(0f, -0.25f, 0.05f), new Vector3(0.26f, 0.13f, 0.26f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, Mat("Milk", new Color(0.98f, 0.98f, 0.95f)), new Vector3(0f, -0.13f, 0.05f), new Vector3(0.23f, 0.005f, 0.23f), Vector3.zero);
                break;
            case "instrument": // a phin-like guitar across the chest
                Part(p, PrimitiveType.Sphere, Mat("PropGuitar", new Color(0.75f, 0.45f, 0.2f)), new Vector3(0f, 0f, 0f), new Vector3(0.32f, 0.36f, 0.1f), Vector3.zero);
                Part(p, PrimitiveType.Cube, wood, new Vector3(0f, 0.42f, 0f), new Vector3(0.05f, 0.55f, 0.03f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, Mat("PropReel", new Color(0.2f, 0.2f, 0.22f)), new Vector3(0f, 0f, 0.05f), new Vector3(0.08f, 0.005f, 0.08f), new Vector3(90f, 0f, 0f));
                break;
        }
    }

    private static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static Material Mat(string name, Color c, bool unlit = false)
    {
        if (mats.TryGetValue(name, out var m) && m != null) return m;
        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { name = name, color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        mats[name] = m;
        return m;
    }

    private void OnDestroy()
    {
        Unequip();
        if (local == this) local = null;
    }
}
