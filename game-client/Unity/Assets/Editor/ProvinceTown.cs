using System;
using UnityEngine;

// What makes one province's town its own: names on the signs, its airport, whether it's by the sea
public class ProvinceSpec
{
    public string Scene, Root;          // Assets/Scenes/<Scene>.unity, the root object's name
    public string Name;                 // ชื่อจังหวัด ("นครศรีธรรมราช")
    public string Airport;              // "ท่าอากาศยานนครศรีธรรมราช"
    public string Station;              // "สถานีรถไฟนครศรีธรรมราช"
    public string MainStreet;           // the street across the roundabout
    public string Monument;             // on the roundabout ("อนุสาวรีย์…")
    public string GantryWest, GantryEast;
    public string[] ShopsWest, ShopsEast;
    public bool Coast;                  // a beach and the sea beyond the north side of the ring road
    public Color Accent = new Color(0.85f, 0.65f, 0.2f);
    public int[] Airlines = { 0, 1 };
    public string[] CoachRoutes = { "กรุงเทพฯ (หมอชิต 2)", "เชียงใหม่", "หาดใหญ่" };      // on the coaches at the terminal
    public string[] SongthaewRoutes = { "รอบเมือง", "ตลาด - บขส.", "อำเภอ", "สถานีรถไฟ" };
}

// ตัวเมืองต่างจังหวัด: the shared town every province is built on — the four-lane avenue with its
// traffic lights, the roundabout and town streets with tuk-tuks and taxis, the ring road, the railway
// and its station (train), the bus shelter (บขส.), the provincial airport with its runway and planes
// coming and going, a welcome arch, and (by the sea) a beach. Each province then adds its ten
// landmarks into the free blocks (see the layout notes below) and they show up on the taxi map.
//
// Free blocks (the ground is ±120, the ring road runs at ±94):
//   A  north-west   (-27, 31)  36 x 22      B  north-east  (29, 31)  44 x 22
//   C  east         (62, 17)   48 x 38      D  far west    (-70, 22) 32 x 44
//   E  past railway (-50, 78)  70 x 20      F  past railway (45, 78)  80 x 20
//   G  south-west   (-58, -55) 56 x 64      H  south       (-2, -55) 48 x 64
//   beyond the ring north (z 98..116): the beach, or open country
//   beyond the ring east (x 98..118, z -42..18): the bus terminal (BusTerminal)
public static class ProvinceTown
{
    public const float GroundScale = 24f;      // ±120
    private static readonly Vector3 Terminal = new Vector3(58f, 0f, -44f);

    public static void Build(ProvinceSpec spec, bool ask, Action<Transform> landmarks) =>
        TambonBuilder.BuildLocation($"Assets/Scenes/{spec.Scene}.unity", spec.Root, GroundScale, ask, root =>
        {
            Town(root, spec);
            landmarks(root);
            TouristMaps(root, spec);
        }, road: false);

    // ป้ายแผนที่ท่องเที่ยว with the ten sights where you arrive: by the start on the avenue, at the
    // airport, at the station and in the bus terminal's hall
    private static void TouristMaps(Transform root, ProvinceSpec spec)
    {
        var sights = TouristMap.Collect(root);
        var at = new[]
        {
            (new Vector3(-44f, 0f, -20.4f), 0f),
            (new Vector3(47f, 0f, -27f), 0f),
            (new Vector3(8f, 0f, 53f), 0f),
            (BusTerminal.TownSite + Quaternion.Euler(0f, 90f, 0f) * new Vector3(6.5f, 0f, -5f), 90f),
        };
        foreach (var (p, yaw) in at)
            TouristMap.Build(root, spec.Name, sights, p, yaw, sea: spec.Coast ? 1 : 0);
    }

