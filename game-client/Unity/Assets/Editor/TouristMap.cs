using System.Collections.Generic;
using UnityEngine;

// ป้ายแผนที่ท่องเที่ยว: a tourist map board — the town drawn north-up (ring road, avenue, railway, the
// sea), a numbered red pin at each sight, "คุณอยู่ที่นี่", and the numbered list beside it. Both faces
// carry the map, so it reads from either side. Walking up to it pops the list as a card (SignPost).
// The sights are found in the scene as built: a province's ten "Attr<Kind>_<ชื่อ>" groups, or, for a
// town without them (กรุงเทพฯ, ตัวเมือง), the places on the ride map (MapPlaces). Build it last.
public static class TouristMap
{
    public struct Sight
    {
        public string name;
        public Vector3 at;
    }

    private const float MapSize = 2.6f, MapY = 2.35f, MapX = -1.25f;

    // The sights to show: the Attr groups under root, else the ride map's places (not rivers, not travel)
    public static List<Sight> Collect(Transform root, int max = 10)
    {
        var list = new List<Sight>();
        foreach (Transform c in root)
        {
            if (!c.name.StartsWith("Attr")) continue;
            int u = c.name.IndexOf('_');
            if (u < 0) continue;
            list.Add(new Sight { name = c.name.Substring(u + 1), at = Centre(c) });
        }
        if (list.Count == 0)
        {
            // the sights first: temples, fairs, the boxing camp, landmarks, markets, then the rest
            string[] order = { "temple", "fair", "boxing", "city", "market", "island", "hotel", "food", "shop", "hospital" };
            var places = MapPlaces.Find();
            foreach (var icon in order)
                foreach (var p in places)
                    if (!p.water && p.icon == icon) list.Add(new Sight { name = p.name, at = p.at });
        }
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
        return list;
    }

    private static Vector3 Centre(Transform t)
    {
        var rs = t.GetComponentsInChildren<MeshRenderer>();
        if (rs.Length == 0) return t.position;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    // Short name for the board: up to the first " · "
    private static string Short(string name)
    {
        int i = name.IndexOf(" · ");
        return i > 0 ? name.Substring(0, i) : name;
    }

    /// <param name="half">half the drawn area (the ground), metres</param>
    /// <param name="ring">the ring road's centre line (0 = none)</param>
    /// <param name="sea">+1 the sea to the north, 0 none</param>
    public static Transform Build(Transform root, string province, List<Sight> sights, Vector3 at, float yaw,
        float half = 120f, float ring = 94f, float railZ = 62f, float avenueZ = -11.5f, int sea = 0)
    {
        var t = PrimitiveKit.Group("TouristMap_แผนที่ท่องเที่ยว", root, at, yaw).transform;
        var frame = PrimitiveKit.Mat("PK_MapFrame", new Color(0.35f, 0.2f, 0.1f), 0.4f);
        foreach (float x in new[] { -2.75f, 2.75f })
            PrimitiveKit.Box("Post", t, new Vector3(x, 2.15f, 0f), new Vector3(0.18f, 4.3f, 0.18f), frame);
        PrimitiveKit.Box("Panel", t, new Vector3(0f, 2.5f, 0f), new Vector3(5.4f, 3.6f, 0.12f), PrimitiveKit.Mat("PK_MapBoard", new Color(0.96f, 0.94f, 0.86f), 0.2f));
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 4.42f, 0f), new Vector3(5.9f, 0.14f, 0.7f), PrimitiveKit.Mat("PK_RoofRed", new Color(0.72f, 0.2f, 0.12f), 0.45f), collider: false);

        foreach (float side in new[] { 0f, 180f })
            Face(PrimitiveKit.Group("Face", t, Vector3.zero, side).transform, province, sights, t, half, ring, railZ, avenueZ, sea);

