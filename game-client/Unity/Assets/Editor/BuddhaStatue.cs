using UnityEngine;

// A seated Buddha (พระพุทธรูปปางมารวิชัย) in the Sukhothai manner, built from shaped meshes: sitting
// ขัดสมาธิราบ on a lotus base (right leg over the left), the right hand over the right knee touching the
// earth, the left resting palm-up in the lap; broad shoulders and a slim waist, the robe's sash over
// the left shoulder, a calm oval face with downcast eyes, long earlobes, the snail-shell curls
// (ขมวดพระเกศา), the topknot and its flame (พระรัศมีเปลว), and the gilded arch behind (ซุ้มเรือนแก้ว).
// Faces -Z. `at` = the seat (the top of the altar); `scale` 1 = about 2.1 m from seat to flame tip.
// `detail` false leaves out the curls and the face (for small shrine figures).
public static class BuddhaStatue
{
    public static Transform Build(Transform parent, Vector3 at, float scale, bool detail = true, bool arch = true)
    {
        var b = PrimitiveKit.Group("Buddha_พระพุทธรูป", parent, at).transform;
        b.localScale = Vector3.one * scale;
        var gold = PrimitiveKit.Mat("BuddhaGold", new Color(0.95f, 0.75f, 0.25f), 0.8f, 0.85f);
        var shade = PrimitiveKit.Mat("BuddhaGoldShade", new Color(0.62f, 0.42f, 0.12f), 0.7f, 0.85f);
        var dark = PrimitiveKit.Mat("BuddhaFeature", new Color(0.32f, 0.2f, 0.06f), 0.5f, 0.6f);

        // ── Lotus base (ฐานบัว): an oval drum ringed with up-turned petals ──
        MeshKit.Place("LotusBase", b, MeshKit.Lathe("Buddha_Base", new[]
        {
            new Vector2(0.62f, -0.08f), new Vector2(0.74f, -0.02f), new Vector2(0.76f, 0.04f), new Vector2(0.7f, 0.1f), new Vector2(0.64f, 0.13f),
        }, 32), gold, Vector3.zero, default, new Vector3(1f, 1f, 0.78f));
        var petal = MeshKit.Leaf("Buddha_Petal", 0.2f, 0.14f, 0.05f, 0f, 6, 0.5f);
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2f / 18f;
            var p = new Vector3(Mathf.Sin(a) * 0.66f, 0.0f, Mathf.Cos(a) * 0.66f * 0.78f);
            MeshKit.Place("Petal", b, petal, i % 2 == 0 ? gold : shade, p, new Vector3(-55f, a * Mathf.Rad2Deg, 0f));
        }

        // ── Crossed legs: the lap, the left shin underneath, the right on top with its sole up ──
        PrimitiveKit.Sphere("Lap", b, new Vector3(0f, 0.22f, -0.08f), new Vector3(1.18f, 0.3f, 0.66f), gold);
        MeshKit.Place("LeftShin", b, MeshKit.Sweep("Buddha_LeftShin",
            MeshKit.Curve(new Vector3(0.52f, 0.17f, -0.22f), new Vector3(0.15f, 0.15f, -0.42f), new Vector3(-0.3f, 0.17f, -0.36f), 8),
            new[] { 0.11f, 0.1f, 0.095f, 0.09f, 0.085f, 0.08f, 0.075f, 0.07f }, 12), gold, Vector3.zero);
        MeshKit.Place("RightShin", b, MeshKit.Sweep("Buddha_RightShin",
            MeshKit.Curve(new Vector3(-0.54f, 0.2f, -0.24f), new Vector3(-0.1f, 0.3f, -0.46f), new Vector3(0.3f, 0.3f, -0.3f), 8),
            new[] { 0.115f, 0.105f, 0.1f, 0.095f, 0.09f, 0.085f, 0.08f, 0.075f }, 12), gold, Vector3.zero);
        PrimitiveKit.Sphere("RightSole", b, new Vector3(0.34f, 0.33f, -0.29f), new Vector3(0.26f, 0.06f, 0.12f), gold);

