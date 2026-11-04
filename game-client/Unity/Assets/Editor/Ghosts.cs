using UnityEngine;

// Cartoon Thai ghosts built from primitives (no colliders), each with its tell-tale look and a dim
// coloured glow so it reads in the dark. Placed by TempleLife at GhostSpots; GhostSpot shows them
// only at night and makes them drift and stare.
public static class Ghosts
{
    private static Material M(string name, Color c) => PrimitiveKit.Mat("Ghost" + name, c, 0.3f);
    private static Material G(string name, Color c, float i = 2.5f) => PrimitiveKit.Glow("GhostGlow" + name, c, i);

    public static Transform Build(Transform parent, Vector3 local, string id)
    {
        var root = PrimitiveKit.Group("Ghost_" + id, parent, local).transform;
        var body = PrimitiveKit.Group("Body", root, Vector3.zero).transform;
        switch (id)
        {
            case "krasue": Krasue(body); Glow(root, new Color(0.4f, 1f, 0.5f), 1.6f); break;
            case "krahang": Krahang(body); Glow(root, new Color(1f, 0.6f, 0.3f), 2f); break;
            case "pop": Pop(body); Glow(root, new Color(0.8f, 0.4f, 1f), 2f); break;
            case "tani": Lady(body, new Color(0.22f, 0.55f, 0.25f), new Color(0.8f, 0.92f, 0.78f), 1.7f); Banana(body); Glow(root, new Color(0.4f, 1f, 0.45f), 2f); break;
            case "takhian": Lady(body, new Color(0.6f, 0.1f, 0.1f), new Color(0.9f, 0.88f, 0.84f), 1.7f); Sash(body); Glow(root, new Color(1f, 0.45f, 0.35f), 2f); break;
            case "phrai": Lady(body, new Color(0.94f, 0.95f, 0.97f), new Color(0.88f, 0.92f, 0.95f), 1.75f); Glow(root, new Color(0.6f, 0.85f, 1f), 2.2f); break;
            case "pret": Pret(body); Glow(root, new Color(0.75f, 0.75f, 0.9f), 3.5f); break;
            case "nak": Lady(body, new Color(0.45f, 0.25f, 0.55f), new Color(0.9f, 0.88f, 0.86f), 1.75f, longest: true); Baby(body); Glow(root, new Color(0.75f, 0.55f, 1f), 2.2f); break;
            default: Lady(body, Color.white, new Color(0.9f, 0.9f, 0.9f), 1.7f); break;
        }
        return root;
    }

    // A dim point light the colour of the ghost, so it glows in the dark
    private static void Glow(Transform root, Color c, float height)
    {
        var go = new GameObject("GhostLight");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, height, 0.6f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = 4.5f;
        l.intensity = 1.6f;
        l.shadows = LightShadows.None;
    }

