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

    // A job's work loop (WorkScene): the motion keeps going until Clear(), its prop in hand
    public static void Work(string what, Vector3? face = null)
    {
        var p = Local;
        if (p == null) return;
        p.Equip(PropFor(what));
        if (p.motion != what) p.motionStart = Time.time;
        p.motion = what;
        p.motionLength = 1e6f;
        p.clearAt = -1f;
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
        "gather" or "pluck" => "basket", // หาของป่า: stoop and pick into a basket on the other arm
        "dig" => "spade",                // ขุดหน่อไม้ with a เสียม
        "poke" => "pole",                // แหย่รังมดแดง with a long pole and a net bag
        // jobs (WorkScene)
        "type" => "hands",               // typing at the keyboard
        "repairpc" => "screwdriver",     // working inside an open computer
        "write" or "explain" => "marker",// at the whiteboard, then turning to the class
        "wrench" => "wrench",            // bent over the motorbike
        "whistle" => "whistle",          // the coach: arm raises with the kids, a blow on the whistle
        "examine" => "stethoscope",      // listening to the patient's chest
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
        if (WorkPose(root)) return;
        if (Forage(root, k, wave)) return;

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

    // หาของป่า: the whole body takes part — bend over at the waist (knees giving a little), the free arm
    // carrying the basket in the crook of the elbow, the other hand reaching down and back to the basket
    private bool Forage(Transform root, float k, float wave)
    {
        if (held != "basket" && held != "spade" && held != "pole") return false;
        // ease into the stoop and back up at the end
        float env = motion == null ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Min(k / 0.15f, (1f - k) / 0.15f));
        float stoop = motion switch { "gather" => 70f, "pluck" => 25f, "dig" => 40f, "poke" => -8f, _ => 0f } * env;
        Bend(HumanBodyBones.Spine, stoop * 0.55f, root.right);
        Bend(HumanBodyBones.Chest, stoop * 0.3f, root.right);
        Bend(HumanBodyBones.Neck, -stoop * 0.35f, root.right); // keep looking at what you're picking
        foreach (var (upper, lower) in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg) })
        {
            Bend(upper, -Mathf.Max(0f, stoop) * 0.22f, root.right);
            Bend(lower, Mathf.Max(0f, stoop) * 0.32f, root.right);
        }
        float reach = Mathf.Abs(wave);
        switch (motion ?? "")
        {
            case "gather":
                Bend(HumanBodyBones.RightUpperArm, -(15f + 45f * reach) * env, root.right);
                Bend(HumanBodyBones.RightLowerArm, -15f - 30f * (1f - reach), root.right);
                break;
            case "pluck":
                Bend(HumanBodyBones.RightUpperArm, -(55f + 30f * reach) * env, root.right);
                Bend(HumanBodyBones.RightLowerArm, -25f, root.right);
                break;
            case "dig": // both hands on the handle, chopping down
                foreach (var b in new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm })
                    Bend(b, (-55f + 40f * Mathf.Max(0f, wave)) * env, root.right);
                Bend(HumanBodyBones.RightLowerArm, -25f, root.right);
                Bend(HumanBodyBones.LeftLowerArm, -35f, root.right);
                break;
            case "poke": // the pole held high, jabbing up at the nest
                Bend(HumanBodyBones.RightUpperArm, (-140f + 18f * wave) * env, root.right);
                Bend(HumanBodyBones.LeftUpperArm, (-115f + 18f * wave) * env, root.right);
                Bend(HumanBodyBones.RightLowerArm, -15f, root.right);
                Bend(HumanBodyBones.LeftLowerArm, -20f, root.right);
                break;
        }
        if (held == "basket") // the basket on the left forearm
        {
            Bend(HumanBodyBones.LeftUpperArm, -20f, root.right);
            Bend(HumanBodyBones.LeftLowerArm, -75f, root.right);
        }

        var hand = animator.GetBoneTransform(held == "basket" ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if (hand == null) return true;
        Vector3 pos = hand.position;
        Quaternion rot = root.rotation;
        if (held == "basket") pos = hand.position + Vector3.down * 0.1f - root.right * 0.06f;
        else if (held == "spade") rot = root.rotation * Quaternion.Euler(55f + 35f * Mathf.Max(0f, wave), 0f, 0f);
        else rot = root.rotation * Quaternion.Euler(-62f + 5f * wave, 0f, 0f);
        prop.SetPositionAndRotation(pos, rot);
        return true;
    }

    // Jobs: a loop that runs as long as the shift (time-based, not a 0..1 motion)
    private bool WorkPose(Transform root)
    {
        if (held != "hands" && held != "screwdriver" && held != "marker" && held != "wrench" && held != "whistle" && held != "stethoscope") return false;
        float t = Time.time - motionStart;
        var R = HumanBodyBones.RightUpperArm; var RL = HumanBodyBones.RightLowerArm;
        var L = HumanBodyBones.LeftUpperArm; var LL = HumanBodyBones.LeftLowerArm;
        Vector3 propTilt = Vector3.zero;
        switch (motion ?? "")
        {
            case "type": // both hands on the keys, fingers pecking in turn, eyes on the screen
                Bend(R, -38f + 5f * Mathf.Sin(t * 13f), root.right);
                Bend(L, -38f + 5f * Mathf.Sin(t * 13f + 2.2f), root.right);
                Bend(RL, -55f, root.right);
                Bend(LL, -55f, root.right);
                Bend(HumanBodyBones.Neck, 10f, root.right);
                break;
            case "repairpc": // reach into the open case and turn the screwdriver; now and then type a test
                bool typing = t % 9f > 6f;
                if (typing) goto case "type";
                Bend(HumanBodyBones.Spine, 14f, root.right);
                Bend(R, -50f + 4f * Mathf.Sin(t * 2f), root.right);
                Bend(RL, -35f, root.right);
                Bend(L, -42f, root.right);
                Bend(LL, -45f, root.right);
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var fore = animator.GetBoneTransform(RL);
                if (hand != null && fore != null) hand.rotation = Quaternion.AngleAxis(45f * Mathf.Sin(t * 6f), (hand.position - fore.position).normalized) * hand.rotation;
                propTilt = new Vector3(70f, 0f, 45f * Mathf.Sin(t * 6f));
                Bend(HumanBodyBones.Neck, 18f, root.right);
                break;
            case "write": // marker on the board, little strokes left to right
                Bend(R, -105f + 8f * Mathf.Sin(t * 7f), root.right);
                Bend(R, 12f * Mathf.Sin(t * 1.3f), root.up);
                Bend(RL, -30f, root.right);
                Bend(L, -10f, root.right);
                propTilt = new Vector3(-80f, 0f, 0f);
                break;
            case "explain": // turned to the class: point back at the board, open hand to the students
                Bend(R, -70f + 15f * Mathf.Sin(t * 2.4f), root.right);
                Bend(RL, -25f, root.right);
                Bend(L, -45f + 20f * Mathf.Sin(t * 2.4f + 1.5f), root.right);
                Bend(LL, -50f, root.right);
                propTilt = new Vector3(-40f, 0f, 0f);
                break;
            case "wrench": // bent over the bike, ratcheting a nut, the other hand steadying the frame
                Bend(HumanBodyBones.Spine, 30f, root.right);
                Bend(HumanBodyBones.Chest, 15f, root.right);
                Bend(HumanBodyBones.Neck, -12f, root.right);
                foreach (var (upper, lower) in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg) })
                {
                    Bend(upper, -25f, root.right);
                    Bend(lower, 40f, root.right);
                }
                Bend(R, -55f + 22f * Mathf.Sin(t * 5f), root.right);
                Bend(RL, -30f, root.right);
                Bend(L, -50f, root.right);
                Bend(LL, -20f, root.right);
                propTilt = new Vector3(60f + 25f * Mathf.Sin(t * 5f), 0f, 0f);
                break;
            case "whistle": // arm raises with the kids; every few seconds the whistle to the lips
                bool blow = t % 5f > 4f;
                if (blow)
                {
                    Bend(R, -75f, root.right);
                    Bend(RL, -115f, root.right);
                    Bend(L, -10f, root.right);
                }
                else
                {
                    float up = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                    Bend(R, -20f - 140f * up, root.right);
                    Bend(L, -20f - 140f * up, root.right);
                }
                break;
            case "examine": // stooping a little, the chest piece moved from place to place
                Bend(HumanBodyBones.Spine, 16f, root.right);
                Bend(HumanBodyBones.Neck, 10f, root.right);
                Bend(R, -62f + 6f * Mathf.Sin(t * 0.9f), root.right);
                Bend(R, 10f * Mathf.Sin(t * 0.6f), root.up);
                Bend(RL, -30f, root.right);
                Bend(L, -30f, root.right);
                Bend(LL, -60f, root.right);
                propTilt = new Vector3(0f, 0f, 90f);
                break;
        }
        var rh = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rh != null) prop.SetPositionAndRotation(rh.position, root.rotation * Quaternion.Euler(propTilt));
        return true;
    }

    private static float Cycles(string m) => m switch { "gather" => 3f, "pluck" => 3f, "dig" => 4f, "poke" => 4f, "chop" => 3f, "plant" => 3f, "sweep" => 3f, "scatter" => 3f, "milk" => 5f, "reel" => 4f, "strum" => 6f, _ => 1f };

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
            case "basket": // ตะกร้าไม้ไผ่ with a few things in it already
                Part(p, PrimitiveType.Cylinder, Mat("PropBasket", new Color(0.75f, 0.6f, 0.35f)), new Vector3(0f, -0.12f, 0f), new Vector3(0.34f, 0.12f, 0.34f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, Mat("PropBasketRim", new Color(0.6f, 0.45f, 0.25f)), new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.015f, 0.36f), Vector3.zero);
                Part(p, PrimitiveType.Sphere, Mat("PropForage", new Color(0.8f, 0.75f, 0.6f)), new Vector3(0.04f, -0.01f, 0.02f), new Vector3(0.22f, 0.07f, 0.2f), Vector3.zero);
                Part(p, PrimitiveType.Sphere, Mat("PropGreens", new Color(0.45f, 0.75f, 0.3f)), new Vector3(-0.07f, 0f, -0.05f), new Vector3(0.14f, 0.06f, 0.12f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, Mat("PropBasketRim", new Color(0.6f, 0.45f, 0.25f)), new Vector3(0f, 0.16f, 0f), new Vector3(0.02f, 0.17f, 0.02f), new Vector3(0f, 0f, 90f));
                break;
            case "spade": // เสียม: a long wooden handle with a narrow iron blade
                Part(p, PrimitiveType.Cylinder, wood, new Vector3(0f, 0f, 0.45f), new Vector3(0.04f, 0.65f, 0.04f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cube, steel, new Vector3(0f, 0f, 1.18f), new Vector3(0.14f, 0.02f, 0.24f), Vector3.zero);
                break;
            case "pole": // a long bamboo pole with a cloth net bag (สวิง) at the top for the ant eggs
                Part(p, PrimitiveType.Cylinder, bamboo, new Vector3(0f, 0f, 1.2f), new Vector3(0.035f, 1.5f, 0.035f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cylinder, Mat("PropRim", new Color(0.4f, 0.3f, 0.2f)), new Vector3(0f, 0f, 2.7f), new Vector3(0.32f, 0.01f, 0.32f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Sphere, Mat("PropNet", new Color(0.9f, 0.88f, 0.8f)), new Vector3(0f, -0.15f, 2.68f), new Vector3(0.3f, 0.32f, 0.3f), Vector3.zero);
                break;
            case "hands": // typing: nothing in hand
                break;
            case "screwdriver":
                Part(p, PrimitiveType.Cylinder, Mat("PropHandleRed", new Color(0.85f, 0.15f, 0.12f)), new Vector3(0f, 0f, 0.04f), new Vector3(0.03f, 0.045f, 0.03f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cylinder, steel, new Vector3(0f, 0f, 0.13f), new Vector3(0.008f, 0.06f, 0.008f), new Vector3(90f, 0f, 0f));
                break;
            case "marker": // whiteboard marker
                Part(p, PrimitiveType.Cylinder, Mat("PropMarker", new Color(0.15f, 0.3f, 0.8f)), new Vector3(0f, 0f, 0.05f), new Vector3(0.022f, 0.065f, 0.022f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cylinder, Mat("PropMarkerCap", Color.white), new Vector3(0f, 0f, -0.02f), new Vector3(0.024f, 0.012f, 0.024f), new Vector3(90f, 0f, 0f));
                break;
            case "wrench": // ประแจ
                Part(p, PrimitiveType.Cube, steel, new Vector3(0f, 0f, 0.1f), new Vector3(0.025f, 0.012f, 0.22f), Vector3.zero);
                Part(p, PrimitiveType.Cylinder, steel, new Vector3(0f, 0f, 0.23f), new Vector3(0.06f, 0.012f, 0.06f), Vector3.zero);
                break;
            case "whistle": // on a lanyard
                Part(p, PrimitiveType.Cube, Mat("PropWhistle", new Color(0.95f, 0.75f, 0.1f)), new Vector3(0f, 0f, 0.03f), new Vector3(0.025f, 0.025f, 0.06f), Vector3.zero);
                break;
            case "stethoscope": // the chest piece in the hand, the tube up to the ears
                Part(p, PrimitiveType.Cylinder, steel, new Vector3(0f, 0f, 0.05f), new Vector3(0.045f, 0.008f, 0.045f), new Vector3(90f, 0f, 0f));
                Part(p, PrimitiveType.Cylinder, Mat("PropTube", new Color(0.1f, 0.1f, 0.12f)), new Vector3(0f, 0.15f, 0.02f), new Vector3(0.012f, 0.15f, 0.012f), new Vector3(-10f, 0f, 0f));
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
