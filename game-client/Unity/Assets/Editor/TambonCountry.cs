using System.Collections.Generic;
using UnityEngine;

// The tambon's countryside around the old 120 m core (BuildTambon makes the ground 200 m now):
//   • ถนนสายรอง north–south at x = 72 that crosses the main road at a marked crossroads (ทางแยก)
//     and runs up to the river over a concrete bridge
//   • แม่น้ำ east–west along the north edge (z 71..89) with five landings for river fishing
//     (FishingSpot water "river" → fishing.lua rolls shop_data RIVER_FISH)
//   • ป่าริมน้ำ along the south bank, ป่าชุมชน down the whole west side, rice paddies in the south-east
public static class TambonCountry
{
    public const float RoadX = 72f;          // the side road
    private const float RoadW = 3.6f;
    private const float MainZ = -8f, MainHalf = 2f; // the main road (TambonBuilder.MainRoad)
    private const float RiverZ = 80f, RiverW = 18f;
    private const float RiverSouth = RiverZ - RiverW * 0.5f; // 71
    private const float Half = 99f;
    private static readonly float[] Landings = { -60f, -30f, 0f, 30f, 52f };

    public static void Build(Transform root)
    {
        var t = PrimitiveKit.Group("TambonCountry_ชนบท", root, Vector3.zero).transform;
        SideRoad(t);
        Crossroads(t);
        River(t);
        Bridge(t);
        RiversideForest(t);
        CommunityForest(t);
        TambonFarm.Build(t); // paddies, farm machines at work and parked, the tambon road traffic
        Grass(t);
    }

    // ── ถนนสายรอง ────────────────────────────────────────────────────────────────
    private static void SideRoad(Transform t)
    {
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("RoadPaintWhite", new Color(0.95f, 0.95f, 0.93f));
        var shoulder = PrimitiveKit.Mat("RoadShoulder", new Color(0.55f, 0.48f, 0.36f));
        void Stretch(float z0, float z1)
        {
            float len = z1 - z0, mid = (z0 + z1) * 0.5f;
            PrimitiveKit.Box("SideRoad_ถนนสายรอง", t, new Vector3(RoadX, 0.02f, mid), new Vector3(RoadW, 0.04f, len), asphalt);
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Shoulder", t, new Vector3(RoadX + s * (RoadW * 0.5f + 0.35f), 0.012f, mid), new Vector3(0.7f, 0.02f, len), shoulder, collider: false);
            for (float z = z0 + 3f; z <= z1 - 3f; z += 4f)
                PrimitiveKit.Box("Dash", t, new Vector3(RoadX, 0.045f, z), new Vector3(0.12f, 0.01f, 1.4f), white, collider: false);
        }
        Stretch(-Half + 3f, MainZ - MainHalf);            // south to the paddies
        Stretch(MainZ + MainHalf, RiverSouth - 5f);       // north to the bridge ramp
        Stretch(RiverZ + RiverW * 0.5f + 5f, Half);        // over the river and on

        // Lamps down the road, pole line along its east side
        for (float z = -80f; z <= 60f; z += 28f)
            if (Mathf.Abs(z - MainZ) > 8f) StreetFurniture.LampPost(t, new Vector3(RoadX - RoadW * 0.5f - 1f, 0f, z));
        StreetFurniture.PoleLine(t, new[] { new Vector3(RoadX + 3f, 0f, 2f), new Vector3(RoadX + 3f, 0f, 30f), new Vector3(RoadX + 3f, 0f, 60f) }, 90f, lamps: false);
        StreetFurniture.PoleLine(t, new[] { new Vector3(RoadX + 3f, 0f, -20f), new Vector3(RoadX + 3f, 0f, -50f), new Vector3(RoadX + 3f, 0f, -80f) }, 90f, lamps: false);
    }

