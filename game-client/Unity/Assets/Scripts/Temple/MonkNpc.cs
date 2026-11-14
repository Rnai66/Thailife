using UnityEngine;

// A monk on the morning alms round (พระบิณฑบาต, MonkProcession): the body model with a shaven head and
// the saffron robe (Look: หัวโล้น + จีวร), worn ห่มคลุม — over both shoulders, as monks wear it outside
// the temple — with a robe collar, barefoot. Before the belly, held in both hands, the black alms bowl
// (บาตร) in its cloth bag (ถลกบาตร) hung on a cord over the left shoulder, its lid (ฝาบาตร) on top.
// To receive (Receive) he stops, turns to the giver, lifts the lid in the left hand and holds the bowl
// out a little; then he gives the blessing (ให้พร) and closes it. Posed by aiming the arm bones at the
// bowl after the animator has moved them, so it fits whichever body model.
public class MonkNpc : MonoBehaviour
{
    private static readonly Color Robe = new Color(0.86f, 0.46f, 0.1f);
    private static readonly Color RobeDark = new Color(0.72f, 0.36f, 0.07f);
    private static readonly Color BowlBlack = new Color(0.05f, 0.05f, 0.05f);

    private static readonly string[] Blessings =
    {
        "อายุ วัณโณ สุขัง พะลัง", "ยะถา วาริวะหา ปูรา...", "สัพพีติโย วิวัชชันตุ", "เจริญพร ขอให้เจริญสุข",
    };

    public int Index { get; private set; }
    public bool Receiving => Time.time < receiveUntil;

    private Animator animator;
    private Transform bowl, lid, cord, props;
    private float receiveUntil, blessAt;
    private Vector3 giver;
    private bool dressed;
    private static Material robe, robeDark, black, gold, skin;

    public void Init(int index, int seed)
    {
        Index = index;
        var model = gameObject.AddComponent<CharacterModel>();
        model.SetLook(new Look { Gender = 0, Face = seed % 2, Hair = 10, Outfit = Look.Robe });
        transform.localScale = Vector3.one * (0.95f + (seed * 37 % 10) * 0.01f);
        BuildBowl();
    }

    // Stop and take the food from someone standing at `from`
    public void Receive(Vector3 from)
    {
        giver = from;
        receiveUntil = Time.time + 4f;
        blessAt = Time.time + 1.6f;
    }

    private static Material Mat(ref Material m, Color c, float smooth = 0.3f)
    {
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = c };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    private static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    // The bowl in its bag, the lid with its knob, and the cord (placed every frame in LateUpdate)
    private void BuildBowl()
    {
        props = new GameObject("AlmsBowl_บาตร").transform;
        props.SetParent(transform, false);
        bowl = new GameObject("Bowl").transform;
        bowl.SetParent(props, false);
        Part(PrimitiveType.Sphere, bowl, Vector3.zero, new Vector3(0.3f, 0.26f, 0.3f), Mat(ref black, BowlBlack, 0.85f));          // the iron bowl
        Part(PrimitiveType.Sphere, bowl, new Vector3(0f, -0.03f, 0f), new Vector3(0.32f, 0.22f, 0.32f), Mat(ref robeDark, RobeDark)); // the bag round its lower half
        Part(PrimitiveType.Cylinder, bowl, new Vector3(0f, 0.115f, 0f), new Vector3(0.235f, 0.008f, 0.235f), Mat(ref gold, new Color(0.35f, 0.3f, 0.25f), 0.6f)); // the rim
        lid = new GameObject("Lid_ฝาบาตร").transform;
        lid.SetParent(bowl, false);
        lid.localPosition = new Vector3(0f, 0.12f, 0f);
        Part(PrimitiveType.Sphere, lid, new Vector3(0f, 0.01f, 0f), new Vector3(0.24f, 0.06f, 0.24f), black);
        Part(PrimitiveType.Sphere, lid, new Vector3(0f, 0.045f, 0f), new Vector3(0.05f, 0.04f, 0.05f), black);
        cord = Part(PrimitiveType.Cylinder, props, Vector3.zero, new Vector3(0.018f, 0.3f, 0.018f), robeDark);
    }

    // ห่มคลุม: the robe over the right shoulder and arm too, and the rolled collar round the neck
    private void Dress()
    {
        dressed = true;
        Transform B(HumanBodyBones b) => animator.GetBoneTransform(b);
        var r = Mat(ref robe, Robe);
        var armR = B(HumanBodyBones.RightUpperArm);
        var foreR = B(HumanBodyBones.RightLowerArm);
        var foreL = B(HumanBodyBones.LeftLowerArm);
        var neck = B(HumanBodyBones.Neck);
        var chest = B(HumanBodyBones.Chest) ?? B(HumanBodyBones.Spine);
        float s = transform.lossyScale.y;
        if (armR != null)
        {
            Sleeve(armR, B(HumanBodyBones.RightLowerArm), r, 0.13f);
            Fit(Part(PrimitiveType.Sphere, armR, Vector3.zero, Vector3.one, r), Vector3.one * 0.15f * s); // the shoulder
        }
        if (foreR != null) Sleeve(foreR, B(HumanBodyBones.RightHand), r, 0.11f, 0.7f);
        if (foreL != null) Sleeve(foreL, B(HumanBodyBones.LeftHand), r, 0.11f, 0.7f);
        if (neck != null)
        {
            var collar = Part(PrimitiveType.Cylinder, neck, Vector3.zero, Vector3.one, robeDark);
            collar.position = neck.position - transform.up * 0.03f * s;
            collar.rotation = transform.rotation;
            Fit(collar, new Vector3(0.21f, 0.025f, 0.19f) * s);
        }
        if (chest != null)
        {
            var wrap = Part(PrimitiveType.Sphere, chest, Vector3.zero, Vector3.one, r);
            wrap.position = chest.position + transform.up * 0.05f * s;
            wrap.rotation = transform.rotation;
            Fit(wrap, new Vector3(0.42f, 0.36f, 0.3f) * s);
        }
    }

