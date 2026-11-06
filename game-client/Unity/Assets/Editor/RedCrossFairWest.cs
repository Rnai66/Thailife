using UnityEngine;

// The fair grown out to a proper size, laid out on aisles (RedCrossFair.Build):
//   · the far west wing (x -88..-70): the รถบั๊ม pavilion and the Red Cross tent (สลากกาชาด)
//   · an aisle between it and the coaster (x ≈ -68.5) running down from the main road
//   · a promenade along the back (z ≈ -66) with a street of food stalls each side
//   · a food court under umbrellas south of it
//   · a flower bed with the Red Cross emblem inside the coaster's oval (where the bumper cars were)
public static partial class RedCrossFair
{
    private const float WestX = -88f, SouthZ = -82f, AisleX = -68.5f, PromenadeZ = -66f;

    private static void Expansion(Transform t)
    {
        var ground = PrimitiveKit.Mat("FairGround", new Color(0.42f, 0.36f, 0.33f));
        var path = PrimitiveKit.Mat("FairAisle", new Color(0.62f, 0.56f, 0.5f));
        // The new ground: the far west wing and the whole south band
        PrimitiveKit.Box("FarWest", t, new Vector3((WestX - 68f) * 0.5f, 0.011f, (-22f + SouthZ) * 0.5f), new Vector3(-68f - WestX + 0.5f, 0.02f, -22f - SouthZ), ground, collider: false);
        PrimitiveKit.Box("SouthBand", t, new Vector3((WestX - 22.5f) * 0.5f, 0.011f, (-59f + SouthZ) * 0.5f), new Vector3(-22.5f - WestX, 0.02f, -59f - SouthZ), ground, collider: false);
        // Aisles, a shade lighter so the layout reads from above
        PrimitiveKit.Box("Aisle", t, new Vector3(AisleX, 0.016f, (-22f + PromenadeZ) * 0.5f), new Vector3(2.6f, 0.02f, -22f - PromenadeZ), path, collider: false);
        PrimitiveKit.Box("Promenade", t, new Vector3((WestX - 22.5f) * 0.5f, 0.016f, PromenadeZ), new Vector3(-22.5f - WestX, 0.02f, 4f), path, collider: false);
        Thai(PrimitiveKit.Label("ถนนอาหารงานกาชาด", t, new Vector3(AisleX, 4.3f, PromenadeZ + 2.44f), 0.07f, new Color(0.75f, 0.1f, 0.1f), 180f)); // read coming down the aisle
        PrimitiveKit.Box("FoodStreetBoard", t, new Vector3(AisleX, 4.3f, PromenadeZ + 2.36f), new Vector3(5.2f, 0.8f, 0.1f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        foreach (float x in new[] { AisleX - 2.5f, AisleX + 2.5f })
            PrimitiveKit.Cylinder("BoardPost", t, new Vector3(x, 2.35f, PromenadeZ + 2.4f), new Vector3(0.1f, 2.35f, 0.1f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)));

        BumperCars(t, new Vector3(-78.5f, 0f, -33f));
        RedCrossTent(t, new Vector3(-78.5f, 0f, -51.5f));
        EmblemGarden(t, CoasterCentre);
        FoodStreet(t);
        FoodCourt(t, new Vector3(-69f, 0f, -76.5f));

        // Strings of lights along the promenade and down the aisle
        for (int i = 0; i < 7; i++)
        {
            float x = WestX + 2f + i * 10.5f;
            FairLights.Pole(t, new Vector3(x, 0f, PromenadeZ - 2.2f), 5f);
            FairLights.Pole(t, new Vector3(x, 0f, PromenadeZ + 2.2f), 5f);
            FairLights.Festoon(t, new Vector3(x, 4.8f, PromenadeZ - 2.2f), new Vector3(x, 4.8f, PromenadeZ + 2.2f), 0.5f);
            if (i < 6)
            {
                FairLights.Festoon(t, new Vector3(x, 4.8f, PromenadeZ - 2.2f), new Vector3(x + 10.5f, 4.8f, PromenadeZ - 2.2f), 0.8f);
                FairLights.Festoon(t, new Vector3(x, 4.8f, PromenadeZ + 2.2f), new Vector3(x + 10.5f, 4.8f, PromenadeZ + 2.2f), 0.8f);
                FairLights.Lantern(t, new Vector3(x + 5.25f, 4f, PromenadeZ), i);
            }
        }
        for (int i = 0; i < 4; i++)
        {
            float z = -26f - i * 11f;
            FairLights.Pole(t, new Vector3(AisleX + 1.3f, 0f, z), 5f);
            if (i < 3) FairLights.Festoon(t, new Vector3(AisleX + 1.3f, 4.8f, z), new Vector3(AisleX + 1.3f, 4.8f, z - 11f), 0.7f);
        }

        Npcs.Crowd(t, new Vector3(-55f, 0f, PromenadeZ), new Vector2(60f, 2.4f), 16, "big", 0, 24,
            "หิวแล้ว กินอะไรดี|หมูปิ้งหอมมาก!|ซื้อสายไหมให้หน่อย|ไอติมกะทิเย็นชื่นใจ|เดินจนเมื่อยขา|ไปนั่งกินกันตรงโน้น");
        Npcs.Crowd(t, new Vector3(AisleX, 0f, -44f), new Vector2(2f, 34f), 6, "big", 0, 24,
            "ไปรถบั๊มกัน!|ซื้อสลากกาชาดไหม|ลุ้นรางวัลใหญ่|รถไฟเหาะเสียงดังจัง");
    }

    // ── รถบั๊ม ────────────────────────────────────────────────────────────────────────────────
    // A pavilion of its own: a chequered steel floor inside a black-and-yellow rubber wall, a striped
    // canvas roof on a steel frame with the contact grid sparking under it, a neon sign over the
    // entrance (east, on the aisle) and the ticket kiosk beside it. Eight cars with rubber bumpers,
    // a seat, a wheel and a pole up to the grid.
    private static void BumperCars(Transform parent, Vector3 at)
    {
        var b = PrimitiveKit.Group("BumperCars_รถบั๊ม", parent, at).transform;
        const float HX = 8f, HZ = 5.5f, Grid = 3.6f;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var rubber = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        var yellow = PrimitiveKit.Mat("BumperRail", new Color(0.95f, 0.75f, 0.15f));
        var chrome = PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.85f);

        // Raised floor, chequered in two steel greys
        PrimitiveKit.Box("Plinth", b, new Vector3(0f, 0.1f, 0f), new Vector3(HX * 2f + 1.2f, 0.2f, HZ * 2f + 1.2f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)), collider: false);
        var tileA = PrimitiveKit.Mat("BumperFloor", new Color(0.55f, 0.58f, 0.62f), 0.8f, 0.6f);
        var tileB = PrimitiveKit.Mat("BumperFloorDark", new Color(0.36f, 0.38f, 0.42f), 0.8f, 0.6f);
        for (int ix = 0; ix < 8; ix++)
        for (int iz = 0; iz < 6; iz++)
        {
            float w = HX * 2f / 8f, d = HZ * 2f / 6f;
            PrimitiveKit.Box("Tile", b, new Vector3(-HX + w * (ix + 0.5f), 0.205f, -HZ + d * (iz + 0.5f)), new Vector3(w, 0.01f, d), (ix + iz) % 2 == 0 ? tileA : tileB, collider: false);
        }
        // The rubber wall: black band with a yellow cap, a gap on the east side to walk in
        void Wall(string name, Vector3 c, Vector3 size)
        {
            PrimitiveKit.Box(name, b, c + new Vector3(0f, 0.45f, 0f), size, rubber);
            PrimitiveKit.Box(name + "Cap", b, c + new Vector3(0f, 0.72f, 0f), new Vector3(size.x + 0.04f, 0.12f, size.z + 0.04f), yellow, collider: false);
        }
        Wall("RailN", new Vector3(0f, 0f, HZ), new Vector3(HX * 2f, 0.5f, 0.3f));
        Wall("RailS", new Vector3(0f, 0f, -HZ), new Vector3(HX * 2f, 0.5f, 0.3f));
        Wall("RailW", new Vector3(-HX, 0f, 0f), new Vector3(0.3f, 0.5f, HZ * 2f));
        Wall("RailE1", new Vector3(HX, 0f, -HZ * 0.5f - 0.9f), new Vector3(0.3f, 0.5f, HZ - 1.8f));
        Wall("RailE2", new Vector3(HX, 0f, HZ * 0.5f + 0.9f), new Vector3(0.3f, 0.5f, HZ - 1.8f));

