using System.Collections.Generic;
using UnityEngine;

// New places from the game art: the beach (ทะเล), the cow farm (ฟาร์มวัว), the city noodle shop
// (ร้านก๋วยเตี๋ยว) and the elevated skytrain (รถไฟฟ้า).
public static class Seaside
{
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);

    // ── Beach scene content (the road runs along z = -8, the sea starts at z = 24) ─────────
    public static void Beach(Transform root)
    {
        var sand = PrimitiveKit.Mat("BeachSand", new Color(0.95f, 0.86f, 0.62f));
        var shallow = PrimitiveKit.Mat("SeaWaterShallow", new Color(0.35f, 0.85f, 0.85f));
        var deep = PrimitiveKit.Mat("SeaWater", new Color(0.1f, 0.55f, 0.75f));
        PrimitiveKit.Box("Sand", root, new Vector3(0f, 0.015f, 9f), new Vector3(100f, 0.03f, 30f), sand, collider: false);
        PrimitiveKit.Box("Shallows", root, new Vector3(0f, 0.03f, 26f), new Vector3(100f, 0.02f, 4f), shallow, collider: false);
        PrimitiveKit.Box("Sea", root, new Vector3(0f, 0.025f, 72f), new Vector3(280f, 0.02f, 96f), deep, collider: false); // the open bay, out to the horizon
        // Can't walk into the sea, except along the pier (x 23..27)
        Blocker(root, new Vector3(-13.5f, 1f, 37f), new Vector3(73f, 2f, 26f));
        Blocker(root, new Vector3(38.5f, 1f, 37f), new Vector3(23f, 2f, 26f));
        Blocker(root, new Vector3(25f, 1f, 43.5f), new Vector3(4f, 2f, 13f));
        Blocker(root, new Vector3(-50.5f, 1.5f, 0f), new Vector3(1f, 3f, 100f));  // town edges: the hills beyond are scenery
        Blocker(root, new Vector3(50.5f, 1.5f, 0f), new Vector3(1f, 3f, 100f));
        Blocker(root, new Vector3(0f, 1.5f, -46f), new Vector3(100f, 3f, 1f));

        // Palms along the back of the beach and by the water
        for (int i = 0; i < 14; i++)
        {
            float x = -46f + i * 7f + (i % 3) * 1.3f;
            Nature.CoconutPalm(root, new Vector3(x, 0f, i % 2 == 0 ? 1.5f : 21f), 0.9f + (i % 3) * 0.12f, i + 40);
        }
        foreach (var p in new[] { new Vector3(-24f, 0f, 22.5f), new Vector3(14f, 0f, 23f), new Vector3(33f, 0f, 22f) })
            Nature.Rock(root, p, 1.2f, (int)p.x);

        // Beach umbrellas with deck chairs; lie on a chair to rest (พักผ่อนริมทะเล)
        Color[] canopy = { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.75f, 0.45f), new Color(1f, 0.5f, 0.75f) };
        for (int i = 0; i < 5; i++)
            Umbrella(root, new Vector3(-30f + i * 10f, 0f, 14f), canopy[i], i);

        Pier(root, new Vector3(25f, 0f, 18f));
        TourBoat(root);
        Island.FerryStop(root, new Vector3(20f, 0f, 17f), 0f, "เรือพาเที่ยวเกาะ", "เรือพาเที่ยวเกาะ"); // the only way to the island (อ่าวมาหยา)

        // Longtail boats (เรือหางยาว) and a fishing boat moored by the pier
        LongtailBoat(root, new Vector3(-12f, 0f, 30f), 20f, new Color(0.95f, 0.35f, 0.25f));
        LongtailBoat(root, new Vector3(3f, 0f, 29f), -15f, new Color(0.2f, 0.6f, 0.9f));
        LongtailBoat(root, new Vector3(13f, 0f, 33f), 5f, new Color(0.95f, 0.8f, 0.2f));
        FishingBoat(root, new Vector3(30f, 0f, 30f));

        SeafoodShack(root, new Vector3(-40f, 0f, 5f));

        foreach (float x in new[] { -36f, -18f, 0f, 18f, 36f })
            StreetFurniture.LampPost(root, new Vector3(x, 0f, 4f));
        Npcs.Crowd(root, new Vector3(0f, 0f, 16f), new Vector2(70f, 10f), 8, "", 7, 18, "ทะเลสวยจัง|น้ำใสมาก|ไปนั่งเรือกันไหม?|ร้อนจัง กินมะพร้าวดีกว่า|ถ่ายรูปหน่อย!");
        Places.Sign(root, new Vector3(-3f, 0f, -4.5f), "ทะเล\nชายหาด · ท่าเรือประมง", 0f);

        BeachRoad(root);
        Mountains(root);
        ConcertStage(root, new Vector3(40f, 0f, 12f));
        HotelRooms(root);
    }

    // Beach concert stage (เวทีคอนเสิร์ตริมหาด) for the national events: the stage, band, screen
    // and lights only appear while a concert is on (FestivalGate "beach"); the frame stays all year.
    private static void ConcertStage(Transform root, Vector3 pos)
    {
        var st = PrimitiveKit.Group("ConcertStage_เวทีคอนเสิร์ต", root, pos, 90f).transform; // faces the beach (-X)
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var black = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        PrimitiveKit.Box("StageDeck", st, new Vector3(0f, 0.6f, 1f), new Vector3(14f, 1.2f, 8f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussTower", st, new Vector3(x, 4.5f, 4.5f), new Vector3(0.5f, 9f, 0.5f), steel);
        PrimitiveKit.Box("TrussBeam", st, new Vector3(0f, 9f, 4.5f), new Vector3(15f, 0.5f, 0.5f), steel, collider: false);
        PrimitiveKit.Box("TrussFront", st, new Vector3(0f, 8.2f, -2.5f), new Vector3(15f, 0.4f, 0.4f), steel, collider: false);
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussSide", st, new Vector3(x, 8.6f, 1f), new Vector3(0.4f, 0.4f, 7f), steel, collider: false);
        PrimitiveKit.Box("BannerBoard", st, new Vector3(0f, 10f, 4.5f), new Vector3(10f, 1.4f, 0.2f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label("เวทีคอนเสิร์ตริมหาด", st, new Vector3(0f, 10f, 4.38f), 0.11f, new Color(0.6f, 0.1f, 0.1f));

        var show = PrimitiveKit.Group("Attractions", st, Vector3.zero).transform;
        st.gameObject.AddComponent<FestivalGate>().Configure("beach", null);
        PrimitiveKit.Box("LedScreen", show, new Vector3(0f, 4.6f, 4.2f), new Vector3(12f, 5.4f, 0.2f), PrimitiveKit.Glow("LedScreen", new Color(0.35f, 0.25f, 0.95f), 2.2f, 0.4f), collider: false);
        foreach (float x in new[] { -8.8f, 8.8f })
        {
            PrimitiveKit.Box("SpeakerStack", show, new Vector3(x, 2.2f, 0f), new Vector3(1.6f, 4.4f, 1.4f), black);
            for (int i = 0; i < 3; i++)
                PrimitiveKit.Cylinder("Cone", show, new Vector3(x, 0.9f + i * 1.4f, -0.72f), new Vector3(0.9f, 0.02f, 0.9f), steel, new Vector3(90f, 0f, 0f), collider: false);
        }
        // The band: drum kit, keyboard, mic stand and players
        PrimitiveKit.Cylinder("BassDrum", show, new Vector3(0f, 1.65f, 2.6f), new Vector3(0.9f, 0.25f, 0.9f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), new Vector3(90f, 0f, 0f), collider: false);
        foreach (float x in new[] { -0.8f, 0.8f })
            PrimitiveKit.Cylinder("Cymbal", show, new Vector3(x, 2.4f, 2.4f), new Vector3(0.6f, 0.02f, 0.6f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        PrimitiveKit.Box("Keyboard", show, new Vector3(-4f, 2f, 1.4f), new Vector3(1.6f, 0.15f, 0.5f), black, collider: false);
        PrimitiveKit.Cylinder("MicStand", show, new Vector3(0f, 2f, -1.6f), new Vector3(0.04f, 0.8f, 0.04f), steel, collider: false);
        int look = 5;
        foreach (var p in new[] { new Vector3(0f, 1.2f, -1.2f), new Vector3(-4f, 1.2f, 1.9f), new Vector3(3.8f, 1.2f, 0.6f), new Vector3(0f, 1.2f, 3.2f) })
            PrimitiveKit.Group("Performer", show, p, 180f).AddComponent<MarketVendor>().Configure(look++);

        // Coloured stage lights on the front truss, and two bright wash lights
        Color[] colors = { new Color(1f, 0.2f, 0.5f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 1f, 0.5f), new Color(0.75f, 0.3f, 1f) };
        for (int i = 0; i < 7; i++)
        {
            var c = colors[i % colors.Length];
            float x = -6f + i * 2f;
            PrimitiveKit.Cylinder("StageCan", show, new Vector3(x, 7.8f, -2.5f), new Vector3(0.35f, 0.25f, 0.35f), PrimitiveKit.Glow($"StageCan{i % colors.Length}", c, 6f, 0.4f), collider: false);
            if (i % 2 == 0)
            {
                var lg = new GameObject("StageLight");
                lg.transform.SetParent(show, false);
                lg.transform.localPosition = new Vector3(x, 7.5f, -2.5f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = 14f;
                l.intensity = 6f;
                l.shadows = LightShadows.None;
            }
        }
        foreach (float x in new[] { -4f, 4f })
            StreetFurniture.RingLight(show, new Vector3(x, 8.6f, -1f));
        ConcertRig(show);

        // Musicians (Lv 12) can play the concert: stand in front of the stage while it's on
        var music = new GameObject("MusicZone");
        music.transform.SetParent(show, false);
        music.transform.localPosition = new Vector3(0f, 1f, -4.6f);
        var musicBox = music.AddComponent<BoxCollider>();
        musicBox.isTrigger = true;
        musicBox.size = new Vector3(4f, 2f, 2f);
        music.AddComponent<MusicSpot>().Configure("beach_concert");
        // Bunting from the truss to the beach poles
        FairLights.Pole(show, new Vector3(-7f, 0f, -12f));
        FairLights.Pole(show, new Vector3(7f, 0f, -12f));
        FairLights.Festoon(show, new Vector3(-7.2f, 8f, -2.5f), new Vector3(-7f, 4.5f, -12f), 0.8f, 0.8f);
        FairLights.Festoon(show, new Vector3(7.2f, 8f, -2.5f), new Vector3(7f, 4.5f, -12f), 0.8f, 0.8f);

        Npcs.Crowd(root, new Vector3(pos.x - 10f, 0f, pos.z), new Vector2(9f, 11f), 18, "beach", 0, 24,
            "เพลงนี้มันส์มาก!|กรี๊ดดด!|ร้องตามกันเลย|ปีหน้ามาอีกนะ|ใครร้องเพลงต่อไป?");
    }

    // ── Across the road from the sand: hotels, restaurants, a minimart and a massage shop (like Patong) ──
    // Everything faces the sea (+Z); fronts line up along the pavement at z ≈ -13.
    private static void BeachRoad(Transform root)
    {
        var pavement = PrimitiveKit.Mat("Pavement", new Color(0.78f, 0.74f, 0.66f));
        PrimitiveKit.Box("BeachRoadPavement", root, new Vector3(0f, 0.01f, -12f), new Vector3(100f, 0.02f, 4f), pavement, collider: false);
        PrimitiveKit.Box("Curb", root, new Vector3(0f, 0.08f, -10.1f), new Vector3(100f, 0.16f, 0.25f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)), collider: false);

        Hotel(root, new Vector3(-40f, 0f, -18.5f), "โรงแรมป่าตองบีช", 6, 14f, new Color(0.97f, 0.95f, 0.9f), new Color(0.15f, 0.5f, 0.7f));
        OpenRestaurant(root, new Vector3(-28f, 0f, -16.5f), "ร้านอาหารทะเลสด", new Color(0.15f, 0.45f, 0.75f), seafood: true);
        CityCivic.Block(root, new Vector3(-18.5f, 0f, -18.5f), 180f, "ร้านสะดวกซื้อ 24 ชม.", 8f, 10f, 2,
            new Color(0.98f, 0.98f, 0.96f), new Color(0.95f, 0.5f, 0.1f), new Color(0.1f, 0.55f, 0.3f), Color.white, "Minimart_ร้านสะดวกซื้อ");
        Hotel(root, new Vector3(-7f, 0f, -18.5f), "โรงแรมอันดามัน", 8, 12f, new Color(0.98f, 0.9f, 0.75f), new Color(0.6f, 0.35f, 0.2f));
        CityCivic.Block(root, new Vector3(3.5f, 0f, -18.5f), 180f, "นวดแผนไทย", 8f, 10f, 2,
            new Color(0.75f, 0.6f, 0.4f), new Color(0.4f, 0.25f, 0.12f), new Color(0.45f, 0.15f, 0.35f), new Color(1f, 0.9f, 0.6f), "Massage_นวดแผนไทย");
        OpenRestaurant(root, new Vector3(9.8f, 0f, -16.5f), "ก๋วยเตี๋ยว ส้มตำ", new Color(0.85f, 0.25f, 0.2f), seafood: false, width: 6f);
        OpenRestaurant(root, new Vector3(26f, 0f, -16.5f), "ร้านอาหารริมหาด", new Color(0.2f, 0.6f, 0.35f), seafood: true);
        Hotel(root, new Vector3(40f, 0f, -18.5f), "รีสอร์ทริมหาด", 4, 14f, new Color(0.95f, 0.88f, 0.8f), new Color(0.2f, 0.55f, 0.45f));

        // Palms and gardens behind the row, then the slope up to the hills
        for (int i = 0; i < 12; i++)
            Nature.CoconutPalm(root, new Vector3(-46f + i * 8.3f, 0f, -27f - (i % 3) * 3f), 1f + (i % 2) * 0.2f, 70 + i);
        for (int i = 0; i < 16; i++)
            Nature.Bush(root, new Vector3(-47f + i * 6.2f, 0f, -33f - (i % 4) * 2.5f), 1.3f, 90 + i, flowers: i % 3 == 0);

        Npcs.Crowd(root, new Vector3(0f, 0f, -12f), new Vector2(90f, 2f), 6, "", 8, 23,
            "หิวแล้ว ไปกินซีฟู้ดกัน|คืนนี้พักโรงแรมไหนดี?|ไปนวดกันไหม?|ร้านนี้อร่อยนะ|ซื้อน้ำเย็นก่อน");
    }

    private static void Hotel(Transform root, Vector3 pos, string name, int floors, float width, Color wall, Color trim)
    {
        var b = CityCivic.Block(root, pos, 180f, name, width, 10f, floors, wall, trim, trim, Color.white, "BeachHotel_" + name);
        var rail = PrimitiveKit.Mat("RailMetal", new Color(0.2f, 0.22f, 0.24f));
        var slab = PrimitiveKit.Mat("BalconySlab", new Color(0.92f, 0.92f, 0.9f));
        for (int f = 1; f < floors; f++)
        {
            PrimitiveKit.Box("Balcony", b, new Vector3(0f, f * 3.2f + 0.05f, -5.5f), new Vector3(width - 1.4f, 0.15f, 1f), slab, collider: false);
            PrimitiveKit.Box("BalconyRail", b, new Vector3(0f, f * 3.2f + 0.6f, -5.95f), new Vector3(width - 1.4f, 0.07f, 0.07f), rail, collider: false);
        }
        // Rooftop name in lights, readable from the beach
        PrimitiveKit.Box("RoofSign", b, new Vector3(0f, floors * 3.2f + 1.1f, -3f), new Vector3(width - 2f, 1.4f, 0.15f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);
        PrimitiveKit.Label(name, b, new Vector3(0f, floors * 3.2f + 1.1f, -3.1f), 0.12f, new Color(0.35f, 0.15f, 0.05f));
        foreach (float x in new[] { -width * 0.5f - 0.8f, width * 0.5f + 0.8f })
            Nature.CoconutPalm(b, new Vector3(x, 0f, -6.5f), 0.9f, (int)(x * 10f) + floors);

        // Front desk under the entrance canopy: book a room, go up to it
        var desk = new GameObject("HotelDesk_" + name);
        desk.transform.SetParent(root, false);
        desk.transform.localPosition = new Vector3(pos.x, 1f, -13.4f);
        desk.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var box = desk.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 2f, 1.8f);
        desk.AddComponent<HotelDesk>().Configure(name);
    }

    // ── Hotel rooms (one per tier), out of sight beyond the hills: the desk teleports you in ──
    private static void HotelRooms(Transform root)
    {
        var area = PrimitiveKit.Group("HotelRooms_ห้องพักโรงแรม", root, new Vector3(0f, 0f, -220f)).transform;
        PrimitiveKit.Box("Terrace", area, new Vector3(0f, -0.05f, 0f), new Vector3(100f, 0.1f, 40f), PrimitiveKit.Mat("Pavement", new Color(0.78f, 0.74f, 0.66f)));
        for (int i = 0; i < 10; i++)
            Nature.CoconutPalm(area, new Vector3(-45f + i * 10f, 0f, (i % 2 == 0) ? -14f : 14f), 1f, 120 + i);
        Room(area, new Vector3(-26f, 0f, 0f), "standard", "ห้องมาตรฐาน");
        Room(area, new Vector3(0f, 0f, 0f), "deluxe", "ห้องดีลักซ์วิวทะเล");
        Room(area, new Vector3(28f, 0f, 0f), "suite", "สวีทริมหาด");
    }

    // An open-topped room (the camera looks in over the walls): bed to sleep in, decor by tier, door out
    private static void Room(Transform area, Vector3 pos, string tier, string title)
    {
        bool deluxe = tier != "standard", suite = tier == "suite";
        float w = suite ? 16f : 12f, d = suite ? 11f : 9f, h = 2.6f;
        var r = PrimitiveKit.Group("HotelRoom_" + tier, area, pos).transform;
        var floor = PrimitiveKit.Mat(suite ? "MarbleFloor" : deluxe ? "WoodFloorWarm" : "CarpetBeige",
            suite ? new Color(0.95f, 0.94f, 0.9f) : deluxe ? new Color(0.62f, 0.43f, 0.26f) : new Color(0.8f, 0.72f, 0.58f));
        var wall = PrimitiveKit.Mat(suite ? "SuiteWall" : deluxe ? "DeluxeWall" : "RoomWall",
            suite ? new Color(0.97f, 0.93f, 0.85f) : deluxe ? new Color(0.9f, 0.95f, 0.97f) : new Color(0.95f, 0.92f, 0.85f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var white = PrimitiveKit.Mat("Linen", new Color(0.97f, 0.97f, 0.95f));
        PrimitiveKit.Box("Floor", r, new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), floor);
        PrimitiveKit.Box("WallBack", r, new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.2f), wall);
        PrimitiveKit.Box("WallFront", r, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.2f), wall);
        PrimitiveKit.Box("WallL", r, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
        PrimitiveKit.Box("WallR", r, new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
        PrimitiveKit.Box("Door", r, new Vector3(-w * 0.5f + 1.6f, 1.05f, -d * 0.5f + 0.12f), new Vector3(1.1f, 2.1f, 0.06f), PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f)), collider: false);
        PrimitiveKit.Label(title, r, new Vector3(0f, 2.2f, d * 0.5f - 0.12f), 0.06f, new Color(0.45f, 0.3f, 0.15f));

        // Window (sea view in the better rooms)
        float winW = suite ? 7f : deluxe ? 5f : 2f;
        PrimitiveKit.Box("WindowSky", r, new Vector3(2f, 1.75f, d * 0.5f - 0.12f), new Vector3(winW, 0.9f, 0.04f), PrimitiveKit.Glow("ViewSky", new Color(0.55f, 0.8f, 1f), 1.2f), collider: false);
        if (deluxe)
            PrimitiveKit.Box("WindowSea", r, new Vector3(2f, 1.05f, d * 0.5f - 0.12f), new Vector3(winW, 0.5f, 0.04f), PrimitiveKit.Glow("ViewSea", new Color(0.1f, 0.5f, 0.8f), 1.2f), collider: false);

        // Bed (king size with posts in the suite)
        float bedW = suite ? 2.6f : 2f;
        var bedPos = new Vector3(-w * 0.5f + 2.2f, 0f, d * 0.5f - 1.6f);
        PrimitiveKit.Box("BedFrame", r, bedPos + new Vector3(0f, 0.25f, 0f), new Vector3(bedW, 0.4f, 2.3f), wood);
        PrimitiveKit.Box("Mattress", r, bedPos + new Vector3(0f, 0.55f, 0f), new Vector3(bedW - 0.1f, 0.22f, 2.2f), white, collider: false);
        PrimitiveKit.Box("Headboard", r, bedPos + new Vector3(0f, 0.9f, 1.1f), new Vector3(bedW, 1.1f, 0.12f), wood, collider: false);
        foreach (float x in suite ? new[] { -0.7f, 0f, 0.7f } : new[] { -0.45f, 0.45f })
            PrimitiveKit.Box("Pillow", r, bedPos + new Vector3(x, 0.72f, 0.8f), new Vector3(0.55f, 0.14f, 0.35f), white, collider: false);
        PrimitiveKit.Box("Blanket", r, bedPos + new Vector3(0f, 0.67f, -0.4f), new Vector3(bedW - 0.05f, 0.05f, 1.2f),
            PrimitiveKit.Mat(suite ? "BlanketGold" : deluxe ? "BlanketBlue" : "BlanketGreen", suite ? new Color(0.85f, 0.65f, 0.25f) : deluxe ? new Color(0.2f, 0.45f, 0.75f) : new Color(0.35f, 0.6f, 0.45f)), collider: false);
        if (suite)
            foreach (float x in new[] { -1.3f, 1.3f })
                foreach (float z in new[] { -1.15f, 1.15f })
                    PrimitiveKit.Cylinder("BedPost", r, bedPos + new Vector3(x, 1.2f, z), new Vector3(0.08f, 1.2f, 0.08f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        var restPoint = PrimitiveKit.Group("RestPoint", r, bedPos + new Vector3(0f, 0.75f, -0.1f), 180f);
        var sleep = new GameObject("BedZone");
        sleep.transform.SetParent(r, false);
        sleep.transform.localPosition = bedPos + new Vector3(bedW * 0.5f + 0.6f, 1f, 0f);
        var sleepBox = sleep.AddComponent<BoxCollider>();
        sleepBox.isTrigger = true;
        sleepBox.size = new Vector3(1.2f, 2f, 2.4f);
        var bed = sleep.AddComponent<SleepSpot>();
        bed.Configure(title, "นอนพักในห้อง", restPoint.transform);
        bed.ConfigurePaid("hotel", 0); // free while the booking lasts (life.lua checks it)

        // Nightstand with a lamp, TV
        PrimitiveKit.Box("Nightstand", r, bedPos + new Vector3(bedW * 0.5f + 0.45f, 0.3f, 0.9f), new Vector3(0.5f, 0.6f, 0.5f), wood);
        PrimitiveKit.Sphere("BedLamp", r, bedPos + new Vector3(bedW * 0.5f + 0.45f, 0.8f, 0.9f), Vector3.one * 0.3f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        PrimitiveKit.Box("TVStand", r, new Vector3(w * 0.5f - 0.5f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 2f), wood);
        PrimitiveKit.Box("TV", r, new Vector3(w * 0.5f - 0.35f, 1.35f, 0f), new Vector3(0.08f, 0.8f, 1.4f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);

        if (deluxe)
        {
            var sofa = PrimitiveKit.Mat("SofaFabric", suite ? new Color(0.55f, 0.15f, 0.2f) : new Color(0.35f, 0.5f, 0.6f));
            PrimitiveKit.Box("Sofa", r, new Vector3(1.5f, 0.3f, -1.5f), new Vector3(2.4f, 0.6f, 0.9f), sofa);
            PrimitiveKit.Box("SofaBack", r, new Vector3(1.5f, 0.7f, -1.9f), new Vector3(2.4f, 0.6f, 0.2f), sofa, collider: false);
            PrimitiveKit.Box("Rug", r, new Vector3(1.5f, 0.11f, 0f), new Vector3(3.6f, 0.02f, 2.6f), PrimitiveKit.Mat("RugRed", new Color(0.7f, 0.25f, 0.2f)), collider: false);
            PrimitiveKit.Box("CoffeeTable", r, new Vector3(1.5f, 0.25f, -0.2f), new Vector3(1.2f, 0.08f, 0.7f), wood);
            Nature.Bush(r, new Vector3(w * 0.5f - 0.8f, 0.1f, d * 0.5f - 0.8f), 0.6f, 9, flowers: true);
        }
        if (suite)
        {
            PrimitiveKit.Box("Bathtub", r, new Vector3(w * 0.5f - 1.6f, 0.35f, -d * 0.5f + 1.3f), new Vector3(1.8f, 0.7f, 0.9f), white);
            PrimitiveKit.Box("BathWater", r, new Vector3(w * 0.5f - 1.6f, 0.62f, -d * 0.5f + 1.3f), new Vector3(1.6f, 0.04f, 0.7f), PrimitiveKit.Mat("CanalWater", new Color(0.25f, 0.45f, 0.5f)), collider: false);
            PrimitiveKit.Box("Minibar", r, new Vector3(-w * 0.5f + 0.5f, 0.5f, -1f), new Vector3(0.6f, 1f, 1.4f), wood);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                PrimitiveKit.Sphere("Chandelier", r, new Vector3(1.5f + Mathf.Cos(a) * 0.5f, 2.3f, Mathf.Sin(a) * 0.5f), Vector3.one * 0.18f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
            }
        }

        // Warm room light (always on: rooms are lit for guests)
        var lightGo = new GameObject("RoomLight");
        lightGo.transform.SetParent(r, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = w;
        l.intensity = 3f;
        l.color = new Color(1f, 0.88f, 0.7f);
        l.shadows = LightShadows.None;

        // Arrive just inside the door; the door takes you back to the lobby
        var entry = PrimitiveKit.Group("Entry", r, new Vector3(-w * 0.5f + 1.6f, 0f, -d * 0.5f + 1.4f), 0f);
        var exit = new GameObject("DoorZone");
        exit.transform.SetParent(r, false);
        exit.transform.localPosition = new Vector3(-w * 0.5f + 1.6f, 1f, -d * 0.5f + 0.5f);
        var exitBox = exit.AddComponent<BoxCollider>();
        exitBox.isTrigger = true;
        exitBox.size = new Vector3(1.6f, 2f, 0.8f);
        exit.AddComponent<HotelRoomExit>();
        r.gameObject.AddComponent<HotelRoom>().Configure(tier, entry.transform);
    }

    // Open-front restaurant: roof on posts, kitchen counter at the back, tables and a string of lights
    private static void OpenRestaurant(Transform root, Vector3 pos, string name, Color roofColor, bool seafood, float width = 8f)
    {
        var r = PrimitiveKit.Group("Restaurant_" + name, root, pos, 180f).transform;
        var roof = PrimitiveKit.Mat("RestaurantRoof" + name.Length, roofColor);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        float hw = width * 0.5f;
        PrimitiveKit.Box("Floor", r, new Vector3(0f, 0.1f, 0f), new Vector3(width, 0.2f, 7f), PrimitiveKit.Mat("FloorTile", new Color(0.86f, 0.8f, 0.7f)));
        PrimitiveKit.Box("BackWall", r, new Vector3(0f, 1.6f, 3.4f), new Vector3(width, 3.2f, 0.2f), PrimitiveKit.Mat("RestaurantWall", new Color(0.96f, 0.92f, 0.82f)));
        foreach (float x in new[] { -hw + 0.2f, hw - 0.2f })
            foreach (float z in new[] { -3.3f, 3.2f })
                PrimitiveKit.Cylinder("Post", r, new Vector3(x, 1.6f, z), new Vector3(0.2f, 1.6f, 0.2f), wood);
        PrimitiveKit.Box("Roof", r, new Vector3(0f, 3.45f, 0f), new Vector3(width + 0.8f, 0.15f, 8f), roof, new Vector3(-6f, 0f, 0f), collider: false);
        PrimitiveKit.Box("SignBoard", r, new Vector3(0f, 3.95f, -3.7f), new Vector3(Mathf.Min(width, 1.6f + name.Length * 0.28f), 0.8f, 0.1f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label(name, r, new Vector3(0f, 3.95f, -3.77f), 0.06f, new Color(0.6f, 0.1f, 0.1f));
        Entrance.MenuBoard(r, new Vector3(-hw + 1f, 0f, -4.3f), name, roofColor);

        PrimitiveKit.Box("Kitchen", r, new Vector3(0f, 0.6f, 2.6f), new Vector3(width - 1.5f, 1f, 0.8f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)));
        if (seafood)
        {
            // Ice display out front with the day's catch
            PrimitiveKit.Box("IceDisplay", r, new Vector3(hw - 1.2f, 0.5f, -3f), new Vector3(1.8f, 0.8f, 0.9f), PrimitiveKit.Mat("IceWhite", new Color(0.92f, 0.96f, 1f)));
            Color[] catchColors = { new Color(0.95f, 0.45f, 0.3f), new Color(0.6f, 0.65f, 0.7f), new Color(0.9f, 0.6f, 0.5f) };
            for (int i = 0; i < 5; i++)
                MeshKit.Place("Catch", r, MeshKit.Blob("Seafood", 30 + i, 0.25f, 1), PrimitiveKit.Mat($"Seafood{i % 3}", catchColors[i % 3]),
                    new Vector3(hw - 1.9f + i * 0.35f, 0.95f, -3f), default, new Vector3(0.18f, 0.08f, 0.28f));
        }
        var stool = PrimitiveKit.Mat("PlasticRed", new Color(0.9f, 0.15f, 0.15f));
        var table = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
        int cols = Mathf.Max(1, Mathf.FloorToInt(width / 3.5f));
        for (int c = 0; c < cols; c++)
            foreach (float z in new[] { -1.4f, 0.6f })
            {
                float x = (c - (cols - 1) * 0.5f) * 3.3f - (seafood ? 0.8f : 0f);
                PrimitiveKit.Box("Table", r, new Vector3(x, 0.6f, z), new Vector3(1.2f, 0.06f, 0.8f), table, collider: false);
                PrimitiveKit.Box("TableLeg", r, new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.4f, 0.1f), table);
                foreach (float dx in new[] { -0.85f, 0.85f })
                    PrimitiveKit.Cylinder("Stool", r, new Vector3(x + dx, 0.42f, z), new Vector3(0.36f, 0.22f, 0.36f), stool, collider: false);
            }
        FairLights.Festoon(r, new Vector3(-hw, 3.1f, -3.6f), new Vector3(hw, 3.1f, -3.6f), 0.3f, 0.7f);
    }

    // ── Green mountains behind the town and headlands closing the bay (หาดป่าตอง) ──────────
    private static void Mountains(Transform root)
    {
        var grass = PrimitiveKit.Mat("HillGrass", new Color(0.36f, 0.58f, 0.26f));
        PrimitiveKit.Box("HillFoot", root, new Vector3(0f, 0.005f, -95f), new Vector3(280f, 0.01f, 90f), grass); // ground beyond the town
        foreach (float x in new[] { -95f, 95f })
            PrimitiveKit.Box("HillFootSide", root, new Vector3(x, 0.005f, 0f), new Vector3(90f, 0.01f, 100f), grass);

        var forest = PrimitiveKit.Mat("MountainForest", new Color(0.16f, 0.4f, 0.17f));
        var forestLight = PrimitiveKit.Mat("MountainForestLight", new Color(0.26f, 0.5f, 0.2f));
        var farRidge = PrimitiveKit.Mat("MountainFar", new Color(0.22f, 0.42f, 0.32f));

        // Near hills right behind the town, then a taller far ridge
        for (int i = 0; i < 8; i++)
        {
            float x = -105f + i * 30f;
            Hill(root, $"Hill{i}", 200 + i, new Vector3(x, -8f, -72f - (i % 2) * 8f), new Vector3(42f + (i % 3) * 8f, 48f + (i % 4) * 8f, 34f), i % 2 == 0 ? forest : forestLight);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = -120f + i * 48f;
            Hill(root, $"Ridge{i}", 300 + i, new Vector3(x, -12f, -118f), new Vector3(66f, 100f + (i % 3) * 16f, 40f), farRidge);
        }
        // Headlands at both ends of the bay, running out into the sea
        Hill(root, "HeadlandWest", 401, new Vector3(-90f, -8f, 12f), new Vector3(56f, 56f, 80f), forest);
        Hill(root, "HeadlandWestTip", 402, new Vector3(-74f, -5f, 56f), new Vector3(30f, 28f, 34f), forestLight);
        Hill(root, "HeadlandEast", 403, new Vector3(92f, -8f, 8f), new Vector3(56f, 64f, 84f), forest);
        Hill(root, "HeadlandEastTip", 404, new Vector3(76f, -5f, 54f), new Vector3(28f, 24f, 32f), forestLight);
        foreach (var p in new[] { new Vector3(-58f, 0f, 30f), new Vector3(-55f, 0f, 40f), new Vector3(57f, 0f, 34f), new Vector3(60f, 0f, 44f) })
            Nature.Rock(root, p, 3f, (int)p.z);
    }

    private static void Hill(Transform root, string name, int seed, Vector3 pos, Vector3 size, Material mat)
    {
        var hill = MeshKit.Place("Mountain_" + name, root, MeshKit.Blob("Mountain" + seed, seed, 0.22f, 2), mat, pos, new Vector3(0f, seed * 37f % 360f, 0f), size);
        hill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // The light show (ConcertLights animates it): moving heads with visible beams on the front truss,
    // uplights along the stage lip, a fan of lasers from behind the band and a strobe
    internal static void ConcertRig(Transform show)
    {
        var housing = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        var beamDown = MeshKit.Lathe("LightBeamCone", new[] { new Vector2(0.12f, 0f), new Vector2(2.6f, 14f) }, 20, capTop: false, capBottom: false);
        var beamUp = MeshKit.Lathe("UplightCone", new[] { new Vector2(0.1f, 0f), new Vector2(1.4f, 9f) }, 16, capTop: false, capBottom: false);
        var beamMat = PrimitiveKit.Beam("StageBeam", new Color(0.5f, 0.5f, 0.5f, 1f));
        for (int i = 0; i < 6; i++)
        {
            var head = PrimitiveKit.Group("MovingHead", show, new Vector3(-6.25f + i * 2.5f, 7.75f, -2.5f)).transform;
            PrimitiveKit.Box("Yoke", head, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.12f, 0.3f), housing, collider: false);
            PrimitiveKit.Cylinder("Head", head, new Vector3(0f, -0.1f, 0f), new Vector3(0.38f, 0.22f, 0.38f), housing, collider: false);
            PrimitiveKit.Cylinder("Lens", head, new Vector3(0f, -0.33f, 0f), new Vector3(0.3f, 0.02f, 0.3f), PrimitiveKit.Glow("StageLens", Color.white, 6f, 0.5f), collider: false);
            var beam = MeshKit.Place("Beam", head, beamDown, beamMat, new Vector3(0f, -0.35f, 0f), new Vector3(180f, 0f, 0f));
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lg = new GameObject("HeadLight");
            lg.transform.SetParent(head, false);
            lg.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            lg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // points down the beam
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 22f;
            l.innerSpotAngle = 10f;
            l.range = 22f;
            l.intensity = 30f;
            l.shadows = LightShadows.None;
        }
        for (int i = 0; i < 6; i++)
        {
            var up = PrimitiveKit.Group("Uplight", show, new Vector3(-6.5f + i * 2.6f, 1.25f, -2.9f)).transform;
            PrimitiveKit.Cylinder("Can", up, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.12f, 0.3f), housing, collider: false);
            var beam = MeshKit.Place("Beam", up, beamUp, beamMat, new Vector3(0f, 0.25f, 0f), new Vector3(-8f, 0f, 0f));
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // Lasers: thin rays fanning out over the crowd from behind the drummer
        var laserMat = PrimitiveKit.Beam("LaserBeam", new Color(0.2f, 1f, 0.3f, 1f));
        for (int i = 0; i < 8; i++)
        {
            var pivot = PrimitiveKit.Group("LaserPivot", show, new Vector3(0f, 6.8f, 3.6f), -42f + i * 12f).transform;
            var ray = PrimitiveKit.Box("Ray", pivot, new Vector3(0f, 0f, -20f), new Vector3(0.05f, 0.05f, 40f), laserMat, collider: false);
            ray.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var strobe = new GameObject("Strobe");
        strobe.transform.SetParent(show, false);
        strobe.transform.localPosition = new Vector3(0f, 8.5f, -3f);
        var s = strobe.AddComponent<Light>();
        s.type = LightType.Point;
        s.range = 25f;
        s.intensity = 0f;
        s.color = Color.white;
        s.shadows = LightShadows.None;
        show.gameObject.AddComponent<ConcertLights>();
    }

    private static void Blocker(Transform root, Vector3 pos, Vector3 size)
    {
        var b = new GameObject("SeaBlocker");
        b.transform.SetParent(root, false);
        b.transform.localPosition = pos;
        b.AddComponent<BoxCollider>().size = size;
    }

    private static void Umbrella(Transform root, Vector3 pos, Color color, int index)
    {
        var u = PrimitiveKit.Group("BeachUmbrella_ร่มชายหาด", root, pos).transform;
        PrimitiveKit.Cylinder("Pole", u, new Vector3(0f, 1.25f, 0f), new Vector3(0.07f, 1.25f, 0.07f), PrimitiveKit.Mat("RopeWhite", Color.white), collider: false);
        MeshKit.Place("Canopy", u, MeshKit.Lathe("UmbrellaCanopy", new[] { new Vector2(1.6f, 0f), new Vector2(1.2f, 0.3f), new Vector2(0.1f, 0.65f), new Vector2(0f, 0.7f) }, 8),
            PrimitiveKit.Mat($"Umbrella{index % 5}", color), new Vector3(0f, 2.3f, 0f));
        var chairMat = PrimitiveKit.Mat("DeckChair", new Color(0.97f, 0.95f, 0.9f));
        foreach (float x in new[] { -0.8f, 0.8f })
        {
            PrimitiveKit.Box("DeckChair", u, new Vector3(x, 0.28f, 0.2f), new Vector3(0.65f, 0.12f, 1.8f), chairMat);
            PrimitiveKit.Box("Backrest", u, new Vector3(x, 0.55f, -0.65f), new Vector3(0.65f, 0.12f, 0.7f), chairMat, new Vector3(-45f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("Towel", u, new Vector3(-0.8f, 0.35f, 0.3f), new Vector3(0.55f, 0.02f, 1.3f), PrimitiveKit.Mat($"Umbrella{(index + 2) % 5}", color), collider: false);

        var bed = PrimitiveKit.Group("RestPoint", u, new Vector3(0.8f, 0.58f, 0.3f), 90f);
        var zone = new GameObject("RestZone");
        zone.transform.SetParent(u, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 1.8f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 2f, 1.4f);
        var rest = zone.AddComponent<SleepSpot>();
        rest.Configure("เก้าอี้ชายหาดใต้ร่ม", "เช่าเก้าอี้นอนพัก", bed.transform);
        rest.ConfigurePaid("beach_chair", 40); // same price as life.lua PAID_SPOTS
    }

    // Wooden fishing pier (ท่าเรือประมง) out into the sea, fishing spots at the end
    private static void Pier(Transform root, Vector3 start)
    {
        var p = PrimitiveKit.Group("FishingPier_ท่าเรือประมง", root, start).transform;
        var wood = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var plank = PrimitiveKit.Mat("WoodPlank", Wood);
        PrimitiveKit.Box("Deck", p, new Vector3(0f, 0.15f, 9.5f), new Vector3(3.2f, 0.3f, 19f), plank);
        for (int i = 0; i <= 6; i++)
            foreach (float x in new[] { -1.5f, 1.5f })
                PrimitiveKit.Cylinder("Post", p, new Vector3(x, 0.2f, i * 3.1f + 0.5f), new Vector3(0.22f, 0.6f, 0.22f), wood, collider: false);
        foreach (float x in new[] { -1.55f, 1.55f })
            PrimitiveKit.Box("Rail", p, new Vector3(x, 0.95f, 9.5f), new Vector3(0.08f, 0.08f, 19f), wood, collider: false);

        foreach (float x in new[] { -0.8f, 0.8f })
        {
            var spot = PrimitiveKit.Group("FishingSpot_ตกปลาทะเล", p, new Vector3(x, 0.3f, 17.6f));
            var bobber = PrimitiveKit.Group("BobberPoint", spot.transform, new Vector3(x * 2f, -0.25f, 5f));
            var zone = new GameObject("FishingZone");
            zone.transform.SetParent(spot.transform, false);
            zone.transform.localPosition = new Vector3(0f, 1f, 0f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.5f, 2f, 2f);
            zone.AddComponent<FishingSpot>().Configure(bobber.transform, "sea");
        }
        Places.Sign(p, new Vector3(2.4f, 0f, 0.5f), "ท่าเรือประมง\nตกปลาทะเลที่ปลายท่า", 0f);
        StreetFurniture.LampPost(p, new Vector3(1.2f, 0.3f, 18.3f));
    }

    // Thai longtail boat (เรือหางยาว): a deep wooden hull whose bow sweeps up high, gunwales, floorboards,
    // bench seats, a canopy, flower garlands and ribbons on the bow, and the car engine on a pivot at the
    // stern driving a long propeller shaft. Bow is +Z. People "sit" by standing low inside the hull.
    internal static Transform LongtailBoat(Transform root, Vector3 pos, float yaw, Color ribbon)
    {
        var b = PrimitiveKit.Group("LongtailBoat_เรือหางยาว", root, pos, yaw).transform;
        var wood = PrimitiveKit.Mat("BoatHullWood", new Color(0.52f, 0.31f, 0.15f));
        wood.SetFloat("_Cull", 0f); // both faces: the inside of the hull is visible from above
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var plank = PrimitiveKit.Mat("WoodPlank", Wood);
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        MeshKit.Place("Hull", b, LongtailHull(), wood, Vector3.zero);

        // Gunwale rails along the top edge, and a painted band just under them
        foreach (float side in new[] { -1f, 1f })
        {
            var rail = new Vector3[16];
            for (int i = 0; i < rail.Length; i++)
            {
                float t = i / (rail.Length - 1f) * 0.97f;
                rail[i] = new Vector3(side * HullBeam(t), HullSheer(t), HullZ(t));
            }
            MeshKit.Place("Gunwale", b, MeshKit.Sweep(side < 0 ? "Boats/GunwaleL" : "Boats/GunwaleR", rail, MeshKit.Taper(rail.Length, 0.05f, 0.03f), 6), dark, Vector3.zero);
        }
        // Painted bands along the hull sides (white and the boat's colour) and a transom board
        foreach (float side in new[] { -1f, 1f })
            for (int band = 0; band < 2; band++)
            {
                var path = new Vector3[18];
                for (int i = 0; i < path.Length; i++)
                {
                    float t = Mathf.Lerp(0.03f, 0.9f, i / (path.Length - 1f));
                    float y = HullSheer(t) - (band == 0 ? 0.09f : 0.17f);
                    path[i] = new Vector3(side * (HullHalfWidthAt(t, y) / 0.98f + 0.012f), y, HullZ(t));
                }
                MeshKit.Place("PaintBand", b, MeshKit.Sweep($"Boats/Band{(side < 0 ? "L" : "R")}{band}", path, MeshKit.Taper(path.Length, 0.032f, 0.022f), 5),
                    band == 0 ? PrimitiveKit.Mat("BoatPaintWhite", new Color(0.96f, 0.95f, 0.9f)) : PrimitiveKit.Mat($"Ribbon0", ribbon), Vector3.zero);
            }
        PrimitiveKit.Box("Transom", b, new Vector3(0f, (HullSheer(0f) + HullKeel(0f)) * 0.5f + 0.05f, HullZ(0f) - 0.02f), new Vector3(HullBeam(0f) * 1.9f, HullSheer(0f) - HullKeel(0f) - 0.1f, 0.06f), dark, collider: false);

        // Solid plank deck above the waterline (hides the sea inside the hull), with plank seams
        // Solid deck of darker boards, wider than the hull at that height so no water shows at the edges
        var deckWood = PrimitiveKit.Mat("BoatDeck", new Color(0.5f, 0.32f, 0.17f));
        deckWood.SetFloat("_Cull", 0f);
        MeshKit.Place("Deck", b, LongtailDeck(DeckY), deckWood, Vector3.zero);
        for (float z = -3.6f; z <= 3.0f; z += 0.45f)
            PrimitiveKit.Box("DeckSeam", b, new Vector3(0f, DeckY + 0.005f, z), new Vector3(HullHalfWidthAt(HullT(z), DeckY) * 2f, 0.01f, 0.03f), dark, collider: false);
        foreach (float z in new[] { -2.9f, -0.9f, 0.6f, 2.0f })
        {
            float t = HullT(z);
            PrimitiveKit.Box("Bench", b, new Vector3(0f, Mathf.Lerp(HullKeel(t), HullSheer(t), 0.62f), z), new Vector3(HullBeam(t) * 1.85f, 0.06f, 0.32f), plank, collider: false);
        }
        // Canopy over the passengers
        var tarp = PrimitiveKit.Mat($"Canopy{(int)(ribbon.r * 10)}", Color.Lerp(ribbon, Color.white, 0.45f));
        foreach (float z in new[] { -1.2f, 2.3f })
            foreach (float side in new[] { -1f, 1f })
            {
                float t = HullT(z);
                PrimitiveKit.Cylinder("CanopyPole", b, new Vector3(side * HullBeam(t) * 0.8f, (HullSheer(t) + 2.25f) * 0.5f, z), new Vector3(0.05f, (2.25f - HullSheer(t)) * 0.5f, 0.05f), dark, collider: false);
            }
        PrimitiveKit.Box("Canopy", b, new Vector3(0f, 2.28f, 0.55f), new Vector3(1.55f, 0.05f, 3.9f), tarp, collider: false);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("CanopyEdge", b, new Vector3(side * 0.78f, 2.2f, 0.55f), new Vector3(0.04f, 0.16f, 3.9f), PrimitiveKit.Mat($"Ribbon0", ribbon), collider: false);

        // Bow: flower garland (พวงมาลัย) and three-colour ribbons (ผ้าสามสี) on the raised prow
        float tip = HullSheer(0.985f), tipZ = HullZ(0.985f);
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            Color c = i % 3 == 0 ? new Color(1f, 0.85f, 0.2f) : i % 3 == 1 ? Color.white : new Color(0.95f, 0.3f, 0.45f);
            PrimitiveKit.Sphere("Garland", b, new Vector3(Mathf.Cos(a) * 0.14f, tip - 0.35f + Mathf.Sin(a) * 0.08f, tipZ - 0.12f + Mathf.Sin(a) * 0.1f), Vector3.one * 0.1f, PrimitiveKit.Mat($"Garland{i % 3}", c));
        }
        // Scarves wrapped round the bow post and streaming down (ผ้าแพรพันหัวเรือ)
        Color[] scarves = { ribbon, new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.65f, 0.35f), new Color(0.9f, 0.3f, 0.6f) };
        for (int i = 0; i < scarves.Length; i++)
        {
            var mat = PrimitiveKit.Mat($"Scarf{i}", scarves[i]);
            PrimitiveKit.Cylinder("ScarfWrap", b, new Vector3(0f, tip - 0.08f - i * 0.09f, tipZ - 0.04f), new Vector3(0.13f, 0.045f, 0.13f), mat, collider: false);
            PrimitiveKit.Box("ScarfTail", b, new Vector3((i - 1.5f) * 0.07f, tip - 0.5f - i * 0.05f, tipZ + 0.06f), new Vector3(0.07f, 0.65f, 0.015f), mat, new Vector3(-12f, 0f, (i - 1.5f) * 6f), collider: false);
        }
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Ribbon", b, new Vector3(0f, tip - 0.1f - i * 0.1f, tipZ + 0.02f), new Vector3(0.32f, 0.09f, 0.04f),
                PrimitiveKit.Mat($"Ribbon{i}", i == 0 ? ribbon : i == 1 ? Color.white : new Color(1f, 0.85f, 0.2f)), new Vector3(0f, 0f, 18f * (i - 1)), collider: false);

        // Stern: engine on its pivot post, tiller, long shaft angled into the water, propeller
        float sternT = 0.02f, sternZ = HullZ(sternT), sternY = HullSheer(sternT);
        PrimitiveKit.Cylinder("EnginePost", b, new Vector3(0f, sternY + 0.12f, sternZ + 0.15f), new Vector3(0.12f, 0.15f, 0.12f), dark, collider: false);
        PrimitiveKit.Box("Engine", b, new Vector3(0f, sternY + 0.48f, sternZ - 0.05f), new Vector3(0.5f, 0.42f, 0.75f), PrimitiveKit.Mat("EngineRed", new Color(0.75f, 0.18f, 0.15f)), collider: false);
        PrimitiveKit.Box("EngineHead", b, new Vector3(0f, sternY + 0.76f, sternZ - 0.05f), new Vector3(0.34f, 0.16f, 0.5f), steel, collider: false);
        // Tiller (คันบังคับ) from the engine forward to the boatman's hand; his seat right by the engine
        var tillerFrom = new Vector3(0f, sternY + 0.55f, sternZ + 0.1f);
        var tillerTo = new Vector3(0.26f, 0.66f, BoatmanZ + 0.38f);
        var tiller = PrimitiveKit.Cylinder("Tiller", b, (tillerFrom + tillerTo) * 0.5f, new Vector3(0.045f, Vector3.Distance(tillerFrom, tillerTo) * 0.5f, 0.045f), steel, default, collider: false);
        tiller.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tillerTo - tillerFrom);
        PrimitiveKit.Cylinder("TillerGrip", b, tillerTo, new Vector3(0.07f, 0.09f, 0.07f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), new Vector3(90f, 0f, 0f), collider: false);
        float bt = HullT(BoatmanZ);
        PrimitiveKit.Box("BoatmanSeat", b, new Vector3(0f, Mathf.Lerp(HullKeel(bt), HullSheer(bt), 0.62f), BoatmanZ), new Vector3(HullBeam(bt) * 1.7f, 0.06f, 0.3f), plank, collider: false);
        var shaftFrom = new Vector3(0f, sternY + 0.35f, sternZ - 0.35f);
        var shaftTo = new Vector3(0f, -0.35f, sternZ - 3.6f);
        var mid = (shaftFrom + shaftTo) * 0.5f;
        float len = Vector3.Distance(shaftFrom, shaftTo);
        var shaft = PrimitiveKit.Cylinder("LongTail", b, mid, new Vector3(0.06f, len * 0.5f, 0.06f), steel, default, collider: false);
        shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, shaftFrom - shaftTo);
        foreach (float a in new[] { 0f, 120f, 240f })
            PrimitiveKit.Box("Propeller", b, shaftTo, new Vector3(0.05f, 0.34f, 0.02f), steel, new Vector3(0f, 0f, a), collider: false);

        b.gameObject.AddComponent<Bob>();
        return b;
    }

    private const float DeckY = 0.07f;      // just above the sea surface (0.025)
    private const float BoatmanZ = -3.58f;  // the boatman sits right in front of the engine

    // Half-width of the hull at height y in section t (inverse of the section curve below)
    private static float HullHalfWidthAt(float t, float y)
    {
        float keel = Mathf.Min(HullKeel(t), HullSheer(t) - 0.05f), sheer = HullSheer(t);
        float r = Mathf.Clamp01((y - keel) / (sheer - keel));
        float a = Mathf.Acos(Mathf.Clamp(1f - Mathf.Pow(r, 1.25f), -1f, 1f));
        return HullBeam(t) * Mathf.Sin(a) * 0.98f;
    }

    // Flat deck that fills the hull at height y, from the stern to where the bow rises above it
    private static Mesh LongtailDeck(float y)
    {
        var v = new List<Vector3>();
        var tri = new List<int>();
        const int n = 40;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            if (HullKeel(t) >= y) break;
            float w = HullHalfWidthAt(t, y) / 0.98f + 0.02f; // reaches into the hull wall: no gap
            v.Add(new Vector3(-w, y, HullZ(t)));
            v.Add(new Vector3(w, y, HullZ(t)));
        }
        for (int k = 0; k + 3 < v.Count; k += 2) tri.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
        var mesh = new Mesh { name = "LongtailDeck" };
        mesh.SetVertices(v);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateNormals();
        return MeshKit.SaveCombined("Boats/LongtailDeck", mesh);
    }

    // Hull shape along its length (t = 0 stern .. 1 bow)
    private const float HullStern = -4.3f, HullLength = 9.4f;
    private static float HullZ(float t) => HullStern + HullLength * t;
    private static float HullT(float z) => Mathf.Clamp01((z - HullStern) / HullLength);
    private static float HullBeam(float t) =>
        t < 0.35f ? Mathf.Lerp(0.5f, 0.8f, Mathf.Sin(t / 0.35f * Mathf.PI * 0.5f))
                  : 0.8f * Mathf.Pow(Mathf.Max(0f, Mathf.Cos((t - 0.35f) / 0.65f * Mathf.PI * 0.5f)), 0.85f) + 0.03f; // Max: cos(90°) is a hair below 0 → NaN
    private static float HullSheer(float t) => 0.6f + (t > 0.7f ? Mathf.Pow((t - 0.7f) / 0.3f, 2.2f) * 1.15f : 0f) + (t < 0.1f ? (0.1f - t) * 0.8f : 0f);
    private static float HullKeel(float t) => -0.22f + (t > 0.72f ? Mathf.Pow((t - 0.72f) / 0.28f, 2f) * 1.3f : 0f) + (t < 0.06f ? (0.06f - t) * 2f : 0f);

    // The hull skin: U-shaped sections lofted from stern to bow, plus the flat transom at the stern
    private static Mesh LongtailHull()
    {
        const int n = 32, m = 12;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, beam = HullBeam(t), sheer = HullSheer(t), keel = Mathf.Min(HullKeel(t), sheer - 0.05f);
            for (int j = 0; j <= m; j++)
            {
                float a = (-1f + 2f * j / m) * Mathf.PI * 0.5f;
                float y = keel + (sheer - keel) * Mathf.Pow(1f - Mathf.Cos(a), 0.8f);
                verts.Add(new Vector3(beam * Mathf.Sin(a), y, HullZ(t)));
            }
        }
        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
            {
                int a = i * (m + 1) + j, b = a + m + 1;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        // Transom: fan from the middle of the first section
        int centre = verts.Count;
        verts.Add(new Vector3(0f, (HullSheer(0f) + HullKeel(0f)) * 0.5f, HullZ(0f)));
        for (int j = 0; j < m; j++) tris.AddRange(new[] { centre, j + 1, j });
        var mesh = new Mesh { name = "LongtailHull" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return MeshKit.SaveCombined("Boats/LongtailHull", mesh);
    }

    // The tour boat moored at the pier (งานพิเศษ: พานักท่องเที่ยวนั่งเรือหางยาวรอบอ่าว)
    private static void TourBoat(Transform root)
    {
        var dock = new Vector3(21.3f, 0f, 26f);
        var boat = PrimitiveKit.Group("TourBoat_เรือพานักท่องเที่ยว", root, dock).transform; // moves along the route
        LongtailBoat(boat, Vector3.zero, 0f, new Color(1f, 0.45f, 0.75f));                     // bobs inside it
        // Boatman at the stern by the tiller; tourists on the benches. Set low so they look seated.
        var seat = PrimitiveKit.Group("Seat", boat, new Vector3(0.05f, -0.55f, BoatmanZ)).transform;
        int look = 1;
        foreach (var p in new[] { new Vector3(-0.28f, -0.6f, -0.9f), new Vector3(0.3f, -0.6f, 0.6f), new Vector3(-0.2f, -0.55f, 2.0f) })
        {
            var tourist = PrimitiveKit.Group("Tourist", boat, p);
            tourist.AddComponent<MarketVendor>().Configure(look++);
            tourist.AddComponent<SeatedPose>();
        }

        var zone = new GameObject("BoatTourZone");
        zone.transform.SetParent(root, false);
        zone.transform.localPosition = new Vector3(23.9f, 1.3f, 26f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.4f, 2f, 3f);
        var route = new[]
        {
            dock, new Vector3(18f, 0f, 40f), new Vector3(0f, 0f, 47f), new Vector3(-25f, 0f, 47f), new Vector3(-40f, 0f, 41f),
            new Vector3(-38f, 0f, 36f), new Vector3(-10f, 0f, 37.5f), new Vector3(10f, 0f, 38f), new Vector3(19f, 0f, 32f), dock,
        };
        zone.AddComponent<BoatTour>().Configure(boat, seat, route);
        Places.Sign(root, new Vector3(23.6f, 0.3f, 22.5f), "เรือหางยาวพาเที่ยว\nงานพิเศษ: ขับเรือรอบอ่าว", 90f);
    }

    private static void FishingBoat(Transform root, Vector3 pos)
    {
        var b = PrimitiveKit.Group("FishingBoat_เรือประมง", root, pos, 0f).transform;
        PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.5f, 0f), new Vector3(2.6f, 1f, 8f), PrimitiveKit.Mat("BoatBlue", new Color(0.2f, 0.45f, 0.8f)), collider: false);
        PrimitiveKit.Box("Stripe", b, new Vector3(0f, 0.85f, 0f), new Vector3(2.62f, 0.2f, 8.02f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), collider: false);
        PrimitiveKit.Box("Cabin", b, new Vector3(0f, 1.6f, -1.8f), new Vector3(1.8f, 1.3f, 2.2f), PrimitiveKit.Mat("ChurchWhite", new Color(0.96f, 0.95f, 0.92f)), collider: false);
        PrimitiveKit.Cylinder("Mast", b, new Vector3(0f, 3f, 1.5f), new Vector3(0.1f, 2.2f, 0.1f), PrimitiveKit.Mat("WoodPlank", Wood), collider: false);
        PrimitiveKit.Box("NetBoom", b, new Vector3(0f, 4.4f, 2.6f), new Vector3(0.08f, 0.08f, 2.4f), PrimitiveKit.Mat("WoodPlank", Wood), new Vector3(-30f, 0f, 0f), collider: false);
        MeshKit.Place("Net", b, MeshKit.Blob("FishingNet", 7, 0.3f, 1), PrimitiveKit.Mat("FishingNet", new Color(0.55f, 0.6f, 0.45f)), new Vector3(0f, 1.2f, 2.8f), default, new Vector3(1.6f, 0.5f, 1.4f));
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Sphere("Lamp", b, new Vector3(0f, 2.6f, -2f + i * 1.4f), Vector3.one * 0.18f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        b.gameObject.AddComponent<Bob>();
    }

    private static void SeafoodShack(Transform root, Vector3 pos)
    {
        var s = PrimitiveKit.Group("SeafoodShack_ร้านอาหารทะเล", root, pos).transform;
        var bamboo = PrimitiveKit.Mat("BambooPole", new Color(0.72f, 0.62f, 0.38f));
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.78f, 0.66f, 0.38f));
        foreach (float x in new[] { -2.4f, 2.4f })
            foreach (float z in new[] { -1.8f, 1.8f })
                PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.4f, z), new Vector3(0.14f, 1.4f, 0.14f), bamboo);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Roof", s, new Vector3(0f, 3.2f, side * 1.25f), new Vector3(5.6f, 0.12f, 2.9f), thatch, new Vector3(side * 28f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Counter", s, new Vector3(0f, 0.55f, -1.4f), new Vector3(4.2f, 1.1f, 0.6f), PrimitiveKit.Mat("WoodPlank", Wood));
        PrimitiveKit.Box("Grill", s, new Vector3(-1.2f, 1.2f, -1.4f), new Vector3(1.2f, 0.15f, 0.5f), PrimitiveKit.Glow("GrillCoals", new Color(1f, 0.45f, 0.15f), 2f), collider: false);
        for (int i = 0; i < 4; i++)
            MeshKit.Place("Coconut", s, MeshKit.Blob("Coconut", 21, 0.15f, 1), PrimitiveKit.Mat("Coconut", new Color(0.38f, 0.45f, 0.15f)), new Vector3(0.6f + i * 0.35f, 1.25f, -1.4f), default, Vector3.one * 0.3f);
        PrimitiveKit.Box("SignBoard", s, new Vector3(0f, 2.6f, -1.95f), new Vector3(3.6f, 0.6f, 0.08f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label("ร้านอาหารทะเล", s, new Vector3(0f, 2.6f, -2.01f), 0.055f, new Color(0.6f, 0.1f, 0.1f));
    }

    // ── Cow farm (ฟาร์มวัว) in the village ─────────────────────────────────────────────
    public static void CowFarm(Transform parent, Vector3 position)
    {
        var f = PrimitiveKit.Group("CowFarm_ฟาร์มวัว", parent, position).transform;
        var fence = PrimitiveKit.Mat("FenceWood", new Color(0.62f, 0.45f, 0.28f));
        const float hx = 9f, hz = 6.5f;
        for (float x = -hx; x <= hx + 0.01f; x += 3f)
            foreach (float z in new[] { -hz, hz })
                PrimitiveKit.Box("FencePost", f, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), fence);
        for (float z = -hz; z <= hz + 0.01f; z += 3.25f)
            foreach (float x in new[] { -hx, hx })
                PrimitiveKit.Box("FencePost", f, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), fence);
        foreach (float y in new[] { 0.5f, 0.95f })
        {
            PrimitiveKit.Box("Rail", f, new Vector3(0f, y, hz), new Vector3(hx * 2f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailL", f, new Vector3(-hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailR", f, new Vector3(hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailFrontL", f, new Vector3(-5.5f, y, -hz), new Vector3(7f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailFrontR", f, new Vector3(5.5f, y, -hz), new Vector3(7f, 0.1f, 0.08f), fence);
        }
        // Open-sided cow shed with hay
        var roof = PrimitiveKit.Mat("RoofRed", new Color(0.7f, 0.22f, 0.15f));
        foreach (float x in new[] { -3f, 3f })
            foreach (float z in new[] { 2.5f, 5.5f })
                PrimitiveKit.Cylinder("ShedPost", f, new Vector3(x, 1.4f, z), new Vector3(0.18f, 1.4f, 0.18f), fence);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("ShedRoof", f, new Vector3(0f, 3.15f, 4f + side * 0.85f), new Vector3(7f, 0.12f, 2f), roof, new Vector3(side * 25f, 0f, 0f), collider: false);
        // Hay stacked together beside the trough, where the cows eat (กองฟาง)
        var hay = PrimitiveKit.Mat("Hay", new Color(0.9f, 0.78f, 0.38f));
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("HayBale", f, new Vector3(-2.4f + i * 1.02f, 0.3f, 4.9f), new Vector3(1f, 0.6f, 0.7f), hay);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("HayBale", f, new Vector3(-1.9f + i * 1.02f, 0.9f, 4.9f), new Vector3(1f, 0.6f, 0.7f), hay);
        PrimitiveKit.Box("HayBale", f, new Vector3(-0.88f, 1.5f, 4.9f), new Vector3(1f, 0.6f, 0.7f), hay);
        MeshKit.Place("LooseHay", f, MeshKit.Blob("HayPile", 55, 0.3f, 1), hay, new Vector3(1.2f, 0.1f, 3.9f), default, new Vector3(1.6f, 0.5f, 1.2f));
        PrimitiveKit.Box("Trough", f, new Vector3(2.6f, 0.3f, 3.9f), new Vector3(2f, 0.5f, 0.7f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)));
        PrimitiveKit.Box("TroughWater", f, new Vector3(2.6f, 0.52f, 3.9f), new Vector3(1.8f, 0.04f, 0.5f), PrimitiveKit.Mat("CanalWater", new Color(0.25f, 0.45f, 0.5f)), collider: false);

        // Orange bulbs under the shed and on two poles at the front of the pen
        foreach (float x in new[] { -1.8f, 1.8f })
            StreetFurniture.OrangeLight(f, new Vector3(x, 2.75f, 4f));
        foreach (float x in new[] { -hx + 0.4f, hx - 0.4f })
        {
            PrimitiveKit.Cylinder("LightPole", f, new Vector3(x, 1.6f, -hz + 0.4f), new Vector3(0.12f, 1.6f, 0.12f), fence);
            StreetFurniture.OrangeLight(f, new Vector3(x, 3.1f, -hz + 0.7f));
        }

        // Room for eight cows; CowPen shows as many as you own (ranch.lua)
        var spots = new[]
        {
            new Vector2(-6f, -3.5f), new Vector2(-3f, -1f), new Vector2(0f, -3.5f), new Vector2(3f, -1f),
            new Vector2(6f, -3.5f), new Vector2(-6f, 1.5f), new Vector2(6f, 1.5f), new Vector2(-1.5f, 1.2f),
        };
        for (int i = 0; i < spots.Length; i++)
        {
            var c = spots[i];
            Cow(f, new Vector3(c.x, 0f, c.y), i, new Vector2(hx - 1.5f - Mathf.Abs(c.x), hz - 1.5f - Mathf.Abs(c.y)));
        }
        f.gameObject.AddComponent<CowPen>();

        // In the pen by the trough: milk the cows (รีดนม) and check feed / growth
        var penZone = new GameObject("CowPenZone");
        penZone.transform.SetParent(f, false);
        penZone.transform.localPosition = new Vector3(2.6f, 1f, 2.6f);
        var penBox = penZone.AddComponent<BoxCollider>();
        penBox.isTrigger = true;
        penBox.size = new Vector3(2.8f, 2f, 1.6f);
        penZone.AddComponent<RanchCounter>().Configure("pen");
        PrimitiveKit.Cylinder("MilkCan", f, new Vector3(3.9f, 0.35f, 3.2f), new Vector3(0.35f, 0.35f, 0.35f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        Places.Sign(f, new Vector3(-1.8f, 0f, -hz - 0.6f), "ฟาร์มวัว", 0f);

        // Cow seller (พ่อค้าวัว) at the gate: buy more cows here
        var seller = PrimitiveKit.Group("CowSeller_พ่อค้าวัว", f, new Vector3(4.5f, 0f, -hz - 2.2f), 180f);
        seller.AddComponent<MarketVendor>().Configure(3);
        var zone = new GameObject("CowSellerZone");
        zone.transform.SetParent(f, false);
        zone.transform.localPosition = new Vector3(4.5f, 1f, -hz - 3.2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 2f, 2f);
        zone.AddComponent<RanchCounter>().Configure("seller");
        Places.Sign(f, new Vector3(6.6f, 0f, -hz - 2.4f), "พ่อค้าวัว\nซื้อวัวเข้าคอก", 0f);
    }

    // A cute, rounded dairy cow: capsule body, round head with big eyes, pink muzzle, floppy ears
    private static void Cow(Transform parent, Vector3 pos, int index, Vector2 room)
    {
        var c = PrimitiveKit.Group("Cow_วัว", parent, pos, index * 70f).transform;
        var white = PrimitiveKit.Mat("CowWhite", new Color(0.97f, 0.96f, 0.93f));
        var black = PrimitiveKit.Mat("CowBlack", new Color(0.15f, 0.12f, 0.12f));
        var pink = PrimitiveKit.Mat("CowPink", new Color(0.98f, 0.72f, 0.74f));
        var hoof = PrimitiveKit.Mat("CowHoof", new Color(0.32f, 0.24f, 0.2f));
        var horn = PrimitiveKit.Mat("CowHorn", new Color(0.98f, 0.92f, 0.75f));
        var shine = PrimitiveKit.Mat("EyeShine", Color.white);

        // Body: a capsule lying front to back, a rounder belly under it
        PrimitiveKit.Create(PrimitiveType.Capsule, "Body", c, new Vector3(0f, 0.95f, 0f), new Vector3(0.82f, 0.85f, 0.8f), white, new Vector3(90f, 0f, 0f));
        PrimitiveKit.Sphere("Belly", c, new Vector3(0f, 0.85f, -0.05f), new Vector3(0.78f, 0.62f, 1.05f), white);
        PrimitiveKit.Sphere("Udder", c, new Vector3(0f, 0.55f, -0.38f), new Vector3(0.28f, 0.18f, 0.26f), pink);
        // Soft round patches hugging the sides and back
        PrimitiveKit.Sphere("Spot", c, new Vector3(0.36f, 1.02f, 0.25f), new Vector3(0.12f, 0.4f, 0.5f), black);
        PrimitiveKit.Sphere("Spot", c, new Vector3(-0.36f, 0.92f, -0.3f), new Vector3(0.12f, 0.36f, 0.46f), black);
        PrimitiveKit.Sphere("Spot", c, new Vector3(0.05f, 1.32f, -0.15f), new Vector3(0.4f, 0.1f, 0.42f), black);
        // Short rounded legs with dark hooves
        // Short rounded legs with dark hooves, each hanging from a hip pivot so CowWander can walk them
        // (order: front-left, front-right, back-left, back-right)
        foreach (float z in new[] { 0.52f, -0.52f })
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                var hip = PrimitiveKit.Group("LegPivot", c, new Vector3(x, 0.7f, z)).transform;
                PrimitiveKit.Create(PrimitiveType.Capsule, "Leg", hip, new Vector3(0f, -0.34f, 0f), new Vector3(0.2f, 0.32f, 0.2f), white, default, false);
                PrimitiveKit.Cylinder("Hoof", hip, new Vector3(0f, -0.64f, 0f), new Vector3(0.2f, 0.06f, 0.2f), hoof, collider: false);
            }
        // Tail with a black tuft
        PrimitiveKit.Cylinder("Tail", c, new Vector3(0f, 0.92f, -0.98f), new Vector3(0.05f, 0.28f, 0.05f), white, new Vector3(-20f, 0f, 0f), collider: false);
        PrimitiveKit.Sphere("TailTuft", c, new Vector3(0f, 0.64f, -1.07f), new Vector3(0.1f, 0.14f, 0.1f), black);

        var head = PrimitiveKit.Group("Head", c, new Vector3(0f, 1.25f, 0.85f)).transform;
        PrimitiveKit.Sphere("Skull", head, new Vector3(0f, 0f, 0.18f), new Vector3(0.52f, 0.5f, 0.52f), white);
        PrimitiveKit.Sphere("Muzzle", head, new Vector3(0f, -0.14f, 0.42f), new Vector3(0.44f, 0.3f, 0.3f), pink);
        foreach (float x in new[] { -0.08f, 0.08f })
            PrimitiveKit.Sphere("Nostril", head, new Vector3(x, -0.12f, 0.57f), new Vector3(0.05f, 0.04f, 0.03f), PrimitiveKit.Mat("CowNose", new Color(0.7f, 0.4f, 0.42f)));
        foreach (float x in new[] { -0.15f, 0.15f })
        {
            PrimitiveKit.Sphere("Eye", head, new Vector3(x, 0.07f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), black);
            PrimitiveKit.Sphere("EyeShine", head, new Vector3(x + 0.02f, 0.1f, 0.43f), new Vector3(0.035f, 0.035f, 0.02f), shine);
            PrimitiveKit.Sphere("Ear", head, new Vector3(x * 2.1f, 0.06f, 0.12f), new Vector3(0.26f, 0.08f, 0.15f), x < 0 ? black : white);
            PrimitiveKit.Create(PrimitiveType.Capsule, "Horn", head, new Vector3(x * 0.85f, 0.27f, 0.12f), new Vector3(0.07f, 0.07f, 0.07f), horn, new Vector3(0f, 0f, x < 0 ? 25f : -25f), false);
        }
        PrimitiveKit.Sphere("Fringe", head, new Vector3(0f, 0.2f, 0.3f), new Vector3(0.24f, 0.1f, 0.14f), black);
        c.gameObject.AddComponent<CowWander>().Configure(new Vector2(Mathf.Max(1f, room.x), Mathf.Max(0.8f, room.y)), head);
    }

    // ── City: noodle shop and skytrain ──────────────────────────────────────────────
    public static void NoodleShop(Transform parent, Vector3 position, float yaw)
    {
        var n = PrimitiveKit.Group("NoodleShop_ร้านก๋วยเตี๋ยว", parent, position, yaw).transform;
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        PrimitiveKit.Box("Cart", n, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 1f), PrimitiveKit.Mat("NoodleCart", new Color(0.95f, 0.93f, 0.85f)));
        PrimitiveKit.Box("GlassCase", n, new Vector3(0.4f, 1.45f, 0f), new Vector3(1.4f, 0.7f, 0.9f), PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f)), collider: false);
        foreach (float x in new[] { -0.75f, -0.25f })
            PrimitiveKit.Cylinder("Pot", n, new Vector3(x, 1.3f, 0f), new Vector3(0.42f, 0.2f, 0.42f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("Sign", n, new Vector3(0f, 2.4f, 0f), new Vector3(2.4f, 0.55f, 0.08f), red, collider: false);
        PrimitiveKit.Label("ก๋วยเตี๋ยว", n, new Vector3(0f, 2.4f, -0.06f), 0.06f, Color.white);
        foreach (float x in new[] { -1.2f, 1.2f })
            PrimitiveKit.Cylinder("SignPole", n, new Vector3(x, 1.5f, 0.45f), new Vector3(0.05f, 1.5f, 0.05f), red, collider: false);
        // Red plastic stools and steel tables
        var stool = PrimitiveKit.Mat("PlasticRed", new Color(0.9f, 0.15f, 0.15f));
        foreach (float x in new[] { -1.6f, 1.6f })
        {
            PrimitiveKit.Box("Table", n, new Vector3(x, 0.38f, -2.2f), new Vector3(0.9f, 0.05f, 0.7f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)));
            foreach (float dz in new[] { -0.7f, 0.7f })
                PrimitiveKit.Cylinder("Stool", n, new Vector3(x, 0.22f, -2.2f + dz), new Vector3(0.36f, 0.22f, 0.36f), stool, collider: false);
        }
        MeshKit.Place("Umbrella", n, MeshKit.Lathe("UmbrellaCanopy", new[] { new Vector2(1.6f, 0f), new Vector2(1.2f, 0.3f), new Vector2(0.1f, 0.65f), new Vector2(0f, 0.7f) }, 8),
            PrimitiveKit.Mat("Umbrella0", new Color(0.95f, 0.3f, 0.3f)), new Vector3(0f, 2.5f, -2.2f));
        PrimitiveKit.Cylinder("UmbrellaPole", n, new Vector3(0f, 1.25f, -2.2f), new Vector3(0.06f, 1.25f, 0.06f), PrimitiveKit.Mat("RopeWhite", Color.white), collider: false);
    }

    // Glass lift tower at the east end of the station: street level beside the road up to the platform
    private static void StationLift(Transform line)
    {
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
        var glass = PrimitiveKit.Glow("LiftGlass", new Color(0.6f, 0.8f, 0.95f), 0.3f, 0.6f);
        var lift = PrimitiveKit.Group("Lift_ลิฟต์สถานี", line, new Vector3(13.4f, 0f, -3.8f)).transform;
        // Four corner posts and a roof; glass on the back and sides (open toward the road side and platform)
        foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -0.9f, 0.9f })
                PrimitiveKit.Box("Post", lift, new Vector3(x, 5f, z), new Vector3(0.12f, 10f, 0.12f), steel);
        PrimitiveKit.Box("Roof", lift, new Vector3(0f, 10.05f, 0f), new Vector3(2f, 0.1f, 2f), steel, collider: false);
        PrimitiveKit.Box("GlassBack", lift, new Vector3(0f, 5f, 0.9f), new Vector3(1.7f, 10f, 0.04f), glass, collider: false);
        PrimitiveKit.Box("GlassEast", lift, new Vector3(0.9f, 5f, 0f), new Vector3(0.04f, 10f, 1.7f), glass, collider: false);
        PrimitiveKit.Box("SignBoard", lift, new Vector3(0f, 2.6f, -0.95f), new Vector3(1.6f, 0.4f, 0.05f), PrimitiveKit.Mat("SignGreen", new Color(0.15f, 0.45f, 0.3f)), collider: false);
        PrimitiveKit.Label("ลิฟต์ขึ้นสถานี", lift, new Vector3(0f, 2.6f, -0.98f), 0.035f, Color.white);
        // Walkway from the lift's top landing to the platform end
        PrimitiveKit.Box("Landing", line, new Vector3(12.6f, 8.2f, -3f), new Vector3(1.6f, 0.3f, 2.6f), PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f)));

        var cabin = PrimitiveKit.Group("Cabin", lift, new Vector3(0f, 0.05f, 0f)).transform;
        PrimitiveKit.Box("CabinFloor", cabin, Vector3.zero, new Vector3(1.6f, 0.1f, 1.6f), steel, collider: false);
        PrimitiveKit.Box("CabinRail", cabin, new Vector3(0f, 0.5f, 0.75f), new Vector3(1.6f, 0.06f, 0.06f), steel, collider: false);

        var bottom = Point(lift, "RideBottom", new Vector3(0f, 0.05f, 0f), 0f);
        var top = Point(lift, "RideTop", new Vector3(0f, 8.35f, 0f), 0f);
        var platformExit = Point(line, "PlatformExit", new Vector3(11.2f, 8.4f, -2.6f), -90f);
        var streetExit = Point(lift, "StreetExit", new Vector3(0f, 0.05f, -2.5f), 180f);

        Zone(lift, "LiftDown_Street", new Vector3(0f, 1f, -0.6f), new Vector3(2.2f, 2f, 2.4f))
            .AddComponent<SkyLift>().Configure("ขึ้นลิฟต์\nไปชานชาลา", cabin, bottom, top, platformExit);
        Zone(line, "LiftUp_Platform", new Vector3(12.4f, 9.4f, -3f), new Vector3(1.6f, 2f, 2.4f))
            .AddComponent<SkyLift>().Configure("ลงลิฟต์\nไปถนน", cabin, top, bottom, streetExit);
    }

    private static Transform Point(Transform parent, string name, Vector3 local, float yaw)
    {
        var p = new GameObject(name).transform;
        p.SetParent(parent, false);
        p.localPosition = local;
        p.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return p;
    }

    private static GameObject Zone(Transform parent, string name, Vector3 local, Vector3 size)
    {
        var z = new GameObject(name);
        z.transform.SetParent(parent, false);
        z.transform.localPosition = local;
        var box = z.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        return z;
    }

    // Elevated track over the main road (z = -8) with a station in the middle and a moving train
    public static void SkyTrainLine(Transform root)
    {
        var line = PrimitiveKit.Group("SkyTrain_รถไฟฟ้า", root, new Vector3(0f, 0f, -8f)).transform;
        var concrete = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        for (float x = -48f; x <= 48.1f; x += 16f)
        {
            PrimitiveKit.Box("Pillar", line, new Vector3(x, 3.8f, 0f), new Vector3(1f, 7.6f, 1f), concrete);
            PrimitiveKit.Box("PillarCap", line, new Vector3(x, 7.7f, 0f), new Vector3(1.4f, 0.4f, 3.6f), concrete, collider: false);
        }
        PrimitiveKit.Box("Guideway", line, new Vector3(0f, 8.1f, 0f), new Vector3(110f, 0.5f, 3.4f), concrete, collider: false);
        // Station: platform canopy on both sides of the track (you can walk on the platforms; a rail keeps you off the edge)
        foreach (float z in new[] { -2.6f, 2.6f })
        {
            PrimitiveKit.Box("Platform", line, new Vector3(0f, 8.2f, z), new Vector3(24f, 0.3f, 1.8f), concrete);
            PrimitiveKit.Box("PlatformRail", line, new Vector3(0f, 8.85f, z + Mathf.Sign(z) * 0.88f), new Vector3(24f, 1f, 0.06f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)));
            PrimitiveKit.Box("TrackEdge", line, new Vector3(0f, 8.55f, z - Mathf.Sign(z) * 0.88f), new Vector3(24f, 0.4f, 0.06f), PrimitiveKit.Mat("SafetyYellow", new Color(0.98f, 0.82f, 0.1f)));
        }
        // Open-air station (no roof, so the platform and trains stay in view): a digital name board on two posts
        PrimitiveKit.Box("StationSign", line, new Vector3(0f, 10.2f, -3.6f), new Vector3(5f, 0.7f, 0.1f), PrimitiveKit.Glow("StationScreen", new Color(0.1f, 0.35f, 0.6f), 0.6f, 0.5f), collider: false);
        PrimitiveKit.Label("สถานีรถไฟฟ้าใจกลางเมือง", line, new Vector3(0f, 10.2f, -3.67f), 0.05f, Color.white);
        foreach (float x in new[] { -2.3f, 2.3f })
            PrimitiveKit.Box("StationSignPost", line, new Vector3(x, 9.3f, -3.6f), new Vector3(0.1f, 1.9f, 0.1f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        StationLift(line);

        // Ticket gate on the platform: trains to other provinces (ต่างจังหวัด)
        var gate = new GameObject("TrainGate_ไปต่างจังหวัด");
        gate.transform.SetParent(line, false);
        gate.transform.localPosition = new Vector3(0f, 9.4f, -2.6f);
        var gateZone = gate.AddComponent<BoxCollider>();
        gateZone.isTrigger = true;
        gateZone.size = new Vector3(3f, 2f, 1.8f);
        gate.AddComponent<TravelStop>().Configure("สถานีรถไฟฟ้า · ไปต่างจังหวัด", trainPlatform: true);
        PrimitiveKit.Box("TicketMachine", line, new Vector3(-1.8f, 8.95f, -3.2f), new Vector3(0.7f, 1.2f, 0.4f), PrimitiveKit.Mat("TrainBlue", new Color(0.1f, 0.4f, 0.8f)));
        PrimitiveKit.Label("ตั๋วไปต่างจังหวัด", line, new Vector3(-1.8f, 9.75f, -3.42f), 0.03f, Color.white);

        var train = PrimitiveKit.Group("Train", line, new Vector3(-70f, 9.75f, 0f)).transform;
        var body = PrimitiveKit.Mat("TrainWhite", new Color(0.95f, 0.96f, 0.97f));
        var stripe = PrimitiveKit.Mat("TrainBlue", new Color(0.1f, 0.4f, 0.8f));
        var windows = PrimitiveKit.Glow("TrainWindows", new Color(0.75f, 0.9f, 1f), 1.2f, 0.5f);
        for (int i = 0; i < 3; i++)
        {
            float x = -i * 9.4f;
            PrimitiveKit.Box("Car", train, new Vector3(x, 0f, 0f), new Vector3(9f, 2.6f, 2.8f), body, collider: false);
            PrimitiveKit.Box("Stripe", train, new Vector3(x, -0.6f, 0f), new Vector3(9.02f, 0.35f, 2.82f), stripe, collider: false);
            PrimitiveKit.Box("Windows", train, new Vector3(x, 0.35f, 0f), new Vector3(8.2f, 0.8f, 2.84f), windows, collider: false);
        }
        train.gameObject.AddComponent<SkyTrain>();
    }
}