    // A cloth tube from bone a toward bone b covering `cover` of the way
    private void Sleeve(Transform a, Transform b, Material m, float thick, float cover = 1f)
    {
        if (b == null) return;
        var t = Part(PrimitiveType.Capsule, a, Vector3.zero, Vector3.one, m);
        Vector3 d = (b.position - a.position) * cover;
        t.position = a.position + d * 0.5f;
        t.rotation = Quaternion.FromToRotation(Vector3.up, d);
        float s = transform.lossyScale.y;
        Fit(t, new Vector3(thick * s, d.magnitude * 0.5f + thick * 0.3f * s, thick * s));
    }

    // Give a part this world size whatever its parents' scales
    private static void Fit(Transform t, Vector3 world)
    {
        var p = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(world.x / Mathf.Max(1e-4f, p.x), world.y / Mathf.Max(1e-4f, p.y), world.z / Mathf.Max(1e-4f, p.z));
    }

    private void Update()
    {
        if (blessAt > 0f && Time.time >= blessAt)
        {
            blessAt = 0f;
            SpeechBubble.Show(transform, Blessings[Random.Range(0, Blessings.Length)]);
        }
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) { PlaceBowlFallback(); return; }
        }
        if (!dressed) Dress();
        bool receiving = Receiving;
        if (receiving)
        {
            Vector3 to = giver - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f);
        }

        var root = transform;
        Vector3 f = root.forward, u = root.up, r = root.right;
        float s = root.lossyScale.y;
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Vector3 belly = (hips != null ? hips.position : root.position + u * 0.95f * s) + u * 0.16f * s + f * 0.24f * s;
        // offered: the bowl comes forward and a little up, the lid lifts off in the left hand
        float k = receiving ? Mathf.Clamp01(Mathf.Min(receiveUntil - Time.time, 4f - (receiveUntil - Time.time)) * 3f) : 0f;
        Vector3 at = belly + f * 0.12f * s * k + u * 0.04f * s * k;
        bowl.position = at;
        bowl.rotation = root.rotation;
        bowl.localScale = Vector3.one;
        // both hands hold the bowl from the sides; the left takes the lid when it's open
        var lidHome = at + u * 0.12f * s;
        Vector3 lidAt = Vector3.Lerp(lidHome, at - r * 0.3f * s + u * 0.12f * s, k);
        lid.position = lidAt;
        lid.rotation = root.rotation * Quaternion.Euler(0f, 0f, 70f * k);
        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + 0.25f * f + 0.1f * r, at + r * 0.15f * s - u * 0.02f * s);
        Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + 0.25f * f - 0.1f * r,
            Vector3.Lerp(at - r * 0.15f * s - u * 0.02f * s, lidAt + u * 0.03f * s, k));
        // eyes down while walking (สำรวม), up to the giver while receiving
        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head != null) head.rotation = Quaternion.AngleAxis(receiving ? 6f : 16f, r) * head.rotation;
        // the cord from the bag up over the left shoulder
        var shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder) ?? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        if (shoulder != null) Stretch(cord, at - r * 0.13f * s, shoulder.position + u * 0.04f * s, 0.018f * s);
    }

    // No body model on this machine: carry the bowl where the hands would be
    private void PlaceBowlFallback()
    {
        float s = transform.lossyScale.y;
        bowl.position = transform.position + transform.up * 1.05f * s + transform.forward * 0.28f * s;
        bowl.rotation = transform.rotation;
        cord.gameObject.SetActive(false);
    }

    private void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 upperDir, Vector3 handAt)
    {
        Aim(upper, lower, upperDir);
        var elbow = animator.GetBoneTransform(lower);
        if (elbow != null) Aim(lower, hand, handAt - elbow.position);
    }

    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir)
    {
        var t = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (t == null || c == null) return;
        Vector3 now = c.position - t.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        t.rotation = Quaternion.FromToRotation(now, dir) * t.rotation;
    }

    private static void Stretch(Transform t, Vector3 a, Vector3 b, float thick)
    {
        Vector3 d = b - a;
        t.position = (a + b) * 0.5f;
        t.rotation = Quaternion.FromToRotation(Vector3.up, d);
        Fit(t, new Vector3(thick, d.magnitude * 0.5f, thick));
    }
}