        // Steel frame, a striped canvas roof, the glowing contact grid under it
        foreach (float x in new[] { -HX - 0.4f, 0f, HX + 0.4f })
        foreach (float z in new[] { -HZ - 0.4f, HZ + 0.4f })
            PrimitiveKit.Cylinder("Column", b, new Vector3(x, 2.4f, z), new Vector3(0.22f, 2.4f, 0.22f), steel);
        foreach (float z in new[] { -HZ - 0.4f, HZ + 0.4f })
            PrimitiveKit.Box("Beam", b, new Vector3(0f, 4.8f, z), new Vector3(HX * 2f + 1.2f, 0.3f, 0.3f), steel, collider: false);
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        var white = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.94f, 0.9f));
        const int strips = 10;
        float halfD = HZ + 0.9f, rise = 1.6f;
        float slope = Mathf.Atan2(rise, halfD) * Mathf.Rad2Deg, slab = Mathf.Sqrt(halfD * halfD + rise * rise);
        for (int i = 0; i < strips; i++)
        {
            float w = (HX * 2f + 1.8f) / strips, x = -HX - 0.9f + w * (i + 0.5f);
            var m = i % 2 == 0 ? red : white;
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Canvas", b, new Vector3(x, 4.95f + rise * 0.5f, s * halfD * 0.5f), new Vector3(w, 0.06f, slab), m, new Vector3(s * slope, 0f, 0f), collider: false);
        }
        foreach (float s in new[] { -1f, 1f })
            MeshKit.Place("GableEnd", b, MeshKit.Gable("BumperGable", halfD * 2f, rise, 0.06f), white, new Vector3(s * (HX + 0.9f), 4.95f, 0f), new Vector3(0f, s * -90f, 0f));
        PrimitiveKit.Box("ContactGrid", b, new Vector3(0f, Grid, 0f), new Vector3(HX * 2f, 0.03f, HZ * 2f), PrimitiveKit.Glow("BumperNet", new Color(0.35f, 0.5f, 1f), 1.2f, 0.3f), collider: false);
        for (int i = 1; i < 8; i++)
            PrimitiveKit.Box("GridBar", b, new Vector3(-HX + i * HX * 2f / 8f, Grid + 0.03f, 0f), new Vector3(0.05f, 0.05f, HZ * 2f), steel, collider: false);
        Bulbs(b, new Vector3(-HX - 0.4f, 4.6f, -HZ - 0.55f), new Vector3(HX + 0.4f, 4.6f, -HZ - 0.55f), 22);
        Bulbs(b, new Vector3(-HX - 0.4f, 4.6f, HZ + 0.55f), new Vector3(HX + 0.4f, 4.6f, HZ + 0.55f), 22);

        // Neon sign over the entrance on the east gable, facing the aisle
        var sign = PrimitiveKit.Group("Sign", b, new Vector3(HX + 1.1f, 5.6f, 0f), -90f).transform;
        PrimitiveKit.Box("SignBack", sign, new Vector3(0f, 0f, 0.1f), new Vector3(5.4f, 1.3f, 0.15f), rubber, collider: false);
        PrimitiveKit.Box("NeonFrame", sign, new Vector3(0f, 0.62f, 0f), new Vector3(5.4f, 0.06f, 0.06f), PrimitiveKit.Glow("NeonPink", new Color(1f, 0.3f, 0.75f), 6f), collider: false);
        PrimitiveKit.Box("NeonFrame", sign, new Vector3(0f, -0.62f, 0f), new Vector3(5.4f, 0.06f, 0.06f), PrimitiveKit.Glow("NeonPink", new Color(1f, 0.3f, 0.75f), 6f), collider: false);
        Thai(PrimitiveKit.Label("รถบั๊ม", sign, new Vector3(0f, 0.18f, -0.02f), 0.11f, new Color(1f, 0.85f, 0.2f)));
        PrimitiveKit.Label("BUMPER CARS", sign, new Vector3(0f, -0.35f, -0.02f), 0.045f, new Color(0.45f, 0.9f, 1f)).gameObject.AddComponent<NoTranslate>();

        // The cars
        Color[] paint =
        {
            new Color(0.95f, 0.18f, 0.22f), new Color(0.15f, 0.45f, 0.95f), new Color(1f, 0.8f, 0.15f), new Color(0.2f, 0.78f, 0.4f),
            new Color(0.8f, 0.35f, 0.9f), new Color(1f, 0.5f, 0.15f), new Color(0.1f, 0.8f, 0.85f), new Color(0.98f, 0.45f, 0.65f),
        };
        var cars = new Transform[paint.Length];
        Transform seat = null;
        var seatMat = PrimitiveKit.Mat("SeatBlack", new Color(0.12f, 0.12f, 0.14f), 0.4f);
        var lamp = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);
        for (int i = 0; i < cars.Length; i++)
        {
            var car = PrimitiveKit.Group("BumperCar", b, new Vector3(-5.4f + (i % 4) * 3.6f, 0.21f, i < 4 ? -2.2f : 2.2f)).transform;
            var mat = PrimitiveKit.Mat($"BumperCar{i}", paint[i], 0.85f, 0.3f);
            PrimitiveKit.Cylinder("Bumper", car, new Vector3(0f, 0.16f, 0f), new Vector3(1.55f, 0.13f, 1.95f), rubber, collider: false); // the fat rubber ring
            PrimitiveKit.Cylinder("Tub", car, new Vector3(0f, 0.36f, 0f), new Vector3(1.38f, 0.1f, 1.78f), mat, collider: false);
            PrimitiveKit.Sphere("Shell", car, new Vector3(0f, 0.48f, 0.08f), new Vector3(1.3f, 0.5f, 1.65f), mat, collider: false);           // rounded body
            PrimitiveKit.Sphere("Hood", car, new Vector3(0f, 0.62f, 0.5f), new Vector3(1.05f, 0.42f, 0.7f), mat, collider: false);             // the nose rising in front
            PrimitiveKit.Box("Trim", car, new Vector3(0f, 0.46f, 0f), new Vector3(1.32f, 0.04f, 1.62f), chrome, collider: false);
            PrimitiveKit.Box("Seat", car, new Vector3(0f, 0.72f, -0.25f), new Vector3(0.75f, 0.14f, 0.55f), seatMat, collider: false);
            PrimitiveKit.Box("SeatBack", car, new Vector3(0f, 1.05f, -0.55f), new Vector3(0.8f, 0.6f, 0.14f), seatMat, new Vector3(-10f, 0f, 0f), collider: false);
            PrimitiveKit.Cylinder("Column", car, new Vector3(0f, 0.95f, 0.38f), new Vector3(0.05f, 0.2f, 0.05f), chrome, new Vector3(-35f, 0f, 0f), collider: false);
            PrimitiveKit.Cylinder("Wheel", car, new Vector3(0f, 1.12f, 0.26f), new Vector3(0.36f, 0.025f, 0.36f), rubber, new Vector3(-55f, 0f, 0f), collider: false);
            foreach (float x in new[] { -0.32f, 0.32f })
                PrimitiveKit.Sphere("Headlight", car, new Vector3(x, 0.62f, 0.86f), Vector3.one * 0.15f, lamp, collider: false);
            PrimitiveKit.Box("Number", car, new Vector3(0f, 0.55f, -0.84f), new Vector3(0.36f, 0.26f, 0.02f), white, collider: false);
            PrimitiveKit.Label((i + 1).ToString(), car, new Vector3(0f, 0.55f, -0.855f), 0.03f, new Color(0.1f, 0.1f, 0.12f));
            // The pole up to the grid with its sparking shoe
            PrimitiveKit.Cylinder("Pole", car, new Vector3(0f, (Grid - 0.21f + 0.75f) * 0.5f, -0.78f), new Vector3(0.04f, (Grid - 0.21f - 0.75f) * 0.5f, 0.04f), steel, collider: false);
            PrimitiveKit.Box("Flag", car, new Vector3(0.12f, 2f, -0.78f), new Vector3(0.22f, 0.15f, 0.01f), mat, collider: false);
            var spark = PrimitiveKit.Sphere("Spark", car, new Vector3(0f, Grid - 0.25f, -0.78f), Vector3.one * 0.16f, PrimitiveKit.Glow("BumperSpark", new Color(0.7f, 0.85f, 1f), 12f, 0.5f));
            spark.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (i == 0) seat = PrimitiveKit.Group("Seat", car, new Vector3(0f, 0.3f, -0.2f)).transform;
            cars[i] = car;
        }
        b.gameObject.AddComponent<global::BumperCars>().Configure(cars, new Vector2(HX - 0.15f, HZ - 0.15f));

        // Ticket kiosk at the entrance, on the aisle side
        var k = PrimitiveKit.Group("BumperKiosk", b, new Vector3(HX + 1.4f, 0f, -2.6f), -90f).transform;
        PrimitiveKit.Box("Booth", k, new Vector3(0f, 1.1f, 0.4f), new Vector3(1.5f, 2.2f, 1.1f), yellow);
        PrimitiveKit.Box("Window", k, new Vector3(0f, 1.35f, -0.16f), new Vector3(0.9f, 0.6f, 0.02f), PrimitiveKit.Glow("KioskWindow", new Color(1f, 0.9f, 0.6f), 1.5f), collider: false);
        PrimitiveKit.Box("Awning", k, new Vector3(0f, 2.35f, 0f), new Vector3(1.8f, 0.08f, 1.6f), red, new Vector3(-12f, 0f, 0f), collider: false);
        Thai(PrimitiveKit.Label("คูปอง 2 ใบ", k, new Vector3(0f, 1.85f, -0.17f), 0.035f, new Color(0.6f, 0.08f, 0.08f)));
        Zone(b, new Vector3(HX + 1.2f, 1f, 0f), new Vector3(2.4f, 2f, 3.4f), "bumper", "รถบั๊ม", seat, 22f);
        Npcs.Crowd(b, new Vector3(HX + 1.6f, 0f, 3.6f), new Vector2(1.6f, 2.5f), 4, "big", 0, 24, "รอคิวรถบั๊ม|ชนเลย ชนเลย!|คันสีแดงเก่งสุด|ขอขับอีกรอบ");
    }

    // ── ซุ้มกาชาด: the Red Cross tent selling สลากกาชาด, with prize cars on show ──────────────
    private static void RedCrossTent(Transform parent, Vector3 at)
    {
        var r = PrimitiveKit.Group("RedCrossTent_ซุ้มสลากกาชาด", parent, at, -90f).transform; // front faces the aisle (east)
        var white = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.94f, 0.9f));
        var red = PrimitiveKit.Mat("RedCross", new Color(0.85f, 0.08f, 0.1f));
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        foreach (float x in new[] { -5f, 0f, 5f })
        foreach (float z in new[] { -3f, 3f })
            PrimitiveKit.Cylinder("Post", r, new Vector3(x, 1.5f, z), new Vector3(0.1f, 1.5f, 0.1f), steel);
        PrimitiveKit.Box("Roof", r, new Vector3(0f, 3.1f, 0f), new Vector3(10.6f, 0.15f, 6.6f), white, collider: false);
        foreach (float z in new[] { -1.65f, 1.65f })
            PrimitiveKit.Box("RoofPeak", r, new Vector3(0f, 3.55f, z), new Vector3(10.6f, 0.08f, 3.5f), white, new Vector3(z < 0 ? -15f : 15f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Valance", r, new Vector3(0f, 2.85f, -3.32f), new Vector3(10.6f, 0.4f, 0.05f), red, collider: false);
        PrimitiveKit.Box("BackWall", r, new Vector3(0f, 1.5f, 3.2f), new Vector3(10f, 3f, 0.08f), white);
        // The emblem: a red cross on a white board above the front
        PrimitiveKit.Box("EmblemBoard", r, new Vector3(0f, 4.6f, -3.1f), new Vector3(3.6f, 1.4f, 0.12f), white, collider: false);
        PrimitiveKit.Box("CrossH", r, new Vector3(-1.2f, 4.6f, -3.18f), new Vector3(0.9f, 0.3f, 0.03f), red, collider: false);
        PrimitiveKit.Box("CrossV", r, new Vector3(-1.2f, 4.6f, -3.18f), new Vector3(0.3f, 0.9f, 0.03f), red, collider: false);
        Thai(PrimitiveKit.Label("สลากกาชาด", r, new Vector3(0.45f, 4.6f, -3.2f), 0.055f, new Color(0.75f, 0.08f, 0.1f)));
        // Ticket counters and the girls selling
        for (int i = 0; i < 3; i++)
        {
            float x = -3.4f + i * 3.4f;
            PrimitiveKit.Box("Counter", r, new Vector3(x, 0.5f, -1.8f), new Vector3(2.6f, 1f, 0.7f), white);
            PrimitiveKit.Box("CounterBand", r, new Vector3(x, 0.8f, -2.16f), new Vector3(2.6f, 0.18f, 0.02f), red, collider: false);
            for (int k = 0; k < 6; k++)
                PrimitiveKit.Box("Ticket", r, new Vector3(x - 0.9f + k * 0.36f, 1.02f, -1.85f), new Vector3(0.28f, 0.02f, 0.18f),
                    PrimitiveKit.Mat("TicketPink", new Color(1f, 0.75f, 0.8f)), new Vector3(0f, k * 9f - 20f, 0f), collider: false);
        }
        // Prizes on show: a car on a turntable and a fridge, a motorbike
        var prize = PrimitiveKit.Group("PrizeCar", r, new Vector3(-2.6f, 0f, 1.4f), 90f).transform;
        PrimitiveKit.Cylinder("Turntable", prize, new Vector3(0f, 0.1f, 0f), new Vector3(3.6f, 0.1f, 3.6f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        var carPaint = PrimitiveKit.Mat("PrizeCarWhite", new Color(0.96f, 0.96f, 0.98f), 0.9f, 0.5f);
        PrimitiveKit.Box("Body", prize, new Vector3(0f, 0.65f, 0f), new Vector3(1.6f, 0.6f, 3.6f), carPaint);
        PrimitiveKit.Box("Cabin", prize, new Vector3(0f, 1.15f, -0.2f), new Vector3(1.45f, 0.5f, 1.9f), PrimitiveKit.Mat("WindowGlassBlue", new Color(0.45f, 0.6f, 0.7f)), collider: false);
        foreach (float x in new[] { -0.8f, 0.8f })
        foreach (float z in new[] { -1.15f, 1.15f })
            PrimitiveKit.Cylinder("Wheel", prize, new Vector3(x, 0.45f, z), new Vector3(0.6f, 0.1f, 0.6f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Bow", prize, new Vector3(0f, 1.45f, -0.2f), new Vector3(0.5f, 0.25f, 0.5f), red, collider: false);
        Thai(PrimitiveKit.Label("รางวัลที่ 1", r, new Vector3(-2.6f, 2.35f, 1.4f), 0.04f, new Color(0.75f, 0.08f, 0.1f)));
        PrimitiveKit.Box("Fridge", r, new Vector3(2.2f, 0.9f, 2.4f), new Vector3(0.8f, 1.8f, 0.7f), PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.85f));
        PrimitiveKit.Box("Tv", r, new Vector3(3.6f, 1.2f, 2.9f), new Vector3(1.4f, 0.85f, 0.08f), PrimitiveKit.Glow("TvScreen", new Color(0.3f, 0.55f, 0.95f), 1.5f));
        Bulbs(r, new Vector3(-5.2f, 2.7f, -3.35f), new Vector3(5.2f, 2.7f, -3.35f), 18);
        Npcs.Crowd(r, new Vector3(0f, 0f, -3.2f), new Vector2(8f, 1.6f), 6, "big", 0, 24,
            "ขอสลากกาชาดสองใบ|ลุ้นรถยนต์!|ปีที่แล้วได้พัดลม|ช่วยกาชาดได้บุญด้วย|ถูกรางวัลแน่ปีนี้");
    }

    // ── Inside the coaster's oval: a raised flower bed with the Red Cross emblem, lit at night ──
    private static void EmblemGarden(Transform parent, Vector3 at)
    {
        var g = PrimitiveKit.Group("EmblemGarden_สวนกาชาด", parent, at).transform;
        var white = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var red = PrimitiveKit.Mat("RedCross", new Color(0.85f, 0.08f, 0.1f));
        PrimitiveKit.Cylinder("Kerb", g, new Vector3(0f, 0.2f, 0f), new Vector3(8f, 0.2f, 10.5f), white);
        PrimitiveKit.Cylinder("Soil", g, new Vector3(0f, 0.25f, 0f), new Vector3(7.4f, 0.2f, 9.9f), PrimitiveKit.Mat("Soil", new Color(0.3f, 0.22f, 0.15f)), collider: false);
        // The cross in red flowers on a white bed
        PrimitiveKit.Box("Bed", g, new Vector3(0f, 0.42f, 0f), new Vector3(4.2f, 0.1f, 4.2f), white, collider: false);
        PrimitiveKit.Box("CrossH", g, new Vector3(0f, 0.5f, 0f), new Vector3(3.4f, 0.14f, 1.1f), red, collider: false);
        PrimitiveKit.Box("CrossV", g, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 0.14f, 3.4f), red, collider: false);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Nature.Bush(g, new Vector3(Mathf.Cos(a) * 3.6f, 0.3f, Mathf.Sin(a) * 5.2f), 0.7f, 140 + i, flowers: true);
        }
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Sphere("Uplight", g, new Vector3(s * 2.6f, 0.5f, 0f), Vector3.one * 0.25f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f), collider: false);
    }

    // ── ถนนอาหาร: stalls both sides of the promenade ─────────────────────────────────────────
    private static readonly string[] Foods =
    {
        "หมูปิ้ง", "ลูกชิ้นปิ้ง", "ไก่ทอด", "หอยทอด", "ส้มตำ", "ข้าวเหนียวมะม่วง", "ไอติมกะทิ",
        "โรตี", "น้ำแข็งใส", "ขนมครก", "ผัดไทย", "ชานมไข่มุก", "ข้าวโพดคั่ว", "สายไหม",
    };

    private static void FoodStreet(Transform t)
    {
        int n = 0;
        // North side (behind the coaster and the shows, clear of the concert stage), facing south
        for (float x = WestX + 3f; x <= -50f; x += 4.4f)
            if (Mathf.Abs(x - AisleX) > 3f) FoodStall(t, new Vector3(x, 0f, PromenadeZ + 4.4f), 180f, n++);
        // South side all the way along, facing north
        for (float x = WestX + 3f; x <= -25f; x += 4.4f)
            if (Mathf.Abs(x - AisleX) > 3f) FoodStall(t, new Vector3(x, 0f, PromenadeZ - 4.4f), 0f, n++);
    }

    private static void FoodStall(Transform parent, Vector3 at, float yaw, int index)
    {
        // yaw 0: the counter faces +Z (north, the promenade); yaw 180 faces south
        var s = PrimitiveKit.Group("FoodStall_ร้านอาหาร", parent, at, yaw).transform;
        Color[] canopy = { new Color(0.85f, 0.2f, 0.18f), new Color(0.2f, 0.45f, 0.85f), new Color(0.25f, 0.62f, 0.35f), new Color(0.95f, 0.6f, 0.15f), new Color(0.7f, 0.3f, 0.75f) };
        var stripeA = PrimitiveKit.Mat($"StallCanopy{index % canopy.Length}", canopy[index % canopy.Length]);
        var stripeB = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.94f, 0.9f));
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        PrimitiveKit.Box("Counter", s, new Vector3(0f, 0.5f, 0.3f), new Vector3(2.6f, 1f, 0.9f), stripeB);
        PrimitiveKit.Box("CounterFront", s, new Vector3(0f, 0.5f, 0.76f), new Vector3(2.6f, 0.9f, 0.02f), stripeA, collider: false);
        foreach (float x in new[] { -1.3f, 1.3f })
        foreach (float z in new[] { -0.6f, 0.75f })
            PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.15f, z), new Vector3(0.05f, 1.15f, 0.05f), steel, collider: false);
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Box("Canopy", s, new Vector3(-1.3f + 0.4333f * (i + 0.5f) * 1f, 2.35f, 0.1f), new Vector3(0.4333f, 0.05f, 1.9f), i % 2 == 0 ? stripeA : stripeB, new Vector3(-8f, 0f, 0f), collider: false);
        PrimitiveKit.Box("SignBoard", s, new Vector3(0f, 2.75f, 0.95f), new Vector3(2.4f, 0.5f, 0.06f), stripeB, collider: false);
        Thai(PrimitiveKit.Label(Foods[index % Foods.Length], s, new Vector3(0f, 2.75f, 0.99f), 0.04f, new Color(0.6f, 0.1f, 0.1f), 180f));
        // Food on the counter, a grill or a pot, a bulb under the canopy
        var food = PrimitiveKit.Mat($"Goods3_{index % 3}", new[] { new Color(0.75f, 0.45f, 0.2f), new Color(0.9f, 0.7f, 0.3f), new Color(0.55f, 0.3f, 0.15f) }[index % 3]);
        var blob = MeshKit.Blob("Produce", 41, 0.2f, 1);
        for (int i = 0; i < 6; i++)
            MeshKit.Place("Food", s, blob, food, new Vector3(-0.9f + i * 0.36f, 1.06f, 0.45f), new Vector3(0f, i * 50f, 0f), new Vector3(0.3f, 0.16f, 0.22f));
        if (index % 2 == 0)
        {
            PrimitiveKit.Box("Grill", s, new Vector3(0.6f, 1.08f, 0.1f), new Vector3(0.9f, 0.12f, 0.45f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
            PrimitiveKit.Box("Embers", s, new Vector3(0.6f, 1.15f, 0.1f), new Vector3(0.8f, 0.02f, 0.38f), PrimitiveKit.Glow("IncenseEmber", new Color(1f, 0.35f, 0.1f), 3f), collider: false);
        }
        else PrimitiveKit.Cylinder("Pot", s, new Vector3(0.6f, 1.2f, 0.1f), new Vector3(0.5f, 0.2f, 0.5f), PrimitiveKit.Mat("Chrome", new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.85f), collider: false);
        PrimitiveKit.Sphere("Bulb", s, new Vector3(0f, 2.2f, 0.4f), Vector3.one * 0.14f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f), collider: false);
    }

    // ── ลานนั่งกิน: tables under umbrellas south of the food street ──────────────────────────
    private static void FoodCourt(Transform parent, Vector3 at)
    {
        var c = PrimitiveKit.Group("FoodCourt_ลานนั่งกิน", parent, at).transform;
        var table = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.94f, 0.9f));
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        Color[] brolly = { new Color(0.85f, 0.2f, 0.18f), new Color(0.2f, 0.45f, 0.85f), new Color(0.95f, 0.6f, 0.15f), new Color(0.25f, 0.62f, 0.35f) };
        var stool = PrimitiveKit.Mat("StoolRed", new Color(0.85f, 0.15f, 0.15f));
        var top = MeshKit.Lathe("Umbrella", new[] { new Vector2(1.5f, 0f), new Vector2(0.05f, 0.45f) }, 8, capTop: false, capBottom: false);
        int k = 0;
        for (int ix = 0; ix < 6; ix++)
        for (int iz = 0; iz < 2; iz++)
        {
            var p = new Vector3(-15f + ix * 6f, 0f, -1.6f + iz * 3.6f);
            PrimitiveKit.Cylinder("Table", c, p + new Vector3(0f, 0.72f, 0f), new Vector3(1.2f, 0.03f, 1.2f), table);
            PrimitiveKit.Cylinder("Leg", c, p + new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.36f, 0.08f), steel, collider: false);
            PrimitiveKit.Cylinder("Pole", c, p + new Vector3(0f, 1.3f, 0f), new Vector3(0.04f, 1.3f, 0.04f), steel, collider: false);
            MeshKit.Place("Umbrella", c, top, PrimitiveKit.Mat($"Umbrella{k % 4}", brolly[k % 4]), p + new Vector3(0f, 2.2f, 0f));
            for (int s = 0; s < 4; s++)
            {
                float a = (s * 90f + 45f) * Mathf.Deg2Rad;
                PrimitiveKit.Cylinder("Stool", c, p + new Vector3(Mathf.Cos(a) * 0.95f, 0.22f, Mathf.Sin(a) * 0.95f), new Vector3(0.36f, 0.22f, 0.36f), stool, collider: false);
            }
            k++;
        }
        Npcs.Crowd(c, Vector3.zero, new Vector2(32f, 5f), 10, "big", 0, 24, "อิ่มแล้ว|อร่อยมาก|นั่งพักก่อน|เดี๋ยวไปดูคอนเสิร์ตต่อ|ลมเย็นสบาย");
    }
}
