using UnityEngine;

// The ring road's bridges over the river (CityRingRoad, RingSpec with a river): the road climbs a long
// ramp on an embankment, crosses the water on a deck high enough for the royal barges to pass under
// (RoyalBarges' tallest figureheads are under 6 m) and comes down the far side. Footways and a parapet
// either side, lamps on the deck. Two looks, after Bangkok's river bridges:
//   memorial  the steel through-arch of สะพานพระพุทธยอดฟ้า (สะพานพุทธ): two grey-green arch trusses
//             spring from the river piers over the road, hangers down to the deck, braced overhead,
//             stone portals with gilded tops at either end
//   otherwise the cable-stayed สะพานพระราม 8: one tall white inverted-Y pylon on the far bank with a
//             gilded finial, yellow cables fanning down to both edges of the deck either side of it
public static class RiverBridges
{
    private const float DeckHalf = 4.8f;   // road 3.2 m each side of the centre line + a footway
    private const float Slab = 0.6f;

    public static void Build(Transform parent, RingSpec ring, float x, bool memorial)
    {
        var profile = ring.Profile; // (ramp foot, bank, bank, ramp foot) going north
        float z0 = profile[0].z, zA = profile[1].z, zB = profile[2].z, z1 = profile[3].z, h = ring.BridgeTop;
        var t = PrimitiveKit.Group(memorial ? "Bridge_สะพานพระพุทธยอดฟ้า" : "Bridge_สะพานพระราม8", parent, new Vector3(x, 0f, 0f)).transform;
        var concrete = PrimitiveKit.Mat("BridgeConcrete", new Color(0.74f, 0.73f, 0.7f));
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var paint = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        var white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        float Height(float z) => z <= zA ? h * Mathf.InverseLerp(z0, zA, z) : z >= zB ? h * Mathf.InverseLerp(z1, zB, z) : h;

        // the deck over the water and the two ramps, each a slab with the road on top and a parapet
        void Span(string name, float za, float zb)
        {
            float ya = Height(za), yb = Height(zb), dz = zb - za, dy = yb - ya;
            float len = Mathf.Sqrt(dz * dz + dy * dy), pitch = -Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
            var mid = new Vector3(0f, (ya + yb) * 0.5f, (za + zb) * 0.5f);
            var rot = new Vector3(pitch, 0f, 0f);
            PrimitiveKit.Box(name, t, mid + Vector3.down * (Slab * 0.5f), new Vector3(DeckHalf * 2f, Slab, len), concrete, rot);
            PrimitiveKit.Box(name + "Road", t, mid + Vector3.up * 0.012f, new Vector3(CityRingRoad.HalfWidth * 2f, 0.02f, len), asphalt, rot, collider: false);
            foreach (float s in new[] { -1f, 1f })
            {
                PrimitiveKit.Box("Parapet", t, mid + new Vector3(s * (DeckHalf - 0.15f), 0.5f, 0f), new Vector3(0.3f, 1f, len), concrete, rot);
                PrimitiveKit.Box("Kerb", t, mid + new Vector3(s * (CityRingRoad.HalfWidth + 0.1f), 0.08f, 0f), new Vector3(0.2f, 0.16f, len), white, rot, collider: false);
            }
            for (float d = 2f; d < len - 1f; d += 4f)
                PrimitiveKit.Box("Dash", t, mid + Quaternion.Euler(rot) * new Vector3(0f, 0.03f, d - len * 0.5f), new Vector3(0.12f, 0.005f, 1.8f), paint, rot, collider: false);
        }
        Span("Ramp", z0, zA);
        Span("Deck", zA, zB);
        Span("Ramp", zB, z1);

        // the embankment walls under the ramps (open under the deck: the promenades pass beneath)
        foreach (var (a, b) in new[] { (z0, zA), (zB, z1) })
            for (float z = a; z < b - 0.01f; z += 1.5f)
            {
                float top = Mathf.Min(Height(z), Height(Mathf.Min(b, z + 1.5f))) - Slab;
                if (top > 0.15f)
                    PrimitiveKit.Box("Embankment", t, new Vector3(0f, top * 0.5f, z + 0.75f), new Vector3(DeckHalf * 2f - 0.2f, top, 1.5f), concrete);
            }
        // piers at the river's edges
        foreach (float z in new[] { ring.RiverZ - ring.RiverHalf, ring.RiverZ + ring.RiverHalf })
        {
            PrimitiveKit.Box("Pier", t, new Vector3(0f, (h - Slab) * 0.5f, z), new Vector3(DeckHalf * 2f - 1f, h - Slab, 1.8f), concrete);
            PrimitiveKit.Box("PierCap", t, new Vector3(0f, h - Slab - 0.25f, z), new Vector3(DeckHalf * 2f + 0.4f, 0.5f, 2.4f), concrete, collider: false);
        }
        // lamps along the deck
        for (float z = zA + 4f; z < zB - 2f; z += 12f)
            foreach (float s in new[] { -1f, 1f })
                StreetFurniture.LampPost(parent, new Vector3(x + s * (DeckHalf - 0.45f), h, z)); // not merged: they light up

        if (memorial) MemorialArches(t, ring, h);
        else CableStays(t, ring, h, zB, z1);
        MeshKit.MergeByMaterial(t, memorial ? "Bridges/Memorial" : "Bridges/Rama8");
        foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) // a bridge this size should cast its shadow
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    // A stick from a to b
    private static void Beam(string name, Transform t, Vector3 a, Vector3 b, float w, float d, Material m, bool collider = false)
    {
        var dir = b - a;
        if (dir.sqrMagnitude < 1e-4f) return;
        var up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        PrimitiveKit.Box(name, t, (a + b) * 0.5f, new Vector3(w, d, dir.magnitude), m, Quaternion.LookRotation(dir, up).eulerAngles, collider);
    }

