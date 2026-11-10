using System.Collections.Generic;
using UnityEngine;

// สถานีขนส่งผู้โดยสาร (บขส.): the town's bus terminal, just outside the ring road at the east end of
// the avenue. The hall with the coach companies' ticket booths (the ticket point for coaches to other
// provinces, and where you get off when you come by coach), the departures board, the waiting seats
// and a food kiosk; the coach bays where the double-deckers come and go (CoachYard); and the
// songthaew queue (คิวรถสองแถว), its trucks lined up under a canopy with their routes on the roof.
// Also the vehicles themselves: the coach and the songthaew (CityTraffic drives songthaews round town).
//
// Local space: the front (the hall's open side, facing the ring road) is local -Z; the hall is in the
// middle, the coach bays along +X, the songthaew queue along -X, the yard lane in front of the bays.
public static class BusTerminal
{
    private const float LaneZ = -8.3f, BayZ = 1f;
    // where it stands in a town on the Town ring road (ground ±120): across the ring road from the
    // east end of the avenue, its front to the road (yaw 90)
    public static readonly Vector3 TownSite = new Vector3(108f, 0f, -11.5f);
    private static readonly float[] Bays = { 14f, 20f, 26f };

    public static Transform Build(Transform root, Vector3 at, float yaw, string town, string[] coachRoutes, string[] songthaewRoutes, bool songthaewTravel)
    {
        var t = PrimitiveKit.Group("BusTerminal_สถานีขนส่ง" + town, root, at, yaw).transform;
        var concrete = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        var floor = PrimitiveKit.Mat("PK_TerminalTile", new Color(0.82f, 0.8f, 0.75f), 0.5f);
        var roof = PrimitiveKit.Mat("PK_TerminalRoofBlue", new Color(0.18f, 0.36f, 0.66f), 0.5f, 0.3f);
        var steel = PrimitiveKit.Mat("PK_Steel", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.6f);
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        var yellow = PrimitiveKit.Mat("RoadYellow", new Color(0.98f, 0.8f, 0.15f));

        // ── ground: the forecourt along the ring road, the bus yard, the lane out to the south ──
        PrimitiveKit.Box("Forecourt", t, new Vector3(-10f, 0.02f, -8.6f), new Vector3(42f, 0.03f, 3f), ProvinceKit.Paving, collider: false);
        PrimitiveKit.Box("Yard", t, new Vector3(21f, 0.018f, -1.5f), new Vector3(20f, 0.03f, 17f), asphalt, collider: false);
        PrimitiveKit.Box("YardLane", t, new Vector3(45f, 0.017f, LaneZ), new Vector3(70f, 0.03f, 4.6f), asphalt, collider: false);
        for (float x = 12f; x < 78f; x += 5f)
            PrimitiveKit.Box("LaneDash", t, new Vector3(x, 0.04f, LaneZ - 2.1f), new Vector3(2.4f, 0.005f, 0.12f), white, collider: false);

        // a zebra crossing over the ring road from the end of the avenue (local -Z is the road)
        for (float z = -16.7f; z <= -11.2f; z += 1f)
            PrimitiveKit.Box("Zebra", t, new Vector3(-9f, 0.045f, z), new Vector3(3f, 0.01f, 0.5f), white, collider: false);
        PrimitiveKit.Box("CrossingPath", t, new Vector3(-9f, 0.02f, -10.4f), new Vector3(3f, 0.03f, 1f), ProvinceKit.Paving, collider: false);

        // a tall sign tower by the ring road, lit, seen from down the avenue
        var tower = PrimitiveKit.Group("SignTower_ป้ายบขส", t, new Vector3(-12f, 0f, -9.6f), 0f).transform;
        PrimitiveKit.Box("Pole", tower, new Vector3(0f, 6f, 0f), new Vector3(0.8f, 12f, 0.8f), PrimitiveKit.Mat("PK_FasciaWhite", new Color(0.95f, 0.95f, 0.94f), 0.5f));
        var red = PrimitiveKit.Glow("PK_TerminalSignRed", new Color(0.85f, 0.12f, 0.1f), 1.4f, 0.8f);
        foreach (float side in new[] { 0f, 90f, 180f, 270f })
        {
            var face = PrimitiveKit.Group("Face", tower, new Vector3(0f, 10.5f, 0f), side).transform;
            PrimitiveKit.Box("Panel", face, new Vector3(0f, 0f, -0.75f), new Vector3(3.2f, 2.6f, 0.1f), red, collider: false);
            PrimitiveKit.Label("บขส.\nสถานีขนส่ง", face, new Vector3(0f, 0f, -0.82f), 0.09f, Color.white);
        }

        // ── the hall: open front, a long blue roof on round columns ──
        PrimitiveKit.Box("HallFloor", t, new Vector3(0f, 0.1f, 0f), new Vector3(20f, 0.2f, 12f), floor);
        PrimitiveKit.Box("BackWall", t, new Vector3(0f, 2.8f, 5.8f), new Vector3(20f, 5.6f, 0.4f), concrete);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("SideWall", t, new Vector3(s * 10f, 2.8f, 3.6f), new Vector3(0.4f, 5.6f, 4.8f), concrete);
        PrimitiveKit.Box("HallRoof", t, new Vector3(0f, 6f, 0f), new Vector3(24f, 0.5f, 15f), roof);
        PrimitiveKit.Box("Fascia", t, new Vector3(0f, 6.6f, -7.4f), new Vector3(24f, 1.4f, 0.3f), PrimitiveKit.Mat("PK_FasciaWhite", new Color(0.95f, 0.95f, 0.94f), 0.5f), collider: false);
        PrimitiveKit.Label("สถานีขนส่งผู้โดยสาร" + town, t, new Vector3(0f, 6.8f, -7.6f), 0.09f, new Color(0.1f, 0.22f, 0.5f));
        PrimitiveKit.Label("บขส.  BUS TERMINAL", t, new Vector3(0f, 6.25f, -7.6f), 0.05f, new Color(0.75f, 0.12f, 0.1f));
        for (float x = -10f; x <= 10.1f; x += 5f)
            PrimitiveKit.Cylinder("Column", t, new Vector3(x, 3f, -6.6f), new Vector3(0.5f, 2.9f, 0.5f), steel);

        // ticket booths of the coach companies along the back wall
        string[] companies = { "บขส. (สายกลาง)", "ราชสีห์ทัวร์", "เพชรทัวร์", "ทองคำทัวร์", "สยามสายใต้" };
        Color[] colours = { new Color(0.75f, 0.12f, 0.1f), new Color(0.1f, 0.35f, 0.75f), new Color(0.55f, 0.15f, 0.6f), new Color(0.85f, 0.6f, 0.1f), new Color(0.1f, 0.55f, 0.35f) };
        for (int i = 0; i < companies.Length; i++)
        {
            float x = -8f + i * 4f;
            var booth = PrimitiveKit.Group("TicketBooth_ช่องขายตั๋ว", t, new Vector3(x, 0.2f, 4.6f)).transform;
            PrimitiveKit.Box("Counter", booth, new Vector3(0f, 0.55f, 0f), new Vector3(3.4f, 1.1f, 1f), PrimitiveKit.Mat("PK_Booth" + i, colours[i], 0.5f));
            PrimitiveKit.Box("Glass", booth, new Vector3(0f, 1.6f, 0.1f), new Vector3(3.2f, 1f, 0.05f), PrimitiveKit.Mat("PK_BoothGlass", new Color(0.6f, 0.75f, 0.85f), 0.95f), collider: false);
            PrimitiveKit.Box("SignBox", booth, new Vector3(0f, 2.55f, 0.2f), new Vector3(3.6f, 0.6f, 0.2f), PrimitiveKit.Glow("PK_BoothSign" + i, colours[i], 1f, 0.7f), collider: false);
            PrimitiveKit.Label(companies[i], booth, new Vector3(0f, 2.55f, 0.08f), 0.035f, Color.white);
        }
        // the departures board over the booths
        PrimitiveKit.Box("Board", t, new Vector3(0f, 4.3f, 5.5f), new Vector3(12f, 1.9f, 0.12f), PrimitiveKit.Glow("DepartureBoard", new Color(0.05f, 0.07f, 0.12f), 0.3f, 0.6f), collider: false);
        PrimitiveKit.Label("ขาออก · DEPARTURES\n" + string.Join("   ", coachRoutes), t, new Vector3(0f, 4.3f, 5.42f), 0.04f, new Color(1f, 0.85f, 0.3f));
        // stand at the booths for a coach ticket; coaches drop you off in front of the hall
        var zone = new GameObject("CoachZone_รถทัวร์");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1.2f, 3.2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(18f, 2f, 2.2f);
        zone.AddComponent<TravelStop>().Configure("สถานีขนส่ง " + town + " · รถทัวร์ไปต่างจังหวัด", longDistance: TravelMode.Bus);
        PrimitiveKit.Group("SpawnPoint_Bus", t, new Vector3(0f, 0.2f, -4f), 180f).AddComponent<SpawnPoint>().Configure(TravelMode.Bus);

        // waiting seats, a kiosk, the toilets sign
        var seat = PrimitiveKit.Mat("PK_SeatOrange", new Color(0.95f, 0.55f, 0.15f), 0.6f);
        for (int r = 0; r < 2; r++)
            foreach (float x in new[] { -5.5f, 5.5f })
            {
                PrimitiveKit.Box("Seats", t, new Vector3(x, 0.65f, -0.5f + r * 1.8f), new Vector3(6f, 0.1f, 0.5f), seat, collider: false);
                PrimitiveKit.Box("SeatBack", t, new Vector3(x, 0.95f, -0.25f + r * 1.8f), new Vector3(6f, 0.5f, 0.08f), seat, collider: false);
                PrimitiveKit.Box("SeatFrame", t, new Vector3(x, 0.4f, -0.5f + r * 1.8f), new Vector3(5.6f, 0.4f, 0.1f), steel, collider: false);
            }
        PrimitiveKit.Box("Kiosk", t, new Vector3(-8.5f, 0.75f, -3.5f), new Vector3(2f, 1.1f, 1.4f), PrimitiveKit.Mat("PK_KioskGreen", new Color(0.1f, 0.55f, 0.3f), 0.5f));
        PrimitiveKit.Label("ร้านค้า · น้ำดื่ม ขนม", t, new Vector3(-8.5f, 1.6f, -4.25f), 0.03f, Color.white);
        TambonBuilder.ShopZone(t, new Vector3(-8.5f, 0.2f, -4.6f), "city_food");
        PrimitiveKit.Label("ห้องน้ำ  ↗", t, new Vector3(8.6f, 3.4f, 5.5f), 0.04f, new Color(0.1f, 0.22f, 0.5f));
        Npcs.TouristCrowd(t, new Vector3(0f, 0.2f, 0f), new Vector2(16f, 5f), 9, 2, 5, 22);
        StreetFurniture.HangingLight(t, new Vector3(-5f, 5.7f, 0f));
        StreetFurniture.HangingLight(t, new Vector3(5f, 5.7f, 0f));

        // ── the coach bays: island platforms with canopies between the parked coaches ──
        for (int i = 0; i <= Bays.Length; i++)
        {
            float x = Bays[0] - 3f + i * 6f;
            PrimitiveKit.Box("Platform", t, new Vector3(x, 0.15f, 1.5f), new Vector3(1.4f, 0.3f, 12f), concrete);
            PrimitiveKit.Box("Canopy", t, new Vector3(x, 4.8f, 1.5f), new Vector3(3.4f, 0.2f, 12f), roof, collider: false);
            foreach (float z in new[] { -3.5f, 1.5f, 6.5f })
                PrimitiveKit.Cylinder("CanopyPost", t, new Vector3(x, 2.4f, z), new Vector3(0.22f, 2.4f, 0.22f), steel, collider: false);
        }
        var coaches = new List<Transform>();
        for (int i = 0; i < Bays.Length; i++)
        {
            float x = Bays[i];
            PrimitiveKit.Box("BayLine", t, new Vector3(x, 0.04f, -4.8f), new Vector3(2.8f, 0.005f, 0.15f), yellow, collider: false);
            PrimitiveKit.Box("BayNumber", t, new Vector3(x, 5.25f, -4.6f), new Vector3(1.6f, 0.6f, 0.1f), PrimitiveKit.Glow("PK_BayLed", new Color(0.1f, 0.6f, 0.95f), 1.2f, 0.6f), collider: false);
            PrimitiveKit.Label($"ชานชาลา {i + 1}", t, new Vector3(x, 5.25f, -4.66f), 0.03f, Color.white);
            var c = PrimitiveKit.Group("Coach_รถทัวร์", t, new Vector3(x, 0f, BayZ), 180f).transform;
            Coach(c, i, coachRoutes.Length > 0 ? coachRoutes[i % coachRoutes.Length] : "กรุงเทพฯ");
            coaches.Add(c);
        }
        t.gameObject.AddComponent<CoachYard>().Configure(coaches.ToArray(), Bays, BayZ, LaneZ, 78f);
        PrimitiveKit.Label("ทางรถทัวร์เข้า-ออก →", t, new Vector3(34f, 0.06f, LaneZ), 0.06f, Color.white).transform.localRotation = Quaternion.Euler(90f, 90f, 0f);

        // ── the songthaew queue under a canopy ──
        var q = PrimitiveKit.Group("SongthaewQueue_คิวรถสองแถว", t, new Vector3(-21f, 0f, 0f)).transform;
        PrimitiveKit.Box("QueueYard", q, new Vector3(0f, 0.018f, -1f), new Vector3(18f, 0.03f, 14f), asphalt, collider: false);
        PrimitiveKit.Box("QueueRoof", q, new Vector3(0f, 4.2f, 0f), new Vector3(18f, 0.25f, 10f), PrimitiveKit.Mat("PK_QueueRoof", new Color(0.85f, 0.35f, 0.15f), 0.4f), collider: false);
        foreach (float x in new[] { -8.5f, 0f, 8.5f })
            foreach (float z in new[] { -4.5f, 4.5f })
                PrimitiveKit.Cylinder("Post", q, new Vector3(x, 2.1f, z), new Vector3(0.25f, 2.1f, 0.25f), steel);
        PrimitiveKit.Box("QueueSign", q, new Vector3(0f, 4.8f, -5f), new Vector3(8f, 0.9f, 0.15f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label("คิวรถสองแถว", q, new Vector3(0f, 4.8f, -5.1f), 0.07f, new Color(0.2f, 0.1f, 0.05f));
        for (int i = 0; i < 4; i++)
        {
            var s = PrimitiveKit.Group("Songthaew_รถสองแถว", q, new Vector3(-6.3f + i * 4.2f, 0f, 0.5f), 180f).transform;
            Songthaew(s, i, songthaewRoutes.Length > 0 ? songthaewRoutes[i % songthaewRoutes.Length] : "รอบเมือง");
            var col = s.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.9f, 2.4f, 5f);
            col.center = new Vector3(0f, 1.2f, 0f);
        }
        PrimitiveKit.Box("Bench", q, new Vector3(0f, 0.45f, 4f), new Vector3(14f, 0.1f, 0.5f), seat, collider: false);
        if (songthaewTravel)
        {
            // the queue is also the songthaew stop out to the villages, the sub-district and the beach
            var stop = new GameObject("TravelZone");
            stop.transform.SetParent(q, false);
            stop.transform.localPosition = new Vector3(0f, 1f, 4f);
            var sb = stop.AddComponent<BoxCollider>();
            sb.isTrigger = true;
            sb.size = new Vector3(14f, 2f, 1.6f);
            stop.AddComponent<TravelStop>().Configure("คิวรถสองแถว · ไปหมู่บ้าน ตำบล ทะเล");
        }
        else
            Places.Sign(q, new Vector3(9.5f, 0f, -6f), "รถสองแถว\nวิ่งรอบเมือง · โบกได้ริมถนน 20 บาท", 0f);
        return t;
    }

    // The city's own terminal, small enough for the middle of town: on the homes soi behind the bank
    // (the site x 20..62, z -40..-63), a footpath up beside the bank to the avenue's bus shelter.
    // Local space as Build: front (to the soi) local -Z. Hall at the west end, the songthaew lay-by on
    // the soi in front of it, three coach bays to the east; the coaches use the soi itself as their lane.
    public static readonly Vector3 CitySite = new Vector3(41f, 0f, -52f);
    private static readonly float[] CompactBays = { 3f, 9f, 15f };

    public static Transform Compact(Transform root, Vector3 at, float yaw, string town, string[] coachRoutes, string[] songthaewRoutes, bool songthaewTravel)
    {
        const float bayZ = 3f, laneZ = -13.5f; // laneZ: the soi (world z -66 at the city site)
        var t = PrimitiveKit.Group("BusTerminal_สถานีขนส่ง" + town, root, at, yaw).transform;
        var concrete = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        var floor = PrimitiveKit.Mat("PK_TerminalTile", new Color(0.82f, 0.8f, 0.75f), 0.5f);
        var roof = PrimitiveKit.Mat("PK_TerminalRoofBlue", new Color(0.18f, 0.36f, 0.66f), 0.5f, 0.3f);
        var steel = PrimitiveKit.Mat("PK_Steel", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.6f);
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var yellow = PrimitiveKit.Mat("RoadYellow", new Color(0.98f, 0.8f, 0.15f));
        var white = PrimitiveKit.Mat("PK_FasciaWhite", new Color(0.95f, 0.95f, 0.94f), 0.5f);
        var red = PrimitiveKit.Glow("PK_TerminalSignRed", new Color(0.85f, 0.12f, 0.1f), 1.4f, 0.8f);
        var seat = PrimitiveKit.Mat("PK_SeatOrange", new Color(0.95f, 0.55f, 0.15f), 0.6f);

        // ── ground: forecourt along the soi, the songthaew lay-by, the coach yard ──
        PrimitiveKit.Box("Forecourt", t, new Vector3(-11.25f, 0.025f, -3f), new Vector3(19.5f, 0.03f, 6f), ProvinceKit.Paving, collider: false);
        PrimitiveKit.Box("LayBy", t, new Vector3(-13f, 0.018f, -8.5f), new Vector3(16f, 0.03f, 5f), asphalt, collider: false);
        PrimitiveKit.Box("Yard", t, new Vector3(9.75f, 0.018f, -0.5f), new Vector3(22.5f, 0.03f, 21f), asphalt, collider: false);

        // the footpath up beside the bank to the avenue (the bus shelter at its top)
        PrimitiveKit.Box("Footpath", t, new Vector3(-2.7f, 0.022f, 20f), new Vector3(2.2f, 0.03f, 21f), ProvinceKit.Paving, collider: false);
        PrimitiveKit.Box("FootpathIn", t, new Vector3(-2.7f, 0.024f, 4.5f), new Vector3(2.2f, 0.03f, 10f), ProvinceKit.Paving, collider: false);
        var arch = PrimitiveKit.Group("Arch_ทางไปบขส", t, new Vector3(-2.7f, 0f, 29.5f), 180f).transform;
        foreach (float x in new[] { -1.5f, 1.5f })
            PrimitiveKit.Box("Post", arch, new Vector3(x, 1.7f, 0f), new Vector3(0.25f, 3.4f, 0.25f), white);
        PrimitiveKit.Box("Panel", arch, new Vector3(0f, 3.6f, 0f), new Vector3(3.6f, 0.9f, 0.12f), red, collider: false);
        PrimitiveKit.Label("บขส. สถานีขนส่ง ↓", arch, new Vector3(0f, 3.6f, -0.08f), 0.045f, Color.white);
        PrimitiveKit.Label("บขส. สถานีขนส่ง ↓", arch, new Vector3(0f, 3.6f, 0.08f), 0.045f, Color.white, 180f);

        // a sign tower on the corner by the soi, lit
        var tower = PrimitiveKit.Group("SignTower_ป้ายบขส", t, new Vector3(-20.5f, 0f, -5.5f), 0f).transform;
        PrimitiveKit.Box("Pole", tower, new Vector3(0f, 4f, 0f), new Vector3(0.6f, 8f, 0.6f), white);
        foreach (float side in new[] { 0f, 90f, 180f, 270f })
        {
            var face = PrimitiveKit.Group("Face", tower, new Vector3(0f, 7.2f, 0f), side).transform;
            PrimitiveKit.Box("Panel", face, new Vector3(0f, 0f, -0.6f), new Vector3(2.4f, 1.9f, 0.1f), red, collider: false);
            PrimitiveKit.Label("บขส.\nสถานีขนส่ง", face, new Vector3(0f, 0f, -0.67f), 0.07f, Color.white);
        }

        // ── the hall: 16 x 9, open to the soi ──
        var hall = PrimitiveKit.Group("Hall", t, new Vector3(-12f, 0f, 4.5f)).transform;
        PrimitiveKit.Box("HallFloor", hall, new Vector3(0f, 0.1f, 0f), new Vector3(16f, 0.2f, 9f), floor);
        PrimitiveKit.Box("BackWall", hall, new Vector3(0f, 2.3f, 4.3f), new Vector3(16f, 4.6f, 0.4f), concrete);
        PrimitiveKit.Box("SideWall", hall, new Vector3(-7.8f, 2.3f, 2f), new Vector3(0.4f, 4.6f, 4.6f), concrete);
        PrimitiveKit.Box("HallRoof", hall, new Vector3(0f, 4.9f, -0.3f), new Vector3(18f, 0.4f, 11f), roof);
        PrimitiveKit.Box("Fascia", hall, new Vector3(0f, 5.4f, -5.7f), new Vector3(18f, 1.1f, 0.25f), white, collider: false);
        PrimitiveKit.Label("สถานีขนส่งผู้โดยสาร" + town, hall, new Vector3(0f, 5.55f, -5.85f), 0.07f, new Color(0.1f, 0.22f, 0.5f));
        PrimitiveKit.Label("บขส.  BUS TERMINAL", hall, new Vector3(0f, 5.1f, -5.85f), 0.04f, new Color(0.75f, 0.12f, 0.1f));
        for (float x = -7.5f; x <= 7.6f; x += 5f)
            PrimitiveKit.Cylinder("Column", hall, new Vector3(x, 2.4f, -4.6f), new Vector3(0.4f, 2.35f, 0.4f), steel);

        string[] companies = { "บขส. (สายกลาง)", "ราชสีห์ทัวร์", "เพชรทัวร์" };
        Color[] colours = { new Color(0.75f, 0.12f, 0.1f), new Color(0.1f, 0.35f, 0.75f), new Color(0.55f, 0.15f, 0.6f) };
        for (int i = 0; i < companies.Length; i++)
        {
            var booth = PrimitiveKit.Group("TicketBooth_ช่องขายตั๋ว", hall, new Vector3(-4f + i * 4f, 0.2f, 3.2f)).transform;
            PrimitiveKit.Box("Counter", booth, new Vector3(0f, 0.55f, 0f), new Vector3(3.4f, 1.1f, 1f), PrimitiveKit.Mat("PK_Booth" + i, colours[i], 0.5f));
            PrimitiveKit.Box("Glass", booth, new Vector3(0f, 1.6f, 0.1f), new Vector3(3.2f, 1f, 0.05f), PrimitiveKit.Mat("PK_BoothGlass", new Color(0.6f, 0.75f, 0.85f), 0.95f), collider: false);
            PrimitiveKit.Box("SignBox", booth, new Vector3(0f, 2.45f, 0.2f), new Vector3(3.6f, 0.55f, 0.2f), PrimitiveKit.Glow("PK_BoothSign" + i, colours[i], 1f, 0.7f), collider: false);
            PrimitiveKit.Label(companies[i], booth, new Vector3(0f, 2.45f, 0.08f), 0.035f, Color.white);
        }
        PrimitiveKit.Box("Board", hall, new Vector3(0f, 3.75f, 4f), new Vector3(10f, 1.3f, 0.12f), PrimitiveKit.Glow("DepartureBoard", new Color(0.05f, 0.07f, 0.12f), 0.3f, 0.6f), collider: false);
        PrimitiveKit.Label("ขาออก · DEPARTURES\n" + string.Join("   ", coachRoutes), hall, new Vector3(0f, 3.75f, 3.92f), 0.035f, new Color(1f, 0.85f, 0.3f));
        var zone = new GameObject("CoachZone_รถทัวร์");
        zone.transform.SetParent(hall, false);
        zone.transform.localPosition = new Vector3(0f, 1.2f, 1.9f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(12f, 2f, 2f);
        zone.AddComponent<TravelStop>().Configure("สถานีขนส่ง " + town + " · รถทัวร์ไปต่างจังหวัด", longDistance: TravelMode.Bus);
        PrimitiveKit.Group("SpawnPoint_Bus", t, new Vector3(-6f, 0.2f, -2.5f), 180f).AddComponent<SpawnPoint>().Configure(TravelMode.Bus);

        foreach (float x in new[] { -4f, 4f })
        {
            PrimitiveKit.Box("Seats", hall, new Vector3(x, 0.65f, -1.2f), new Vector3(5f, 0.1f, 0.5f), seat, collider: false);
            PrimitiveKit.Box("SeatBack", hall, new Vector3(x, 0.95f, -0.95f), new Vector3(5f, 0.5f, 0.08f), seat, collider: false);
            PrimitiveKit.Box("SeatFrame", hall, new Vector3(x, 0.4f, -1.2f), new Vector3(4.6f, 0.4f, 0.1f), steel, collider: false);
        }
        PrimitiveKit.Box("Kiosk", hall, new Vector3(-6.4f, 0.75f, -0.5f), new Vector3(1.8f, 1.1f, 1.4f), PrimitiveKit.Mat("PK_KioskGreen", new Color(0.1f, 0.55f, 0.3f), 0.5f));
        PrimitiveKit.Label("ร้านค้า · น้ำดื่ม ขนม", hall, new Vector3(-6.4f, 1.6f, -1.25f), 0.03f, Color.white);
        TambonBuilder.ShopZone(hall, new Vector3(-6.4f, 0.2f, -1.7f), "city_food");
        Npcs.TouristCrowd(hall, new Vector3(0f, 0.2f, 0f), new Vector2(12f, 4f), 6, 2, 5, 22);
        StreetFurniture.HangingLight(hall, new Vector3(-4f, 4.6f, 0f));
        StreetFurniture.HangingLight(hall, new Vector3(4f, 4.6f, 0f));

        // ── the songthaew queue: a lay-by on the soi, a canopy and a bench on the forecourt ──
        var q = PrimitiveKit.Group("SongthaewQueue_คิวรถสองแถว", t, new Vector3(-13f, 0f, 0f)).transform;
        PrimitiveKit.Box("QueueRoof", q, new Vector3(0f, 3.6f, -4.3f), new Vector3(15f, 0.2f, 3f), PrimitiveKit.Mat("PK_QueueRoof", new Color(0.85f, 0.35f, 0.15f), 0.4f), collider: false);
        foreach (float x in new[] { -7f, 0f, 7f })
            PrimitiveKit.Cylinder("Post", q, new Vector3(x, 1.8f, -3f), new Vector3(0.2f, 1.8f, 0.2f), steel);
        PrimitiveKit.Box("QueueSign", q, new Vector3(0f, 4.15f, -5.7f), new Vector3(6f, 0.8f, 0.12f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label("คิวรถสองแถว", q, new Vector3(0f, 4.15f, -5.78f), 0.06f, new Color(0.2f, 0.1f, 0.05f));
        PrimitiveKit.Box("Bench", q, new Vector3(0f, 0.45f, -3.4f), new Vector3(10f, 0.1f, 0.5f), seat, collider: false);
        for (int i = 0; i < 3; i++)
        {
            // parked nose east (keep left on the soi), along the lay-by
            var s = PrimitiveKit.Group("Songthaew_รถสองแถว", q, new Vector3(-5.5f + i * 6f, 0f, -8.6f), 90f).transform;
            Songthaew(s, i, songthaewRoutes.Length > 0 ? songthaewRoutes[i % songthaewRoutes.Length] : "รอบเมือง");
            var col = s.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.9f, 2.4f, 5f);
            col.center = new Vector3(0f, 1.2f, 0f);
        }
        if (songthaewTravel)
        {
            var stop = new GameObject("TravelZone");
            stop.transform.SetParent(q, false);
            stop.transform.localPosition = new Vector3(0f, 1f, -4f);
            var sb = stop.AddComponent<BoxCollider>();
            sb.isTrigger = true;
            sb.size = new Vector3(12f, 2f, 1.8f);
            stop.AddComponent<TravelStop>().Configure("คิวรถสองแถว · ไปหมู่บ้าน ตำบล ทะเล");
        }
        else
            Places.Sign(q, new Vector3(7.8f, 0f, -5.8f), "รถสองแถว\nวิ่งรอบเมือง · โบกได้ริมถนน 20 บาท", 0f);

        // ── three coach bays with island platforms; the coaches back in from the soi ──
        for (int i = 0; i <= CompactBays.Length; i++)
        {
            float x = CompactBays[0] - 3f + i * 6f;
            PrimitiveKit.Box("Platform", t, new Vector3(x, 0.15f, 4f), new Vector3(1.4f, 0.3f, 11f), concrete);
            PrimitiveKit.Box("Canopy", t, new Vector3(x, 4.8f, 4f), new Vector3(3.4f, 0.2f, 11f), roof, collider: false);
            foreach (float z in new[] { 0f, 8f })
                PrimitiveKit.Cylinder("CanopyPost", t, new Vector3(x, 2.4f, z), new Vector3(0.22f, 2.4f, 0.22f), steel, collider: false);
        }
        var coaches = new List<Transform>();
        for (int i = 0; i < CompactBays.Length; i++)
        {
            float x = CompactBays[i];
            PrimitiveKit.Box("BayLine", t, new Vector3(x, 0.04f, -3.4f), new Vector3(2.8f, 0.005f, 0.15f), yellow, collider: false);
            PrimitiveKit.Box("BayNumber", t, new Vector3(x, 5.25f, -1.4f), new Vector3(1.6f, 0.6f, 0.1f), PrimitiveKit.Glow("PK_BayLed", new Color(0.1f, 0.6f, 0.95f), 1.2f, 0.6f), collider: false);
            PrimitiveKit.Label($"ชานชาลา {i + 1}", t, new Vector3(x, 5.25f, -1.46f), 0.03f, Color.white);
            var c = PrimitiveKit.Group("Coach_รถทัวร์", t, new Vector3(x, 0f, bayZ), 180f).transform;
            Coach(c, i, coachRoutes.Length > 0 ? coachRoutes[i % coachRoutes.Length] : "กรุงเทพฯ");
            coaches.Add(c);
        }
        // out along the soi to the ring road (world x 88) and away; in the same way
        t.gameObject.AddComponent<CoachYard>().Configure(coaches.ToArray(), CompactBays, bayZ, laneZ, 47f);
        return t;
    }

    // ── รถทัวร์ 2 ชั้น: a double-deck VIP coach, nose to local +Z ──
    internal static Vector3 Coach(Transform v, int variant, string route)
    {
        Color[] bodies = { new Color(0.95f, 0.95f, 0.96f), new Color(0.1f, 0.3f, 0.7f), new Color(0.6f, 0.1f, 0.15f), new Color(0.95f, 0.75f, 0.15f) };
        Color[] stripes = { new Color(0.1f, 0.35f, 0.8f), new Color(0.95f, 0.8f, 0.2f), new Color(0.95f, 0.95f, 0.95f), new Color(0.75f, 0.12f, 0.1f) };
        var body = PrimitiveKit.Mat("PK_Coach" + variant % 4, bodies[variant % 4], 0.8f, 0.25f);
        var stripe = PrimitiveKit.Mat("PK_CoachStripe" + variant % 4, stripes[variant % 4], 0.8f, 0.25f);
        var glass = PrimitiveKit.Mat("CarGlassTint", new Color(0.2f, 0.28f, 0.36f), 0.95f, 0.3f);
        var tyre = PrimitiveKit.Mat("PK_Tyre", new Color(0.06f, 0.06f, 0.06f), 0.2f);
        var rim = PrimitiveKit.Mat("PK_Rim", new Color(0.8f, 0.82f, 0.85f), 0.8f, 0.8f);
        const float L = 12f, W = 2.5f, H = 4f;
        PrimitiveKit.Box("Body", v, new Vector3(0f, 0.5f + H * 0.5f, 0f), new Vector3(W, H, L), body, collider: false);
        PrimitiveKit.Box("LowerWindows", v, new Vector3(0f, 1.9f, -1f), new Vector3(W + 0.02f, 0.8f, L - 4.5f), glass, collider: false);
        PrimitiveKit.Box("UpperWindows", v, new Vector3(0f, 3.4f, -0.2f), new Vector3(W + 0.02f, 1.1f, L - 1.4f), glass, collider: false);
        PrimitiveKit.Box("Stripe", v, new Vector3(0f, 2.65f, 0f), new Vector3(W + 0.03f, 0.35f, L + 0.02f), stripe, collider: false);
        PrimitiveKit.Box("Swoosh", v, new Vector3(0f, 1.1f, -2f), new Vector3(W + 0.03f, 0.25f, L - 4f), stripe, collider: false);
        PrimitiveKit.Box("Windscreen", v, new Vector3(0f, 2.6f, L * 0.5f + 0.01f), new Vector3(W - 0.15f, 3.2f, 0.04f), glass, collider: false);
        PrimitiveKit.Box("RouteBoard", v, new Vector3(0f, 4.2f, L * 0.5f + 0.03f), new Vector3(W - 0.4f, 0.32f, 0.04f), PrimitiveKit.Glow("BusLed", new Color(1f, 0.6f, 0.1f), 1.8f, 0.2f), collider: false);
        PrimitiveKit.Label(route, v, new Vector3(0f, 4.2f, L * 0.5f + 0.06f), 0.02f, new Color(0.15f, 0.05f, 0f), 180f);
        PrimitiveKit.Box("Door", v, new Vector3(-W * 0.5f - 0.01f, 1.6f, L * 0.5f - 1.2f), new Vector3(0.03f, 2.2f, 1f), glass, collider: false);
        PrimitiveKit.Box("Bumper", v, new Vector3(0f, 0.6f, L * 0.5f + 0.05f), new Vector3(W, 0.3f, 0.1f), PrimitiveKit.Mat("Bumper", new Color(0.12f, 0.12f, 0.13f)), collider: false);
        foreach (float x in new[] { -0.85f, 0.85f })
            PrimitiveKit.Box("Headlamp", v, new Vector3(x, 0.95f, L * 0.5f + 0.03f), new Vector3(0.5f, 0.2f, 0.04f), PrimitiveKit.Glow("PK_Headlamp", new Color(1f, 0.97f, 0.85f), 1.5f), collider: false);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { L * 0.5f - 2.2f, -L * 0.5f + 3.1f, -L * 0.5f + 1.8f })
            {
                PrimitiveKit.Cylinder("Tyre", v, new Vector3(x * (W * 0.5f - 0.15f), 0.5f, z), new Vector3(1f, 0.16f, 1f), tyre, new Vector3(0f, 0f, 90f), collider: false);
                PrimitiveKit.Cylinder("Rim", v, new Vector3(x * (W * 0.5f - 0.05f), 0.5f, z), new Vector3(0.55f, 0.02f, 0.55f), rim, new Vector3(0f, 0f, 90f), collider: false);
            }
        if (v.GetComponent<Collider>() == null)
        {
            var col = v.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(W, H + 0.5f, L);
            col.center = new Vector3(0f, (H + 0.5f) * 0.5f, 0f);
        }
        return new Vector3(W, H + 0.5f, L);
    }

    // ── รถสองแถว: a pickup with a roofed bed and two long benches, nose to local +Z ──
    public static readonly Vector3 SongthaewSeat = new Vector3(-0.5f, 0.92f, -1.3f);

    internal static Vector3 Songthaew(Transform v, int variant, string route)
    {
        Color[] paints = { new Color(0.15f, 0.4f, 0.85f), new Color(0.85f, 0.15f, 0.15f), new Color(0.95f, 0.8f, 0.15f), new Color(0.15f, 0.6f, 0.35f), new Color(0.95f, 0.95f, 0.95f) };
        var paint = PrimitiveKit.Mat("PK_Songthaew" + variant % 5, paints[variant % 5], 0.8f, 0.25f);
        var glass = PrimitiveKit.Mat("CarGlassTint", new Color(0.2f, 0.28f, 0.36f), 0.95f, 0.3f);
        var dark = PrimitiveKit.Mat("Bumper", new Color(0.12f, 0.12f, 0.13f));
        var chrome = PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.95f, 1f);
        var bench = PrimitiveKit.Mat("PK_SongthaewBench", new Color(0.55f, 0.12f, 0.12f), 0.4f);
        var tyre = PrimitiveKit.Mat("PK_Tyre", new Color(0.06f, 0.06f, 0.06f), 0.2f);
        // cab
        PrimitiveKit.Box("Bonnet", v, new Vector3(0f, 0.85f, 1.95f), new Vector3(1.8f, 0.6f, 1.1f), paint, collider: false);
        PrimitiveKit.Box("Cab", v, new Vector3(0f, 1.35f, 0.95f), new Vector3(1.8f, 1.1f, 1.2f), paint, collider: false);
        PrimitiveKit.Box("Windscreen", v, new Vector3(0f, 1.55f, 1.56f), new Vector3(1.6f, 0.6f, 0.04f), glass, collider: false);
        PrimitiveKit.Box("SideWindows", v, new Vector3(0f, 1.55f, 0.95f), new Vector3(1.82f, 0.5f, 0.9f), glass, collider: false);
        PrimitiveKit.Box("Grille", v, new Vector3(0f, 0.8f, 2.51f), new Vector3(1.4f, 0.35f, 0.03f), chrome, collider: false);
        PrimitiveKit.Box("Bumper", v, new Vector3(0f, 0.5f, 2.55f), new Vector3(1.85f, 0.18f, 0.1f), chrome, collider: false);
        // the bed with its roof on posts, benches along both sides, a step at the back
        PrimitiveKit.Box("Bed", v, new Vector3(0f, 0.7f, -1.1f), new Vector3(1.85f, 0.25f, 2.9f), paint, collider: false);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Side", v, new Vector3(s * 0.9f, 1.05f, -1.1f), new Vector3(0.06f, 0.5f, 2.9f), paint, collider: false);
            PrimitiveKit.Box("Bench", v, new Vector3(s * 0.62f, 0.95f, -1.15f), new Vector3(0.4f, 0.1f, 2.6f), bench, collider: false);
            foreach (float z in new[] { 0.3f, -2.5f })
                PrimitiveKit.Box("RoofPost", v, new Vector3(s * 0.88f, 1.75f, z), new Vector3(0.06f, 1.1f, 0.06f), chrome, collider: false);
            PrimitiveKit.Box("GrabRail", v, new Vector3(s * 0.88f, 1.7f, -1.1f), new Vector3(0.04f, 0.04f, 2.8f), chrome, collider: false);
        }
        PrimitiveKit.Box("Roof", v, new Vector3(0f, 2.33f, -0.6f), new Vector3(1.95f, 0.08f, 4.1f), paint, collider: false);
        PrimitiveKit.Box("Step", v, new Vector3(0f, 0.38f, -2.75f), new Vector3(1.2f, 0.06f, 0.4f), chrome, collider: false);
        PrimitiveKit.Box("RouteSign", v, new Vector3(0f, 2.55f, 0.9f), new Vector3(1.5f, 0.38f, 0.06f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label(route, v, new Vector3(0f, 2.55f, 0.94f), 0.022f, new Color(0.25f, 0.05f, 0.05f), 180f);
        PrimitiveKit.Label(route, v, new Vector3(0f, 2.55f, 0.86f), 0.022f, new Color(0.25f, 0.05f, 0.05f));
        foreach (float x in new[] { -0.82f, 0.82f })
            foreach (float z in new[] { 1.75f, -1.5f })
                PrimitiveKit.Cylinder("Wheel", v, new Vector3(x, 0.36f, z), new Vector3(0.72f, 0.14f, 0.72f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        foreach (float x in new[] { -0.7f, 0.7f })
            PrimitiveKit.Box("Headlamp", v, new Vector3(x, 0.95f, 2.51f), new Vector3(0.3f, 0.16f, 0.03f), PrimitiveKit.Glow("PK_Headlamp", new Color(1f, 0.97f, 0.85f), 1.5f), collider: false);
        PrimitiveKit.Box("Underbody", v, new Vector3(0f, 0.45f, -0.3f), new Vector3(1.6f, 0.2f, 4.6f), dark, collider: false);
        return new Vector3(1.95f, 2.45f, 5.2f);
    }
}
