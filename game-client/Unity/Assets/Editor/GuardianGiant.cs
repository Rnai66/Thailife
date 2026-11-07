using UnityEngine;

// ยักษ์ทวารบาล: a temple guardian giant after the ones at the gates of วัดพระแก้ว — about six metres
// on a stepped plinth, standing square with both hands resting on a great club planted between the
// feet. Glazed colour skin (the giants are clad in coloured porcelain and glass), gold armour: the
// pointed shoes, the knee guards, the pleated front panel and the hip flaps flaring out (ชายไหว), a
// jewelled belt, the breastplate with its pendant (ทับทรวง), the shoulder flames curling up (อินทรธนู).
// The face: bulging eyes under heavy brows, a broad nose, a wide red mouth with tusks turned up at the
// corners, flared ear ornaments (กรรเจียก). On top the tall tiered crown (ชฎา) set with glass —
// style 0 the pointed ยอดชัย spire, style 1 the curled ยอดหางไก่ crest, style 2 the flame-leaf ยอดกระหนก.
// Parts are merged by material. The front faces local -Z.
public static class GuardianGiant
{
    public static void Build(Transform parent, Vector3 at, Color skinColor, Color flapColor, int style, string key, float yaw = 0f)
    {
        var y = PrimitiveKit.Group("Yaksha_ยักษ์", parent, at, yaw).transform;
        var g = PrimitiveKit.Group("Figure", y, Vector3.zero).transform;
        string hex = ColorUtility.ToHtmlStringRGB(skinColor);
        var skin = PrimitiveKit.Mat("YakshaSkin" + hex, skinColor, 0.78f, 0.15f);              // glazed
        var leg = PrimitiveKit.Mat("YakshaLeg" + hex, new Color(skinColor.r * 0.78f, skinColor.g * 0.78f, skinColor.b * 0.78f), 0.7f, 0.15f);
        var gold = PrimitiveKit.Mat("YakshaGold", new Color(0.93f, 0.72f, 0.26f), 0.85f, 0.85f);
        var flap = PrimitiveKit.Mat("YakshaFlap" + ColorUtility.ToHtmlStringRGB(flapColor), flapColor, 0.75f, 0.2f);
        var white = PrimitiveKit.Mat("YakshaWhite", new Color(0.97f, 0.96f, 0.92f), 0.7f);
        var black = PrimitiveKit.Mat("YakshaBlack", new Color(0.04f, 0.04f, 0.04f), 0.9f);
        var lip = PrimitiveKit.Mat("YakshaLip", new Color(0.75f, 0.08f, 0.06f), 0.7f);
        var plinth = PrimitiveKit.Mat("TempleWhite", new Color(0.95f, 0.94f, 0.9f));
        var club = PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f);
        Material[] glass =
        {
            PrimitiveKit.Mat("YakshaGlassRed", new Color(0.85f, 0.1f, 0.12f), 0.95f, 0.3f),
            PrimitiveKit.Mat("YakshaGlassGreen", new Color(0.1f, 0.6f, 0.35f), 0.95f, 0.3f),
            PrimitiveKit.Mat("YakshaGlassBlue", new Color(0.15f, 0.35f, 0.85f), 0.95f, 0.3f),
        };
        Mesh cube = RoyalBarges.Cube, ball = RoyalBarges.Ball, cone = RoyalBarges.Cone, rod = RoyalBarges.Rod;
        GameObject P(string n, Mesh m, Material mat, Vector3 p, Vector3 s, Vector3 e = default) => RoyalBarges.Part(n, g, m, mat, p, s, e);
        void S(string n, Material mat, Vector3 a, Vector3 b, float r) => RoyalBarges.Stick(n, g, mat, a, b, r);

        // the stepped plinth (redented look: two tiers and a gold band)
        P("Plinth", cube, plinth, new Vector3(0f, 0.25f, 0f), new Vector3(2.5f, 0.5f, 2.5f));
        P("PlinthBand", cube, gold, new Vector3(0f, 0.53f, 0f), new Vector3(2.35f, 0.07f, 2.35f));
        P("PlinthTop", cube, plinth, new Vector3(0f, 0.72f, 0f), new Vector3(2.1f, 0.36f, 2.1f));
        const float B = 0.9f;