        // walk up to it: the list as a card on screen
        var lines = new List<string>();
        for (int i = 0; i < sights.Count; i++) lines.Add($"{i + 1}. {sights[i].name}");
        var zone = new GameObject("SignZone");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 2f, 4f);
        zone.AddComponent<SignPost>().Configure("แผนที่ท่องเที่ยว จังหวัด" + province, string.Join("\n", lines));
        return t;
    }

    private static void Face(Transform f, string province, List<Sight> sights, Transform board, float half, float ring, float railZ, float avenueZ, int sea)
    {
        const float z = -0.07f;
        float s = MapSize / (half * 2f);
        Vector3 P(float wx, float wz, float lift) => new Vector3(MapX + Mathf.Clamp(wx * s, -MapSize * 0.5f + 0.06f, MapSize * 0.5f - 0.06f),
            MapY + Mathf.Clamp(wz * s, -MapSize * 0.5f + 0.06f, MapSize * 0.5f - 0.06f), z - lift);

        var title = PrimitiveKit.Mat("PK_MapTitle", new Color(0.1f, 0.35f, 0.25f), 0.3f);
        PrimitiveKit.Box("TitleBar", f, new Vector3(0f, 4.0f, z + 0.01f), new Vector3(5.2f, 0.5f, 0.01f), title, collider: false);
        PrimitiveKit.Label("แผนที่ท่องเที่ยว  จังหวัด" + province + "  ·  TOURIST MAP", f, new Vector3(0f, 4.0f, z - 0.01f), 0.03f, Color.white);

        // the map: land, sea, ring road, avenue, railway
        PrimitiveKit.Box("Land", f, new Vector3(MapX, MapY, z + 0.005f), new Vector3(MapSize, MapSize, 0.01f), PrimitiveKit.Mat("PK_MapLand", new Color(0.78f, 0.88f, 0.62f), 0.1f), collider: false);
        var road = PrimitiveKit.Mat("PK_MapRoad", new Color(0.55f, 0.55f, 0.58f), 0.2f);
        if (sea > 0)
        {
            float from = (ring + 20f) * s;
            PrimitiveKit.Box("Sea", f, new Vector3(MapX, MapY + (from + MapSize * 0.5f) * 0.5f, z), new Vector3(MapSize, MapSize * 0.5f - from, 0.01f), PrimitiveKit.Mat("PK_MapSea", new Color(0.45f, 0.72f, 0.92f), 0.3f), collider: false);
        }
        if (ring > 0f)
        {
            float r = ring * s, w = 0.035f;
            PrimitiveKit.Box("Ring", f, new Vector3(MapX, MapY + r, z), new Vector3(r * 2f + w, w, 0.01f), road, collider: false);
            PrimitiveKit.Box("Ring", f, new Vector3(MapX, MapY - r, z), new Vector3(r * 2f + w, w, 0.01f), road, collider: false);
            PrimitiveKit.Box("Ring", f, new Vector3(MapX - r, MapY, z), new Vector3(w, r * 2f + w, 0.01f), road, collider: false);
            PrimitiveKit.Box("Ring", f, new Vector3(MapX + r, MapY, z), new Vector3(w, r * 2f + w, 0.01f), road, collider: false);
        }
        PrimitiveKit.Box("Avenue", f, new Vector3(MapX, MapY + avenueZ * s, z), new Vector3(MapSize, 0.05f, 0.01f), road, collider: false);
        if (railZ != 0f)
            PrimitiveKit.Box("Railway", f, new Vector3(MapX, MapY + railZ * s, z), new Vector3(MapSize, 0.018f, 0.01f), PrimitiveKit.Mat("PK_MapRail", new Color(0.3f, 0.2f, 0.15f)), collider: false);
        PrimitiveKit.Label("N ↑", f, new Vector3(MapX + MapSize * 0.5f - 0.15f, MapY + MapSize * 0.5f - 0.12f, z - 0.02f), 0.016f, new Color(0.2f, 0.2f, 0.2f));

        // the pins, numbered
        var pin = PrimitiveKit.Mat("PK_MapPin", new Color(0.85f, 0.12f, 0.1f), 0.5f);
        for (int i = 0; i < sights.Count; i++)
        {
            var w = board.parent.InverseTransformPoint(sights[i].at); // in the town root's space
            var p = P(w.x, w.z, 0.02f);
            PrimitiveKit.Cylinder("Pin", f, p, new Vector3(0.17f, 0.01f, 0.17f), pin, new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Label((i + 1).ToString(), f, p + new Vector3(0f, 0f, -0.02f), 0.012f, Color.white);
        }
        // you are here
        var here = board.localPosition;
        var hp = P(here.x, here.z, 0.02f);
        PrimitiveKit.Cylinder("Here", f, hp, new Vector3(0.13f, 0.01f, 0.13f), PrimitiveKit.Mat("PK_MapHere", new Color(0.1f, 0.4f, 0.9f), 0.5f), new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Label("คุณอยู่ที่นี่", f, hp + new Vector3(0f, -0.13f, -0.02f), 0.011f, new Color(0.1f, 0.3f, 0.75f));

        // the list
        var lines = new List<string>();
        for (int i = 0; i < sights.Count; i++) lines.Add($"{i + 1}. {Short(sights[i].name)}");
        var list = PrimitiveKit.Label(string.Join("\n", lines), f, new Vector3(0.25f, MapY + MapSize * 0.5f - 0.05f, z - 0.01f), 0.019f, new Color(0.2f, 0.12f, 0.05f));
        list.anchor = TextAnchor.UpperLeft;
        list.alignment = TextAlignment.Left;
        list.lineSpacing = 1.1f;
    }
}
