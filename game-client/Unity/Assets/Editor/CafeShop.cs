using UnityEngine;

// ร้านกาแฟ in the City, east of the land office lot: a two-storey shop with a walk-in ground floor left
// empty for the owner to furnish. CafeSite (runtime) makes it the shop of whoever runs a coffee
// business at the bank — their name over the door, their style and furniture (cafe.lua) — and an
// empty shop to let for everyone else.
public static class CafeShop
{
    public static readonly Vector3 Site = new Vector3(66f, 0f, -31f); // front to the road (yaw 180 faces +Z)

    public static void Build(Transform root)
    {
        const float width = 13f, depth = 10.5f;
        var b = CityCivic.Block(root, Site, 180f, "ร้านกาแฟ", width, depth, 2, new Color(0.93f, 0.9f, 0.84f), new Color(0.24f, 0.18f, 0.14f),
            new Color(0.24f, 0.18f, 0.14f), new Color(1f, 0.86f, 0.6f), "CoffeeShop_ร้านกาแฟ", kiosk: null, interior: "empty");
        float s = b.localScale.x;
        // the room in metres (Interiors.Rooms: 0.2 walls, a 3.2 storey, all scaled)
        b.gameObject.AddComponent<CafeSite>().Configure((width * 0.5f - 0.2f) * s, (depth * 0.5f - 0.2f) * s, 3.2f * s,
            new Vector3(0f, 3.7f, -depth * 0.5f - 0.1f));
        // a couple of tables out front under parasols
        var wood = PrimitiveKit.Mat("CafeTerraceWood", new Color(0.45f, 0.3f, 0.18f));
        var canvas = PrimitiveKit.Mat("CafeParasol", new Color(0.9f, 0.86f, 0.78f));
        foreach (float x in new[] { -4.2f, 4.2f })
        {
            var t = PrimitiveKit.Group("Terrace", b, new Vector3(x, 0f, -depth * 0.5f - 2.4f)).transform;
            PrimitiveKit.Cylinder("TableTop", t, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 0.03f, 1.1f), wood, collider: false);
            PrimitiveKit.Cylinder("TableLeg", t, new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.1f), wood);
            PrimitiveKit.Cylinder("ParasolPole", t, new Vector3(0f, 1.9f, 0f), new Vector3(0.06f, 0.9f, 0.06f), wood, collider: false);
            PrimitiveKit.Cylinder("Parasol", t, new Vector3(0f, 2.85f, 0f), new Vector3(2.4f, 0.08f, 2.4f), canvas, collider: false);
            foreach (float cx in new[] { -0.85f, 0.85f })
                PrimitiveKit.Box("Stool", t, new Vector3(cx, 0.4f, 0f), new Vector3(0.45f, 0.08f, 0.45f), wood, collider: false);
        }
    }
}
