using UnityEngine;

// Builds each furniture piece out of simple shapes (no downloads needed). The origin is the
// middle of the piece on the floor; the front faces +Z.
public static class FurnitureFactory
{
    private static readonly Color Teak = new Color(0.45f, 0.27f, 0.13f);
    private static readonly Color DarkWood = new Color(0.3f, 0.18f, 0.09f);
    private static readonly Color Straw = new Color(0.86f, 0.73f, 0.45f);
    private static readonly Color Cream = new Color(0.96f, 0.93f, 0.85f);
    private static readonly Color ThaiRed = new Color(0.68f, 0.13f, 0.12f);
    private static readonly Color Gold = new Color(0.9f, 0.7f, 0.25f);

    private static Shader shader;

    public static GameObject Build(string item, Shader litShader)
    {
        shader = litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit");
        var root = new GameObject("Furniture_" + item).transform;

        switch (item)
        {
            case "mat":
                Box(root, new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.02f, 1.8f), Straw);
                foreach (float z in new[] { -0.6f, 0f, 0.6f })
                    Box(root, new Vector3(0f, 0.022f, z), new Vector3(1.2f, 0.005f, 0.08f), ThaiRed);
                break;
            case "pillow": // หมอนข้าง: a long bolster lying on the floor
                Cylinder(root, new Vector3(0f, 0.11f, 0f), new Vector3(0.22f, 0.45f, 0.22f), Cream, new Vector3(0f, 0f, 90f));
                Cylinder(root, new Vector3(-0.42f, 0.11f, 0f), new Vector3(0.225f, 0.03f, 0.225f), ThaiRed, new Vector3(0f, 0f, 90f));
                Cylinder(root, new Vector3(0.42f, 0.11f, 0f), new Vector3(0.225f, 0.03f, 0.225f), ThaiRed, new Vector3(0f, 0f, 90f));
                break;
            case "plant":
                Cylinder(root, new Vector3(0f, 0.18f, 0f), new Vector3(0.38f, 0.18f, 0.38f), new Color(0.7f, 0.35f, 0.2f));
                Sphere(root, new Vector3(0f, 0.65f, 0f), new Vector3(0.65f, 0.7f, 0.65f), new Color(0.2f, 0.55f, 0.25f));
                break;
            case "chair":
                Box(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.48f, 0.05f, 0.48f), Teak);
                Legs(root, 0.2f, 0.2f, 0.45f, 0.04f);
                Box(root, new Vector3(0f, 0.75f, -0.22f), new Vector3(0.48f, 0.55f, 0.05f), Teak);
                break;
            case "lamp":
                Cylinder(root, new Vector3(0f, 0.02f, 0f), new Vector3(0.32f, 0.02f, 0.32f), DarkWood);
                Cylinder(root, new Vector3(0f, 0.7f, 0f), new Vector3(0.04f, 0.68f, 0.04f), DarkWood);
                Cylinder(root, new Vector3(0f, 1.45f, 0f), new Vector3(0.42f, 0.16f, 0.42f), new Color(1f, 0.92f, 0.7f));
                break;
            case "low_table":
                Box(root, new Vector3(0f, 0.32f, 0f), new Vector3(1f, 0.06f, 0.6f), Teak);
                Legs(root, 0.44f, 0.24f, 0.32f, 0.05f);
                break;
            case "rug":
                Box(root, new Vector3(0f, 0.008f, 0f), new Vector3(2f, 0.015f, 1.4f), ThaiRed);
                Box(root, new Vector3(0f, 0.012f, 0f), new Vector3(1.7f, 0.016f, 1.1f), Gold);
                Box(root, new Vector3(0f, 0.016f, 0f), new Vector3(1.5f, 0.017f, 0.9f), ThaiRed);
                break;
            case "radio":
                Box(root, new Vector3(0f, 0.14f, 0f), new Vector3(0.45f, 0.28f, 0.16f), DarkWood);
                Cylinder(root, new Vector3(-0.1f, 0.14f, 0.081f), new Vector3(0.15f, 0.005f, 0.15f), Straw, new Vector3(90f, 0f, 0f));
                Box(root, new Vector3(0.12f, 0.17f, 0.081f), new Vector3(0.14f, 0.06f, 0.01f), Cream);
                break;
            case "fan":
                Cylinder(root, new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), new Color(0.85f, 0.87f, 0.9f));
                Cylinder(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.5f, 0.05f), new Color(0.85f, 0.87f, 0.9f));
                Cylinder(root, new Vector3(0f, 1.1f, 0.05f), new Vector3(0.5f, 0.06f, 0.5f), new Color(0.35f, 0.6f, 0.85f), new Vector3(90f, 0f, 0f));
                break;
            case "table":
                Box(root, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.06f, 0.7f), Teak);
                Legs(root, 0.54f, 0.29f, 0.75f, 0.06f);
                break;
            case "shelf":
                foreach (float y in new[] { 0.05f, 0.5f, 0.95f, 1.35f })
                    Box(root, new Vector3(0f, y, 0f), new Vector3(1f, 0.04f, 0.35f), Teak);
                Box(root, new Vector3(-0.5f, 0.7f, 0f), new Vector3(0.04f, 1.4f, 0.35f), DarkWood);
                Box(root, new Vector3(0.5f, 0.7f, 0f), new Vector3(0.04f, 1.4f, 0.35f), DarkWood);
                Box(root, new Vector3(-0.2f, 0.65f, 0f), new Vector3(0.2f, 0.25f, 0.2f), ThaiRed);
                Box(root, new Vector3(0.25f, 1.08f, 0f), new Vector3(0.3f, 0.22f, 0.2f), Gold);
                break;
            case "wardrobe":
                Box(root, new Vector3(0f, 0.95f, 0f), new Vector3(1f, 1.9f, 0.55f), Teak);
                Box(root, new Vector3(0f, 0.95f, 0.28f), new Vector3(0.02f, 1.8f, 0.01f), DarkWood);
                Box(root, new Vector3(-0.07f, 1f, 0.29f), new Vector3(0.03f, 0.2f, 0.03f), Gold);
                Box(root, new Vector3(0.07f, 1f, 0.29f), new Vector3(0.03f, 0.2f, 0.03f), Gold);
                break;
            case "bed":
                Box(root, new Vector3(0f, 0.18f, 0f), new Vector3(1.15f, 0.36f, 2.05f), Teak);
                Box(root, new Vector3(0f, 0.43f, 0.02f), new Vector3(1.05f, 0.16f, 1.95f), Cream);
                Box(root, new Vector3(0f, 0.55f, -0.75f), new Vector3(0.7f, 0.1f, 0.35f), Color.white);
                Box(root, new Vector3(0f, 0.53f, 0.35f), new Vector3(1.07f, 0.06f, 1.1f), new Color(0.35f, 0.55f, 0.75f));
                Box(root, new Vector3(0f, 0.6f, -1.02f), new Vector3(1.15f, 0.85f, 0.06f), DarkWood);
                break;
            case "tv":
                Box(root, new Vector3(0f, 0.23f, 0f), new Vector3(1.3f, 0.46f, 0.42f), Teak);
                Box(root, new Vector3(0f, 0.52f, 0f), new Vector3(0.3f, 0.12f, 0.15f), new Color(0.15f, 0.15f, 0.15f));
                Box(root, new Vector3(0f, 0.9f, 0f), new Vector3(1.1f, 0.65f, 0.05f), new Color(0.08f, 0.08f, 0.1f));
                Box(root, new Vector3(0f, 0.9f, 0.026f), new Vector3(1.02f, 0.57f, 0.01f), new Color(0.2f, 0.35f, 0.55f));
                break;
            case "fridge":
                Box(root, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 1.7f, 0.65f), new Color(0.9f, 0.92f, 0.93f));
                Box(root, new Vector3(0f, 1.2f, 0.33f), new Vector3(0.68f, 0.015f, 0.01f), new Color(0.6f, 0.62f, 0.65f));
                Box(root, new Vector3(0.28f, 1.45f, 0.34f), new Vector3(0.04f, 0.3f, 0.03f), new Color(0.6f, 0.62f, 0.65f));
                Box(root, new Vector3(0.28f, 0.8f, 0.34f), new Vector3(0.04f, 0.3f, 0.03f), new Color(0.6f, 0.62f, 0.65f));
                break;
            default:
                Box(root, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f), Gold);
                break;
        }
        return root.gameObject;
    }

    private static void Legs(Transform root, float x, float z, float height, float thickness)
    {
        foreach (float sx in new[] { -x, x })
        foreach (float sz in new[] { -z, z })
            Box(root, new Vector3(sx, height * 0.5f, sz), new Vector3(thickness, height, thickness), DarkWood);
    }

    private static void Box(Transform root, Vector3 pos, Vector3 size, Color color, Vector3? euler = null) =>
        Part(PrimitiveType.Cube, root, pos, size, color, euler);

    private static void Cylinder(Transform root, Vector3 pos, Vector3 scale, Color color, Vector3? euler = null) =>
        Part(PrimitiveType.Cylinder, root, pos, scale, color, euler);

    private static void Sphere(Transform root, Vector3 pos, Vector3 scale, Color color) =>
        Part(PrimitiveType.Sphere, root, pos, scale, color, null);

    private static void Part(PrimitiveType type, Transform root, Vector3 pos, Vector3 scale, Color color, Vector3? euler)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>()); // decoration only: never blocks walking
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        go.GetComponent<Renderer>().sharedMaterial = Material(color);
    }

    private static readonly System.Collections.Generic.Dictionary<Color, Material> materials = new System.Collections.Generic.Dictionary<Color, Material>();

    private static Material Material(Color color)
    {
        if (materials.TryGetValue(color, out var m) && m != null) return m;
        m = new Material(shader) { color = color };
        materials[color] = m;
        return m;
    }
}