    private static void Town(Transform root, ProvinceSpec spec)
    {
        CityTraffic.Build(root, spec.GantryWest, spec.GantryEast, "ป้ายรถเมล์ · " + spec.Name, roundabout: true, ringRoad: CityRingRoad.Town, coachStop: false);
        BusTerminal.Build(root, BusTerminal.TownSite, 90f, spec.Name, spec.CoachRoutes, spec.SongthaewRoutes, songthaewTravel: false);
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        PrimitiveKit.Box("CrossStreet", root, new Vector3(0f, 0.02f, 12.3f), new Vector3(6f, 0.04f, 35.4f), asphalt);
        CityStreets.Build(root, spec.MainStreet);
        var railway = ThaiRailway.Line(root, CityRingRoad.RailZ, spec.Station);
        CityRingRoad.Build(root, CityRingRoad.Town, railway);

        Monument(root, CityStreets.Roundabout, spec);
        // ตำรวจจราจร at the roundabout, a speed trap on the avenue, a checkpoint on the west ring road,
        // and the ปั๊มน้ำมัน just outside it (where you drive in from other towns)
        Police.TrafficPoint(root, CityStreets.Roundabout, 8.6f, new Vector3(-6.5f, 0f, 4.5f));
        Police.SpeedTrap(root, new Vector3(60f, 0f, -21f), 0f, 60);
        Police.Checkpoint(root, new Vector3(-CityRingRoad.Town.Edge, 0f, 7f), 0f, CityRingRoad.HalfWidth * 2f, -1f);
        PetrolStation.Build(root, new Vector3(-109f, 0f, -20f), 270f, 32f, 20f, " " + spec.Name);
        // เวทีดนตรีประจำจังหวัด (musician Lv 12, บทที่ 6): just outside the west ring road, facing town
        Places2.MusicStage(root, new Vector3(-108f, 0f, 36f), -90f, "province_stage", "เวทีดนตรีประจำจังหวัด\n" + spec.Name, new Color(0.45f, 0.12f, 0.35f));
        Places2.Shophouses(root, new Vector3(-28f, 0f, 2f), 0f, 4, spec.ShopsWest ?? new[] { "ร้านข้าวแกง", "ร้านของฝาก", "ร้านขายยา", "ร้านกาแฟ" });
        Places2.Shophouses(root, new Vector3(22f, 0f, 2f), 0f, 4, spec.ShopsEast ?? new[] { "ร้านทอง", "ร้านโจ๊ก", "มินิมาร์ท", "ร้านตัดผม" });
        WelcomeArch(root, new Vector3(-66f, 0f, -11.5f), spec);
        Airport(root, spec);
        if (spec.Coast) Beach(root);
        else Countryside(root);

        // shade trees round the edge of town, inside the ring road
        for (float x = -84f; x <= 84f; x += 14f)
            foreach (float z in new[] { -88f, 88f })
                if (Mathf.Abs(x) > 6f && !(z < 0f && x > 26f)) Places.ShadeTree(root, new Vector3(x, 0f, z), 0.9f);
        for (float z = -80f; z <= 80f; z += 16f)
            foreach (float x in new[] { -87f, 87f })
                if (Mathf.Abs(z - CityRingRoad.RailZ) > 8f && Mathf.Abs(z + 11.5f) > 12f && Mathf.Abs(z - 47f) > 5f) Places.ShadeTree(root, new Vector3(x, 0f, z), 0.85f);

        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.transform.position = new Vector3(-34f, 1f, -22f);
    }