    // ── Faces and hair ───────────────────────────────────────────────────────────────────
    private static void Face(Transform t, Vector3 at, float size, Color skin, Color hair, bool longHair, float hairLength = 1.1f,
        bool fangs = false, bool wild = false, bool tongue = false)
    {
        var black = M("Hair" + Key(hair), hair);
        PrimitiveKit.Sphere("Head", t, at, new Vector3(0.36f, 0.42f, 0.36f) * size, M("Skin" + Key(skin), skin));
        PrimitiveKit.Sphere("HairTop", t, at + new Vector3(0f, 0.07f, 0.04f) * size, new Vector3(0.4f, 0.42f, 0.38f) * size, black);
        PrimitiveKit.Box("Fringe", t, at + new Vector3(0f, 0.13f, -0.15f) * size, new Vector3(0.36f, 0.12f, 0.08f) * size, black, collider: false);
        if (longHair)
        {
            PrimitiveKit.Box("HairBack", t, at + new Vector3(0f, -hairLength * 0.45f, 0.1f) * size, new Vector3(0.42f, hairLength, 0.12f) * size, black, collider: false);
            foreach (float sx in new[] { -1f, 1f }) // two hanks over the shoulders, framing the face
                PrimitiveKit.Box("HairSide", t, at + new Vector3(sx * 0.17f, -hairLength * 0.35f, -0.08f) * size, new Vector3(0.08f, hairLength * 0.8f, 0.08f) * size, black, collider: false);
        }
        if (wild)
            for (int i = -2; i <= 2; i++)
                PrimitiveKit.Box("Wild", t, at + new Vector3(i * 0.09f, 0.24f, 0f) * size, new Vector3(0.05f, 0.18f, 0.05f) * size, black, new Vector3(0f, 0f, i * 18f), collider: false);
        var eye = G("Eye", new Color(1f, 0.15f, 0.1f), 4f);
        foreach (float sx in new[] { -1f, 1f })
        {
            PrimitiveKit.Sphere("EyeWhite", t, at + new Vector3(sx * 0.075f, 0.02f, -0.16f) * size, new Vector3(0.09f, wild ? 0.09f : 0.06f, 0.04f) * size, M("EyeWhite", Color.white));
            PrimitiveKit.Sphere("Eye", t, at + new Vector3(sx * 0.075f, 0.02f, -0.18f) * size, new Vector3(0.045f, 0.045f, 0.02f) * size, eye);
            PrimitiveKit.Sphere("EyeBag", t, at + new Vector3(sx * 0.075f, -0.03f, -0.165f) * size, new Vector3(0.09f, 0.025f, 0.02f) * size, M("EyeBag", new Color(0.35f, 0.33f, 0.4f)));
        }
        PrimitiveKit.Sphere("Mouth", t, at + new Vector3(0f, -0.11f, -0.165f) * size, new Vector3(0.1f, 0.04f, 0.02f) * size, M("Lips", new Color(0.6f, 0.05f, 0.08f)));
        if (fangs)
            foreach (float sx in new[] { -1f, 1f })
                PrimitiveKit.Box("Fang", t, at + new Vector3(sx * 0.03f, -0.14f, -0.17f) * size, new Vector3(0.015f, 0.04f, 0.01f) * size, M("Fang", Color.white), collider: false);
        if (tongue)
            PrimitiveKit.Box("Tongue", t, at + new Vector3(0f, -0.3f, -0.18f) * size, new Vector3(0.06f, 0.36f, 0.02f) * size, M("Tongue", new Color(0.8f, 0.1f, 0.15f)), collider: false);
    }

    // A flowing gown that narrows to nothing at the hem: the ghost floats off the ground
    private static void Gown(Transform t, Color cloth, float top, float height, float width)
    {
        var m = M("Gown" + Key(cloth), cloth);
        for (int i = 0; i < 4; i++)
        {
            float k = i / 3f;
            float w = Mathf.Lerp(width * 0.6f, width, k);
            PrimitiveKit.Cylinder("Gown", t, new Vector3(0f, top - height * (0.12f + k * 0.26f), 0f), new Vector3(w, height * 0.14f, w * 0.8f), m, collider: false);
        }
        foreach (float sx in new[] { -1f, 1f }) // tattered hem strips
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Tatter", t, new Vector3(sx * (0.1f + i * 0.1f), top - height * 0.98f, (i - 1) * 0.08f), new Vector3(0.07f, height * 0.2f, 0.04f), m, new Vector3(0f, 0f, sx * 8f), collider: false);
    }

    private static void Arms(Transform t, Color skin, float shoulder, float reach = 0.5f)
    {
        var m = M("Skin" + Key(skin), skin);
        foreach (float sx in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Arm", t, new Vector3(sx * 0.24f, shoulder - 0.05f, -reach * 0.5f), new Vector3(0.08f, 0.08f, reach), m, new Vector3(10f, sx * -8f, 0f), collider: false);
            PrimitiveKit.Sphere("Hand", t, new Vector3(sx * 0.26f, shoulder - 0.12f, -reach), new Vector3(0.1f, 0.06f, 0.12f), m);
        }
    }

