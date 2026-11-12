using UnityEngine;

// ปั๊มน้ำมัน: a petrol station by the road at the edge of the map, laid out like a real Thai one.
//  • the forecourt: smooth concrete with yellow-and-black kerbs round it, driveways down to the road
//    with painted เข้า / ออก arrows, lanes marked round the pump islands, planters in the corners
//  • the canopy: a deep white roof on green-clad columns, a green band with a gold stripe and the
//    station's name front and back, glowing light panels underneath (lit lamps after dark)
//  • the pump islands: raised kerbs, crash bollards at the ends, two dispensers each with a lit
//    display, green (แก๊สโซฮอล์ 95) and yellow (ดีเซล) nozzles on their hoses, a bin and a squeegee
//    bucket; each island is a FuelPump zone (fill up, or drive off from here to another town)
//  • the shop (มินิมาร์ท · กาแฟ · ห้องน้ำ) with its glass front, ATM, ice chest and a café table; at a
//    village pump a small cashier's booth with an engine-oil rack instead
//  • the price pylon by the road with today's prices (as the server sells them, fuel.lua), and the
//    free air and water post
// Where you arrive when you drive in from somewhere else: the SpawnPoint "drive" on the forecourt.
//
// Local space: the front (the road side, where you drive in) is local -Z; `width` along the road
// (local X), `depth` back from it (local Z). The size picks how much there is: a small village pump
// with one island and a booth, a town station with two, a city station with three and a shop.
public static class PetrolStation
{
    public const string Brand = "สถานีบริการน้ำมัน";

    private static Material M(string name, Color c, float smooth = 0.4f, float metal = 0f) => PrimitiveKit.Mat("PK_Station" + name, c, smooth, metal);

    private static Material Concrete => M("Concrete", new Color(0.78f, 0.78f, 0.76f), 0.2f);
    private static Material White => M("White", new Color(0.96f, 0.96f, 0.95f), 0.55f);
    private static Material Green => M("Green", new Color(0.05f, 0.5f, 0.3f), 0.55f);
    private static Material DarkGreen => M("DarkGreen", new Color(0.03f, 0.32f, 0.2f), 0.5f);
    private static Material Gold => M("Gold", new Color(0.98f, 0.76f, 0.15f), 0.6f, 0.3f);
    private static Material Yellow => M("Yellow", new Color(0.98f, 0.82f, 0.1f), 0.4f);
    private static Material Black => M("Black", new Color(0.08f, 0.08f, 0.09f), 0.4f);
    private static Material Steel => M("Steel", new Color(0.72f, 0.74f, 0.77f), 0.7f, 0.6f);
    private static Material Glass => M("Glass", new Color(0.5f, 0.68f, 0.8f), 0.95f);
    private static Material Paint => M("Paint", new Color(0.97f, 0.97f, 0.95f), 0.3f);
    private static Material Asphalt => PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
    private static Material Soil => M("Soil", new Color(0.36f, 0.26f, 0.18f), 0.1f);
    private static Material Leaf => M("Leaf", new Color(0.2f, 0.52f, 0.2f), 0.2f);

    private static GameObject Box(string n, Transform p, Vector3 at, Vector3 size, Material m, bool collider = false, Vector3 euler = default) =>
        PrimitiveKit.Box(n, p, at, size, m, euler, collider);

    private static GameObject Cyl(string n, Transform p, Vector3 at, Vector3 size, Material m, bool collider = false, Vector3 euler = default) =>
        PrimitiveKit.Cylinder(n, p, at, size, m, euler, collider);