        // feet in pointed gold shoes, legs in patterned trousers, gold knee guards
        foreach (float s in new[] { -1f, 1f })
        {
            P("Shoe", cube, gold, new Vector3(s * 0.42f, B + 0.11f, -0.08f), new Vector3(0.34f, 0.22f, 0.6f));
            P("Toe", cone, gold, new Vector3(s * 0.42f, B + 0.12f, -0.36f), new Vector3(0.22f, 0.32f, 0.16f), new Vector3(-75f, 0f, 0f));
            P("Ankle", rod, gold, new Vector3(s * 0.42f, B + 0.2f, -0.05f), new Vector3(0.4f, 0.1f, 0.4f));
            P("Shin", cube, leg, new Vector3(s * 0.42f, B + 0.72f, -0.02f), new Vector3(0.36f, 0.95f, 0.38f));
            P("Knee", ball, gold, new Vector3(s * 0.41f, B + 1.2f, -0.12f), new Vector3(0.36f, 0.3f, 0.3f));
            P("Thigh", cube, leg, new Vector3(s * 0.37f, B + 1.62f, 0f), new Vector3(0.44f, 0.85f, 0.46f), new Vector3(0f, 0f, s * -5f));
            // ชายไหว: hip flaps flaring out and up, coloured, edged in gold
            P("HipFlap", cube, flap, new Vector3(s * 0.78f, B + 1.95f, 0f), new Vector3(0.62f, 0.1f, 0.5f), new Vector3(0f, 0f, s * 28f));
            P("HipFlapEdge", cube, gold, new Vector3(s * 1.06f, B + 2.1f, 0f), new Vector3(0.14f, 0.12f, 0.54f), new Vector3(0f, 0f, s * 28f));
            P("SidePanel", cube, flap, new Vector3(s * 0.5f, B + 1.55f, 0f), new Vector3(0.08f, 0.85f, 0.55f), new Vector3(0f, 0f, s * 8f));
        }
        // the pleated front panel and the belt with its jewels
        P("FrontPanel", cube, gold, new Vector3(0f, B + 1.5f, -0.36f), new Vector3(0.5f, 1.05f, 0.05f));
        P("FrontPanelTip", cone, gold, new Vector3(0f, B + 0.95f, -0.36f), new Vector3(0.5f, 0.25f, 0.05f), new Vector3(180f, 0f, 0f));
        for (int i = 0; i < 4; i++)
            P("PanelGem", ball, glass[i % 3], new Vector3(0f, B + 1.9f - i * 0.25f, -0.4f), Vector3.one * 0.1f);
        P("Belt", rod, gold, new Vector3(0f, B + 2.0f, 0f), new Vector3(1.1f, 0.16f, 0.82f));
        for (int i = 0; i < 5; i++)
            P("BeltGem", ball, glass[i % 3], new Vector3(-0.36f + i * 0.18f, B + 2.08f, -0.42f), Vector3.one * 0.09f);

        // torso: waist, chest, gold breastplate with the pendant, collar
        P("Waist", cube, skin, new Vector3(0f, B + 2.25f, 0f), new Vector3(0.86f, 0.36f, 0.62f));
        P("Chest", cube, skin, new Vector3(0f, B + 2.75f, 0f), new Vector3(1.1f, 0.8f, 0.7f));
        P("Breastplate", cube, gold, new Vector3(0f, B + 2.72f, -0.36f), new Vector3(0.82f, 0.62f, 0.05f));
        P("Pendant", cone, gold, new Vector3(0f, B + 2.42f, -0.39f), new Vector3(0.3f, 0.35f, 0.06f), new Vector3(180f, 0f, 0f));
        P("PendantGem", ball, glass[0], new Vector3(0f, B + 2.55f, -0.42f), Vector3.one * 0.13f);
        P("Collar", rod, gold, new Vector3(0f, B + 3.08f, 0f), new Vector3(0.95f, 0.1f, 0.75f));
        foreach (float s in new[] { -1f, 1f })
            for (int i = 0; i < 3; i++)
                P("ChestGem", ball, glass[(i + 1) % 3], new Vector3(s * (0.15f + i * 0.11f), B + 2.95f, -0.4f), Vector3.one * 0.08f);