    // ── สะพานพระพุทธยอดฟ้า: steel through-arches ──────────────────────────────────
    private static void MemorialArches(Transform t, RingSpec ring, float h)
    {
        var steel = PrimitiveKit.Mat("BridgeSteel", new Color(0.5f, 0.6f, 0.58f), 0.45f, 0.35f);
        var stone = PrimitiveKit.Mat("PortalStone", new Color(0.86f, 0.84f, 0.78f));
        var gold = PrimitiveKit.Mat("TempleGold", new Color(0.95f, 0.75f, 0.2f), 0.75f, 0.8f);
        float za = ring.RiverZ - ring.RiverHalf, zb = ring.RiverZ + ring.RiverHalf;
        const int n = 16;
        const float rise = 8.5f, depth = 1.5f, rib = 3.9f;
        Vector3 Upper(float s, int i) { float u = i / (float)n, k = 1f - (2f * u - 1f) * (2f * u - 1f); return new Vector3(s * rib, h + 0.2f + rise * k, Mathf.Lerp(za, zb, u)); }
        Vector3 Lower(float s, int i) { float u = i / (float)n, k = 1f - (2f * u - 1f) * (2f * u - 1f); return new Vector3(s * rib, h + 0.2f + (rise - depth) * k, Mathf.Lerp(za, zb, u)); }
        foreach (float s in new[] { -1f, 1f })
        {
            for (int i = 0; i < n; i++)
            {
                Beam("ArchTop", t, Upper(s, i), Upper(s, i + 1), 0.55f, 0.6f, steel);
                Beam("ArchBottom", t, Lower(s, i), Lower(s, i + 1), 0.45f, 0.45f, steel);
                Beam("Web", t, i % 2 == 0 ? Upper(s, i) : Lower(s, i), i % 2 == 0 ? Lower(s, i + 1) : Upper(s, i + 1), 0.22f, 0.22f, steel);
                if (i > 0) Beam("Post", t, Upper(s, i), Lower(s, i), 0.25f, 0.25f, steel);
                if (i > 0 && i % 2 == 0) Beam("Hanger", t, Lower(s, i), new Vector3(s * rib, h, Lower(s, i).z), 0.12f, 0.12f, steel);
            }
            // the chord along the deck edge the hangers carry
            Beam("Tie", t, new Vector3(s * rib, h + 0.25f, za), new Vector3(s * rib, h + 0.25f, zb), 0.4f, 0.5f, steel, collider: true);
        }
        // overhead bracing where it is well clear of the traffic
        for (int i = 2; i <= n - 2; i++)
        {
            var a = Upper(-1f, i); var b = Upper(1f, i);
            if (a.y - h < 6f) continue;
            Beam("Brace", t, a, b, 0.3f, 0.35f, steel);
            if (i < n - 2) { Beam("XBrace", t, a, Upper(1f, i + 1), 0.15f, 0.15f, steel); Beam("XBrace", t, b, Upper(-1f, i + 1), 0.15f, 0.15f, steel); }
        }
        // stone portals at both ends of the river span, the bridge's name on them
        foreach (float z in new[] { za - 2.2f, zb + 2.2f })
        {
            float face = z < ring.RiverZ ? -1f : 1f;
            foreach (float s in new[] { -1f, 1f })
            {
                var p = new Vector3(s * (DeckHalf + 0.6f), h, z);
                PrimitiveKit.Box("Portal", t, p + new Vector3(0f, 1.8f, 0f), new Vector3(1.1f, 3.6f, 1.1f), stone, collider: false);
                PrimitiveKit.Box("PortalCap", t, p + new Vector3(0f, 3.75f, 0f), new Vector3(1.4f, 0.3f, 1.4f), stone, collider: false);
                PrimitiveKit.Cylinder("PortalFinial", t, p + new Vector3(0f, 4.4f, 0f), new Vector3(0.6f, 0.5f, 0.6f), gold, collider: false);
                PrimitiveKit.Sphere("PortalTop", t, p + new Vector3(0f, 5f, 0f), Vector3.one * 0.45f, gold);
            }
            var plate = new Vector3(-(DeckHalf + 0.6f), h + 2.4f, z + face * 0.57f);
            PrimitiveKit.Label("สะพานพระพุทธยอดฟ้า", t, plate, 0.028f, new Color(0.35f, 0.25f, 0.05f), face < 0f ? 0f : 180f);
            PrimitiveKit.Label("สะพานพระพุทธยอดฟ้า", t, new Vector3(-plate.x, plate.y, plate.z), 0.028f, new Color(0.35f, 0.25f, 0.05f), face < 0f ? 0f : 180f);
        }
    }

