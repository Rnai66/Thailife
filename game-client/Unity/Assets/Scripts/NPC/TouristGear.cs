using UnityEngine;

// Foreign tourists (นักท่องเที่ยวต่างชาติ) in town: a look with fair hair in holiday colours, and
// the gear — a backpack, a sun hat or cap, sunglasses, a camera on a strap — built from simple
// shapes and fixed to the bones, sized from the shoulders like OutfitParts.
public static class TouristGear
{
    private static readonly int[] FairHair = { 2, 3, 3, 3, 4, 8 }; // Look.HairColors: brown, blonde, red, silver
    private static readonly int[] HolidayTops = { 1, 3, 4, 5, 6, 7 };

    public static Look RandomLook(int seed)
    {
        var r = new System.Random(seed);
        var look = new Look
        {
            Gender = r.Next(2), Face = r.Next(2),
            Hair = 1 + r.Next(Look.HairStyles.Length - 2),
            HairColor = FairHair[r.Next(FairHair.Length)],
            Top = HolidayTops[r.Next(HolidayTops.Length)] % Look.ClothColors.Length,
            Bottom = 1 + r.Next(Mathf.Min(5, Look.ClothColors.Length - 1)),
        };
        if (look.Hair == 10) look.Hair = 2; // keep their hair: no shaved heads
        return look;
    }

    public static void Build(GameObject host, int seed)
    {
        var animator = host.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;
        var root = animator.transform;
        var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        var armL = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var armR = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if (chest == null || head == null || armL == null || armR == null) return;
        float s = Mathf.Clamp(Vector3.Distance(armL.position, armR.position) / 0.36f, 0.5f, 2f);
        var r = new System.Random(seed);
        Color[] packs = { new Color(0.85f, 0.35f, 0.1f), new Color(0.15f, 0.35f, 0.6f), new Color(0.25f, 0.45f, 0.25f), new Color(0.6f, 0.15f, 0.2f) };

        void On(Transform bone, PrimitiveType type, Vector3 offset, Vector3 size, Color color, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "TouristGear";
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
            go.transform.SetPositionAndRotation(bone.position + root.TransformDirection(offset * s), root.rotation * Quaternion.Euler(euler));
            go.transform.localScale = size * s;
            go.GetComponent<Renderer>().material.color = color;
            go.transform.SetParent(bone, true);
        }

        // Backpack with a top flap
        var pack = packs[r.Next(packs.Length)];
        On(chest, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.2f), new Vector3(0.28f, 0.36f, 0.16f), pack);
        On(chest, PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.21f), new Vector3(0.26f, 0.06f, 0.15f), pack * 0.75f);
        foreach (float x in new[] { -0.09f, 0.09f })
            On(chest, PrimitiveType.Cube, new Vector3(x, 0.03f, 0.12f), new Vector3(0.03f, 0.3f, 0.01f), pack * 0.6f);

        // Sun hat or a cap
        if (r.Next(3) > 0)
        {
            var straw = r.Next(2) == 0 ? new Color(0.9f, 0.82f, 0.6f) : new Color(0.95f, 0.95f, 0.92f);
            On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(0.4f, 0.008f, 0.4f), straw);
            On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.23f, 0.04f, 0.23f), straw);
            On(head, PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.235f, 0.01f, 0.235f), new Color(0.2f, 0.25f, 0.5f));
        }
        else
        {
            var cap = Color.HSVToRGB((float)r.NextDouble(), 0.6f, 0.85f);
            On(head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, -0.01f), new Vector3(0.23f, 0.12f, 0.24f), cap);
            On(head, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.13f), new Vector3(0.16f, 0.012f, 0.1f), cap, new Vector3(-8f, 0f, 0f));
        }
        // Sunglasses
        On(head, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.11f), new Vector3(0.15f, 0.035f, 0.01f), new Color(0.05f, 0.05f, 0.06f));
        // Camera on a strap
        if (r.Next(2) == 0)
        {
            On(chest, PrimitiveType.Cube, new Vector3(0.02f, -0.1f, 0.15f), new Vector3(0.1f, 0.065f, 0.05f), new Color(0.1f, 0.1f, 0.11f));
            On(chest, PrimitiveType.Cylinder, new Vector3(0.02f, -0.1f, 0.185f), new Vector3(0.04f, 0.02f, 0.04f), new Color(0.2f, 0.2f, 0.22f), new Vector3(90f, 0f, 0f));
        }
    }
}