        // shoulders, the curling shoulder flames, arms down to both hands on the club
        foreach (float s in new[] { -1f, 1f })
        {
            P("Shoulder", ball, skin, new Vector3(s * 0.62f, B + 3.05f, 0f), Vector3.one * 0.46f);
            P("Epaulette", ball, gold, new Vector3(s * 0.66f, B + 3.12f, 0f), new Vector3(0.5f, 0.2f, 0.42f));
            var flame = MeshKit.Curve(new Vector3(s * 0.62f, B + 3.2f, 0.05f), new Vector3(s * 1.15f, B + 3.55f, 0.1f), new Vector3(s * 0.9f, B + 4.15f, 0.12f), 9);
            MeshKit.Place("ShoulderFlame", g, MeshKit.Sweep($"Yaksha/Flame{(s < 0f ? "L" : "R")}", flame, MeshKit.Taper(9, 0.12f, 0.02f), 6), gold, Vector3.zero);
            var shoulder = new Vector3(s * 0.7f, B + 3.0f, 0f);
            var elbow = new Vector3(s * 0.7f, B + 2.3f, -0.22f);
            var hand = new Vector3(s * 0.17f, B + 2.42f, -0.66f);
            S("UpperArm", skin, shoulder, elbow, 0.18f);
            S("Forearm", skin, elbow, hand, 0.16f);
            P("Armband", rod, gold, shoulder + (elbow - shoulder) * 0.35f, new Vector3(0.42f, 0.09f, 0.42f));
            P("Bracelet", rod, gold, elbow + (hand - elbow) * 0.75f, new Vector3(0.38f, 0.1f, 0.38f), new Vector3(70f, s * -50f, 0f));
            P("Hand", ball, skin, hand + new Vector3(0f, 0.04f, 0f), new Vector3(0.26f, 0.24f, 0.3f));
        }
        // the club: planted in front, both hands on its head
        S("Club", club, new Vector3(0f, B, -0.66f), new Vector3(0f, B + 2.35f, -0.66f), 0.14f);
        P("ClubHead", rod, club, new Vector3(0f, B + 2.0f, -0.66f), new Vector3(0.42f, 0.42f, 0.42f));
        foreach (float h in new[] { 0.3f, 1.2f, 2.0f, 2.42f })
            P("ClubBand", rod, gold, new Vector3(0f, B + h, -0.66f), new Vector3(h > 1.9f ? 0.46f : 0.32f, 0.07f, h > 1.9f ? 0.46f : 0.32f));
        P("ClubCap", cone, gold, new Vector3(0f, B + 2.47f, -0.66f), new Vector3(0.3f, 0.3f, 0.3f));