    public static Transform Build(Transform root, Vector3 at, float yaw, float width, float depth, string town)
    {
        var t = PrimitiveKit.Group("PetrolStation_ปั๊มน้ำมัน", root, at, yaw).transform;
        int islands = width >= 24f ? 3 : width >= 14f ? 2 : 1;
        bool small = islands == 1;
        float hw = width * 0.5f, hd = depth * 0.5f;
        float spacing = small ? 6f : 6.5f;
        float canopyZ = small ? -hd * 0.12f : -hd * 0.18f;
        float canopyW = islands * spacing + 2.5f;
        float canopyD = small ? Mathf.Min(depth - 2.5f, 7f) : Mathf.Min(depth * 0.5f, 10f);
        float roofY = 5.2f;

        Forecourt(t, hw, hd, canopyZ, canopyW, canopyD, islands, spacing);
        Canopy(t, canopyZ, canopyW, canopyD, roofY, islands, spacing, small);
        for (int i = 0; i < islands; i++)
            Island(t, new Vector3((i - (islands - 1) * 0.5f) * spacing, 0f, canopyZ), Mathf.Min(canopyD - 1.6f, 4.8f), canopyD, town);
        if (small) Booth(t, new Vector3(hw - 2f, 0f, hd - 1.7f));
        else Shop(t, hw, hd, width, depth);
        Pylon(t, new Vector3(hw - 1.1f, 0f, -hd + 1.1f), small ? 4.2f : 6.5f, town);
        AirPost(t, new Vector3(-hw + 1.1f, 0f, small ? hd - 1.2f : hd - Mathf.Min(depth * 0.28f, 6f) - 2f));

        // where you pull in when you drive here from another place: on the forecourt, nose to the road
        float spawnX = -(islands * spacing * 0.5f) - 0.3f;
        if (spawnX < -hw + 1.6f) spawnX = -hw + 1.6f;
        PrimitiveKit.Group("SpawnPoint_Drive", t, new Vector3(spawnX, 0f, canopyZ), 180f)
            .AddComponent<SpawnPoint>().Configure(ParkedCarSpawner.DriveSpawn);
        return t;
    }

