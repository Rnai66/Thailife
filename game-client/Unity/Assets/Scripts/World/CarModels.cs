using UnityEngine;

// Runtime models of the vehicles sold at the bank (bank.lua ASSETS, kind "car"): bicycle, motorbike,
// pickup, sedan and luxury car, built from primitives with rounded parts. Front faces +Z, wheels on
// the ground at y = 0.
public static class CarModels
{
    public static bool TwoWheels(string id) => id == "car_bicycle" || id == "car_motorbike";

    // Drive speed multiplier on top of walking
    public static float DriveSpeed(string id) =>
        id == "car_bicycle" ? 1.6f : id == "car_motorbike" ? 2.4f : id == "car_pickup" ? 2.8f : id == "car_sedan" ? 3.0f : 3.4f;

    public static void Build(string id, Transform root, Material template)
    {
        switch (id)
        {
            case "car_bicycle": Bicycle(root, template); break;
            case "car_motorbike": Motorbike(root, template); break;
            case "car_pickup": Car(root, template, new Color(0.92f, 0.93f, 0.94f), pickup: true, length: 4.2f, width: 1.75f); break;
            case "car_sedan": Car(root, template, new Color(0.85f, 0.18f, 0.2f), pickup: false, length: 4.0f, width: 1.7f); break;
            default: Car(root, template, new Color(0.08f, 0.09f, 0.12f), pickup: false, length: 4.4f, width: 1.85f, trim: new Color(0.95f, 0.78f, 0.3f)); break;
        }
    }

    private static readonly Color Tyre = new Color(0.1f, 0.1f, 0.11f);
    private static readonly Color Rim = new Color(0.78f, 0.8f, 0.83f);
    private static readonly Color Glass = new Color(0.45f, 0.62f, 0.75f);

    private static void Car(Transform root, Material template, Color paint, bool pickup, float length, float width, Color? trim = null)
    {
        float half = length * 0.5f;
        // Lower body with round bumpers front and back
        Part(PrimitiveType.Cube, root, new Vector3(0f, 0.62f, 0f), new Vector3(width, 0.55f, length - 0.5f), paint, template);
        foreach (float z in new[] { -half + 0.25f, half - 0.25f })
            Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.6f, z), new Vector3(0.55f, width * 0.5f, 0.5f), paint, template, new Vector3(0f, 0f, 90f));
        // Cabin: rounded roof over glass
        float cabinZ = pickup ? 0.55f : -0.1f, cabinLength = pickup ? 1.6f : 2.1f;
        Part(PrimitiveType.Cube, root, new Vector3(0f, 1.15f, cabinZ), new Vector3(width - 0.16f, 0.55f, cabinLength), Glass, template);
        Part(PrimitiveType.Capsule, root, new Vector3(0f, 1.42f, cabinZ), new Vector3(0.18f, width * 0.46f, cabinLength + 0.1f), paint, template, new Vector3(0f, 0f, 90f));
        foreach (float x in new[] { -width * 0.5f + 0.06f, width * 0.5f - 0.06f })
            Part(PrimitiveType.Cube, root, new Vector3(x, 1.15f, cabinZ), new Vector3(0.06f, 0.5f, 0.12f), paint, template); // door pillar
        if (pickup) // open cargo bed
        {
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.92f, -half + 0.95f), new Vector3(width - 0.1f, 0.05f, 1.7f), new Color(0.25f, 0.25f, 0.27f), template);
            foreach (float x in new[] { -width * 0.5f + 0.05f, width * 0.5f - 0.05f })
                Part(PrimitiveType.Cube, root, new Vector3(x, 1.05f, -half + 0.95f), new Vector3(0.08f, 0.28f, 1.8f), paint, template);
        }
        // Lights: round headlamps, red tail lamps
        foreach (float x in new[] { -width * 0.33f, width * 0.33f })
        {
            Part(PrimitiveType.Sphere, root, new Vector3(x, 0.72f, half - 0.05f), new Vector3(0.28f, 0.2f, 0.12f), new Color(1f, 0.97f, 0.8f), template);
            Part(PrimitiveType.Sphere, root, new Vector3(x, 0.78f, -half + 0.05f), new Vector3(0.24f, 0.14f, 0.1f), new Color(0.85f, 0.1f, 0.1f), template);
        }
        if (trim.HasValue) Part(PrimitiveType.Cube, root, new Vector3(0f, 0.72f, 0f), new Vector3(width + 0.02f, 0.05f, length - 0.6f), trim.Value, template);
        // Wheels
        foreach (float x in new[] { -width * 0.5f + 0.05f, width * 0.5f - 0.05f })
            foreach (float z in new[] { -half + 0.85f, half - 0.85f })
                Wheel(root, template, new Vector3(x, 0.36f, z), 0.72f, 0.26f);
    }

    private static void Motorbike(Transform root, Material template)
    {
        Color red = new Color(0.85f, 0.15f, 0.15f);
        Wheel(root, template, new Vector3(0f, 0.32f, 0.62f), 0.64f, 0.12f);
        Wheel(root, template, new Vector3(0f, 0.32f, -0.62f), 0.64f, 0.14f);
        Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.62f, 0f), new Vector3(0.36f, 0.6f, 0.4f), red, template, new Vector3(90f, 0f, 0f)); // body
        Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.86f, -0.25f), new Vector3(0.3f, 0.36f, 0.22f), new Color(0.12f, 0.12f, 0.13f), template, new Vector3(90f, 0f, 0f)); // seat
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.85f, 0.55f), new Vector3(0.06f, 0.3f, 0.06f), Rim, template, new Vector3(-20f, 0f, 0f)); // fork
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 1.12f, 0.48f), new Vector3(0.05f, 0.34f, 0.05f), Rim, template, new Vector3(0f, 0f, 90f)); // handlebar
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 1.0f, 0.68f), new Vector3(0.2f, 0.18f, 0.12f), new Color(1f, 0.97f, 0.8f), template); // headlamp
    }

    private static void Bicycle(Transform root, Material template)
    {
        Color frame = new Color(0.15f, 0.55f, 0.85f);
        Wheel(root, template, new Vector3(0f, 0.34f, 0.52f), 0.68f, 0.06f);
        Wheel(root, template, new Vector3(0f, 0.34f, -0.52f), 0.68f, 0.06f);
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 0.52f, 0.05f), frame, template, new Vector3(90f, 0f, 0f));
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.5f, -0.25f), new Vector3(0.05f, 0.3f, 0.05f), frame, template, new Vector3(30f, 0f, 0f));
        Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.86f, -0.3f), new Vector3(0.14f, 0.06f, 0.24f), new Color(0.12f, 0.12f, 0.13f), template); // seat
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.95f, 0.45f), new Vector3(0.04f, 0.28f, 0.04f), Rim, template, new Vector3(0f, 0f, 90f)); // handlebar
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.66f, 0.5f), new Vector3(0.04f, 0.3f, 0.04f), frame, template, new Vector3(-15f, 0f, 0f)); // fork
    }

    private static void Wheel(Transform root, Material template, Vector3 at, float diameter, float width)
    {
        Part(PrimitiveType.Cylinder, root, at, new Vector3(diameter, width * 0.5f, diameter), Tyre, template, new Vector3(0f, 0f, 90f));
        Part(PrimitiveType.Cylinder, root, at, new Vector3(diameter * 0.55f, width * 0.5f + 0.01f, diameter * 0.55f), Rim, template, new Vector3(0f, 0f, 90f));
    }

    private static void Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color, Material template, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        var material = template != null ? new Material(template) : renderer.material;
        material.color = color;
        renderer.sharedMaterial = material;
    }
}
