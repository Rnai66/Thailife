using UnityEngine;

// The hotel's grounds (HotelBiz), on the strip of land behind it and the café, down to the house lots'
// fence: a drive in from the road between the café and the hotel, a car park with cars in the bays, a
// pool deck with a pool, sun loungers and umbrellas, a thatched pool bar with stools, garden lamps and
// palms, a low wall round it all. "Spots" are where HotelGuests (runtime) puts people: the doorman at
// the front, the bartender, the lifeguard and the car-park attendant (always, for the owner) and the
// guests on the loungers, at the bar, in the pool and chatting on the deck (more with more staff).
// World coordinates (the group sits at the origin): the hotel is at (82, -31), its front to the road
// (+Z), its back wall at z -35.5; the café's east wall at x 70.9; the ring road from x 91.
public static class HotelGrounds
{
    private const float Back = -35.8f, South = -46.4f;     // from the hotel's back wall to the lots' fence
    private const float ParkW = 63.6f, DeckW = 75.8f, East = 90.3f;

    public static void Build(Transform root)
    {
        var g = PrimitiveKit.Group("HotelGrounds_บริเวณโรงแรม", root, Vector3.zero).transform;
        var spots = PrimitiveKit.Group("Spots", g, Vector3.zero).transform;
        Drive(g);
        CarPark(g, spots);
        Pool(g, spots);
        PoolBar(g, spots);
        Garden(g);
        // the doorman by the front door, under the canopy
        Spot(spots, "Staff", new Vector3(80.3f, 0f, -25.3f), 0f);
        Spot(spots, "Stand", new Vector3(84.2f, 0f, -24.8f), 200f); // a guest waiting for a taxi
        g.gameObject.AddComponent<HotelGuests>();
    }

