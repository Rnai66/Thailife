using UnityEngine;

// The details that make an outfit (Look.Outfits) more than a colour: buttons, epaulettes, a tie,
// boxing gloves and the มงคล, a งอบ hat, a doctor's coat tails and stethoscope... built from simple
// shapes and fixed to the body's bones, sized from the shoulders so they fit every body model.
// CharacterLook.Apply recolours the top and bottom first, then calls Build.
public static class OutfitParts
{
    private static readonly Color Gold = new Color(0.95f, 0.78f, 0.3f);
    private static readonly Color Black = new Color(0.08f, 0.08f, 0.09f);
    private static readonly Color White = new Color(0.97f, 0.97f, 0.96f);

    // Top colour for an outfit (the boxer goes bare-chested, so his top takes a skin tone)
    public static Color Top(int outfit) => outfit == 3 ? new Color(0.86f, 0.66f, 0.5f) : Look.Outfits[outfit].Color;

    public static void Build(GameObject model, int outfit)
    {
        if (model == null || outfit <= 0) return;
        var animator = model.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;
        var root = animator.transform;
        Transform Bone(HumanBodyBones b) => animator.GetBoneTransform(b);
        var chest = Bone(HumanBodyBones.UpperChest) ?? Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);
        var hips = Bone(HumanBodyBones.Hips);
        var neck = Bone(HumanBodyBones.Neck);
        var head = Bone(HumanBodyBones.Head);
        var armL = Bone(HumanBodyBones.LeftUpperArm);
        var armR = Bone(HumanBodyBones.RightUpperArm);
        if (chest == null || hips == null || armL == null || armR == null) return;

        // Body scale from the shoulders: about 0.36 m apart on an adult
        float s = Mathf.Clamp(Vector3.Distance(armL.position, armR.position) / 0.36f, 0.5f, 2f);
        float front = 0.13f * s; // chest surface in front of the chest bone