        // the head
        float hy = B + 3.72f;
        P("Neck", rod, skin, new Vector3(0f, B + 3.1f, 0f), new Vector3(0.42f, 0.3f, 0.42f));
        P("Head", ball, skin, new Vector3(0f, hy, -0.02f), new Vector3(0.84f, 0.88f, 0.8f));
        P("Jaw", cube, skin, new Vector3(0f, hy - 0.26f, -0.12f), new Vector3(0.62f, 0.26f, 0.5f));
        foreach (float s in new[] { -1f, 1f })
        {
            P("Eye", ball, white, new Vector3(s * 0.17f, hy + 0.1f, -0.36f), new Vector3(0.22f, 0.2f, 0.14f));
            P("Pupil", ball, black, new Vector3(s * 0.17f, hy + 0.1f, -0.43f), Vector3.one * 0.09f);
            P("Brow", cube, gold, new Vector3(s * 0.19f, hy + 0.24f, -0.37f), new Vector3(0.28f, 0.07f, 0.06f), new Vector3(0f, 0f, s * -18f));
            P("Tusk", cone, white, new Vector3(s * 0.2f, hy - 0.26f, -0.38f), new Vector3(0.08f, 0.22f, 0.08f), new Vector3(0f, 0f, s * -12f));
            P("EarOrnament", cube, gold, new Vector3(s * 0.46f, hy + 0.05f, 0.04f), new Vector3(0.08f, 0.55f, 0.36f), new Vector3(0f, s * 20f, s * 28f));
            P("EarGem", ball, glass[2], new Vector3(s * 0.47f, hy - 0.05f, -0.03f), Vector3.one * 0.1f);
            P("Earring", cone, gold, new Vector3(s * 0.43f, hy - 0.42f, 0f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(180f, 0f, 0f));
            P("Cheek", ball, skin, new Vector3(s * 0.25f, hy - 0.08f, -0.3f), Vector3.one * 0.2f);
        }
        P("Nose", ball, skin, new Vector3(0f, hy - 0.04f, -0.42f), new Vector3(0.22f, 0.18f, 0.18f));
        P("Mouth", cube, lip, new Vector3(0f, hy - 0.24f, -0.37f), new Vector3(0.44f, 0.11f, 0.06f));
        P("Teeth", cube, white, new Vector3(0f, hy - 0.215f, -0.39f), new Vector3(0.36f, 0.04f, 0.03f));
        P("Diadem", rod, gold, new Vector3(0f, hy + 0.32f, -0.02f), new Vector3(0.86f, 0.14f, 0.82f));
        for (int i = 0; i < 5; i++)
            P("DiademGem", ball, glass[i % 3], new Vector3(-0.28f + i * 0.14f, hy + 0.38f, -0.4f), Vector3.one * 0.09f);

        // ชฎา: the tall tiered crown
        var crown = MeshKit.Lathe("Yaksha/Crown", new[]
        {
            new Vector2(0.42f, 0f), new Vector2(0.4f, 0.22f), new Vector2(0.44f, 0.27f), new Vector2(0.32f, 0.5f),
            new Vector2(0.35f, 0.56f), new Vector2(0.25f, 0.82f), new Vector2(0.28f, 0.88f), new Vector2(0.18f, 1.15f),
            new Vector2(0.2f, 1.2f), new Vector2(0.12f, 1.5f), new Vector2(0.07f, 1.85f), new Vector2(0f, 2.2f),
        }, 14);
        float cy = hy + 0.36f;
        MeshKit.Place("Crown", g, crown, gold, new Vector3(0f, cy, 0f));
        float[] rings = { 0.27f, 0.56f, 0.88f, 1.2f };
        float[] radii = { 0.44f, 0.35f, 0.28f, 0.2f };
        for (int r = 0; r < rings.Length; r++)
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                P("CrownGem", ball, glass[(i + r) % 3], new Vector3(Mathf.Sin(a) * radii[r], cy + rings[r], -Mathf.Cos(a) * radii[r]), Vector3.one * 0.07f);
            }
        if (style == 1) // ยอดหางไก่: a crest curling back like a cockerel's tail
        {
            var curl = MeshKit.Curve(new Vector3(0f, cy + 1.6f, 0f), new Vector3(0f, cy + 2.5f, 0.7f), new Vector3(0f, cy + 1.9f, 0.9f), 10);
            MeshKit.Place("Crest", g, MeshKit.Sweep("Yaksha/CrestCurl", curl, MeshKit.Taper(10, 0.1f, 0.02f), 6), gold, Vector3.zero);
        }
        else if (style == 2) // ยอดกระหนก: flame leaves round the top
            for (int i = 0; i < 4; i++)
                P("CrownLeaf", cone, gold, new Vector3(0f, cy + 1.45f, 0f), new Vector3(0.12f, 0.6f, 0.04f), new Vector3(0f, i * 90f, 35f));
        // กรรเจียกจร: the flared wings at the back of the crown
        foreach (float s in new[] { -1f, 1f })
            P("CrownWing", cube, gold, new Vector3(s * 0.4f, cy + 0.2f, 0.25f), new Vector3(0.06f, 0.6f, 0.4f), new Vector3(-20f, 0f, s * 35f));

        MeshKit.MergeByMaterial(g, "Yaksha/" + key);
        var col = y.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 3.1f, 0f);
        col.size = new Vector3(2.5f, 6.2f, 2.5f);
    }
}