    // A marker for HotelGuests: name says who ("Staff" always, "Sit" on a seat at height y, "Swim"
    // waist-deep in the pool, "Stand" on their feet), yaw the way they face
    private static void Spot(Transform spots, string kind, Vector3 at, float yaw)
    {
        var s = new GameObject(kind).transform;
        s.SetParent(spots, false);
        s.localPosition = at;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // ── The drive and the car park ─────────────────────────────────────────────────────────
    private static void Drive(Transform g)
    {
        var tar = PrimitiveKit.Mat("HotelAsphalt", new Color(0.24f, 0.24f, 0.25f), 0.2f);
        var kerb = PrimitiveKit.Mat("HotelKerb", new Color(0.78f, 0.77f, 0.74f));
        // between the café and the hotel, from the pavement to the car park
        PrimitiveKit.Box("Driveway", g, new Vector3(73.4f, 0.012f, -30.2f), new Vector3(4.4f, 0.02f, 11.4f), tar, collider: false);
        foreach (float x in new[] { 71.15f, 75.65f })
            PrimitiveKit.Box("Kerb", g, new Vector3(x, 0.06f, -30.2f), new Vector3(0.12f, 0.12f, 11.4f), kerb, collider: false);
        var white = PrimitiveKit.Mat("HotelLine", new Color(0.95f, 0.95f, 0.92f), 0.3f);
        for (float z = -25.6f; z > -35f; z -= 2f)
            PrimitiveKit.Box("CentreLine", g, new Vector3(73.4f, 0.025f, z), new Vector3(0.1f, 0.01f, 1f), white, collider: false);
        // the way in: a gold sign on a post at the pavement
        var post = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        PrimitiveKit.Box("SignPost", g, new Vector3(71.0f, 1.1f, -24.9f), new Vector3(0.1f, 2.2f, 0.1f), post);
        PrimitiveKit.Box("DriveSign", g, new Vector3(71.0f, 2.05f, -24.9f), new Vector3(1.3f, 0.55f, 0.08f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);
        Thai(PrimitiveKit.Label("ทางเข้า · ที่จอดรถ", g, new Vector3(71.0f, 2.05f, -24.85f), 0.03f, new Color(0.35f, 0.15f, 0.05f), 180f));
    }

    private static void CarPark(Transform g, Transform spots)
    {
        var tar = PrimitiveKit.Mat("HotelAsphalt", new Color(0.24f, 0.24f, 0.25f), 0.2f);
        var white = PrimitiveKit.Mat("HotelLine", new Color(0.95f, 0.95f, 0.92f), 0.3f);
        PrimitiveKit.Box("CarPark", g, new Vector3((ParkW + DeckW) * 0.5f, 0.012f, (Back + South) * 0.5f), new Vector3(DeckW - ParkW, 0.02f, Back - South), tar, collider: false);
        // four bays along the back wall, 2.5 m by 5, nose out to the aisle
        float bayZ = South + 2.6f;
        for (int i = 0; i <= 4; i++)
            PrimitiveKit.Box("BayLine", g, new Vector3(ParkW + 0.1f + i * 2.5f, 0.025f, bayZ), new Vector3(0.1f, 0.01f, 5f), white, collider: false);
        PrimitiveKit.Box("BayEnd", g, new Vector3(ParkW + 5.1f, 0.025f, bayZ + 2.5f), new Vector3(10f, 0.01f, 0.1f), white, collider: false);
        var stop = PrimitiveKit.Mat("WheelStop", new Color(0.95f, 0.8f, 0.15f));
        Color[] paints = { new Color(0.92f, 0.92f, 0.9f), new Color(0.08f, 0.09f, 0.1f), new Color(0.55f, 0.08f, 0.1f), new Color(0.6f, 0.62f, 0.65f) };
        for (int i = 0; i < 4; i++)
        {
            float x = ParkW + 1.35f + i * 2.5f;
            PrimitiveKit.Box("WheelStop", g, new Vector3(x, 0.06f, South + 0.6f), new Vector3(1.4f, 0.12f, 0.2f), stop);
            if (i == 2) continue; // a free bay
            var car = PrimitiveKit.Group("ParkedCar", g, new Vector3(x, 0f, bayZ - 0.1f), 0f).transform;
            CityTraffic.Sedan(car, paints[i], 4.4f, 1.8f, chrome: i == 1);
            var body = new GameObject("Body");
            body.transform.SetParent(car, false);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.AddComponent<BoxCollider>().size = new Vector3(1.8f, 1.5f, 4.4f);
        }
        // a P sign at the drive's end, and the attendant by it
        var post = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        PrimitiveKit.Box("ParkPost", g, new Vector3(ParkW + 0.4f, 1.2f, Back - 0.4f), new Vector3(0.1f, 2.4f, 0.1f), post);
        PrimitiveKit.Box("ParkSign", g, new Vector3(ParkW + 0.4f, 2.35f, Back - 0.4f), new Vector3(0.7f, 0.7f, 0.06f), PrimitiveKit.Glow("ParkBlue", new Color(0.15f, 0.4f, 0.95f), 1.3f), collider: false);
        PrimitiveKit.Label("P", g, new Vector3(ParkW + 0.4f, 2.35f, Back - 0.36f), 0.12f, Color.white, 180f);
        Thai(PrimitiveKit.Label("ที่จอดรถโรงแรม", g, new Vector3(ParkW + 0.4f, 1.85f, Back - 0.36f), 0.022f, Color.white, 180f));
        Spot(spots, "Staff", new Vector3(72.6f, 0f, -37.4f), 225f);   // the car-park attendant
        Spot(spots, "Stand", new Vector3(ParkW + 3.9f, 0f, bayZ + 3.1f), 160f); // a guest back at their car
    }

    // ── The pool ──────────────────────────────────────────────────────────────────────────
    private const float PoolX = 84.5f, PoolZ = -41.3f, PoolHalfX = 4.5f, PoolHalfZ = 2.3f;

    private static void Pool(Transform g, Transform spots)
    {
        var deck = PrimitiveKit.Mat("PoolDeck", new Color(0.88f, 0.84f, 0.76f), 0.25f);
        var joint = PrimitiveKit.Mat("PoolDeckJoint", new Color(0.74f, 0.7f, 0.62f), 0.2f);
        PrimitiveKit.Box("PoolDeck", g, new Vector3((DeckW + East) * 0.5f, 0.015f, (Back + South) * 0.5f), new Vector3(East - DeckW, 0.02f, Back - South), deck, collider: false);
        for (float x = DeckW + 1.2f; x < East; x += 1.2f)
            PrimitiveKit.Box("DeckJoint", g, new Vector3(x, 0.027f, (Back + South) * 0.5f), new Vector3(0.03f, 0.004f, Back - South), joint, collider: false);

        // the water, a tiled band round it and the coping stones; steps and a chrome ladder
        var water = PrimitiveKit.Mat("PoolWater", new Color(0.18f, 0.62f, 0.82f), 0.97f);
        var tile = PrimitiveKit.Mat("PoolTile", new Color(0.1f, 0.32f, 0.55f), 0.8f);
        var coping = PrimitiveKit.Mat("PoolCoping", new Color(0.95f, 0.94f, 0.9f), 0.35f);
        PrimitiveKit.Box("PoolTileBand", g, new Vector3(PoolX, 0.03f, PoolZ), new Vector3(PoolHalfX * 2f, 0.02f, PoolHalfZ * 2f), tile, collider: false);
        PrimitiveKit.Box("PoolWater", g, new Vector3(PoolX, 0.05f, PoolZ), new Vector3(PoolHalfX * 2f - 0.3f, 0.02f, PoolHalfZ * 2f - 0.3f), water, collider: false);
        for (float x = PoolX - PoolHalfX + 1.5f; x < PoolX + PoolHalfX - 1f; x += 1.5f) // lane marks on the floor, seen through
            PrimitiveKit.Box("LaneMark", g, new Vector3(x, 0.062f, PoolZ), new Vector3(0.12f, 0.004f, PoolHalfZ * 2f - 1.2f), tile, collider: false);
        PrimitiveKit.Box("Coping", g, new Vector3(PoolX, 0.08f, PoolZ + PoolHalfZ + 0.15f), new Vector3(PoolHalfX * 2f + 0.6f, 0.1f, 0.3f), coping, collider: false);
        PrimitiveKit.Box("Coping", g, new Vector3(PoolX, 0.08f, PoolZ - PoolHalfZ - 0.15f), new Vector3(PoolHalfX * 2f + 0.6f, 0.1f, 0.3f), coping, collider: false);
        PrimitiveKit.Box("Coping", g, new Vector3(PoolX - PoolHalfX - 0.15f, 0.08f, PoolZ), new Vector3(0.3f, 0.1f, PoolHalfZ * 2f), coping, collider: false);
        PrimitiveKit.Box("Coping", g, new Vector3(PoolX + PoolHalfX + 0.15f, 0.08f, PoolZ), new Vector3(0.3f, 0.1f, PoolHalfZ * 2f), coping, collider: false);
        var glow = PrimitiveKit.Glow("PoolLight", new Color(0.55f, 0.9f, 1f), 1.6f);
        foreach (float x in new[] { PoolX - 2.5f, PoolX + 2.5f })
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("PoolLight", g, new Vector3(x, 0.058f, PoolZ + side * (PoolHalfZ - 0.2f)), new Vector3(0.3f, 0.01f, 0.12f), glow, collider: false);
        var chrome = PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.95f, 1f);
        foreach (float dz in new[] { -0.3f, 0.3f })
        {
            PrimitiveKit.Cylinder("LadderRail", g, new Vector3(PoolX + PoolHalfX - 0.25f, 0.45f, PoolZ + 1.2f + dz), new Vector3(0.05f, 0.45f, 0.05f), chrome, collider: false);
            PrimitiveKit.Cylinder("LadderTop", g, new Vector3(PoolX + PoolHalfX - 0.05f, 0.88f, PoolZ + 1.2f + dz), new Vector3(0.05f, 0.2f, 0.05f), chrome, new Vector3(0f, 0f, 90f), collider: false);
        }
        // a rubber ring floating, a lifeguard's chair at the east end
        PrimitiveKit.Cylinder("FloatRing", g, new Vector3(PoolX - 1.6f, 0.08f, PoolZ + 0.7f), new Vector3(0.7f, 0.04f, 0.7f), PrimitiveKit.Mat("FloatRed", new Color(0.95f, 0.3f, 0.25f), 0.6f), collider: false);
        var white = PrimitiveKit.Mat("LifeguardWhite", new Color(0.96f, 0.96f, 0.95f), 0.4f);
        float lx = East - 0.8f;
        foreach (float dz in new[] { -0.3f, 0.3f })
            PrimitiveKit.Box("ChairLeg", g, new Vector3(lx, 0.8f, PoolZ + dz), new Vector3(0.08f, 1.6f, 0.08f), white, collider: false);
        PrimitiveKit.Box("ChairSeat", g, new Vector3(lx, 1.6f, PoolZ), new Vector3(0.6f, 0.08f, 0.7f), white);

        // sun loungers along the hotel's back, facing the water, umbrellas between them
        float rowZ = Back - 1.25f;
        float[] loungers = { 80.6f, 82.7f, 86.3f, 88.4f };
        foreach (float x in loungers)
        {
            Lounger(g, new Vector3(x, 0f, rowZ), 180f);
            Spot(spots, "Sit", new Vector3(x, 0.42f, rowZ), 180f);
        }
        Umbrella(g, new Vector3(84.5f, 0f, rowZ - 0.1f), new Color(0.95f, 0.95f, 0.92f), new Color(0.15f, 0.45f, 0.75f));
        Umbrella(g, new Vector3(78.6f, 0f, rowZ - 0.1f), new Color(0.95f, 0.95f, 0.92f), new Color(0.15f, 0.45f, 0.75f));
        // two more on the far side, facing back
        foreach (float x in new[] { 82.0f, 87.0f })
        {
            Lounger(g, new Vector3(x, 0f, South + 1.3f), 0f);
            Spot(spots, "Sit", new Vector3(x, 0.42f, South + 1.3f), 0f);
        }
        Umbrella(g, new Vector3(84.5f, 0f, South + 1.2f), new Color(0.98f, 0.85f, 0.35f), new Color(0.95f, 0.95f, 0.92f));
        // ลงสระ: walk to the edge and get in (PoolSwim), anyone may swim
        var swim = new GameObject("PoolSwim_ลงสระ");
        swim.transform.SetParent(g, false);
        swim.transform.position = new Vector3(PoolX, 0f, PoolZ);
        var zone = swim.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 1f, 0f);
        zone.size = new Vector3(PoolHalfX * 2f + 1.4f, 2f, PoolHalfZ * 2f + 1.4f);
        swim.AddComponent<PoolSwim>().Configure(new Vector2(PoolHalfX - 0.15f, PoolHalfZ - 0.15f), 0.05f, "สระว่ายน้ำโรงแรม");

        // swimmers, the lifeguard on the deck by the chair, guests chatting at the corner
        Spot(spots, "Swim", new Vector3(PoolX - 2.4f, -1.05f, PoolZ + 0.6f), 120f);
        Spot(spots, "Swim", new Vector3(PoolX + 1.0f, -1.05f, PoolZ - 1.0f), 300f);
        Spot(spots, "Swim", new Vector3(PoolX + 2.9f, -1.05f, PoolZ + 0.9f), 210f);
        Spot(spots, "Swim", new Vector3(PoolX - 0.6f, -1.05f, PoolZ + 1.3f), 160f);
        Spot(spots, "Staff", new Vector3(East - 0.8f, 0f, PoolZ - 1.0f), 270f);
        Spot(spots, "Stand", new Vector3(78.9f, 0f, -38.6f), 60f);
        Spot(spots, "Stand", new Vector3(79.7f, 0f, -38.2f), 240f);
    }