        // ── Body: slim waist, swelling chest, broad rounded shoulders ──
        MeshKit.Place("Torso", b, MeshKit.Lathe("Buddha_Torso", new[]
        {
            new Vector2(0.3f, 0f), new Vector2(0.29f, 0.12f), new Vector2(0.31f, 0.28f), new Vector2(0.37f, 0.45f),
            new Vector2(0.43f, 0.6f), new Vector2(0.45f, 0.68f), new Vector2(0.4f, 0.76f), new Vector2(0.24f, 0.82f), new Vector2(0.1f, 0.84f),
        }, 28), gold, new Vector3(0f, 0.26f, 0.02f), default, new Vector3(1f, 1f, 0.66f));
        // the robe's folded sash (สังฆาฏิ) over the left shoulder, down to the navel; the robe's edge
        PrimitiveKit.Box("Sash", b, new Vector3(0.17f, 0.78f, -0.22f), new Vector3(0.11f, 0.62f, 0.03f), shade, new Vector3(-8f, 0f, -14f), collider: false);
        PrimitiveKit.Box("SashBack", b, new Vector3(0.2f, 0.85f, 0.25f), new Vector3(0.11f, 0.5f, 0.03f), shade, new Vector3(8f, 0f, -10f), collider: false);
        MeshKit.Place("RobeEdge", b, MeshKit.Sweep("Buddha_RobeEdge",
            MeshKit.Curve(new Vector3(0.36f, 1.0f, -0.12f), new Vector3(-0.05f, 0.75f, -0.32f), new Vector3(-0.3f, 0.4f, -0.22f), 8),
            MeshKit.Taper(8, 0.012f, 0.012f), 6), shade, Vector3.zero);
        MeshKit.Place("Neck", b, MeshKit.Lathe("Buddha_Neck", new[] { new Vector2(0.11f, 0f), new Vector2(0.095f, 0.08f), new Vector2(0.1f, 0.16f) }, 16),
            gold, new Vector3(0f, 1.06f, 0.02f));
        for (int i = 0; i < 3; i++) // the three lines of the neck
            MeshKit.Place("NeckLine", b, MeshKit.Lathe("Buddha_NeckLine", new[] { new Vector2(0.103f, 0f), new Vector2(0.108f, 0.006f), new Vector2(0.103f, 0.012f) }, 16, false, false),
                shade, new Vector3(0f, 1.08f + i * 0.035f, 0.02f));