        // Shape on a bone: offset in the body's frame (x right, y up, z forward), metres at s = 1
        void On(Transform bone, PrimitiveType type, Vector3 offset, Vector3 size, Color color, Vector3 euler = default)
        {
            if (bone == null) return;
            var go = GameObject.CreatePrimitive(type);
            go.name = "OutfitPart";
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
            go.transform.SetPositionAndRotation(bone.position + root.TransformDirection(offset * s), root.rotation * Quaternion.Euler(euler));
            go.transform.localScale = size * s;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            go.transform.SetParent(bone, true);
        }
        void Buttons(Color c, int n) { for (int i = 0; i < n; i++) On(chest, PrimitiveType.Sphere, new Vector3(0f, 0.1f - i * 0.075f, front / s + 0.005f), Vector3.one * 0.022f, c); }
        void Epaulettes(Color c, Color stripe)
        {
            foreach (var (arm, x) in new[] { (armL, -1f), (armR, 1f) })
            {
                On(arm, PrimitiveType.Cube, new Vector3(x * -0.03f, 0.055f, 0f), new Vector3(0.11f, 0.018f, 0.075f), c);
                On(arm, PrimitiveType.Cube, new Vector3(x * -0.03f, 0.066f, 0f), new Vector3(0.1f, 0.006f, 0.02f), stripe);
            }
        }
        void Belt(Color c, Color buckle)
        {
            On(hips, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.33f, 0.025f, 0.24f), c);
            On(hips, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.118f), new Vector3(0.06f, 0.045f, 0.012f), buckle);
        }
        void Badge(Color c, float x, float y) => On(chest, PrimitiveType.Cylinder, new Vector3(x, y, front / s + 0.006f), new Vector3(0.045f, 0.004f, 0.045f), c, new Vector3(90f, 0f, 0f));
        void NameTag(float x) => On(chest, PrimitiveType.Cube, new Vector3(x, 0.07f, front / s + 0.006f), new Vector3(0.08f, 0.02f, 0.005f), Black);
        void Cap(Color crown, Color band, Color visor)
        {
            if (head == null) return;
            On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0f), new Vector3(0.24f, 0.035f, 0.25f), crown);
            On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.125f, 0f), new Vector3(0.2f, 0.025f, 0.21f), band);
            On(head, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0.11f), new Vector3(0.16f, 0.012f, 0.07f), visor, new Vector3(15f, 0f, 0f));
            On(head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0.125f), new Vector3(0.035f, 0.035f, 0.01f), Gold);
        }

        switch (outfit)
        {
            case 1: // ชุดปกติขาว ข้าราชการ: gold buttons, black-and-gold epaulettes, ribbons, white peaked cap
                Buttons(Gold, 5);
                Epaulettes(Black, Gold);
                On(chest, PrimitiveType.Cube, new Vector3(-0.07f, 0.08f, front / s + 0.006f), new Vector3(0.08f, 0.025f, 0.006f), new Color(0.75f, 0.15f, 0.2f)); // ribbon bar
                On(chest, PrimitiveType.Cube, new Vector3(-0.07f, 0.055f, front / s + 0.006f), new Vector3(0.08f, 0.02f, 0.006f), new Color(0.2f, 0.35f, 0.75f));
                NameTag(0.07f);
                Cap(White, Black, Black);
                break;
            case 2: // ชุดสูท: white shirt front, red tie, pocket square, black belt
                On(chest, PrimitiveType.Cube, new Vector3(0f, 0.08f, front / s), new Vector3(0.08f, 0.16f, 0.008f), White, new Vector3(0f, 0f, 0f));
                On(chest, PrimitiveType.Cube, new Vector3(0f, 0.06f, front / s + 0.008f), new Vector3(0.028f, 0.2f, 0.006f), new Color(0.7f, 0.08f, 0.12f));
                On(chest, PrimitiveType.Cube, new Vector3(0f, 0.16f, front / s + 0.008f), new Vector3(0.035f, 0.03f, 0.008f), new Color(0.7f, 0.08f, 0.12f)); // knot
                On(chest, PrimitiveType.Cube, new Vector3(-0.08f, 0.06f, front / s + 0.006f), new Vector3(0.04f, 0.015f, 0.006f), White); // pocket square
                Belt(Black, new Color(0.75f, 0.75f, 0.78f));
                break;
            case 3: // ชุดนักมวย: red gloves, มงคล on the head, ประเจียด on the arms, a gold-trimmed waistband
                foreach (var hand in new[] { Bone(HumanBodyBones.LeftHand), Bone(HumanBodyBones.RightHand) })
                    On(hand, PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0.02f), new Vector3(0.11f, 0.12f, 0.12f), new Color(0.85f, 0.1f, 0.1f));
                if (head != null)
                {
                    On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.11f, 0f), new Vector3(0.21f, 0.02f, 0.22f), White);
                    On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.115f, 0f), new Vector3(0.215f, 0.008f, 0.225f), new Color(0.85f, 0.1f, 0.1f));
                    On(head, PrimitiveType.Capsule, new Vector3(0f, 0.12f, -0.15f), new Vector3(0.03f, 0.06f, 0.03f), White, new Vector3(60f, 0f, 0f)); // tail
                }
                foreach (var arm in new[] { armL, armR })
                    On(arm, PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.1f, 0.012f, 0.1f), new Color(0.15f, 0.3f, 0.75f));
                On(hips, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.03f, 0.25f), Gold);
                break;
            case 4: // ชุดกากี อบต.: gold badge, name tag, khaki epaulettes, black belt
                Buttons(new Color(0.85f, 0.7f, 0.3f), 4);
                Epaulettes(new Color(0.5f, 0.44f, 0.28f), Gold);
                Badge(Gold, -0.08f, 0.08f);
                NameTag(0.08f);
                Belt(Black, Gold);
                break;
            case 5: // ม่อฮ่อม: ผ้าขาวม้า round the waist and a งอบ hat
                On(hips, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.045f, 0.26f), new Color(0.8f, 0.2f, 0.2f));
                On(hips, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.345f, 0.012f, 0.265f), White);
                On(hips, PrimitiveType.Capsule, new Vector3(0.12f, -0.03f, 0.08f), new Vector3(0.04f, 0.1f, 0.03f), new Color(0.8f, 0.2f, 0.2f));
                if (head != null)
                {
                    var straw = new Color(0.82f, 0.68f, 0.4f);
                    On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.012f, 0.5f), straw);
                    On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.19f, 0f), new Vector3(0.32f, 0.04f, 0.32f), straw);
                    On(head, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.18f, 0.08f, 0.18f), straw);
                }
                break;
            case 6: // เสื้อกาวน์: white coat tails, stethoscope round the neck, name tag
                foreach (float x in new[] { -0.08f, 0.08f })
                    On(hips, PrimitiveType.Cube, new Vector3(x, -0.2f, 0.11f), new Vector3(0.15f, 0.42f, 0.015f), White);
                On(hips, PrimitiveType.Cube, new Vector3(0f, -0.2f, -0.12f), new Vector3(0.3f, 0.42f, 0.015f), White);
                if (neck != null)
                {
                    On(neck, PrimitiveType.Cylinder, new Vector3(0f, -0.04f, 0.02f), new Vector3(0.15f, 0.006f, 0.13f), Black);
                    On(chest, PrimitiveType.Cylinder, new Vector3(0.05f, 0.02f, front / s + 0.01f), new Vector3(0.035f, 0.006f, 0.035f), new Color(0.8f, 0.82f, 0.85f), new Vector3(90f, 0f, 0f));
                }
                NameTag(-0.08f);
                break;
            case 7: // ราชปะแตน: gold buttons up a high collar, a gold sash belt
                Buttons(Gold, 5);
                if (neck != null) On(neck, PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0f), new Vector3(0.13f, 0.025f, 0.12f), new Color(0.55f, 0.2f, 0.45f));
                Belt(Gold, Gold);
                break;
            case 8: // ชุดกีฬา: white side stripes, sweatband
                foreach (float x in new[] { -0.15f, 0.15f })
                    On(chest, PrimitiveType.Cube, new Vector3(x, 0f, 0f), new Vector3(0.02f, 0.25f, 0.2f), White);
                if (head != null) On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.21f, 0.025f, 0.22f), White);
                break;
            case 9: // ชุดตำรวจ: badge, name tag, epaulettes, black belt, peaked cap
                Buttons(Gold, 4);
                Epaulettes(new Color(0.35f, 0.22f, 0.12f), Gold);
                Badge(Gold, -0.08f, 0.08f);
                NameTag(0.08f);
                Belt(Black, Gold);
                Cap(new Color(0.42f, 0.28f, 0.16f), Black, Black);
                break;
        }
    }

    private static readonly System.Collections.Generic.Dictionary<Color, Material> mats = new System.Collections.Generic.Dictionary<Color, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => mats.Clear();

    private static Material Mat(Color c)
    {
        if (mats.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { name = "Outfit" };
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
        mats[c] = m;
        return m;
    }
}