    // ── The ghosts ───────────────────────────────────────────────────────────────────────
    private static void Lady(Transform t, Color cloth, Color skin, float headY, bool longest = false)
    {
        Gown(t, cloth, headY - 0.25f, 1.3f, 0.6f);
        Arms(t, skin, headY - 0.35f, 0.55f);
        Face(t, new Vector3(0f, headY, 0f), 1f, skin, new Color(0.05f, 0.05f, 0.07f), true, longest ? 1.5f : 1.1f, fangs: longest);
    }

    private static void Krasue(Transform t)
    {
        // Only a woman's head, floating, trailing her heart, lungs and a coil of gut that glows
        var guts = G("Guts", new Color(0.95f, 0.25f, 0.35f), 1.8f);
        var heart = G("Heart", new Color(0.8f, 0.05f, 0.1f), 2f);
        for (int i = 0; i < 10; i++)
            PrimitiveKit.Sphere("Gut", t, new Vector3(Mathf.Sin(i * 1.4f) * 0.09f, 1.45f - i * 0.11f, Mathf.Cos(i * 1.4f) * 0.06f), Vector3.one * 0.1f, guts);
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Sphere("Lung", t, new Vector3(sx * 0.1f, 1.38f, 0f), new Vector3(0.12f, 0.18f, 0.1f), heart);
        PrimitiveKit.Sphere("Heart", t, new Vector3(0f, 1.25f, -0.04f), new Vector3(0.12f, 0.14f, 0.1f), heart);
        Face(t, new Vector3(0f, 1.75f, 0f), 1f, new Color(0.88f, 0.86f, 0.82f), new Color(0.05f, 0.05f, 0.07f), true, 0.5f, fangs: true);
    }