    // ── สะพานพระราม 8: one inverted-Y pylon, cables fanning both ways ─────────────────
    private static void CableStays(Transform t, RingSpec ring, float h, float zB, float z1)
    {
        var white = PrimitiveKit.Mat("PylonWhite", new Color(0.9f, 0.89f, 0.86f));
        var cable = PrimitiveKit.Mat("CableYellow", new Color(1f, 0.78f, 0.08f), 0.5f, 0.2f);
        var gold = PrimitiveKit.Mat("TempleGold", new Color(0.95f, 0.75f, 0.2f), 0.75f, 0.8f);
        float pz = zB + 2f;                       // on the far bank, just past the river span
        float joint = h + 15f, top = h + 30f, foot = 6.2f;
        foreach (float s in new[] { -1f, 1f })
            Beam("PylonLeg", t, new Vector3(s * foot, 0f, pz), new Vector3(0f, joint + 0.8f, pz), 1.3f, 1.6f, white, collider: true);
        PrimitiveKit.Box("PylonBeam", t, new Vector3(0f, 1.2f, pz), new Vector3(foot * 2f + 1.6f, 2.4f, 2f), white);
        PrimitiveKit.Box("Mast", t, new Vector3(0f, (joint + top) * 0.5f, pz), new Vector3(1.6f, top - joint, 2f), white, collider: false);
        PrimitiveKit.Box("MastHead", t, new Vector3(0f, top + 0.6f, pz), new Vector3(1.2f, 1.2f, 1.6f), white, collider: false);
        // the gilded finial (a tiered crown, as on the real pylon's top)
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Cylinder("Finial", t, new Vector3(0f, top + 1.4f + i * 0.7f, pz), new Vector3(1.1f - i * 0.25f, 0.35f, 1.1f - i * 0.25f), gold, collider: false);
        PrimitiveKit.Sphere("FinialTip", t, new Vector3(0f, top + 4.3f, pz), Vector3.one * 0.35f, gold);
        PrimitiveKit.Label("สะพานพระราม ๘", t, new Vector3(0f, joint - 2f, pz - 1.05f), 0.05f, new Color(0.55f, 0.4f, 0.05f));
        PrimitiveKit.Label("สะพานพระราม ๘", t, new Vector3(0f, joint - 2f, pz + 1.05f), 0.05f, new Color(0.55f, 0.4f, 0.05f), 180f);

        float Deck(float z) => z <= zB ? h : h * Mathf.InverseLerp(z1, zB, z);
        const int n = 9;
        float mainFrom = ring.RiverZ - ring.RiverHalf + 1f;  // the main span reaches back over the water
        for (int i = 0; i < n; i++)
        {
            float y = joint + 2f + (top - joint - 2.5f) * (i + 0.5f) / n;   // higher anchor, farther cable
            float zm = Mathf.Lerp(pz - 4f, mainFrom, (i + 1f) / n);
            float zb = Mathf.Lerp(pz + 4f, z1 - 3f, (i + 1f) / n);
            foreach (float s in new[] { -1f, 1f })
            {
                var a = new Vector3(0f, y, pz + s * 0f);
                Beam("Stay", t, a, new Vector3(s * 4.1f, Deck(zm) + 0.3f, zm), 0.09f, 0.09f, cable);
                Beam("Stay", t, a, new Vector3(s * 4.1f, Deck(zb) + 0.3f, zb), 0.09f, 0.09f, cable);
            }
        }
    }
}