    // อนุสาวรีย์กลางวงเวียน: a gilded emblem on a white column, flower beds round it
    private static void Monument(Transform root, Vector3 at, ProvinceSpec spec)
    {
        var t = PrimitiveKit.Group("Monument_อนุสาวรีย์", root, at).transform;
        var accent = PrimitiveKit.Mat("PK_Accent" + spec.Root, spec.Accent, 0.6f, 0.4f);
        PrimitiveKit.Cylinder("Lawn", t, new Vector3(0f, 0.1f, 0f), new Vector3(9.2f, 0.1f, 9.2f), ProvinceKit.Grass);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            Nature.Bush(t, new Vector3(Mathf.Cos(a) * 3.8f, 0.2f, Mathf.Sin(a) * 3.8f), 0.5f, i, flowers: true);
        }
        PrimitiveKit.Box("Plinth", t, new Vector3(0f, 0.7f, 0f), new Vector3(2.6f, 1.2f, 2.6f), ProvinceKit.Granite);
        PrimitiveKit.Box("Column", t, new Vector3(0f, 3.3f, 0f), new Vector3(1.1f, 4f, 1.1f), ProvinceKit.White);
        PrimitiveKit.Cylinder("Emblem", t, new Vector3(0f, 6.2f, 0f), new Vector3(2.2f, 0.12f, 2.2f), accent, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("EmblemRim", t, new Vector3(0f, 6.2f, 0f), new Vector3(2.45f, 0.08f, 2.45f), ProvinceKit.Gold, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Label(spec.Monument, t, new Vector3(0f, 1f, -1.32f), 0.03f, new Color(0.25f, 0.18f, 0.08f));
        PrimitiveKit.Label(spec.Monument, t, new Vector3(0f, 1f, 1.32f), 0.03f, new Color(0.25f, 0.18f, 0.08f), 180f);
    }

    // ซุ้มประตูเมือง over the avenue at the west end of town
    private static void WelcomeArch(Transform root, Vector3 at, ProvinceSpec spec)
    {
        var t = PrimitiveKit.Group("WelcomeArch_ซุ้มประตูเมือง", root, at).transform;
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        foreach (float z in new[] { -8.6f, 8.6f })
        {
            PrimitiveKit.Box("Pillar", t, new Vector3(0f, 3.8f, z), new Vector3(1.2f, 7.6f, 1.2f), ProvinceKit.White);
            PrimitiveKit.Box("PillarCap", t, new Vector3(0f, 7.7f, z), new Vector3(1.5f, 0.3f, 1.5f), ProvinceKit.Gold, collider: false);
        }
        PrimitiveKit.Box("Beam", t, new Vector3(0f, 8.4f, 0f), new Vector3(1f, 1.6f, 18.4f), ProvinceKit.White, collider: false);
        MeshKit.Place("Roof", t, gable, ProvinceKit.RoofRed, new Vector3(-0.9f, 9.2f, 0f), new Vector3(0f, 90f, 0f), new Vector3(18.6f, 2.2f, 1.8f));
        foreach (float yaw in new[] { 90f, -90f }) // readable coming in and going out
            PrimitiveKit.Label("ยินดีต้อนรับสู่ จังหวัด" + spec.Name, t, new Vector3(yaw > 0f ? -0.55f : 0.55f, 8.4f, 0f), 0.07f, new Color(0.55f, 0.1f, 0.05f), yaw);
    }

    // ── The provincial airport: terminal by the avenue, an airliner on the apron, the runway beyond the
    // south side of the ring road (Aviation.Field: planes landing and taking off all day) ──
    private static void Airport(Transform root, ProvinceSpec spec)
    {
        var t = PrimitiveKit.Group("Airport_" + spec.Airport, root, Terminal).transform;
        var glass = PrimitiveKit.Mat("PK_TerminalGlass", new Color(0.45f, 0.65f, 0.78f), 0.95f, 0.3f);
        var wall = PrimitiveKit.Mat("PK_TerminalWall", new Color(0.93f, 0.93f, 0.9f), 0.4f);
        var roof = PrimitiveKit.Mat("PK_TerminalRoof", new Color(0.62f, 0.66f, 0.7f), 0.6f, 0.5f);
        var floor = PrimitiveKit.Mat("PK_TerminalFloor", new Color(0.85f, 0.84f, 0.8f), 0.7f);
        const float w = 30f, d = 14f, h = 7f;
        // the front (local +Z) faces the avenue; a forecourt and a path up from the pavement
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), floor, collider: false);
        PrimitiveKit.Box("Forecourt", t, new Vector3(0f, 0.02f, d * 0.5f + 4f), new Vector3(w, 0.03f, 8f), ProvinceKit.Paving, collider: false);
        ProvinceKit.Path(t, new Vector3(0f, 0f, d * 0.5f + 8f), new Vector3(0f, 0f, 22.4f), 6f);
        PrimitiveKit.Box("BackWall", t, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.4f), wall);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("SideWall", t, new Vector3(s * w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wall);
            PrimitiveKit.Box("Glass", t, new Vector3(s * (w * 0.25f + 1.25f), h * 0.5f, d * 0.5f), new Vector3(w * 0.5f - 2.5f, h, 0.15f), glass);
        }
        PrimitiveKit.Box("DoorHead", t, new Vector3(0f, h - 1f, d * 0.5f), new Vector3(5f, 2f, 0.3f), glass, collider: false);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, h + 0.3f, 2f), new Vector3(w + 4f, 0.6f, d + 8f), roof);
        for (float x = -w * 0.5f - 1f; x <= w * 0.5f + 1.1f; x += 8f)
            PrimitiveKit.Cylinder("Mast", t, new Vector3(x, h * 0.5f, d * 0.5f + 5f), new Vector3(0.5f, h * 0.5f, 0.5f), ProvinceKit.Steel);
        PrimitiveKit.Label(spec.Airport, t, new Vector3(0f, h + 1.2f, d * 0.5f + 5.6f), 0.12f, new Color(0.12f, 0.25f, 0.5f), 180f);
        // check-in, the departure board and the ticket counter (fly to other provinces)
        PrimitiveKit.Box("CheckIn", t, new Vector3(-8f, 0.55f, -3f), new Vector3(9f, 1.1f, 1f), PrimitiveKit.Mat("CounterBlue", new Color(0.2f, 0.32f, 0.55f), 0.5f));
        PrimitiveKit.Box("TicketCounter", t, new Vector3(6f, 0.55f, -2f), new Vector3(6f, 1.1f, 1.2f), ProvinceKit.Gold);
        PrimitiveKit.Label("ขายตั๋วเครื่องบิน", t, new Vector3(6f, 1.6f, -1.35f), 0.035f, new Color(0.4f, 0.2f, 0.05f), 180f);
        PrimitiveKit.Box("Board", t, new Vector3(0f, 4.4f, -d * 0.5f + 0.3f), new Vector3(10f, 2.4f, 0.12f), PrimitiveKit.Glow("DepartureBoard", new Color(0.05f, 0.07f, 0.12f), 0.3f, 0.6f), collider: false);
        PrimitiveKit.Label("ขาออก · เที่ยวบินในประเทศ\nกรุงเทพฯ (สุวรรณภูมิ)  เชียงใหม่  อู่ตะเภา\nขอนแก่น  อุบลฯ  โคราช  หาดใหญ่  บุรีรัมย์  นครศรีฯ", t, new Vector3(0f, 4.4f, -d * 0.5f + 0.38f), 0.04f, new Color(1f, 0.85f, 0.3f), 180f);
        var zone = new GameObject("FlightZone_ตั๋วเครื่องบิน");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(6f, 1f, -0.4f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 2f, 2.2f);
        zone.AddComponent<TravelStop>().Configure(spec.Airport + " · เที่ยวบินในประเทศ", longDistance: TravelMode.Air);
        PrimitiveKit.Group("SpawnPoint_Air", t, new Vector3(0f, 0f, d * 0.5f + 2.5f), 0f).AddComponent<SpawnPoint>().Configure(TravelMode.Air);

        // airside: a fence, the apron, an airliner of one of the airlines with its ground crew
        var chain = PrimitiveKit.Mat("ChainLink", new Color(0.55f, 0.57f, 0.6f), 0.4f, 0.6f);
        PrimitiveKit.Box("Fence", t, new Vector3(0f, 1.2f, -d * 0.5f - 1.5f), new Vector3(56f, 2.4f, 0.05f), chain);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Fence", t, new Vector3(s * 28f, 1.2f, -d * 0.5f - 19.5f), new Vector3(0.05f, 2.4f, 36f), chain);
        PrimitiveKit.Box("Apron", t, new Vector3(0f, 0.012f, -d * 0.5f - 19.5f), new Vector3(56f, 0.012f, 36f), PrimitiveKit.Mat("ApronConcrete", new Color(0.62f, 0.62f, 0.6f)), collider: false);
        Aviation.Jet(t, new Vector3(-2f, 0f, -d * 0.5f - 19.5f), 90f, Aviation.Airlines[spec.Airlines[0] % Aviation.Airlines.Length], spec.Root + "Gate");
        Aviation.GroundCrew(t, new Vector3(12f, 0f, -d * 0.5f - 10f), 0f);

        Aviation.Field(root, new Vector3(0f, 0f, -160f), 90f, 380f, spec.Airport);
    }

    // ── By the sea: the beach beyond the ring road, the sea out to the horizon ──
    private static void Beach(Transform root)
    {
        var t = PrimitiveKit.Group("Seafront_ริมทะเล", root, Vector3.zero).transform;
        var shallow = PrimitiveKit.Mat("SeaWaterShallow", new Color(0.35f, 0.85f, 0.85f));
        var deep = PrimitiveKit.Mat("SeaWater", new Color(0.1f, 0.55f, 0.75f));
        PrimitiveKit.Box("Sand", t, new Vector3(0f, 0.015f, 106f), new Vector3(240f, 0.03f, 18f), ProvinceKit.Sand, collider: false);
        PrimitiveKit.Box("Shallows", t, new Vector3(0f, 0.03f, 116f), new Vector3(240f, 0.02f, 4f), shallow, collider: false);
        PrimitiveKit.Box("Sea", t, new Vector3(0f, 0.025f, 320f), new Vector3(900f, 0.02f, 404f), deep, collider: false);
        ProvinceKit.Blocker(t, new Vector3(0f, 1f, 116.5f), new Vector3(240f, 2f, 3f));
        for (int i = 0; i < 22; i++)
        {
            float x = -110f + i * 10.4f + (i % 3) * 1.4f;
            Nature.CoconutPalm(t, new Vector3(x, 0f, i % 2 == 0 ? 100.5f : 110.5f), 0.9f + (i % 3) * 0.12f, i + 60);
        }
    }

    // ── Inland: rice fields and sugar palms out past the ring road ──
    private static void Countryside(Transform root)
    {
        var t = PrimitiveKit.Group("Countryside_ท้องทุ่ง", root, Vector3.zero).transform;
        var paddy = PrimitiveKit.Mat("PK_Paddy", new Color(0.55f, 0.75f, 0.28f), 0.15f);
        var paddy2 = PrimitiveKit.Mat("PK_Paddy2", new Color(0.62f, 0.8f, 0.32f), 0.15f);
        var dyke = PrimitiveKit.Mat("PK_Dyke", new Color(0.55f, 0.45f, 0.3f));
        for (int i = 0; i < 10; i++)
        {
            float x = -105f + i * 23.3f;
            if (Mathf.Abs(x) < 45f) continue; // the middle is left for the province's own sight
            PrimitiveKit.Box("Paddy", t, new Vector3(x, 0.015f, 107f), new Vector3(22f, 0.02f, 16f), i % 2 == 0 ? paddy : paddy2, collider: false);
            PrimitiveKit.Box("Dyke", t, new Vector3(x + 11.6f, 0.08f, 107f), new Vector3(0.8f, 0.16f, 16f), dyke, collider: false);
            Nature.CoconutPalm(t, new Vector3(x + 11.6f, 0f, 101f + (i % 3) * 5f), 1.05f, i + 90);
        }
    }
}