    // A sun lounger: a white frame, a cushion, the back raised at the head end; you sit facing +Z
    private static void Lounger(Transform g, Vector3 at, float yaw)
    {
        var l = PrimitiveKit.Group("SunLounger", g, at, yaw).transform;
        var frame = PrimitiveKit.Mat("LoungerFrame", new Color(0.95f, 0.95f, 0.93f), 0.5f);
        var cushion = PrimitiveKit.Mat("LoungerCushion", new Color(0.2f, 0.5f, 0.72f), 0.15f);
        PrimitiveKit.Box("Frame", l, new Vector3(0f, 0.25f, 0.15f), new Vector3(0.7f, 0.1f, 1.5f), frame);
        foreach (float x in new[] { -0.3f, 0.3f })
        foreach (float z in new[] { -0.55f, 0.8f })
            PrimitiveKit.Box("Leg", l, new Vector3(x, 0.1f, z), new Vector3(0.05f, 0.2f, 0.05f), frame, collider: false);
        PrimitiveKit.Box("Cushion", l, new Vector3(0f, 0.34f, 0.25f), new Vector3(0.64f, 0.08f, 1.3f), cushion, collider: false);
        PrimitiveKit.Box("Back", l, new Vector3(0f, 0.6f, -0.68f), new Vector3(0.66f, 0.08f, 0.75f), cushion, new Vector3(-55f, 0f, 0f), collider: false);
        var towel = PrimitiveKit.Mat("Towel" + (int)(at.x * 10f) % 3, (int)(at.x * 10f) % 3 == 0 ? new Color(0.98f, 0.98f, 0.96f) : new Color(0.95f, 0.75f, 0.3f), 0.1f);
        PrimitiveKit.Box("Towel", l, new Vector3(0f, 0.39f, 0.65f), new Vector3(0.5f, 0.02f, 0.4f), towel, collider: false);
    }