    private static void Krahang(Transform t)
    {
        // A dark man flying with winnowing baskets (กระด้ง) on his arms and a pestle (สาก) for a tail
        var skin = new Color(0.45f, 0.32f, 0.25f);
        var basket = M("Kradong", new Color(0.72f, 0.55f, 0.3f));
        var rim = M("KradongRim", new Color(0.5f, 0.36f, 0.18f));
        PrimitiveKit.Cylinder("Torso", t, new Vector3(0f, 1.2f, 0f), new Vector3(0.42f, 0.3f, 0.3f), M("Skin" + Key(skin), skin), collider: false);
        PrimitiveKit.Cylinder("Loincloth", t, new Vector3(0f, 0.85f, 0f), new Vector3(0.44f, 0.12f, 0.32f), M("Loincloth", new Color(0.4f, 0.12f, 0.1f)), collider: false);
        foreach (float sx in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Leg", t, new Vector3(sx * 0.1f, 0.55f, 0.05f), new Vector3(0.11f, 0.5f, 0.11f), M("Skin" + Key(skin), skin), new Vector3(25f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Arm", t, new Vector3(sx * 0.45f, 1.35f, 0f), new Vector3(0.5f, 0.08f, 0.08f), M("Skin" + Key(skin), skin), new Vector3(0f, 0f, sx * 15f), collider: false);
            var wing = PrimitiveKit.Group("Wing", t, new Vector3(sx * 0.85f, 1.45f, 0.05f)).transform;
            wing.localRotation = Quaternion.Euler(0f, 0f, sx * 70f);
            PrimitiveKit.Cylinder("Kradong", wing, Vector3.zero, new Vector3(1f, 0.03f, 1f), basket, collider: false);
            PrimitiveKit.Cylinder("Rim", wing, Vector3.zero, new Vector3(1.04f, 0.02f, 1.04f), rim, collider: false);
        }
        PrimitiveKit.Cylinder("Sak", t, new Vector3(0f, 0.5f, 0.35f), new Vector3(0.09f, 0.5f, 0.09f), M("Pestle", new Color(0.45f, 0.3f, 0.18f)), new Vector3(-40f, 0f, 0f), collider: false);
        Face(t, new Vector3(0f, 1.62f, 0f), 1f, skin, new Color(0.08f, 0.06f, 0.05f), false, fangs: true);
    }

    private static void Pop(Transform t)
    {
        // A hunched old woman with wild grey hair, staring eyes and a long tongue
        var cloth = new Color(0.28f, 0.26f, 0.3f);
        var skin = new Color(0.72f, 0.66f, 0.6f);
        var hunch = PrimitiveKit.Group("Hunch", t, Vector3.zero).transform;
        hunch.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        Gown(hunch, cloth, 1.25f, 1.1f, 0.62f);
        Arms(hunch, skin, 1.15f, 0.65f);
        Face(hunch, new Vector3(0f, 1.5f, -0.1f), 1f, skin, new Color(0.78f, 0.78f, 0.8f), true, 0.7f, fangs: true, wild: true, tongue: true);
    }

    private static void Pret(Transform t)
    {
        // Taller than a palm-sugar tree, thin as a reed, a mouth no bigger than a needle's eye
        var skin = new Color(0.52f, 0.5f, 0.54f);
        var m = M("Skin" + Key(skin), skin);
        foreach (float sx in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Leg", t, new Vector3(sx * 0.12f, 1.1f, 0f), new Vector3(0.09f, 2.2f, 0.09f), m, collider: false);
            PrimitiveKit.Box("Arm", t, new Vector3(sx * 0.3f, 2.6f, -0.1f), new Vector3(0.07f, 1.6f, 0.07f), m, new Vector3(-12f, 0f, sx * 8f), collider: false);
        }
        PrimitiveKit.Cylinder("Belly", t, new Vector3(0f, 2.45f, 0f), new Vector3(0.5f, 0.3f, 0.45f), m, collider: false);
        PrimitiveKit.Cylinder("Chest", t, new Vector3(0f, 3.05f, 0f), new Vector3(0.3f, 0.35f, 0.22f), m, collider: false);
        PrimitiveKit.Cylinder("Neck", t, new Vector3(0f, 3.75f, 0f), new Vector3(0.08f, 0.4f, 0.08f), m, collider: false);
        PrimitiveKit.Cylinder("Loincloth", t, new Vector3(0f, 2.2f, 0f), new Vector3(0.48f, 0.1f, 0.42f), M("Rag", new Color(0.35f, 0.3f, 0.25f)), collider: false);
        Face(t, new Vector3(0f, 4.25f, 0f), 0.75f, skin, new Color(0.2f, 0.2f, 0.22f), false);
        PrimitiveKit.Sphere("PinMouth", t, new Vector3(0f, 4.17f, -0.135f), new Vector3(0.02f, 0.02f, 0.01f), M("PinMouth", Color.black));
    }

    private static void Banana(Transform t)
    {
        // นางตานี lives in a wild banana (กล้วยตานี): a clump of big leaves behind her
        var leaf = M("BananaLeaf", new Color(0.25f, 0.58f, 0.2f));
        for (int i = 0; i < 4; i++)
        {
            float a = -60f + i * 40f;
            PrimitiveKit.Box("Leaf", t, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 1.7f, 0.7f), new Vector3(0.5f, 1.6f, 0.03f), leaf, new Vector3(20f, a, 0f), collider: false);
        }
    }

    private static void Sash(Transform t) =>
        PrimitiveKit.Box("Sabai", t, new Vector3(0f, 1.3f, -0.1f), new Vector3(0.5f, 0.12f, 0.45f), M("SabaiGold", new Color(0.95f, 0.75f, 0.25f)), new Vector3(0f, 0f, 30f), collider: false);

    private static void Baby(Transform t)
    {
        PrimitiveKit.Sphere("Swaddle", t, new Vector3(0.22f, 1.25f, -0.32f), new Vector3(0.32f, 0.22f, 0.22f), M("Swaddle", new Color(0.95f, 0.95f, 0.93f)));
        PrimitiveKit.Sphere("BabyHead", t, new Vector3(0.36f, 1.32f, -0.34f), Vector3.one * 0.14f, M("BabySkin", new Color(0.9f, 0.85f, 0.8f)));
    }

    private static string Key(Color c) => ColorUtility.ToHtmlStringRGB(c);
}
