using UnityEngine;

// Concrete power poles (เสาไฟฟ้า) with cross-arms and insulators, sagging wires between them,
// street lamps that light up at night, and service wires from a pole to a house.
public static class StreetFurniture
{
    private const float PoleHeight = 8f;
    private static readonly float[] WireOffsets = { -0.75f, 0f, 0.75f }; // along the cross-arm

    // Poles at the given points (in parent space), wired one to the next; every pole faces `yaw`
    public static Transform[] PoleLine(Transform parent, Vector3[] points, float yaw, bool lamps)
    {
        var concrete = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        var wire = PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f));
        var poles = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
            poles[i] = Pole(parent, points[i], yaw, lamps, concrete).transform;

        for (int i = 0; i < poles.Length - 1; i++)
        foreach (float off in WireOffsets)
        {
            Vector3 a = parent.InverseTransformPoint(poles[i].TransformPoint(new Vector3(off, PoleHeight - 0.45f, 0f)));
            Vector3 b = parent.InverseTransformPoint(poles[i + 1].TransformPoint(new Vector3(off, PoleHeight - 0.45f, 0f)));
            Wire(parent, a, b, 0.55f, wire);
        }
        return poles;
    }

    // A thin wire from a pole down to a point on a house
    public static void ServiceDrop(Transform parent, Transform pole, Vector3 houseLocalPoint)
    {
        var wire = PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f));
        Vector3 a = parent.InverseTransformPoint(pole.TransformPoint(new Vector3(0.75f, PoleHeight - 0.45f, 0f)));
        Wire(parent, a, houseLocalPoint, 0.35f, wire);
    }

    private static GameObject Pole(Transform parent, Vector3 position, float yaw, bool lamp, Material concrete)
    {
        var pole = PrimitiveKit.Group("PowerPole_เสาไฟฟ้า", parent, position, yaw);
        var t = pole.transform;
        MeshKit.Place("Pole", t, MeshKit.Lathe("PowerPole", new[]
        {
            new Vector2(0.17f, 0f), new Vector2(0.15f, 0.4f), new Vector2(0.11f, PoleHeight), new Vector2(0.09f, PoleHeight + 0.05f),
        }, 10), concrete, Vector3.zero);
        var col = pole.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.2f, 0f);
        col.height = 2.4f;
        col.radius = 0.16f;

        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var porcelain = PrimitiveKit.Mat("Insulator", new Color(0.92f, 0.9f, 0.86f));
        PrimitiveKit.Box("CrossArm", t, new Vector3(0f, PoleHeight - 0.6f, 0f), new Vector3(1.9f, 0.1f, 0.1f), steel, collider: false);
        PrimitiveKit.Box("Brace", t, new Vector3(0f, PoleHeight - 0.85f, 0f), new Vector3(0.9f, 0.06f, 0.06f), steel, new Vector3(0f, 0f, 0f), collider: false);
        foreach (float off in WireOffsets)
            PrimitiveKit.Cylinder("Insulator", t, new Vector3(off, PoleHeight - 0.48f, 0f), new Vector3(0.09f, 0.07f, 0.09f), porcelain, collider: false);
        // Transformer drum on some poles looks right in Thailand
        if ((int)(Mathf.Abs(position.x) + Mathf.Abs(position.z)) % 3 == 0)
            PrimitiveKit.Cylinder("Transformer", t, new Vector3(0f, PoleHeight - 2f, -0.32f), new Vector3(0.45f, 0.4f, 0.45f), PrimitiveKit.Mat("TransformerGrey", new Color(0.55f, 0.58f, 0.6f)), collider: false);

        if (lamp)
        {
            // Arm reaching out over the road (local -Z), lamp head and a light that turns on at night
            var arm = MeshKit.Curve(new Vector3(0f, 5.6f, 0f), new Vector3(0f, 6.3f, -0.4f), new Vector3(0f, 6.2f, -1.6f), 7);
            MeshKit.Place("LampArm", t, MeshKit.Sweep("LampArm", arm, MeshKit.Taper(7, 0.05f, 0.04f), 6), steel, Vector3.zero);
            PrimitiveKit.Box("LampHead", t, new Vector3(0f, 6.12f, -1.7f), new Vector3(0.28f, 0.12f, 0.55f), steel, collider: false);
            PrimitiveKit.Box("LampGlass", t, new Vector3(0f, 6.05f, -1.7f), new Vector3(0.22f, 0.04f, 0.45f), PrimitiveKit.Glow("LampGlow", new Color(1f, 0.82f, 0.55f), 3f), collider: false);
            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 5.9f, -1.7f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 13f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.shadows = LightShadows.None;
            light.enabled = false;
            pole.AddComponent<StreetLamp>();
        }
        return pole;
    }

    // Modern lamp post (3.6 m) for places: temple, offices, school, park, camp...
    public static void LampPost(Transform parent, Vector3 position)
    {
        var post = PrimitiveKit.Group("LampPost_เสาไฟ", parent, position);
        var t = post.transform;
        var metal = PrimitiveKit.Mat("LampPostMetal", new Color(0.18f, 0.2f, 0.22f));
        MeshKit.Place("Post", t, MeshKit.Lathe("LampPost", new[]
        {
            new Vector2(0.14f, 0f), new Vector2(0.14f, 0.3f), new Vector2(0.07f, 0.4f), new Vector2(0.05f, 3.4f), new Vector2(0.05f, 3.45f),
        }, 10), metal, Vector3.zero);
        MeshKit.Place("Head", t, MeshKit.Lathe("LampPostHead", new[]
        {
            new Vector2(0.06f, 0.3f), new Vector2(0.24f, 0.08f), new Vector2(0.26f, 0f), new Vector2(0f, -0.02f),
        }, 14), metal, new Vector3(0f, 3.4f, 0f));
        PrimitiveKit.Sphere("Bulb", t, new Vector3(0f, 3.38f, 0f), new Vector3(0.22f, 0.1f, 0.22f), PrimitiveKit.Glow("LampGlow", new Color(1f, 0.82f, 0.55f), 3f));
        AddLight(post, new Vector3(0f, 3.3f, 0f), StreetLamp.Kind.Post);
        post.AddComponent<CapsuleCollider>().radius = 0.12f;
    }

    // Solar lamp (โคมไฟโซล่าเซลล์) for the paddies and the canal: post, tilted panel, LED head
    public static void SolarLamp(Transform parent, Vector3 position, float yaw = 0f)
    {
        var post = PrimitiveKit.Group("SolarLamp_ไฟโซล่าเซลล์", parent, position, yaw);
        var t = post.transform;
        var metal = PrimitiveKit.Mat("SolarPost", new Color(0.75f, 0.77f, 0.8f));
        var panel = PrimitiveKit.Mat("SolarPanelGlass", new Color(0.08f, 0.14f, 0.32f));
        PrimitiveKit.Cylinder("Post", t, new Vector3(0f, 1.6f, 0f), new Vector3(0.09f, 1.6f, 0.09f), metal);
        PrimitiveKit.Box("PanelFrame", t, new Vector3(0f, 3.35f, 0.15f), new Vector3(0.95f, 0.05f, 0.62f), metal, new Vector3(-30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Panel", t, new Vector3(0f, 3.38f, 0.15f), new Vector3(0.9f, 0.03f, 0.57f), panel, new Vector3(-30f, 0f, 0f), collider: false);
        for (int i = 1; i < 4; i++)
            PrimitiveKit.Box("Cell", t, new Vector3(-0.45f + i * 0.225f, 3.4f, 0.15f), new Vector3(0.012f, 0.032f, 0.56f), metal, new Vector3(-30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Arm", t, new Vector3(0f, 2.95f, -0.35f), new Vector3(0.06f, 0.06f, 0.7f), metal, collider: false);
        PrimitiveKit.Box("LedHead", t, new Vector3(0f, 2.92f, -0.75f), new Vector3(0.24f, 0.07f, 0.42f), metal, collider: false);
        PrimitiveKit.Box("Led", t, new Vector3(0f, 2.88f, -0.75f), new Vector3(0.2f, 0.02f, 0.36f), PrimitiveKit.Glow("LedGlow", new Color(0.85f, 0.92f, 1f), 3f), collider: false);
        AddLight(post, new Vector3(0f, 2.85f, -0.75f), StreetLamp.Kind.Solar);
    }

    // A bulb hanging under a roof (market halls, ตลาดนัด aisle)
    public static void HangingLight(Transform parent, Vector3 position)
    {
        var go = PrimitiveKit.Group("HangingLight", parent, position);
        PrimitiveKit.Sphere("Bulb", go.transform, Vector3.zero, Vector3.one * 0.2f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        AddLight(go, new Vector3(0f, -0.1f, 0f), StreetLamp.Kind.Hanging);
    }

    // Stage light over a boxing ring: always on, so fights are bright even under a roof
    public static void RingLight(Transform parent, Vector3 position)
    {
        var go = PrimitiveKit.Group("RingLight", parent, position);
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        PrimitiveKit.Box("Housing", go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.16f, 0.6f), steel, collider: false);
        PrimitiveKit.Box("Lens", go.transform, new Vector3(0f, -0.01f, 0f), new Vector3(0.5f, 0.03f, 0.5f), PrimitiveKit.Glow("StageGlow", new Color(1f, 0.96f, 0.88f), 3f), collider: false);
        AddLight(go, new Vector3(0f, -0.1f, 0f), StreetLamp.Kind.Ring);
    }

    private static void AddLight(GameObject owner, Vector3 localPosition, StreetLamp.Kind kind)
    {
        var lightGo = new GameObject("LampLight");
        lightGo.transform.SetParent(owner.transform, false);
        lightGo.transform.localPosition = localPosition;
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.shadows = LightShadows.None;
        light.enabled = false;
        owner.AddComponent<StreetLamp>().Configure(kind);
    }

    private static void Wire(Transform parent, Vector3 a, Vector3 b, float sag, Material material)
    {
        var path = MeshKit.Curve(a, (a + b) * 0.5f + Vector3.down * sag, b, 10);
        // Each span has its own shape, so key the mesh by its end points
        string key = $"Wire_{a.x:0.0}_{a.y:0.0}_{a.z:0.0}_{b.x:0.0}_{b.y:0.0}_{b.z:0.0}".Replace('-', 'm');
        MeshKit.Place("Wire", parent, MeshKit.Sweep("Wires/" + key, path, MeshKit.Taper(10, 0.018f, 0.018f), 4), material, Vector3.zero)
            .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