        // ── Arms ──
        // right (the statue's right is -X): down the side, forearm forward, the hand hanging over the knee
        MeshKit.Place("RightUpperArm", b, MeshKit.Sweep("Buddha_RUpperArm",
            MeshKit.Curve(new Vector3(-0.4f, 0.98f, 0.02f), new Vector3(-0.52f, 0.8f, 0.04f), new Vector3(-0.5f, 0.56f, -0.02f), 7),
            new[] { 0.1f, 0.095f, 0.088f, 0.082f, 0.078f, 0.075f, 0.072f }, 12), gold, Vector3.zero);
        MeshKit.Place("RightForearm", b, MeshKit.Sweep("Buddha_RForearm",
            MeshKit.Curve(new Vector3(-0.5f, 0.56f, -0.02f), new Vector3(-0.53f, 0.42f, -0.2f), new Vector3(-0.52f, 0.33f, -0.36f), 7),
            new[] { 0.072f, 0.066f, 0.06f, 0.055f, 0.05f, 0.046f, 0.043f }, 12), gold, Vector3.zero);
        Hand(b, gold, new Vector3(-0.52f, 0.24f, -0.42f), new Vector3(20f, 0f, 0f)); // fingers down to the earth
        // left: the forearm comes in, the hand lies palm-up in the lap
        MeshKit.Place("LeftUpperArm", b, MeshKit.Sweep("Buddha_LUpperArm",
            MeshKit.Curve(new Vector3(0.4f, 0.98f, 0.02f), new Vector3(0.52f, 0.8f, 0.04f), new Vector3(0.48f, 0.56f, -0.02f), 7),
            new[] { 0.1f, 0.095f, 0.088f, 0.082f, 0.078f, 0.075f, 0.072f }, 12), gold, Vector3.zero);
        MeshKit.Place("LeftForearm", b, MeshKit.Sweep("Buddha_LForearm",
            MeshKit.Curve(new Vector3(0.48f, 0.56f, -0.02f), new Vector3(0.4f, 0.4f, -0.18f), new Vector3(0.14f, 0.36f, -0.26f), 7),
            new[] { 0.072f, 0.066f, 0.06f, 0.055f, 0.05f, 0.046f, 0.043f }, 12), gold, Vector3.zero);
        var palm = PrimitiveKit.Sphere("LeftHand", b, new Vector3(0.03f, 0.355f, -0.27f), new Vector3(0.2f, 0.05f, 0.12f), gold);
        palm.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);

        // ── Head: oval face, long earlobes, curls, topknot and flame ──
        var head = PrimitiveKit.Group("Head", b, new Vector3(0f, 1.36f, 0f)).transform;
        PrimitiveKit.Sphere("Face", head, Vector3.zero, new Vector3(0.34f, 0.42f, 0.37f), gold);
        PrimitiveKit.Sphere("Chin", head, new Vector3(0f, -0.13f, -0.08f), new Vector3(0.16f, 0.12f, 0.14f), gold);
        foreach (float sx in new[] { -1f, 1f })
            MeshKit.Place(sx < 0 ? "RightEar" : "LeftEar", head, MeshKit.Sweep(sx < 0 ? "Buddha_EarR" : "Buddha_EarL",
                MeshKit.Curve(new Vector3(sx * 0.165f, 0.06f, 0.03f), new Vector3(sx * 0.2f, -0.08f, 0.04f), new Vector3(sx * 0.19f, -0.24f, 0.02f), 7),
                new[] { 0.035f, 0.04f, 0.038f, 0.035f, 0.035f, 0.04f, 0.03f }, 8), gold, Vector3.zero);
        if (detail)
        {
            foreach (float sx in new[] { -1f, 1f })
            {
                // arched brows sweeping into the nose, eyes cast down as half-closed lids
                MeshKit.Place("Brow", head, MeshKit.Sweep(sx < 0 ? "Buddha_BrowR" : "Buddha_BrowL",
                    MeshKit.Curve(new Vector3(sx * 0.015f, 0.04f, -0.183f), new Vector3(sx * 0.06f, 0.085f, -0.178f), new Vector3(sx * 0.125f, 0.05f, -0.15f), 7),
                    MeshKit.Taper(7, 0.007f, 0.005f), 6), shade, Vector3.zero);
                MeshKit.Place("Eye", head, MeshKit.Sweep(sx < 0 ? "Buddha_EyeR" : "Buddha_EyeL",
                    MeshKit.Curve(new Vector3(sx * 0.03f, 0.012f, -0.178f), new Vector3(sx * 0.065f, -0.0f, -0.18f), new Vector3(sx * 0.105f, 0.016f, -0.158f), 7),
                    MeshKit.Taper(7, 0.006f, 0.004f), 6), dark, Vector3.zero);
            }
            PrimitiveKit.Sphere("Nose", head, new Vector3(0f, -0.04f, -0.185f), new Vector3(0.05f, 0.1f, 0.05f), gold);
            MeshKit.Place("Lips", head, MeshKit.Sweep("Buddha_Lips",
                MeshKit.Curve(new Vector3(-0.035f, -0.11f, -0.17f), new Vector3(0f, -0.118f, -0.182f), new Vector3(0.035f, -0.11f, -0.17f), 7),
                new[] { 0.006f, 0.009f, 0.011f, 0.012f, 0.011f, 0.009f, 0.006f }, 6), shade, Vector3.zero);
            PrimitiveKit.Sphere("Urna", head, new Vector3(0f, 0.075f, -0.185f), Vector3.one * 0.016f, shade);
            // the curls (ขมวดพระเกศา): rings of small knobs over the crown, above the hairline in front
            for (int ring = 0; ring < 6; ring++)
            {
                float lat = Mathf.Lerp(18f, 78f, ring / 5f) * Mathf.Deg2Rad;
                int count = Mathf.Max(6, Mathf.RoundToInt(26 * Mathf.Cos(lat)));
                for (int k = 0; k < count; k++)
                {
                    float lon = (k + ring * 0.5f) * Mathf.PI * 2f / count;
                    var dir = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                    if (dir.z < -0.35f && dir.y < 0.62f) continue; // the forehead stays smooth
                    var p = new Vector3(dir.x * 0.17f, dir.y * 0.21f, dir.z * 0.185f);
                    PrimitiveKit.Sphere("Curl", head, p, Vector3.one * 0.045f, shade);
                }
            }
        }
        else PrimitiveKit.Sphere("Hair", head, new Vector3(0f, 0.06f, 0.01f), new Vector3(0.355f, 0.36f, 0.38f), shade);
        PrimitiveKit.Sphere("Topknot", head, new Vector3(0f, 0.2f, 0.01f), new Vector3(0.17f, 0.13f, 0.17f), detail ? shade : gold);
        MeshKit.Place("Flame", head, MeshKit.Lathe("Buddha_Flame", new[]
        {
            new Vector2(0.055f, 0f), new Vector2(0.07f, 0.05f), new Vector2(0.06f, 0.1f), new Vector2(0.035f, 0.2f),
            new Vector2(0.018f, 0.3f), new Vector2(0.006f, 0.38f), new Vector2(0f, 0.42f),
        }, 16), gold, new Vector3(0f, 0.25f, 0.01f));

        // ── The arch behind (ซุ้มเรือนแก้ว): a gilded frame edged with flames ──
        if (arch)
        {
            var path = MeshKit.Curve(new Vector3(-0.92f, -0.05f, 0.42f), new Vector3(0f, 3.6f, 0.42f), new Vector3(0.92f, -0.05f, 0.42f), 17);
            MeshKit.Place("Arch", b, MeshKit.Sweep("Buddha_Arch", path, MeshKit.Taper(17, 0.06f, 0.06f), 8), gold, Vector3.zero);
            var flame = MeshKit.Lathe("Buddha_ArchFlame", new[] { new Vector2(0.04f, 0f), new Vector2(0.045f, 0.04f), new Vector2(0.02f, 0.13f), new Vector2(0f, 0.2f) }, 8);
            for (int i = 1; i < path.Length - 1; i++)
            {
                Vector3 along = (path[i + 1] - path[i - 1]).normalized;
                Vector3 outward = new Vector3(-along.y, along.x, 0f); // left of the way round = away from the middle
                if (Vector3.Dot(outward, path[i] - new Vector3(0f, 0.8f, 0.42f)) < 0f) outward = -outward;
                float tilt = Mathf.Atan2(-outward.x, outward.y) * Mathf.Rad2Deg;
                MeshKit.Place("ArchFlame", b, flame, gold, path[i] + outward * 0.04f, new Vector3(0f, 0f, tilt));
            }
            PrimitiveKit.Box("ArchBack", b, new Vector3(0f, 0.8f, 0.46f), new Vector3(1.6f, 1.7f, 0.02f), PrimitiveKit.Mat("BuddhaArchRed", new Color(0.42f, 0.06f, 0.05f), 0.5f), collider: false);
        }
        return b;
    }

    // A hand: palm and four fingers together, hanging down (euler turns it)
    private static void Hand(Transform b, Material gold, Vector3 at, Vector3 euler)
    {
        var h = PrimitiveKit.Group("RightHand", b, at).transform;
        h.localRotation = Quaternion.Euler(euler);
        PrimitiveKit.Sphere("Palm", h, new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.12f, 0.045f), gold);
        for (int f = 0; f < 4; f++)
            PrimitiveKit.Sphere("Finger", h, new Vector3(-0.03f + f * 0.02f, -0.05f, 0f), new Vector3(0.02f, 0.11f, 0.025f), gold);
    }
}