    // ── the forecourt ────────────────────────────────────────────────────────────────────────────
    private static void Forecourt(Transform t, float hw, float hd, float canopyZ, float canopyW, float canopyD, int islands, float spacing)
    {
        Box("Forecourt", t, new Vector3(0f, 0.025f, 0f), new Vector3(hw * 2f, 0.04f, hd * 2f), Concrete);
        // expansion joints in the concrete
        var joint = M("Joint", new Color(0.6f, 0.6f, 0.58f), 0.1f);
        for (float x = -hw + 4f; x < hw; x += 4f)
            Box("Joint", t, new Vector3(x, 0.047f, 0f), new Vector3(0.05f, 0.005f, hd * 2f), joint);
        for (float z = -hd + 4f; z < hd; z += 4f)
            Box("Joint", t, new Vector3(0f, 0.047f, z), new Vector3(hw * 2f, 0.005f, 0.05f), joint);

        // yellow-and-black kerbs down the sides and along the back
        int k = 0;
        for (float z = -hd + 0.5f; z < hd; z += 1f, k++)
            foreach (float s in new[] { -1f, 1f })
                Box("Kerb", t, new Vector3(s * (hw - 0.15f), 0.1f, z), new Vector3(0.3f, 0.2f, 1f), k % 2 == 0 ? Yellow : Black);
        k = 0;
        for (float x = -hw + 0.5f; x < hw; x += 1f, k++)
            Box("Kerb", t, new Vector3(x, 0.1f, hd - 0.15f), new Vector3(1f, 0.2f, 0.3f), k % 2 == 0 ? Yellow : Black);

        // the driveways down to the road, and the arrows painted on them
        float driveW = Mathf.Min(5f, hw * 0.7f);
        foreach (float s in new[] { -1f, 1f })
        {
            float x = s * (hw - driveW * 0.5f - 0.4f);
            Box("Driveway", t, new Vector3(x, 0.02f, -hd - 0.75f), new Vector3(driveW, 0.04f, 1.5f), Asphalt);
            Arrow(t, new Vector3(x, 0.05f, -hd + 1.6f), s < 0f ? 0f : 180f); // in on the left, out on the right
        }
        // the lane lines round the islands
        for (int i = 0; i <= islands; i++)
        {
            float x = (i - islands * 0.5f) * spacing;
            Box("LaneLine", t, new Vector3(x, 0.05f, canopyZ), new Vector3(0.12f, 0.005f, canopyD + 1f), Paint);
        }

        // planters in the back corners
        foreach (float s in new[] { -1f, 1f })
        {
            var p = new Vector3(s * (hw - 1.1f), 0f, hd - 1.1f);
            if (hw < 8f) continue; // a village pump: the booth and the air post stand there
            Box("Planter", t, p + new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.6f, 1.4f), White, collider: true);
            Box("Soil", t, p + new Vector3(0f, 0.61f, 0f), new Vector3(1.25f, 0.04f, 1.25f), Soil);
            PrimitiveKit.Sphere("Bush", t, p + new Vector3(0f, 1f, 0f), new Vector3(1.3f, 0.9f, 1.3f), Leaf);
            PrimitiveKit.Sphere("Flowers", t, p + new Vector3(0.3f, 1.25f, -0.2f), new Vector3(0.5f, 0.35f, 0.5f), M("Flower", new Color(0.92f, 0.3f, 0.55f), 0.3f));
        }
    }

    private static void Arrow(Transform t, Vector3 at, float yaw)
    {
        var a = PrimitiveKit.Group("Arrow", t, at, yaw).transform;
        Box("Shaft", a, new Vector3(0f, 0f, -0.4f), new Vector3(0.3f, 0.005f, 1.4f), Paint);
        Box("HeadL", a, new Vector3(-0.22f, 0f, 0.35f), new Vector3(0.25f, 0.005f, 0.8f), Paint, euler: new Vector3(0f, 35f, 0f));
        Box("HeadR", a, new Vector3(0.22f, 0f, 0.35f), new Vector3(0.25f, 0.005f, 0.8f), Paint, euler: new Vector3(0f, -35f, 0f));
    }

    // ── the canopy ──────────────────────────────────────────────────────────────────────────────
    private static void Canopy(Transform t, float z, float w, float d, float y, int islands, float spacing, bool small)
    {
        var c = PrimitiveKit.Group("Canopy", t, new Vector3(0f, 0f, z)).transform;
        Box("Roof", c, new Vector3(0f, y + 0.35f, 0f), new Vector3(w, 0.7f, d), White);
        // the green band all round with a gold stripe
        foreach (float s in new[] { -1f, 1f })
        {
            Box("Band", c, new Vector3(0f, y + 0.4f, s * (d * 0.5f + 0.06f)), new Vector3(w + 0.24f, 0.95f, 0.12f), Green);
            Box("Stripe", c, new Vector3(0f, y + 0.02f, s * (d * 0.5f + 0.13f)), new Vector3(w + 0.24f, 0.16f, 0.02f), Gold);
            Box("Band", c, new Vector3(s * (w * 0.5f + 0.06f), y + 0.4f, 0f), new Vector3(0.12f, 0.95f, d + 0.24f), Green);
            Box("Stripe", c, new Vector3(s * (w * 0.5f + 0.13f), y + 0.02f, 0f), new Vector3(0.02f, 0.16f, d + 0.24f), Gold);
        }
        // the name, front and back, and a round emblem
        PrimitiveKit.Label(Brand, c, new Vector3(0.6f, y + 0.45f, -d * 0.5f - 0.14f), small ? 0.075f : 0.1f, Color.white);
        PrimitiveKit.Label(Brand, c, new Vector3(0f, y + 0.45f, d * 0.5f + 0.14f), small ? 0.075f : 0.1f, Color.white, 180f);
        float emblemX = -(w * 0.5f) + 0.75f;
        Cyl("Emblem", c, new Vector3(emblemX, y + 0.42f, -d * 0.5f - 0.14f), new Vector3(0.8f, 0.02f, 0.8f), Gold, euler: new Vector3(90f, 0f, 0f));
        Cyl("EmblemFace", c, new Vector3(emblemX, y + 0.42f, -d * 0.5f - 0.16f), new Vector3(0.62f, 0.02f, 0.62f), DarkGreen, euler: new Vector3(90f, 0f, 0f));
        PrimitiveKit.Label("น", c, new Vector3(emblemX, y + 0.42f, -d * 0.5f - 0.18f), 0.07f, new Color(1f, 0.85f, 0.3f));

        // glowing panels underneath, over each island, with a lamp that lights after dark
        var panel = PrimitiveKit.Glow("PK_StationCanopyLight", new Color(1f, 0.98f, 0.92f), 1.6f, 1f);
        for (int i = 0; i < islands; i++)
        {
            float x = (i - (islands - 1) * 0.5f) * spacing;
            foreach (float s in new[] { -1f, 1f })
                Box("LightPanel", c, new Vector3(x + s * 1.9f, y - 0.02f, 0f), new Vector3(0.9f, 0.04f, d * 0.6f), panel);
            var lampGo = PrimitiveKit.Group("CanopyLamp", c, new Vector3(x, y - 0.1f, 0f));
            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(lampGo.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.shadows = LightShadows.None;
            light.enabled = false;
            lampGo.AddComponent<StreetLamp>().Configure(StreetLamp.Kind.Post);
        }

        // the columns: steel, clad in green at the bottom, one at each end of every island
        for (int i = 0; i < islands; i++)
        {
            float x = (i - (islands - 1) * 0.5f) * spacing;
            foreach (float s in new[] { -1f, 1f })
            {
                var p = new Vector3(x, 0f, s * Mathf.Min(d * 0.5f - 1.1f, 2.7f));
                Box("Column", c, p + new Vector3(0f, y * 0.5f, 0f), new Vector3(0.4f, y, 0.4f), Steel, collider: true);
                Box("Cladding", c, p + new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 1.2f, 0.5f), Green);
                Box("CladTrim", c, p + new Vector3(0f, 1.22f, 0f), new Vector3(0.52f, 0.06f, 0.52f), Gold);
            }
        }
    }

    // ── a pump island ───────────────────────────────────────────────────────────────────────────
    private static void Island(Transform t, Vector3 at, float length, float canopyD, string town)
    {
        var island = PrimitiveKit.Group("PumpIsland_ตู้จ่ายน้ำมัน", t, at).transform;
        float half = length * 0.5f;
        Box("Base", island, new Vector3(0f, 0.12f, 0f), new Vector3(1.3f, 0.24f, length), Concrete, collider: true);
        int n = 0;
        for (float z = -half + 0.25f; z < half; z += 0.5f, n++)
            foreach (float s in new[] { -1f, 1f })
                Box("KerbPaint", island, new Vector3(s * 0.66f, 0.12f, z), new Vector3(0.02f, 0.24f, 0.5f), n % 2 == 0 ? Yellow : Black);
        // crash bollards at both ends
        foreach (float s in new[] { -1f, 1f })
        {
            Cyl("Bollard", island, new Vector3(0f, 0.7f, s * (half + 0.35f)), new Vector3(0.28f, 0.45f, 0.28f), Yellow, collider: true);
            Cyl("BollardBand", island, new Vector3(0f, 0.85f, s * (half + 0.35f)), new Vector3(0.29f, 0.08f, 0.29f), Black);
            Cyl("BollardBase", island, new Vector3(0f, 0.2f, s * (half + 0.35f)), new Vector3(0.4f, 0.1f, 0.4f), Concrete);
        }
        // the two dispensers
        var screen = PrimitiveKit.Glow("PK_StationScreen", new Color(0.15f, 0.55f, 0.95f), 1.1f, 0.6f);
        var green = M("Nozzle95", new Color(0.1f, 0.65f, 0.3f), 0.5f);
        var hose = M("Hose", new Color(0.1f, 0.1f, 0.11f), 0.3f);
        foreach (float z in new[] { -half * 0.45f, half * 0.45f })
        {
            var p = PrimitiveKit.Group("Dispenser", island, new Vector3(0f, 0.24f, z)).transform;
            Box("Plinth", p, new Vector3(0f, 0.1f, 0f), new Vector3(0.95f, 0.2f, 0.7f), Steel);
            Box("Body", p, new Vector3(0f, 0.95f, 0f), new Vector3(0.85f, 1.5f, 0.62f), White, collider: true);
            Box("Header", p, new Vector3(0f, 1.9f, 0f), new Vector3(0.88f, 0.4f, 0.65f), Green);
            Box("HeaderStripe", p, new Vector3(0f, 1.72f, 0f), new Vector3(0.89f, 0.05f, 0.66f), Gold);
            foreach (float side in new[] { -1f, 1f })
            {
                // the lit display and the keypad on each face
                Box("Display", p, new Vector3(side * 0.43f, 1.35f, 0f), new Vector3(0.02f, 0.32f, 0.46f), screen);
                Box("Keypad", p, new Vector3(side * 0.43f, 0.98f, 0.12f), new Vector3(0.02f, 0.2f, 0.16f), Black);
                // the nozzles in their holsters, the hoses hanging down
                Box("Nozzle95", p, new Vector3(side * 0.47f, 0.85f, -0.16f), new Vector3(0.08f, 0.24f, 0.1f), green);
                Box("NozzleDiesel", p, new Vector3(side * 0.47f, 0.85f, 0.2f), new Vector3(0.08f, 0.24f, 0.1f), Yellow);
                Cyl("Hose", p, new Vector3(side * 0.5f, 0.45f, -0.16f), new Vector3(0.05f, 0.35f, 0.05f), hose);
                Cyl("Hose", p, new Vector3(side * 0.5f, 0.45f, 0.2f), new Vector3(0.05f, 0.35f, 0.05f), hose);
                PrimitiveKit.Label("95 · ดีเซล", p, new Vector3(side * 0.45f, 1.9f, 0f), 0.022f, Color.white, side > 0f ? -90f : 90f);
            }
        }
        // the bin and the squeegee bucket at the end
        Cyl("Bin", island, new Vector3(0.3f, 0.6f, 0f), new Vector3(0.38f, 0.36f, 0.38f), M("Bin", new Color(0.2f, 0.45f, 0.75f), 0.4f));
        Cyl("Bucket", island, new Vector3(-0.3f, 0.45f, 0f), new Vector3(0.3f, 0.22f, 0.3f), M("Bucket", new Color(0.9f, 0.35f, 0.2f), 0.4f));
        Box("Squeegee", island, new Vector3(-0.3f, 0.85f, 0f), new Vector3(0.03f, 0.6f, 0.03f), Steel, euler: new Vector3(0f, 0f, 12f));

        // drive or walk up beside the island
        var zone = PrimitiveKit.Group("FuelPump", island, Vector3.zero).transform;
        var box = zone.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 1.5f, 0f);
        box.size = new Vector3(5.8f, 3f, Mathf.Min(canopyD, 6.5f));
        zone.gameObject.AddComponent<FuelPump>().Configure($"{Brand}{town}");
    }

    // ── the shop ────────────────────────────────────────────────────────────────────────────────
    private static void Shop(Transform t, float hw, float hd, float width, float depth)
    {
        float w = Mathf.Min(width * 0.55f, 15f), d = Mathf.Min(depth * 0.28f, 6.5f);
        var s = PrimitiveKit.Group("Shop_ร้านในปั๊ม", t, new Vector3(0f, 0f, hd - d * 0.5f - 0.5f)).transform;
        float front = -d * 0.5f;
        Box("Floor", s, new Vector3(0f, 0.1f, 0f), new Vector3(w + 1f, 0.2f, d + 1f), M("Tile", new Color(0.86f, 0.85f, 0.82f), 0.5f));
        Box("Back", s, new Vector3(0f, 1.9f, d * 0.5f - 0.1f), new Vector3(w, 3.6f, 0.2f), White, collider: true);
        foreach (float side in new[] { -1f, 1f })
            Box("Side", s, new Vector3(side * (w * 0.5f - 0.1f), 1.9f, 0f), new Vector3(0.2f, 3.6f, d), White, collider: true);
        Box("Roof", s, new Vector3(0f, 3.8f, 0f), new Vector3(w + 0.4f, 0.3f, d + 0.4f), White);
        // the glass front with a lit inside behind it and the door
        Box("Glass", s, new Vector3(0f, 1.6f, front), new Vector3(w - 0.4f, 2.8f, 0.06f), Glass, collider: true);
        Box("InsideGlow", s, new Vector3(0f, 1.7f, front + 1.2f), new Vector3(w - 1f, 2.4f, 0.05f), PrimitiveKit.Glow("PK_StationShopInside", new Color(1f, 0.97f, 0.88f), 0.7f, 1f));
        for (float x = -w * 0.5f + 0.2f; x <= w * 0.5f - 0.1f; x += 2.5f)
            Box("Mullion", s, new Vector3(x, 1.6f, front - 0.02f), new Vector3(0.08f, 2.8f, 0.06f), Steel);
        Box("DoorFrame", s, new Vector3(0f, 1.25f, front - 0.04f), new Vector3(1.9f, 2.5f, 0.05f), Steel);
        Box("Door", s, new Vector3(0f, 1.2f, front - 0.06f), new Vector3(1.7f, 2.3f, 0.02f), M("DoorGlass", new Color(0.62f, 0.78f, 0.86f), 0.95f));
        // shelves seen through the glass
        Color[] goods = { new Color(0.9f, 0.3f, 0.2f), new Color(0.2f, 0.5f, 0.85f), new Color(0.95f, 0.8f, 0.2f), new Color(0.3f, 0.7f, 0.35f) };
        for (float x = -w * 0.5f + 1.5f; x < w * 0.5f - 1f; x += 3f)
            for (int r = 0; r < 3; r++)
                Box("Goods", s, new Vector3(x, 0.6f + r * 0.55f, d * 0.5f - 0.5f), new Vector3(2.2f, 0.4f, 0.4f), M("Goods" + ((Mathf.RoundToInt(x) + r) & 3), goods[(Mathf.RoundToInt(x) + r) & 3], 0.4f));
        // the green fascia and awning
        Box("Fascia", s, new Vector3(0f, 3.45f, front - 0.15f), new Vector3(w + 0.4f, 0.75f, 0.12f), Green);
        Box("FasciaStripe", s, new Vector3(0f, 3.05f, front - 0.22f), new Vector3(w + 0.4f, 0.08f, 0.02f), Gold);
        PrimitiveKit.Label("มินิมาร์ท · กาแฟ · ห้องน้ำ", s, new Vector3(0f, 3.45f, front - 0.23f), 0.06f, Color.white);
        Box("Awning", s, new Vector3(0f, 3f, front - 0.8f), new Vector3(w, 0.1f, 1.4f), DarkGreen, euler: new Vector3(-10f, 0f, 0f));

        // ATM, ice chest, the café table out front
        var atm = PrimitiveKit.Group("ATM", s, new Vector3(-w * 0.5f + 1f, 0.2f, front - 0.5f)).transform;
        Box("Body", atm, new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 1.9f, 0.7f), M("Atm", new Color(0.15f, 0.3f, 0.65f), 0.5f), collider: true);
        Box("Screen", atm, new Vector3(0f, 1.25f, -0.36f), new Vector3(0.5f, 0.35f, 0.02f), PrimitiveKit.Glow("PK_StationAtmScreen", new Color(0.4f, 0.8f, 1f), 1f, 0.7f));
        PrimitiveKit.Label("ATM", atm, new Vector3(0f, 1.75f, -0.37f), 0.035f, Color.white);
        var ice = PrimitiveKit.Group("IceChest", s, new Vector3(w * 0.5f - 1.1f, 0.2f, front - 0.6f)).transform;
        Box("Chest", ice, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.8f), White, collider: true);
        Box("Lid", ice, new Vector3(0f, 1.02f, 0f), new Vector3(1.45f, 0.06f, 0.85f), M("IceBlue", new Color(0.3f, 0.6f, 0.9f), 0.5f));
        PrimitiveKit.Label("น้ำแข็ง", ice, new Vector3(0f, 0.55f, -0.41f), 0.04f, new Color(0.15f, 0.35f, 0.7f));
        if (w >= 12f)
        {
            var cafe = PrimitiveKit.Group("CafeTable", s, new Vector3(w * 0.5f - 3.2f, 0.2f, front - 1.6f)).transform;
            Cyl("Table", cafe, new Vector3(0f, 0.72f, 0f), new Vector3(0.9f, 0.03f, 0.9f), White);
            Cyl("Leg", cafe, new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.36f, 0.08f), Steel);
            Cyl("UmbrellaPole", cafe, new Vector3(0f, 1.4f, 0f), new Vector3(0.05f, 1.4f, 0.05f), Steel);
            Cyl("Umbrella", cafe, new Vector3(0f, 2.6f, 0f), new Vector3(2.4f, 0.08f, 2.4f), Green);
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 0.85f;
                Cyl("Stool", cafe, dir + new Vector3(0f, 0.25f, 0f), new Vector3(0.38f, 0.25f, 0.38f), M("Stool", new Color(0.85f, 0.55f, 0.2f), 0.4f));
            }
        }
    }

    // a village pump's cashier booth, with engine oil on a rack beside it
    private static void Booth(Transform t, Vector3 at)
    {
        var b = PrimitiveKit.Group("Booth_ซุ้มแคชเชียร์", t, at).transform;
        Box("Body", b, new Vector3(0f, 1.3f, 0f), new Vector3(2.2f, 2.6f, 2f), White, collider: true);
        Box("Window", b, new Vector3(0f, 1.45f, -1.01f), new Vector3(1.4f, 0.8f, 0.03f), Glass);
        Box("Counter", b, new Vector3(0f, 1f, -1.15f), new Vector3(1.5f, 0.06f, 0.3f), Steel);
        Box("Roof", b, new Vector3(0f, 2.75f, -0.2f), new Vector3(2.7f, 0.18f, 2.7f), Green);
        Box("RoofStripe", b, new Vector3(0f, 2.62f, -1.56f), new Vector3(2.7f, 0.06f, 0.02f), Gold);
        PrimitiveKit.Label("จ่ายเงินที่นี่", b, new Vector3(0f, 2.2f, -1.02f), 0.035f, new Color(0.05f, 0.4f, 0.25f));
        var rack = PrimitiveKit.Group("OilRack_น้ำมันเครื่อง", b, new Vector3(-1.75f, 0f, -0.3f)).transform;
        Box("Frame", rack, new Vector3(0f, 0.7f, 0f), new Vector3(0.9f, 1.4f, 0.4f), Steel);
        Color[] oils = { new Color(0.95f, 0.75f, 0.1f), new Color(0.85f, 0.15f, 0.12f), new Color(0.15f, 0.4f, 0.85f) };
        for (int r = 0; r < 3; r++)
            for (int i = 0; i < 4; i++)
                Box("Bottle", rack, new Vector3(-0.32f + i * 0.21f, 0.3f + r * 0.45f, -0.05f), new Vector3(0.14f, 0.28f, 0.14f), M("Oil" + r, oils[r], 0.5f));
    }

    // ── the price pylon ─────────────────────────────────────────────────────────────────────────
    private static void Pylon(Transform t, Vector3 at, float height, string town)
    {
        var p = PrimitiveKit.Group("PriceSign_ป้ายราคาน้ำมัน", t, at).transform;
        Box("Footing", p, new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 0.8f), Concrete, collider: true);
        Box("Body", p, new Vector3(0f, height * 0.5f + 0.25f, 0f), new Vector3(1.4f, height - 0.5f, 0.45f), Green, collider: true);
        foreach (float s in new[] { -1f, 1f })
            Box("Trim", p, new Vector3(s * 0.72f, height * 0.5f + 0.25f, 0f), new Vector3(0.06f, height - 0.5f, 0.5f), White);
        // the lit brand box on top
        Box("Top", p, new Vector3(0f, height + 0.5f, 0f), new Vector3(1.7f, 1f, 0.55f), PrimitiveKit.Glow("PK_StationPylonTop", new Color(0.08f, 0.6f, 0.35f), 1.3f, 0.8f));
        PrimitiveKit.Label("น้ำมัน", p, new Vector3(0f, height + 0.55f, -0.29f), 0.07f, Color.white);
        PrimitiveKit.Label("น้ำมัน", p, new Vector3(0f, height + 0.55f, 0.29f), 0.07f, Color.white, 180f);
        // today's prices
        var board = PrimitiveKit.Glow("PK_StationPriceBoard", new Color(0.04f, 0.05f, 0.07f), 0.2f, 0.6f);
        float y = height - 0.7f;
        PrimitiveKit.Label("ราคาวันนี้ (บาท/ลิตร)", p, new Vector3(0f, y + 0.45f, -0.24f), 0.022f, new Color(1f, 0.95f, 0.75f));
        foreach (var (name, price, color, dy) in new[] { ("แก๊สโซฮอล์ 95", "33.05", new Color(0.3f, 1f, 0.45f), 0f), ("ดีเซล", "31.94", new Color(1f, 0.82f, 0.2f), -0.75f) })
        {
            Box("Row", p, new Vector3(0f, y + dy, -0.22f), new Vector3(1.25f, 0.6f, 0.02f), board);
            PrimitiveKit.Label(name, p, new Vector3(0f, y + dy + 0.15f, -0.24f), 0.022f, Color.white);
            PrimitiveKit.Label(price, p, new Vector3(0f, y + dy - 0.1f, -0.24f), 0.045f, color);
        }
    }

    // free air and water for your tyres and radiator
    private static void AirPost(Transform t, Vector3 at)
    {
        var a = PrimitiveKit.Group("AirWater_เติมลม", t, at).transform;
        Box("Post", a, new Vector3(0f, 0.75f, 0f), new Vector3(0.45f, 1.5f, 0.35f), M("AirRed", new Color(0.82f, 0.15f, 0.1f), 0.5f), collider: true);
        Box("Gauge", a, new Vector3(0f, 1.2f, -0.18f), new Vector3(0.28f, 0.28f, 0.02f), White);
        Cyl("Coil", a, new Vector3(0.3f, 0.7f, 0f), new Vector3(0.4f, 0.05f, 0.4f), M("Hose", new Color(0.1f, 0.1f, 0.11f), 0.3f), euler: new Vector3(0f, 0f, 90f));
        PrimitiveKit.Label("เติมลม · น้ำ\nฟรี", a, new Vector3(0f, 1.75f, -0.19f), 0.025f, Color.white);
    }
}