    private static void Umbrella(Transform g, Vector3 at, Color a, Color b)
    {
        var u = PrimitiveKit.Group("PoolUmbrella", g, at).transform;
        var pole = PrimitiveKit.Mat("UmbrellaPole", new Color(0.55f, 0.4f, 0.25f), 0.3f);
        PrimitiveKit.Cylinder("Pole", u, new Vector3(0f, 1.2f, 0f), new Vector3(0.06f, 1.2f, 0.06f), pole);
        PrimitiveKit.Cylinder("Base", u, new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.05f, 0.45f), pole, collider: false);
        // eight panels round the top, every other one coloured, sloping down from the middle
        var ma = PrimitiveKit.Mat("Canvas" + ColorUtility.ToHtmlStringRGB(a), a, 0.1f);
        var mb = PrimitiveKit.Mat("Canvas" + ColorUtility.ToHtmlStringRGB(b), b, 0.1f);
        for (int i = 0; i < 8; i++)
        {
            float yaw = i * 45f;
            var q = Quaternion.Euler(0f, yaw, 0f);
            PrimitiveKit.Box("Panel", u, new Vector3(0f, 2.3f, 0f) + q * new Vector3(0f, -0.12f, 0.6f), new Vector3(0.95f, 0.03f, 1.25f), i % 2 == 0 ? ma : mb,
                new Vector3(12f, yaw, 0f), collider: false);
        }
        PrimitiveKit.Sphere("Finial", u, new Vector3(0f, 2.42f, 0f), Vector3.one * 0.1f, pole);
    }

    // ── The pool bar: a thatched sala with a counter to the deck and stools ───────────────────
    private static void PoolBar(Transform g, Transform spots)
    {
        const float x = 78.1f, z = -44.9f;
        var wood = PrimitiveKit.Mat("BarTeak", new Color(0.45f, 0.28f, 0.14f), 0.45f);
        var dark = PrimitiveKit.Mat("BarTeakDark", new Color(0.28f, 0.17f, 0.08f), 0.4f);
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.72f, 0.6f, 0.35f), 0.05f);
        var top = PrimitiveKit.Mat("BarTop", new Color(0.92f, 0.9f, 0.85f), 0.75f);
        PrimitiveKit.Box("BarFloor", g, new Vector3(x, 0.06f, z), new Vector3(4.2f, 0.12f, 2.8f), dark, collider: false);
        // the counter faces north, to the pool; the bartender works behind it
        PrimitiveKit.Box("BarCounter", g, new Vector3(x, 0.55f, z + 0.85f), new Vector3(3.4f, 1.0f, 0.55f), wood);
        PrimitiveKit.Box("BarTop", g, new Vector3(x, 1.08f, z + 0.9f), new Vector3(3.6f, 0.06f, 0.75f), top, collider: false);
        PrimitiveKit.Box("BackShelf", g, new Vector3(x, 1.0f, z - 1.15f), new Vector3(3.4f, 1.6f, 0.35f), dark);
        string[] bottles = { "BottleGreen", "BottleAmber", "BottleClear", "BottleBlue" };
        Color[] colours = { new Color(0.2f, 0.55f, 0.3f), new Color(0.85f, 0.5f, 0.15f), new Color(0.85f, 0.9f, 0.92f), new Color(0.2f, 0.45f, 0.85f) };
        for (int i = 0; i < 12; i++)
        {
            int k = (i * 7) % 4;
            PrimitiveKit.Cylinder(bottles[k], g, new Vector3(x - 1.4f + i * 0.25f, 1.48f, z - 0.97f), new Vector3(0.08f, 0.14f, 0.08f),
                PrimitiveKit.Glow(bottles[k], colours[k], 0.6f), collider: false);
        }
        PrimitiveKit.Box("ShelfGlow", g, new Vector3(x, 1.3f, z - 0.96f), new Vector3(3.2f, 0.03f, 0.05f), PrimitiveKit.Glow("BarShelfGlow", new Color(1f, 0.75f, 0.4f), 2f), collider: false);
        foreach (float dx in new[] { -1.9f, 1.9f })
        foreach (float dz in new[] { -1.25f, 1.25f })
            PrimitiveKit.Cylinder("BarPost", g, new Vector3(x + dx, 1.3f, z + dz), new Vector3(0.12f, 1.3f, 0.12f), wood);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("ThatchRoof", g, new Vector3(x, 2.95f, z + side * 0.85f), new Vector3(4.8f, 0.18f, 2.1f), thatch, new Vector3(side * 24f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Ridge", g, new Vector3(x, 3.4f, z), new Vector3(4.9f, 0.14f, 0.2f), dark, collider: false);
        PrimitiveKit.Box("BarSign", g, new Vector3(x, 2.45f, z + 1.4f), new Vector3(1.8f, 0.4f, 0.06f), PrimitiveKit.Glow("PoolBarSign", new Color(0.3f, 0.95f, 0.85f), 1.6f), collider: false);
        PrimitiveKit.Label("POOL BAR", g, new Vector3(x, 2.45f, z + 1.44f), 0.035f, new Color(0.05f, 0.2f, 0.25f), 180f);
        // stools at the counter
        var chrome = PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.95f, 1f);
        foreach (float dx in new[] { -1.1f, 0f, 1.1f })
        {
            PrimitiveKit.Cylinder("StoolPole", g, new Vector3(x + dx, 0.38f, z + 1.6f), new Vector3(0.05f, 0.38f, 0.05f), chrome, collider: false);
            PrimitiveKit.Cylinder("StoolSeat", g, new Vector3(x + dx, 0.76f, z + 1.6f), new Vector3(0.38f, 0.04f, 0.38f), wood);
            Spot(spots, "Sit", new Vector3(x + dx, 0.74f, z + 1.6f), 180f);
        }
        Spot(spots, "Staff", new Vector3(x - 0.4f, 0.12f, z + 0.1f), 0f); // the bartender
        // walk up to the counter from the pool side: the café menu, mocktails included
        TambonBuilder.ShopZone(g, new Vector3(x, 0f, z + 2.3f), "cafe");
    }

    // ── Palms, lamps, the wall round it ─────────────────────────────────────────────────────
    private static void Garden(Transform g)
    {
        var wall = PrimitiveKit.Mat("GardenWall", new Color(0.93f, 0.9f, 0.84f), 0.15f);
        var hedge = PrimitiveKit.Mat("Hedge", new Color(0.2f, 0.45f, 0.2f), 0.1f);
        PrimitiveKit.Box("GardenWall", g, new Vector3((ParkW + East) * 0.5f, 0.45f, South - 0.15f), new Vector3(East - ParkW, 0.9f, 0.25f), wall);
        PrimitiveKit.Box("Hedge", g, new Vector3((DeckW + East) * 0.5f, 1.05f, South - 0.15f), new Vector3(East - DeckW, 0.35f, 0.5f), hedge, collider: false);
        PrimitiveKit.Box("GardenWall", g, new Vector3(East + 0.1f, 0.45f, (Back + South) * 0.5f), new Vector3(0.25f, 0.9f, Back - South), wall);
        PrimitiveKit.Box("Hedge", g, new Vector3(East + 0.1f, 1.05f, (Back + South) * 0.5f), new Vector3(0.5f, 0.35f, Back - South), hedge, collider: false);
        PrimitiveKit.Box("GardenWall", g, new Vector3(ParkW - 0.1f, 0.45f, (Back + South) * 0.5f), new Vector3(0.25f, 0.9f, Back - South), wall);
        // between the car park and the pool: planters with a gap to walk through
        PrimitiveKit.Box("Planter", g, new Vector3(DeckW, 0.3f, Back - 3.5f), new Vector3(0.5f, 0.6f, 3.4f), wall);
        PrimitiveKit.Box("Planter", g, new Vector3(DeckW, 0.3f, South + 1.1f), new Vector3(0.5f, 0.6f, 1.6f), wall);
        PrimitiveKit.Box("PlanterHedge", g, new Vector3(DeckW, 0.75f, Back - 3.5f), new Vector3(0.55f, 0.35f, 3.4f), hedge, collider: false);
        PrimitiveKit.Box("PlanterHedge", g, new Vector3(DeckW, 0.75f, South + 1.1f), new Vector3(0.55f, 0.35f, 1.6f), hedge, collider: false);
        Nature.CoconutPalm(g, new Vector3(East - 0.9f, 0f, South + 0.9f), 0.6f, 311);
        Nature.CoconutPalm(g, new Vector3(East - 0.9f, 0f, Back - 0.9f), 0.55f, 312);
        Nature.CoconutPalm(g, new Vector3(DeckW + 0.6f, 0f, Back - 0.9f), 0.5f, 313);
        // garden lamps round the deck
        var post = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        var lamp = PrimitiveKit.Glow("GardenLamp", new Color(1f, 0.85f, 0.55f), 2f);
        foreach (var p in new[] { new Vector3(DeckW + 0.6f, 0f, PoolZ), new Vector3(East - 0.5f, 0f, Back - 2.4f), new Vector3(80f, 0f, South + 0.5f), new Vector3(ParkW + 0.5f, 0f, South + 0.5f), new Vector3(DeckW - 0.6f, 0f, Back - 0.6f) })
        {
            PrimitiveKit.Cylinder("LampPost", g, p + new Vector3(0f, 1.1f, 0f), new Vector3(0.07f, 1.1f, 0.07f), post, collider: false);
            PrimitiveKit.Sphere("LampGlobe", g, p + new Vector3(0f, 2.3f, 0f), Vector3.one * 0.32f, lamp);
        }
    }

    // Thai vowels like ำ need the game font (Mitr); the built-in one drops them
    private static TextMesh Thai(TextMesh label)
    {
        var font = Resources.Load<Font>("Fonts/Mitr-Medium");
        if (font == null) return label;
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        return label;
    }
}