    // ── ทางแยก: zebra crossings on all four arms, stop lines, signs, a flashing amber light ─────────
    private static void Crossroads(Transform t)
    {
        var white = PrimitiveKit.Mat("RoadPaintWhite", new Color(0.95f, 0.95f, 0.93f));
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
        var x = PrimitiveKit.Group("Crossroads_ทางแยก", t, new Vector3(RoadX, 0f, MainZ)).transform;

        // Zebras across the side road (north and south arms): stripes run along z
        foreach (float s in new[] { -1f, 1f })
        {
            float z = s * (MainHalf + 1.6f);
            for (float dx = -RoadW * 0.5f + 0.35f; dx <= RoadW * 0.5f - 0.3f; dx += 0.7f)
                PrimitiveKit.Box("Zebra", x, new Vector3(dx, 0.046f, z), new Vector3(0.4f, 0.01f, 2f), white, collider: false);
            PrimitiveKit.Box("StopLine", x, new Vector3(0f, 0.046f, s * (MainHalf + 3.1f)), new Vector3(RoadW - 0.2f, 0.01f, 0.3f), white, collider: false);
        }
        // Zebras across the main road (east and west arms): stripes run along x
        foreach (float s in new[] { -1f, 1f })
        {
            float dxArm = s * (RoadW * 0.5f + 1.6f);
            for (float dz = -MainHalf + 0.35f; dz <= MainHalf - 0.3f; dz += 0.7f)
                PrimitiveKit.Box("Zebra", x, new Vector3(dxArm, 0.046f, dz), new Vector3(2f, 0.01f, 0.4f), white, collider: false);
        }
        // Clear the main road's centre dashes off the junction box with a plain patch
        PrimitiveKit.Box("JunctionBox", x, new Vector3(0f, 0.044f, 0f), new Vector3(RoadW, 0.006f, MainHalf * 2f - 0.05f),
            PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f)), collider: false);

        // Flashing amber light on a mast at the north-east corner
        float cx = RoadW * 0.5f + 1.2f, cz = MainHalf + 1.2f;
        PrimitiveKit.Cylinder("AmberMast", x, new Vector3(cx, 2.4f, cz), new Vector3(0.14f, 2.4f, 0.14f), steel);
        PrimitiveKit.Box("AmberArm", x, new Vector3(cx - 1.6f, 4.75f, cz), new Vector3(3.2f, 0.1f, 0.1f), steel, collider: false);
        PrimitiveKit.Box("AmberHousing", x, new Vector3(cx - 3.1f, 4.4f, cz), new Vector3(0.4f, 0.6f, 0.4f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        StreetFurniture.OrangeLight(x, new Vector3(cx - 3.1f, 4.4f, cz - 0.22f));
        StreetFurniture.OrangeLight(x, new Vector3(cx - 3.1f, 4.4f, cz + 0.22f));

        // Stop signs for the side road, a sign for the main road, the direction board
        Places.Sign(x, new Vector3(-RoadW * 0.5f - 1f, 0f, -MainHalf - 3.4f), "หยุด\nทางแยกข้างหน้า", 180f);
        Places.Sign(x, new Vector3(RoadW * 0.5f + 1f, 0f, MainHalf + 3.4f), "หยุด\nทางแยกข้างหน้า", 0f);
        Places.Sign(x, new Vector3(-RoadW * 0.5f - 10f, 0f, MainHalf + 1.4f), "ระวัง ทางแยก\nขับช้าลง", 270f);
        Places.Sign(x, new Vector3(-RoadW * 0.5f - 1.4f, 0f, MainHalf + 4.5f),
            "↑ แม่น้ำ · ท่าน้ำตกปลา · ป่าริมน้ำ\n← ตำบล · อบต. · โรงเรียน\n↓ ทุ่งนา", 0f);
        foreach (var p in new[] { new Vector3(-RoadW * 0.5f - 1.2f, 0f, -MainHalf - 1.2f), new Vector3(RoadW * 0.5f + 1.2f, 0f, -MainHalf - 1.2f), new Vector3(-RoadW * 0.5f - 1.2f, 0f, MainHalf + 1.2f) })
            StreetFurniture.LampPost(x, p);
    }

    // ── แม่น้ำ ───────────────────────────────────────────────────────────────────
    private static void River(Transform t)
    {
        var r = PrimitiveKit.Group("River_แม่น้ำ", t, new Vector3(0f, 0f, RiverZ)).transform;
        float len = Half * 2f + 2f;
        var water = PrimitiveKit.Mat("RiverWater", new Color(0.32f, 0.42f, 0.36f), 0.85f);
        var deep = PrimitiveKit.Mat("RiverDeep", new Color(0.24f, 0.34f, 0.3f), 0.85f);
        var sand = PrimitiveKit.Mat("RiverSand", new Color(0.72f, 0.64f, 0.48f));
        var mud = PrimitiveKit.Mat("RiverMud", new Color(0.45f, 0.38f, 0.28f));
        var fishMat = PrimitiveKit.Mat("FishShadow", new Color(0.12f, 0.2f, 0.22f));
        var wood = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.24f, 0.15f));
        var reed = PrimitiveKit.Mat("Reed", new Color(0.42f, 0.55f, 0.22f), 0.1f);
        var reedTop = PrimitiveKit.Mat("ReedTop", new Color(0.55f, 0.42f, 0.25f), 0.1f);

        PrimitiveKit.Box("Water", r, new Vector3(0f, 0.03f, 0f), new Vector3(len, 0.02f, RiverW), water, collider: false);
        PrimitiveKit.Box("Channel", r, new Vector3(0f, 0.035f, 0f), new Vector3(len, 0.02f, RiverW * 0.45f), deep, collider: false);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Bank", r, new Vector3(0f, 0.02f, s * (RiverW * 0.5f + 1.2f)), new Vector3(len, 0.03f, 2.4f), sand, collider: false);
            PrimitiveKit.Box("Mud", r, new Vector3(0f, 0.025f, s * (RiverW * 0.5f + 0.1f)), new Vector3(len, 0.03f, 0.6f), mud, collider: false);
        }

        // Invisible walls on the water, with a gap under the bridge deck and a strip at the south edge for the landings
        void Blocker(float x0, float x1)
        {
            var b = new GameObject("WaterBlocker");
            b.transform.SetParent(r, false);
            b.transform.localPosition = new Vector3((x0 + x1) * 0.5f, 1f, 0.5f);
            b.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, 2f, RiverW - 1f);
        }
        Blocker(-len * 0.5f, RoadX - 3.4f);
        Blocker(RoadX + 3.4f, len * 0.5f);

        // Fish shadows
        var rng = new System.Random(717);
        for (int i = 0; i < 22; i++)
        {
            float fx = Mathf.Lerp(-90f, 90f, i / 21f) + (float)rng.NextDouble() * 4f;
            float fz = ((float)rng.NextDouble() - 0.5f) * (RiverW - 5f);
            float big = i % 7 == 3 ? 2.2f : 1f; // now and then a big one (ปลาบึก?)
            var fish = PrimitiveKit.Sphere("Fish", r, new Vector3(fx, 0.045f, fz), new Vector3(0.22f, 0.01f, 0.65f) * big, fishMat);
            fish.AddComponent<FishSwim>();
        }

        // Reeds in clumps along both banks, kept off the landings and the bridge
        for (int i = 0; i < 60; i++)
        {
            float rx = -96f + (float)rng.NextDouble() * 192f;
            float side = i % 3 == 0 ? 1f : -1f;
            if (Mathf.Abs(rx - RoadX) < 6f) continue;
            bool nearLanding = false;
            foreach (float lx in Landings) if (Mathf.Abs(rx - lx) < 3.5f) nearLanding = true;
            if (nearLanding && side < 0f) continue;
            var clump = PrimitiveKit.Group("Reeds_กก", r, new Vector3(rx, 0f, side * (RiverW * 0.5f - 0.4f))).transform;
            for (int k = 0; k < 6; k++)
            {
                float a = k * 1.3f + i, d = 0.15f + (k % 3) * 0.18f, h = 0.7f + (k % 4) * 0.2f;
                var p = new Vector3(Mathf.Cos(a) * d, h * 0.5f, Mathf.Sin(a) * d);
                PrimitiveKit.Cylinder("Reed", clump, p, new Vector3(0.03f, h * 0.5f, 0.03f), reed, new Vector3(Mathf.Sin(a) * 8f, 0f, Mathf.Cos(a) * 8f), collider: false);
                if (k % 2 == 0) PrimitiveKit.Cylinder("Head", clump, p + Vector3.up * (h * 0.5f + 0.08f), new Vector3(0.05f, 0.1f, 0.05f), reedTop, collider: false);
            }
        }
        for (int i = 0; i < 10; i++)
            Nature.Rock(r, new Vector3(-92f + i * 19f, 0f, (i % 2 == 0 ? 1f : -1f) * (RiverW * 0.5f + 1.6f)), 0.6f + (i % 3) * 0.3f, 900 + i);

        // ท่าน้ำ: wooden landings on piles out over the south edge, each with a fishing zone
        foreach (float lx in Landings)
        {
            var spot = PrimitiveKit.Group("RiverSpot_ท่าน้ำแม่น้ำ", r, new Vector3(lx, 0f, -RiverW * 0.5f + 0.2f)).transform;
            PrimitiveKit.Box("Landing", spot, new Vector3(0f, 0.12f, 0f), new Vector3(3f, 0.16f, 2.4f), wood);
            for (float px = -1.3f; px <= 1.31f; px += 2.6f)
                PrimitiveKit.Cylinder("Pile", spot, new Vector3(px, 0.05f, 1.05f), new Vector3(0.14f, 0.3f, 0.14f), wood, collider: false);
            PrimitiveKit.Box("Step", spot, new Vector3(0f, 0.05f, -1.6f), new Vector3(1.6f, 0.08f, 0.8f), wood);
            var bobber = PrimitiveKit.Group("BobberPoint", spot, new Vector3(0f, 0.1f, 5f));
            var zone = new GameObject("FishingZone");
            zone.transform.SetParent(spot, false);
            zone.transform.localPosition = new Vector3(0f, 1f, 0f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.2f, 2f, 2.6f);
            zone.AddComponent<FishingSpot>().Configure(bobber.transform, "river");
            StreetFurniture.SolarLamp(t, new Vector3(lx + 2.2f, 0f, RiverSouth - 1.6f), 0f);
        }
        Places.Sign(t, new Vector3(RoadX - 5f, 0f, RiverSouth - 3f),
            "แม่น้ำ · ตกปลาแม่น้ำ\nปลาสร้อย ปลากด ปลาตะโกก ปลาสวาย ปลากราย ปลาแรด\nปลาบู่ ปลาชะโด ปลาเทโพ ปลาเค้า ปลายี่สก\nปลาบึก หายากมาก!", 180f);
    }

    // ── สะพานคอนกรีต over the river on the side road ─────────────────────────────────────
    private static void Bridge(Transform t)
    {
        var b = PrimitiveKit.Group("Bridge_สะพาน", t, new Vector3(RoadX, 0f, RiverZ)).transform;
        var concrete = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var rail = PrimitiveKit.Mat("BridgeRailYellow", new Color(0.95f, 0.78f, 0.2f));
        const float w = 6f, deckY = 0.7f, span = RiverW + 4f, ramp = 5f;

        PrimitiveKit.Box("Deck", b, new Vector3(0f, deckY - 0.2f, 0f), new Vector3(w, 0.4f, span), concrete);
        PrimitiveKit.Box("DeckTop", b, new Vector3(0f, deckY + 0.005f, 0f), new Vector3(RoadW, 0.02f, span), asphalt, collider: false);
        float angle = Mathf.Atan2(deckY, ramp) * Mathf.Rad2Deg;
        float rampLen = Mathf.Sqrt(ramp * ramp + deckY * deckY);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Ramp", b, new Vector3(0f, deckY * 0.5f - 0.15f, s * (span * 0.5f + ramp * 0.5f)),
                new Vector3(w, 0.3f, rampLen + 0.1f), concrete, new Vector3(s * angle, 0f, 0f));
            foreach (float sx in new[] { -1f, 1f }) // railings: posts and two yellow-and-white bars
            {
                for (float z = -span * 0.5f; z <= span * 0.5f + 0.01f; z += 2f)
                    PrimitiveKit.Box("Post", b, new Vector3(sx * (w * 0.5f - 0.15f), deckY + 0.5f, z), new Vector3(0.2f, 1f, 0.2f), concrete);
                PrimitiveKit.Box("Rail", b, new Vector3(sx * (w * 0.5f - 0.15f), deckY + 0.95f, 0f), new Vector3(0.24f, 0.14f, span), rail);
                PrimitiveKit.Box("RailLow", b, new Vector3(sx * (w * 0.5f - 0.15f), deckY + 0.5f, 0f), new Vector3(0.18f, 0.1f, span), concrete, collider: false);
            }
        }
        foreach (float z in new[] { -RiverW * 0.25f, 0f, RiverW * 0.25f })
            PrimitiveKit.Cylinder("Pier", b, new Vector3(0f, -0.2f, z), new Vector3(1.2f, 0.7f, 1.2f), concrete, collider: false);
        Places.Sign(t, new Vector3(RoadX + 4f, 0f, RiverSouth - 4.5f), "สะพานข้ามแม่น้ำ", 0f);
    }

    // ── ป่าริมน้ำ: a band of trees along the south bank, a dirt path in front of the landings ─────────
    private static void RiversideForest(Transform t)
    {
        var f = PrimitiveKit.Group("RiversideForest_ป่าริมน้ำ", t, Vector3.zero).transform;
        var dirt = PrimitiveKit.Mat("ForestTrail", new Color(0.5f, 0.4f, 0.28f));
        PrimitiveKit.Box("RiverPath", f, new Vector3((-Half + RoadX - 3f) * 0.5f, 0.014f, RiverSouth - 3.6f),
            new Vector3(RoadX - 3f + Half, 0.012f, 1.8f), dirt, collider: false);
        var rng = new System.Random(5151);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var trunks = new List<Vector3>();
        for (int tries = 0; tries < 700 && trunks.Count < 48; tries++)
        {
            var p = new Vector3(R(-96f, RoadX - 6f), 0f, R(60.5f, RiverSouth - 5.6f));
            if (p.x > -50f && p.x < 50f && p.z < 61f) continue; // the canal forest is already here
            bool crowded = false;
            foreach (var q in trunks) if ((p - q).sqrMagnitude < 3.6f * 3.6f) { crowded = true; break; }
            if (crowded) continue;
            trunks.Add(p);
            if (trunks.Count % 4 == 0) Nature.CoconutPalm(f, p, R(0.85f, 1.1f), trunks.Count);
            else if (trunks.Count % 5 == 0) Places.ShadeTree(f, p, R(0.9f, 1.2f));
            else TempleLife.ForestTree(f, p, R(0.8f, 1.1f), trunks.Count + 500);
        }
        for (int i = 0, n = 0; i < 400 && n < 60; i++)
        {
            var p = new Vector3(R(-97f, RoadX - 5f), 0f, R(60f, RiverSouth - 5f));
            Nature.Bush(f, p, R(0.7f, 1.3f), 600 + n, flowers: n % 9 == 0);
            n++;
        }
    }

    // ── ป่าชุมชน: the whole west side, off the main road ──────────────────────────────────────
    private static void CommunityForest(Transform t)
    {
        var f = PrimitiveKit.Group("CommunityForest_ป่าชุมชน", t, Vector3.zero).transform;
        PrimitiveKit.Box("ForestFloor", f, new Vector3(-78f, 0.008f, -18f), new Vector3(40f, 0.012f, 144f),
            PrimitiveKit.Mat("ForestFloor", new Color(0.2f, 0.27f, 0.13f)), collider: false);
        Places.Sign(t, new Vector3(-55f, 0f, -4.6f), "ป่าชุมชน\nช่วยกันรักษาป่า ห้ามเผา", 0f);
        Places.Sign(t, new Vector3(-55f, 0f, -11.4f), "ป่าชุมชน\nช่วยกันรักษาป่า ห้ามเผา", 180f);
        var rng = new System.Random(6262);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        bool Off(Vector3 p) => p.z > MainZ - 4.5f && p.z < MainZ + 4.5f; // keep the road clear
        var trunks = new List<Vector3>();
        for (int tries = 0; tries < 1500 && trunks.Count < 95; tries++)
        {
            var p = new Vector3(R(-97f, -59f), 0f, R(-90f, 54f));
            if (Off(p)) continue;
            bool crowded = false;
            foreach (var q in trunks) if ((p - q).sqrMagnitude < 4f * 4f) { crowded = true; break; }
            if (crowded) continue;
            trunks.Add(p);
            if (trunks.Count % 8 == 0) Places.ShadeTree(f, p, R(1f, 1.3f));
            else TempleLife.ForestTree(f, p, R(0.9f, 1.35f), trunks.Count + 800);
        }
        for (int i = 0, n = 0; i < 600 && n < 110; i++)
        {
            var p = new Vector3(R(-97.5f, -58f), 0f, R(-91f, 55f));
            if (Off(p)) continue;
            Nature.Bush(f, p, R(0.8f, 1.5f), 1000 + n, flowers: n % 13 == 0);
            n++;
        }
        for (int i = 0; i < 12; i++)
            Nature.Rock(f, new Vector3(R(-96f, -60f), 0f, R(-88f, 52f)), R(0.7f, 1.4f), 1200 + i);
    }

    // ── grass on everything new (the old core keeps its own GrassField) ──────────────────────────
    private static void Grass(Transform t)
    {
        Nature.GrassField(t, Vector3.zero, new Vector2(196f, 196f), 3600, 31,
            new Vector4(-49f, -49f, 49f, 60f),                              // the old core and the canal forest
            new Vector4(-100f, MainZ - 2.6f, 100f, MainZ + 2.6f),           // main road
            new Vector4(RoadX - 2.6f, -100f, RoadX + 2.6f, 100f),           // side road
            new Vector4(-100f, RiverSouth - 4.6f, 100f, 100f),              // river, banks and the path
            new Vector4(-98f, -91f, -58f, 56f),                             // community forest
            new Vector4(RoadX + 4.5f, -93f, 99f, -17f),                     // paddies
            new Vector4(RoadX - 14f, -61f, RoadX - 3f, -43f),                // machinery shed
            new Vector4(44f, -6f, 60f, 12f));                               // petrol station
    }
}
