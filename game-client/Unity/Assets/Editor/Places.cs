using UnityEditor;
using UnityEngine;

// Builders for Thai places made from primitives. Each builds under a group whose front faces local -Z.
// Used by VillageBuilder (หมู่บ้าน) and TambonBuilder (ตำบล).
public static class Places
{
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);
    private static readonly Color DarkWood = new Color(0.36f, 0.22f, 0.12f);
    private static readonly Color TempleWhite = new Color(0.95f, 0.94f, 0.9f);
    private static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);
    private static readonly Color TempleOrange = new Color(0.9f, 0.45f, 0.12f);
    private static readonly Color TempleGreen = new Color(0.15f, 0.45f, 0.3f);

    // ── Temple (วัด) ────────────────────────────────────────────────────────────
    public static GameObject Temple(Transform parent, Vector3 position, string name)
    {
        var temple = PrimitiveKit.Group("Temple_วัด", parent, position);
        var t = temple.transform;
        var white = PrimitiveKit.Mat("TempleWhite", TempleWhite, 0.35f);
        var gold = PrimitiveKit.Mat("Gold", Gold, 0.7f, 0.85f);
        var orange = PrimitiveKit.Mat("TempleOrange", TempleOrange, 0.6f);
        var green = PrimitiveKit.Mat("TempleGreen", TempleGreen, 0.6f);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);

        Ubosot(t, new Vector3(0f, 0f, 3f), white, gold, orange, green);
        Chedi(t, new Vector3(9f, 0f, 9f), white, gold);

        // Open pavilion (ศาลา)
        var sala = PrimitiveKit.Group("Sala_ศาลา", t, new Vector3(-9f, 0f, -3f));
        var s = sala.transform;
        PrimitiveKit.Box("Floor", s, new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 4f), wood);
        foreach (float x in new[] { -2.7f, 0f, 2.7f })
        foreach (float z in new[] { -1.7f, 1.7f })
            PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.75f, z), new Vector3(0.18f, 1.25f, 0.18f), wood);
        PrimitiveKit.Box("RoofFront", s, new Vector3(0f, 3.5f, -1.05f), new Vector3(6.8f, 0.12f, 2.8f), orange, new Vector3(-35f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", s, new Vector3(0f, 3.5f, 1.05f), new Vector3(6.8f, 0.12f, 2.8f), orange, new Vector3(35f, 0f, 0f));

        BigTree(t, new Vector3(-9f, 0f, 9f), "BodhiTree_ต้นโพธิ์", 1.4f);

        // Temple fair (งานวัด) outside the walls: contest + music stage and stalls in front of
        // the temple either side of the gate path, the boxing ring along the west wall
        TempleFair(t);
        BoxingRing(t, new Vector3(-22.5f, 0f, 2f), 0f, "เวทีมวยงานวัด", "fair");
        var dirt = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        PrimitiveKit.Box("FairPath", t, new Vector3(-7.5f, 0.012f, -25.8f), new Vector3(40f, 0.02f, 2f), dirt, collider: false);
        PrimitiveKit.Box("FairPath", t, new Vector3(-22.5f, 0.012f, -14f), new Vector3(2f, 0.02f, 22f), dirt, collider: false);
        StreetFurniture.LampPost(t, new Vector3(-19.5f, 0f, -12f));
        TempleFairLights(t);
        // สอยดาว and มหาลัย ก.ไก่ pens in front of the temple, right of the gate path, gates facing the
        // road (festival days only)
        var games = FairLights.Gated(t, "TempleFairGames", "any");
        FestivalGrounds.KokaiPen(PrimitiveKit.Group("KokaiSpot", games, new Vector3(10.5f, 0f, -22f), 90f).transform, Vector3.zero, "any");
        FestivalGrounds.SoiDaoPen(PrimitiveKit.Group("SoiDaoSpot", games, new Vector3(20.8f, 0f, -22f), 90f).transform, Vector3.zero, "any");
        Npcs.Crowd(t, new Vector3(-10f, 0f, -20.5f), new Vector2(30f, 5f), 10, "any", 0, 24, Npcs.FairTalk); // left of the pens

        // Low white boundary wall with a gate in the front (-Z)
        PrimitiveKit.Box("WallBack", t, new Vector3(0f, 0.5f, 14f), new Vector3(30f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallL", t, new Vector3(-15f, 0.5f, 0f), new Vector3(0.3f, 1f, 28f), white);
        PrimitiveKit.Box("WallR", t, new Vector3(15f, 0.5f, 0f), new Vector3(0.3f, 1f, 28f), white);
        PrimitiveKit.Box("WallFrontL", t, new Vector3(-8.5f, 0.5f, -14f), new Vector3(13f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallFrontR", t, new Vector3(8.5f, 0.5f, -14f), new Vector3(13f, 1f, 0.3f), white);
        Gate(t, new Vector3(0f, 0f, -14f), name, white, gold, new Color(0.6f, 0.1f, 0.1f));

        return temple;
    }

    // ประดับไฟงานวัด: zig-zag bulb strings over the fair in front of the wall, bunting along the
    // wall, lanterns on the gate and a ring of lights around the boxing ring. Up only during festivals.
    private static void TempleFairLights(Transform t)
    {
        var deco = FairLights.Gated(t, "TempleFairLights", "any");
        float[] xs = { -26f, -16f, 6f, 16f };
        const float wallSide = -16.8f, roadSide = -27.2f, h = 4.4f;
        foreach (float x in xs)
        {
            FairLights.Pole(deco, new Vector3(x, 0f, wallSide));
            FairLights.Pole(deco, new Vector3(x, 0f, roadSide));
        }
        for (int i = 0; i < xs.Length - 1; i++)
        {
            FairLights.Festoon(deco, new Vector3(xs[i], h, wallSide), new Vector3(xs[i + 1], h, roadSide));
            FairLights.Festoon(deco, new Vector3(xs[i], h, roadSide), new Vector3(xs[i + 1], h, wallSide));
            if (i != 1) FairLights.Festoon(deco, new Vector3(xs[i], h, roadSide), new Vector3(xs[i + 1], h, roadSide), 0.35f);
        }
        for (int i = 0; i < xs.Length; i++)
            FairLights.Lantern(deco, new Vector3(xs[i] + 0.2f, h - 0.2f, roadSide), i);

        // Flags along the front wall, lanterns hanging from the gate beam
        FairLights.Bunting(deco, new Vector3(-15f, 1.4f, -14.35f), new Vector3(-2.8f, 1.4f, -14.35f));
        FairLights.Bunting(deco, new Vector3(2.8f, 1.4f, -14.35f), new Vector3(15f, 1.4f, -14.35f));
        FairLights.Lantern(deco, new Vector3(-1.4f, 3.1f, -14.3f), 0);
        FairLights.Lantern(deco, new Vector3(1.4f, 3.1f, -14.3f), 0);

        // Around the boxing ring by the west wall
        var corners = new[] { new Vector3(-27.3f, 0f, -4f), new Vector3(-17.7f, 0f, -4f), new Vector3(-17.7f, 0f, 8f), new Vector3(-27.3f, 0f, 8f) };
        foreach (var c in corners) FairLights.Pole(deco, c);
        for (int i = 0; i < 4; i++)
            FairLights.Festoon(deco, corners[i] + Vector3.up * h, corners[(i + 1) % 4] + Vector3.up * h, 0.4f);
    }

    // Ordination hall (โบสถ์): stepped white plinth, colonnade, gold-framed doors and windows,
    // and a three-section Thai roof (ridge along Z) with gold gables, bai raka fins and chofa horns.
    internal static void Ubosot(Transform parent, Vector3 position, Material white, Material gold, Material orange, Material green)
    {
        var hall = PrimitiveKit.Group("Ubosot_โบสถ์", parent, position);
        var h = hall.transform;
        var lacquer = PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f);
        const float wallTop = 4.3f, pitch = 40f, halfWidth = 4.6f;

        // Plinth and front stairs
        PrimitiveKit.Box("Plinth1", h, new Vector3(0f, 0.175f, 0f), new Vector3(11.4f, 0.35f, 17.4f), white);
        PrimitiveKit.Box("Plinth2", h, new Vector3(0f, 0.525f, 0f), new Vector3(10.6f, 0.35f, 16.6f), white);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Stair", h, new Vector3(0f, 0.12f + i * 0.23f, -8.95f + i * 0.3f), new Vector3(3.6f, 0.24f + i * 0.23f, 0.6f), white);
        // Naga balustrades (นาคราวบันได) along the stairs
        foreach (float x in new[] { -1.95f, 1.95f })
        {
            var naga = MeshKit.Curve(new Vector3(x, 0.3f, -9.6f), new Vector3(x, 0.4f, -8.6f), new Vector3(x, 1.2f, -8.1f), 8);
            MeshKit.Place("Naga", h, MeshKit.Sweep("NagaRail", naga, MeshKit.Taper(8, 0.22f, 0.14f), 8), gold, Vector3.zero);
            MeshKit.Place("NagaHead", h, MeshKit.Blob("NagaHead", 5, 0.1f, 1), gold, new Vector3(x, 0.55f, -9.75f), default, new Vector3(0.45f, 0.6f, 0.5f));
        }

        // Walls with gold-framed windows and the lacquered main door
        PrimitiveKit.Box("Walls", h, new Vector3(0f, 2.5f, 0f), new Vector3(8f, 3.6f, 14f), white);
        PrimitiveKit.Box("DoorFrame", h, new Vector3(0f, 2.2f, -7.02f), new Vector3(2.2f, 3f, 0.06f), gold, collider: false);
        PrimitiveKit.Box("Door", h, new Vector3(0f, 2.15f, -7.06f), new Vector3(1.8f, 2.7f, 0.04f), lacquer, collider: false);
        PrimitiveKit.Box("DoorSeam", h, new Vector3(0f, 2.15f, -7.09f), new Vector3(0.05f, 2.7f, 0.02f), gold, collider: false);
        for (int i = 0; i < 4; i++)
        {
            float z = -4.8f + i * 3.2f;
            foreach (float side in new[] { -1f, 1f })
            {
                PrimitiveKit.Box("WindowFrame", h, new Vector3(side * 4.02f, 2.6f, z), new Vector3(0.06f, 1.9f, 1.15f), gold, collider: false);
                PrimitiveKit.Box("Window", h, new Vector3(side * 4.05f, 2.6f, z), new Vector3(0.04f, 1.65f, 0.9f), lacquer, collider: false);
            }
        }

        // Colonnade (เสาระเบียง) with lotus capitals
        var column = MeshKit.Lathe("TempleColumn", new[]
        {
            new Vector2(0.3f, 0f), new Vector2(0.26f, 0.15f), new Vector2(0.22f, 0.3f), new Vector2(0.2f, 3.1f),
            new Vector2(0.28f, 3.3f), new Vector2(0.34f, 3.5f), new Vector2(0.3f, 3.6f),
        }, 12);
        for (int i = 0; i < 6; i++)
        {
            float z = -7.2f + i * 2.88f;
            foreach (float x in new[] { -4.9f, 4.9f })
            {
                var c = MeshKit.Place("Column", h, column, white, new Vector3(x, 0.7f, z));
                MeshKit.Place("Capital", c.transform, MeshKit.Lathe("ColumnCapital", new[]
                {
                    new Vector2(0.24f, 3.05f), new Vector2(0.33f, 3.25f), new Vector2(0.36f, 3.45f), new Vector2(0.31f, 3.62f),
                }, 12, false, false), gold, Vector3.zero);
            }
        }

        // Lower skirt roof (ปีกนก) over the colonnade
        float skirtSlope = 1.9f;
        foreach (float side in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("SkirtRoof", h, new Vector3(side * 4.85f, 4.05f, 0f), new Vector3(skirtSlope, 0.14f, 15f), orange, new Vector3(0f, 0f, side * -22f));
            PrimitiveKit.Box("SkirtTrim", h, new Vector3(side * 4.87f, 4.0f, 0f), new Vector3(skirtSlope + 0.1f, 0.1f, 15.2f), green, new Vector3(0f, 0f, side * -22f), collider: false);
        }

        // Main roof in three sections: the middle one highest, front and back a step lower
        float slope = halfWidth / Mathf.Cos(pitch * Mathf.Deg2Rad);
        float rise = Mathf.Tan(pitch * Mathf.Deg2Rad) * halfWidth;
        RoofSection(h, 0f, 9f, wallTop, 0f, halfWidth, slope, rise, pitch, orange, green, gold);
        RoofSection(h, -6.05f, 3.1f, wallTop - 0.45f, 0.45f, halfWidth, slope, rise, pitch, orange, green, gold, frontGable: true);
        RoofSection(h, 6.05f, 3.1f, wallTop - 0.45f, 0.45f, halfWidth, slope, rise, pitch, orange, green, gold, backGable: true);
    }

    private static void RoofSection(Transform h, float z, float length, float baseY, float drop, float halfWidth, float slope, float rise,
        float pitch, Material orange, Material green, Material gold, bool frontGable = false, bool backGable = false)
    {
        float cy = baseY + rise * 0.5f;
        foreach (float side in new[] { -1f, 1f })
        {
            var roll = new Vector3(0f, 0f, side * -pitch);
            PrimitiveKit.Box("Roof", h, new Vector3(side * halfWidth * 0.5f, cy, z), new Vector3(slope, 0.16f, length), orange, roll);
            PrimitiveKit.Box("RoofTrim", h, new Vector3(side * halfWidth * 0.5f, cy - 0.06f, z), new Vector3(slope + 0.3f, 0.1f, length + 0.3f), green, roll, collider: false);
        }
        // Gold ridge
        PrimitiveKit.Box("Ridge", h, new Vector3(0f, baseY + rise + 0.05f, z), new Vector3(0.22f, 0.18f, length + 0.1f), gold, collider: false);

        foreach (bool front in new[] { true, false })
        {
            if (front ? !frontGable : !backGable) continue; // only the outer ends get a gable
            float gz = z + (front ? -length * 0.5f : length * 0.5f);
            float yaw = front ? 0f : 180f;
            var gable = PrimitiveKit.Group(front ? "GableFront" : "GableBack", h, new Vector3(0f, baseY, gz), yaw);
            var g = gable.transform;
            // Gold pediment (หน้าบัน) filling the triangle under the roof
            MeshKit.Place("Pediment", g, MeshKit.Gable("Pediment", halfWidth * 1.8f, rise * 0.9f, 0.12f), gold, new Vector3(0f, 0.05f, 0.02f));
            // Bai raka fins along both sloping edges, chofa horn at the top, hang hong at the eaves
            foreach (float side in new[] { -1f, 1f })
            {
                MeshKit.Place("BaiRaka", g, MeshKit.Fins("BaiRaka", slope * 0.95f, 14, 0.35f, 0.04f), gold,
                    new Vector3(side * halfWidth * 0.5f, rise * 0.5f + 0.12f, -0.12f), new Vector3(0f, 0f, side * -pitch));
                var hook = MeshKit.Curve(Vector3.zero, new Vector3(side * 0.35f, 0.05f, 0f), new Vector3(side * 0.45f, 0.5f, 0f), 7);
                MeshKit.Place("HangHong", g, MeshKit.Sweep(side < 0 ? "HangHongL" : "HangHongR", hook, MeshKit.Taper(7, 0.07f, 0.02f), 6), gold,
                    new Vector3(side * (halfWidth + 0.1f), -0.05f, -0.12f));
            }
            var horn = MeshKit.Curve(Vector3.zero, new Vector3(0f, 0.9f, -0.1f), new Vector3(0f, 1.2f, -0.75f), 9);
            MeshKit.Place("Chofa", g, MeshKit.Sweep("Chofa", horn, new[] { 0.1f, 0.095f, 0.09f, 0.085f, 0.08f, 0.075f, 0.065f, 0.05f, 0.02f }, 8), gold,
                new Vector3(0f, rise + 0.1f, -0.1f));
        }
    }

    // Bell-shaped chedi: square stepped bases, a smooth white bell and a gold ringed spire
    internal static void Chedi(Transform parent, Vector3 position, Material white, Material gold)
    {
        var chedi = PrimitiveKit.Group("Chedi_เจดีย์", parent, position);
        var c = chedi.transform;
        PrimitiveKit.Box("Base1", c, new Vector3(0f, 0.25f, 0f), new Vector3(4.4f, 0.5f, 4.4f), white);
        PrimitiveKit.Box("Base2", c, new Vector3(0f, 0.7f, 0f), new Vector3(3.6f, 0.4f, 3.6f), white);
        PrimitiveKit.Box("Base3", c, new Vector3(0f, 1.05f, 0f), new Vector3(2.9f, 0.3f, 2.9f), white);
        MeshKit.Place("Bell", c, MeshKit.Lathe("ChediBell", new[]
        {
            new Vector2(1.35f, 0f), new Vector2(1.35f, 0.18f), new Vector2(1.2f, 0.2f), new Vector2(1.2f, 0.38f),
            new Vector2(1.05f, 0.4f), new Vector2(1.05f, 0.55f), new Vector2(1.18f, 0.7f), new Vector2(1.28f, 1.05f),
            new Vector2(1.25f, 1.5f), new Vector2(1.12f, 1.9f), new Vector2(0.9f, 2.25f), new Vector2(0.62f, 2.5f),
            new Vector2(0.45f, 2.6f), new Vector2(0.45f, 2.8f),
        }, 32), white, new Vector3(0f, 1.2f, 0f));
        var col = chedi.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 2.4f, 0f);
        col.height = 2.8f;
        col.radius = 1.3f;
        MeshKit.Place("Spire", c, MeshKit.Lathe("ChediSpire", new[]
        {
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0.25f), new Vector2(0.36f, 0.3f), new Vector2(0.36f, 0.45f),
            new Vector2(0.3f, 0.5f), new Vector2(0.33f, 0.62f), new Vector2(0.26f, 0.7f), new Vector2(0.29f, 0.82f),
            new Vector2(0.22f, 0.9f), new Vector2(0.24f, 1.02f), new Vector2(0.17f, 1.1f), new Vector2(0.12f, 1.8f),
            new Vector2(0.07f, 2.5f), new Vector2(0.03f, 3f), new Vector2(0f, 3.2f),
        }, 20), gold, new Vector3(0f, 4f, 0f));
    }

    // Temple fair (งานวัด) corner: chicken beauty contest stage + a couple of fair stalls.
    // The contest only runs during the monthly fair (server-side schedule).
    private static void TempleFair(Transform temple)
    {
        var fair = PrimitiveKit.Group("TempleFair_งานวัด", temple, new Vector3(-10f, 0f, -22f)); // in front of the wall, left of the gate
        var t = fair.transform;
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var banner = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var pink = PrimitiveKit.Mat("FlowerPink", new Color(1f, 0.45f, 0.7f));
        var blue = PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f));

        // Contest stage (เวทีประกวดไก่สวย)
        PrimitiveKit.Box("ContestStage", t, new Vector3(0f, 0.2f, 0f), new Vector3(3.6f, 0.4f, 3f), red);
        foreach (float x in new[] { -1.7f, 1.7f })
            PrimitiveKit.Cylinder("BannerPole", t, new Vector3(x, 1.6f, 1.4f), new Vector3(0.08f, 1.6f, 0.08f), wood, collider: false);
        PrimitiveKit.Box("Banner", t, new Vector3(0f, 2.9f, 1.4f), new Vector3(3.6f, 0.6f, 0.06f), banner, collider: false);
        PrimitiveKit.Label("ประกวดไก่สวย", t, new Vector3(0f, 2.9f, 1.36f), 0.06f, new Color(0.7f, 0.1f, 0.1f));

        var contest = new GameObject("ContestZone");
        contest.transform.SetParent(t, false);
        contest.transform.localPosition = new Vector3(0f, 1f, -2.3f);
        var box = contest.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.6f, 2f, 1.6f);
        contest.AddComponent<WindowSpot>().Configure(WindowSpot.Window.Contest);
        Sign(t, new Vector3(-2.6f, 0f, -2.2f), "ประกวดไก่สวย\nช่วงงานวัดตามประเพณี", 0f);

        // Music stage behind the contest stage (folk instruments shine here); keeps the gate path clear
        Places2.MusicStage(t, new Vector3(0f, 0f, 5.2f), 0f, "temple_fair", "ดนตรีงานวัด", new Color(0.75f, 0.15f, 0.12f));

        // Fair stalls (ซุ้มงานวัด) with striped roofs, both sides of the gate path in front of the wall
        // (right of the gate is the สอยดาว and ก.ไก่ pens' ground)
        var stallSpots = new[] { new Vector3(-13.5f, 0f, -1.5f), new Vector3(-17f, 0f, -1.5f) };
        for (int i = 0; i < stallSpots.Length; i++)
        {
            var stall = PrimitiveKit.Group("FairStall", t, stallSpots[i]);
            PrimitiveKit.Box("Counter", stall.transform, new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.9f, 0.8f), wood);
            foreach (float x in new[] { -1.2f, 1.2f })
                PrimitiveKit.Cylinder("Pole", stall.transform, new Vector3(x, 1.2f, 0.3f), new Vector3(0.06f, 1.2f, 0.06f), wood, collider: false);
            PrimitiveKit.Box("Roof", stall.transform, new Vector3(0f, 2.4f, 0f), new Vector3(3f, 0.08f, 1.6f), i % 2 == 0 ? pink : blue, collider: false);
        }
    }

    // ── School (โรงเรียน) ───────────────────────────────────────────────────────
    public static GameObject School(Transform parent, Vector3 position, string name, float yaw = 0f)
    {
        var school = PrimitiveKit.Group("School_โรงเรียน", parent, position, yaw);
        var t = school.transform;
        var cream = PrimitiveKit.Mat("SchoolCream", new Color(0.95f, 0.88f, 0.7f));
        var roof = PrimitiveKit.Mat("RoofRed", new Color(0.62f, 0.18f, 0.12f));
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var field = PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f));
        var gate = PrimitiveKit.Mat("SchoolGate", new Color(0.2f, 0.35f, 0.7f));

        // Two-storey school you can walk into: a doorway in the middle of the front opens on a lobby,
        // the classroom (ห้องเรียน) to the left and the library (ห้องสมุด) to the right. The upper floor and
        // roof hide while you're inside (InteriorReveal) so the camera can see the rooms.
        SchoolBuilding(t, cream, roof, window);

        // Flagpole with the Thai flag (red-white-blue-white-red)
        PrimitiveKit.Cylinder("Flagpole", t, new Vector3(0f, 4f, -8f), new Vector3(0.12f, 4f, 0.12f), white);
        Color[] stripes = { new Color(0.75f, 0.1f, 0.15f), Color.white, new Color(0.1f, 0.15f, 0.45f), Color.white, new Color(0.75f, 0.1f, 0.15f) };
        float[] heights = { 0.2f, 0.2f, 0.4f, 0.2f, 0.2f };
        float y = 7.6f;
        for (int i = 0; i < stripes.Length; i++)
        {
            y -= heights[i] * 0.5f;
            PrimitiveKit.Box("Flag", t, new Vector3(0.75f, y, -8f), new Vector3(1.5f, heights[i], 0.03f),
                PrimitiveKit.Mat($"Flag{i}", stripes[i]), collider: false);
            y -= heights[i] * 0.5f;
        }

        // Football field beside the building, with goals
        var pitch = PrimitiveKit.Group("FootballField", t, new Vector3(18f, 0f, -4f));
        PrimitiveKit.Box("Pitch", pitch.transform, new Vector3(0f, 0.012f, 0f), new Vector3(12f, 0.02f, 20f), field, collider: false);
        PrimitiveKit.Box("Halfway", pitch.transform, new Vector3(0f, 0.025f, 0f), new Vector3(12f, 0.01f, 0.12f), white, collider: false);
        foreach (float z in new[] { -10f, 10f })
        {
            PrimitiveKit.Box("Crossbar", pitch.transform, new Vector3(0f, 2f, z), new Vector3(4f, 0.1f, 0.1f), white);
            PrimitiveKit.Box("PostL", pitch.transform, new Vector3(-2f, 1f, z), new Vector3(0.1f, 2f, 0.1f), white);
            PrimitiveKit.Box("PostR", pitch.transform, new Vector3(2f, 1f, z), new Vector3(0.1f, 2f, 0.1f), white);
        }

        Gate(t, new Vector3(0f, 0f, -13f), name, gate, PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), new Color(0.15f, 0.2f, 0.5f));
        return school;
    }

    private static void SchoolBuilding(Transform t, Material cream, Material roof, Material window)
    {
        var blue = PrimitiveKit.Mat("SchoolGate", new Color(0.2f, 0.35f, 0.7f));
        var tile = PrimitiveKit.Mat("SchoolTile", new Color(0.86f, 0.84f, 0.78f), 0.4f);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var gold = PrimitiveKit.Glow("ClassMarker", new Color(1f, 0.78f, 0.3f), 0.6f);
        const float W = 16f, D = 8f, H = 3f;

        // Ground floor shell
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.03f, 0f), new Vector3(W - 0.4f, 0.04f, D - 0.4f), tile, collider: false);
        PrimitiveKit.Box("BackWall", t, new Vector3(0f, H * 0.5f, D * 0.5f - 0.1f), new Vector3(W, H, 0.2f), cream);
        foreach (float side in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("SideWall", t, new Vector3(side * (W * 0.5f - 0.1f), H * 0.5f, 0f), new Vector3(0.2f, H, D), cream);
            PrimitiveKit.Box("FrontWall", t, new Vector3(side * 4.65f, H * 0.5f, -D * 0.5f + 0.1f), new Vector3(6.7f, H, 0.2f), cream);
            // Partition between the lobby and each room, with an open doorway in the middle
            foreach (float z in new[] { -2.5f, 2.5f })
                PrimitiveKit.Box("Partition", t, new Vector3(side * 2.4f, H * 0.5f, z), new Vector3(0.15f, H, 3f), cream);
            PrimitiveKit.Box("PartitionLintel", t, new Vector3(side * 2.4f, 2.7f, 0f), new Vector3(0.15f, 0.6f, 2f), cream, collider: false);
            // Door frame posts
            PrimitiveKit.Box("DoorPost", t, new Vector3(side * 1.35f, 1.2f, -D * 0.5f - 0.02f), new Vector3(0.18f, 2.4f, 0.28f), blue, collider: false);
        }
        PrimitiveKit.Box("Lintel", t, new Vector3(0f, 2.7f, -D * 0.5f + 0.1f), new Vector3(2.6f, 0.6f, 0.2f), cream, collider: false);
        PrimitiveKit.Box("DoorHead", t, new Vector3(0f, 2.46f, -D * 0.5f - 0.02f), new Vector3(2.9f, 0.16f, 0.28f), blue, collider: false);
        PrimitiveKit.Box("Doormat", t, new Vector3(0f, 0.04f, -D * 0.5f - 0.7f), new Vector3(2.2f, 0.02f, 1f), blue, collider: false);
        // Ground-floor windows, seen from outside and inside
        foreach (float x in new[] { -6.25f, -3.75f, 3.75f, 6.25f })
            foreach (float z in new[] { -D * 0.5f - 0.03f, -D * 0.5f + 0.23f, D * 0.5f + 0.03f, D * 0.5f - 0.23f })
                PrimitiveKit.Box("Window", t, new Vector3(x, 1.6f, z), new Vector3(1.4f, 1.2f, 0.05f), window, collider: false);
        foreach (float x in new[] { -5f, 0f, 5f })
            StreetFurniture.HangingLight(t, new Vector3(x, 2.75f, 0f));

        // Upper floor, ceiling and roof: hidden while you're inside
        var upper = PrimitiveKit.Group("UpperFloor", t, Vector3.zero).transform;
        PrimitiveKit.Box("Ceiling", upper, new Vector3(0f, H + 0.05f, 0f), new Vector3(W, 0.1f, D), cream, collider: false);
        PrimitiveKit.Box("Storey", upper, new Vector3(0f, H + 1.55f, 0f), new Vector3(W, 2.9f, D), cream, collider: false);
        for (int i = 0; i < 6; i++)
            foreach (float z in new[] { -D * 0.5f - 0.03f, D * 0.5f + 0.03f })
                PrimitiveKit.Box("Window", upper, new Vector3(-6.25f + i * 2.5f, 4.6f, z), new Vector3(1.4f, 1.2f, 0.05f), window, collider: false);
        const float eave = 6.1f, half = D * 0.5f + 0.6f, pitch = 30f;
        float rise = half * Mathf.Tan(pitch * Mathf.Deg2Rad);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box(side < 0 ? "RoofFront" : "RoofBack", upper, new Vector3(0f, eave + rise * 0.5f, side * half * 0.5f),
                new Vector3(W + 1f, 0.15f, half / Mathf.Cos(pitch * Mathf.Deg2Rad) + 0.1f), roof, new Vector3(side * pitch, 0f, 0f), collider: false);
        MeshKit.Place("Gable", upper, MeshKit.Gable("SchoolGable", D, rise - 0.05f, W), cream, new Vector3(-W * 0.5f, 6f, 0f), new Vector3(0f, 90f, 0f));

        var reveal = new GameObject("InteriorZone");
        reveal.transform.SetParent(t, false);
        reveal.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
        var revealBox = reveal.AddComponent<BoxCollider>();
        revealBox.isTrigger = true;
        revealBox.size = new Vector3(W - 0.4f, H, D - 0.8f);
        reveal.AddComponent<InteriorReveal>().Configure(upper);

        // Room names over the inner doorways (seen from the lobby)
        PrimitiveKit.Label("ห้องเรียน", t, new Vector3(-2.3f, 2.7f, 0f), 0.035f, new Color(0.15f, 0.2f, 0.5f), -90f);
        PrimitiveKit.Label("ห้องสมุด", t, new Vector3(2.3f, 2.7f, 0f), 0.035f, new Color(0.15f, 0.2f, 0.5f), 90f);

        // ── Classroom (left): blackboard, teacher's desk, rows of desks ──
        PrimitiveKit.Box("Blackboard", t, new Vector3(-7.75f, 1.7f, 0f), new Vector3(0.06f, 1.2f, 3.4f), PrimitiveKit.Mat("Blackboard", new Color(0.12f, 0.28f, 0.2f)), collider: false);
        PrimitiveKit.Box("BoardFrame", t, new Vector3(-7.78f, 1.7f, 0f), new Vector3(0.04f, 1.36f, 3.56f), wood, collider: false);
        PrimitiveKit.Label("ก ข ค  1+1=2", t, new Vector3(-7.7f, 1.8f, 0f), 0.03f, Color.white, 90f);
        PrimitiveKit.Box("TeacherDesk", t, new Vector3(-6.7f, 0.4f, -0.8f), new Vector3(0.8f, 0.8f, 1.6f), wood);
        for (int col = 0; col < 2; col++)
            for (int row = 0; row < 4; row++)
            {
                float x = -5.4f + col * 1.6f, z = -2.6f + row * 1.7f;
                PrimitiveKit.Box("Desk", t, new Vector3(x, 0.72f, z), new Vector3(0.6f, 0.05f, 0.9f), wood, collider: false);
                PrimitiveKit.Box("DeskLegs", t, new Vector3(x, 0.36f, z), new Vector3(0.5f, 0.7f, 0.05f), dark, collider: false);
                PrimitiveKit.Box("Chair", t, new Vector3(x + 0.55f, 0.42f, z), new Vector3(0.4f, 0.05f, 0.4f), blue, collider: false);
                PrimitiveKit.Box("ChairBack", t, new Vector3(x + 0.75f, 0.68f, z), new Vector3(0.05f, 0.5f, 0.4f), blue, collider: false);
            }
        PrimitiveKit.Box("ClassMarker", t, new Vector3(-3.3f, 0.06f, 0f), new Vector3(1.3f, 0.02f, 1.8f), gold, collider: false);
        var classZone = new GameObject("ClassZone");
        classZone.transform.SetParent(t, false);
        classZone.transform.localPosition = new Vector3(-3.3f, 1f, 0f);
        var classBox = classZone.AddComponent<BoxCollider>();
        classBox.isTrigger = true;
        classBox.size = new Vector3(1.3f, 2f, 1.8f);
        classZone.AddComponent<SchoolDesk>().Configure(false);

        // ── Library (right): bookshelves, reading tables, librarian's counter ──
        Color[] covers = { new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.8f), new Color(0.95f, 0.75f, 0.2f), new Color(0.25f, 0.6f, 0.3f), new Color(0.6f, 0.3f, 0.65f), new Color(0.95f, 0.5f, 0.2f) };
        var bookMats = new Material[covers.Length];
        for (int i = 0; i < covers.Length; i++) bookMats[i] = PrimitiveKit.Mat($"BookCover{i}", covers[i]);
        void Shelf(Vector3 at, float yaw, int seed)
        {
            var shelf = PrimitiveKit.Group("Bookshelf", t, at, yaw).transform;
            PrimitiveKit.Box("Back", shelf, new Vector3(0f, 1.1f, 0.18f), new Vector3(1.6f, 2.2f, 0.05f), dark);
            foreach (float x in new[] { -0.78f, 0.78f })
                PrimitiveKit.Box("Side", shelf, new Vector3(x, 1.1f, 0f), new Vector3(0.05f, 2.2f, 0.4f), dark, collider: false);
            for (int r = 0; r < 4; r++)
            {
                float y = 0.15f + r * 0.52f;
                PrimitiveKit.Box("Board", shelf, new Vector3(0f, y, 0f), new Vector3(1.56f, 0.04f, 0.38f), wood, collider: false);
                float x = -0.7f;
                for (int b = 0; x < 0.68f; b++)
                {
                    float w = 0.08f + ((seed + r * 3 + b) % 3) * 0.03f, h = 0.32f + ((seed + b * 7 + r) % 4) * 0.04f;
                    PrimitiveKit.Box("Book", shelf, new Vector3(x + w * 0.5f, y + 0.02f + h * 0.5f, 0f), new Vector3(w - 0.01f, h, 0.28f), bookMats[(seed + r + b) % bookMats.Length], collider: false);
                    x += w;
                }
            }
        }
        foreach (float x in new[] { 3.6f, 5.3f, 7.0f })
            Shelf(new Vector3(x, 0f, 3.55f), 0f, (int)(x * 3f));
        Shelf(new Vector3(7.55f, 0f, 0.8f), 90f, 11);
        Shelf(new Vector3(7.55f, 0f, -1.2f), 90f, 17);
        foreach (float z in new[] { -2.4f, 0.4f })
        {
            PrimitiveKit.Box("ReadingTable", t, new Vector3(5.2f, 0.74f, z), new Vector3(1.8f, 0.06f, 1f), wood, collider: false);
            PrimitiveKit.Box("TableLegs", t, new Vector3(5.2f, 0.37f, z), new Vector3(1.6f, 0.72f, 0.1f), dark, collider: false);
            foreach (float x in new[] { 4.6f, 5.8f })
                foreach (float dz in new[] { -0.75f, 0.75f })
                    PrimitiveKit.Box("Stool", t, new Vector3(x, 0.42f, z + dz), new Vector3(0.38f, 0.05f, 0.38f), blue, collider: false);
            PrimitiveKit.Box("OpenBook", t, new Vector3(5.0f, 0.79f, z), new Vector3(0.45f, 0.03f, 0.3f), PrimitiveKit.Mat("PagesWhite", new Color(0.97f, 0.95f, 0.88f)), collider: false);
        }
        PrimitiveKit.Sphere("Globe", t, new Vector3(5.8f, 1.0f, -2.4f), Vector3.one * 0.4f, PrimitiveKit.Mat("GlobeBlue", new Color(0.25f, 0.55f, 0.85f)));
        PrimitiveKit.Box("LibrarianDesk", t, new Vector3(3.3f, 0.5f, -3.1f), new Vector3(1.4f, 1f, 0.6f), wood);
        PrimitiveKit.Box("LibraryMarker", t, new Vector3(3.3f, 0.06f, 0f), new Vector3(1.3f, 0.02f, 1.8f), gold, collider: false);
        var libZone = new GameObject("LibraryZone");
        libZone.transform.SetParent(t, false);
        libZone.transform.localPosition = new Vector3(3.3f, 1f, 0f);
        var libBox = libZone.AddComponent<BoxCollider>();
        libBox.isTrigger = true;
        libBox.size = new Vector3(1.3f, 2f, 1.8f);
        libZone.AddComponent<SchoolDesk>().Configure(true);

        // ── Lobby: notice board and the tutoring desk (skilled job) ──
        PrimitiveKit.Box("NoticeBoard", t, new Vector3(0f, 1.7f, 3.75f), new Vector3(2f, 1.1f, 0.05f), PrimitiveKit.Mat("Corkboard", new Color(0.72f, 0.55f, 0.35f)), collider: false);
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("Notice", t, new Vector3(-0.7f + i * 0.35f, 1.65f + (i % 2) * 0.2f, 3.72f), new Vector3(0.26f, 0.34f, 0.01f), bookMats[i % bookMats.Length], collider: false);
        Schooling.JobSpot(t, new Vector3(0f, 0f, 2.2f), 180f, "tutor", "ติวเตอร์ตรรกะ\nค่าจ้าง 380 ฿ · ต้องมี เลเวล 10 · ตรรกะ Lv 5");
    }

    // ── Sub-district health promoting hospital (รพ.สต. / สถานีอนามัย) ─────────────
    public static GameObject HealthStation(Transform parent, Vector3 position, float yaw, string name)
    {
        var station = PrimitiveKit.Group("HealthStation_รพสต", parent, position, yaw);
        var t = station.transform;
        var white = PrimitiveKit.Mat("ClinicWhite", new Color(0.96f, 0.97f, 0.96f));
        var green = PrimitiveKit.Mat("ClinicGreen", new Color(0.1f, 0.55f, 0.35f));
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));

        PrimitiveKit.Box("Building", t, new Vector3(0f, 1.75f, 0f), new Vector3(12f, 3.5f, 7f), white);
        PrimitiveKit.Box("Stripe", t, new Vector3(0f, 3.3f, -3.52f), new Vector3(12f, 0.3f, 0.05f), green, collider: false);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 4.1f, -1.85f), new Vector3(12.8f, 0.15f, 4.3f), green, new Vector3(-22f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 4.1f, 1.85f), new Vector3(12.8f, 0.15f, 4.3f), green, new Vector3(22f, 0f, 0f));
        PrimitiveKit.Box("Door", t, new Vector3(0f, 1.2f, -3.53f), new Vector3(2f, 2.4f, 0.05f), window, collider: false);
        foreach (float x in new[] { -4f, -2.2f, 2.2f, 4f })
            PrimitiveKit.Box("Window", t, new Vector3(x, 1.8f, -3.53f), new Vector3(1.2f, 1f, 0.05f), window, collider: false);

        // Entrance canopy and sign with a green cross
        PrimitiveKit.Box("Canopy", t, new Vector3(0f, 2.9f, -5f), new Vector3(4.5f, 0.15f, 3f), green, collider: false);
        PrimitiveKit.Cylinder("CanopyPostL", t, new Vector3(-2f, 1.4f, -6.3f), new Vector3(0.15f, 1.4f, 0.15f), white);
        PrimitiveKit.Cylinder("CanopyPostR", t, new Vector3(2f, 1.4f, -6.3f), new Vector3(0.15f, 1.4f, 0.15f), white);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 3.4f, -6.55f), new Vector3(4.5f, 0.8f, 0.1f), green, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0.4f, 3.4f, -6.62f), 0.075f, Color.white);
        PrimitiveKit.Box("CrossV", t, new Vector3(-1.75f, 3.4f, -6.62f), new Vector3(0.14f, 0.5f, 0.02f), white, collider: false);
        PrimitiveKit.Box("CrossH", t, new Vector3(-1.75f, 3.4f, -6.62f), new Vector3(0.5f, 0.14f, 0.02f), white, collider: false);

        // Treatment counter under the canopy (injuries from a knockout)
        var clinic = new GameObject("HealZone");
        clinic.transform.SetParent(t, false);
        clinic.transform.localPosition = new Vector3(0f, 1f, -5.2f);
        var clinicZone = clinic.AddComponent<BoxCollider>();
        clinicZone.isTrigger = true;
        clinicZone.size = new Vector3(4f, 2f, 2.2f);
        clinic.AddComponent<HealSpot>().Configure("station", name, 50);
        return station;
    }

    // ── Kok Nong Na model (โคก หนอง นา): paddies, a pond and a raised mound ─────────
    public static GameObject KokNongNa(Transform parent, Vector3 position)
    {
        var farm = PrimitiveKit.Group("KokNongNa_โคกหนองนา", parent, position);
        var t = farm.transform;
        // Flooded paddy: muddy water with a green film of duckweed (แหน), shiny like water
        var rice = PrimitiveKit.Mat("PaddyWater", new Color(0.58f, 0.64f, 0.36f), 0.82f);
        var dike = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        var water = PrimitiveKit.Mat("Water", new Color(0.25f, 0.5f, 0.75f));
        var earth = PrimitiveKit.Mat("Earth", new Color(0.5f, 0.38f, 0.24f));
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.75f, 0.62f, 0.35f));
        var wood = PrimitiveKit.Mat("DarkWood", DarkWood);

        // นา: 3x2 rice paddies separated by low dikes (0.25 m — walkable); each is a farm plot (ids 1..6)
        int plotId = 0;
        foreach (float x in new[] { -16f, -8f, 0f })
        foreach (float z in new[] { -4f, 3.5f })
        {
            PrimitiveKit.Box("Paddy", t, new Vector3(x, 0.015f, z), new Vector3(7.6f, 0.03f, 7.1f), rice, collider: false);

            var plotZone = new GameObject($"FarmPlot_{++plotId}");
            plotZone.transform.SetParent(t, false);
            plotZone.transform.localPosition = new Vector3(x, 1f, z);
            var trigger = plotZone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(7.2f, 2f, 6.7f);
            plotZone.AddComponent<FarmPlot>().Configure(plotId, new Vector2(7.5f, 7.0f)); // rice fills the paddy (7.6 x 7.1)
            PrimitiveKit.Box("DikeN", t, new Vector3(x, 0.12f, z + 3.65f), new Vector3(8f, 0.25f, 0.3f), dike);
            PrimitiveKit.Box("DikeS", t, new Vector3(x, 0.12f, z - 3.65f), new Vector3(8f, 0.25f, 0.3f), dike);
            PrimitiveKit.Box("DikeW", t, new Vector3(x - 3.85f, 0.12f, z), new Vector3(0.3f, 0.25f, 7.6f), dike);
        }
        PrimitiveKit.Box("DikeE", t, new Vector3(3.85f, 0.12f, 0f), new Vector3(0.3f, 0.25f, 15f), dike);

        // หนอง: farm pond
        PrimitiveKit.Box("PondRim", t, new Vector3(11f, 0.01f, -3f), new Vector3(9f, 0.02f, 11f), earth, collider: false);
        PrimitiveKit.Box("Pond", t, new Vector3(11f, 0.025f, -3f), new Vector3(8f, 0.02f, 10f), water, collider: false);

        // โคก: terraced mound with fruit trees
        PrimitiveKit.Box("Mound1", t, new Vector3(11f, 0.15f, 8f), new Vector3(10f, 0.3f, 7f), earth);
        PrimitiveKit.Box("Mound2", t, new Vector3(11f, 0.45f, 8.5f), new Vector3(7f, 0.3f, 5f), earth);
        BananaTree(t, new Vector3(9f, 0.6f, 9f), trainable: false);
        BananaTree(t, new Vector3(13f, 0.6f, 8f), trainable: false);
        ShadeTree(t, new Vector3(11f, 0.6f, 10.5f), 0.7f);

        // เถียงนา: small rest hut on stilts
        var hut = PrimitiveKit.Group("Hut_เถียงนา", t, new Vector3(-8f, 0f, 11f));
        foreach (float x in new[] { -1.2f, 1.2f })
        foreach (float z in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Stilt", hut.transform, new Vector3(x, 0.5f, z), new Vector3(0.15f, 0.5f, 0.15f), wood);
        PrimitiveKit.Box("Floor", hut.transform, new Vector3(0f, 1.05f, 0f), new Vector3(2.8f, 0.1f, 2.4f), wood);
        foreach (float x in new[] { -1.2f, 1.2f })
        foreach (float z in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Post", hut.transform, new Vector3(x, 1.8f, z), new Vector3(0.1f, 0.75f, 0.1f), wood, collider: false);
        PrimitiveKit.Box("RoofFront", hut.transform, new Vector3(0f, 2.75f, -0.75f), new Vector3(3.4f, 0.12f, 1.9f), thatch, new Vector3(-35f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", hut.transform, new Vector3(0f, 2.75f, 0.75f), new Vector3(3.4f, 0.12f, 1.9f), thatch, new Vector3(35f, 0f, 0f), collider: false);

        // Rest on the hut (พักผ่อนที่เถียงนา): mat, bolster, a little ladder, and the rest spot
        PrimitiveKit.Box("Mat", hut.transform, new Vector3(0f, 1.11f, 0.1f), new Vector3(1.2f, 0.02f, 1.9f), PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f)), collider: false);
        PrimitiveKit.Cylinder("Bolster", hut.transform, new Vector3(0f, 1.2f, 0.95f), new Vector3(0.2f, 0.45f, 0.2f), PrimitiveKit.Mat("Bolster", new Color(0.96f, 0.93f, 0.85f)), new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("LadderStep", hut.transform, new Vector3(0f, 0.35f, -1.5f), new Vector3(1f, 0.08f, 0.35f), wood, collider: false);
        PrimitiveKit.Box("LadderStep", hut.transform, new Vector3(0f, 0.7f, -1.3f), new Vector3(1f, 0.08f, 0.35f), wood, collider: false);
        var bed = new GameObject("RestPoint");
        bed.transform.SetParent(hut.transform, false);
        // Lying down rolls the body onto the player's side axis; turning the rest point 90°
        // lays it along the mat with the head toward the bolster (+Z)
        bed.transform.localPosition = new Vector3(0f, 1.24f, 0.2f);
        bed.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        var rest = new GameObject("RestZone");
        rest.transform.SetParent(hut.transform, false);
        rest.transform.localPosition = new Vector3(0f, 1f, -1.6f);
        var restZone = rest.AddComponent<BoxCollider>();
        restZone.isTrigger = true;
        restZone.size = new Vector3(3f, 2f, 1.6f);
        rest.AddComponent<SleepSpot>().Configure("เถียงนาโคกหนองนา", "พักผ่อน", bed.transform);

        Sign(t, new Vector3(2f, 0f, 14f), "โคก หนอง นา\nเดินเข้าแปลงเพื่อทำนา", 180f);
        return farm;
    }

    // ── Songthaew stop (ป้ายรถสองแถว): opens the travel map; arrival spawn in front ──
    public static GameObject BusStop(Transform parent, Vector3 position, float yaw)
    {
        var stop = PrimitiveKit.Group("BusStop_ป้ายรถสองแถว", parent, position, yaw);
        var t = stop.transform;
        var metal = PrimitiveKit.Mat("Metal", new Color(0.35f, 0.35f, 0.38f));
        var roof = PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var sign = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));

        foreach (float x in new[] { -1.6f, 1.6f })
            PrimitiveKit.Cylinder("Post", t, new Vector3(x, 1.25f, 0.6f), new Vector3(0.1f, 1.25f, 0.1f), metal);
        PrimitiveKit.Box("BackPanel", t, new Vector3(0f, 1.3f, 0.65f), new Vector3(3.3f, 1.6f, 0.05f), roof);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 2.55f, 0.1f), new Vector3(3.8f, 0.1f, 1.6f), roof, collider: false);
        PrimitiveKit.Box("Bench", t, new Vector3(0f, 0.45f, 0.35f), new Vector3(2.8f, 0.1f, 0.45f), wood, collider: false);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 2.95f, -0.3f), new Vector3(3f, 0.6f, 0.08f), sign, collider: false);
        PrimitiveKit.Label("ป้ายรถสองแถว", t, new Vector3(0f, 2.95f, -0.36f), 0.06f, new Color(0.2f, 0.1f, 0.05f));
        StreetFurniture.HangingLight(t, new Vector3(0f, 2.4f, 0.1f)); // bulb under the shelter roof

        // Trigger under the roof opens the travel map
        var zone = new GameObject("TravelZone");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0.1f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 2f, 1.4f);
        zone.AddComponent<TravelStop>();

        // Arrival point just outside the trigger, facing away from the shelter
        var spawn = PrimitiveKit.Group("SpawnPoint", t, new Vector3(0f, 0f, -2.6f), 180f);
        spawn.AddComponent<SpawnPoint>().Configure("busstop");
        return stop;
    }

    // ── Boxing camp (ค่ายมวย) — career training tier 3 ─────────────────────────────
    public static GameObject BoxingCamp(Transform parent, Vector3 position, string name)
    {
        var camp = PrimitiveKit.Group("BoxingCamp_ค่ายมวย", parent, position);
        var t = camp.transform;
        var baseMat = PrimitiveKit.Mat("RingBase", new Color(0.2f, 0.2f, 0.22f));
        var canvas = PrimitiveKit.Mat("RingCanvas", new Color(0.35f, 0.6f, 0.9f));
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var blue = PrimitiveKit.Mat("CornerBlue", new Color(0.15f, 0.3f, 0.85f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var banner = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var roofMat = PrimitiveKit.Mat("CampRoof", new Color(0.55f, 0.58f, 0.6f));

        BoxingRing(t, Vector3.zero, 0f, "เวทีค่ายมวย", "pads", "spar", "champ");

        // Open-sided gym roof over the ring and bags
        foreach (float x in new[] { -4.5f, 7.5f })
        foreach (float z in new[] { -4f, 4f })
            PrimitiveKit.Cylinder("RoofPost", t, new Vector3(x, 2.5f, z), new Vector3(0.2f, 2.5f, 0.2f), roofMat);
        PrimitiveKit.Box("GymRoof", t, new Vector3(1.5f, 5.05f, 0f), new Vector3(13f, 0.12f, 9f), roofMat, collider: false);

        // Entrance arch with banner
        PrimitiveKit.Box("ArchPostL", t, new Vector3(-3.6f, 1.9f, -6.5f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("ArchPostR", t, new Vector3(3.6f, 1.9f, -6.5f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("Banner", t, new Vector3(0f, 3.6f, -6.5f), new Vector3(7.5f, 0.9f, 0.15f), banner, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 3.62f, -6.6f), 0.09f, new Color(0.75f, 0.1f, 0.1f));
        PrimitiveKit.Label("ซ้อมกระสอบหนัก Lv 6+", t, new Vector3(0f, 2.9f, -6.6f), 0.05f, Color.white);

        // Career tier 3: heavy bags beside the ring
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.8f, 0f, -1.8f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.8f, 0f, 1.5f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));

        PrimitiveKit.Box("Bench", t, new Vector3(-4.1f, 0.225f, 0f), new Vector3(0.6f, 0.45f, 2.4f), wood);

        // Front desk (โต๊ะรับสมัคร): walk up to it to buy the camp membership
        PrimitiveKit.Box("FrontDesk", t, new Vector3(-6.2f, 0.5f, -5f), new Vector3(2.2f, 1f, 0.7f), wood);
        var desk = new GameObject("FrontDeskZone");
        desk.transform.SetParent(t, false);
        desk.transform.localPosition = new Vector3(-6.2f, 1f, -6.4f);
        var zone = desk.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2.6f, 2f, 1.8f);
        desk.AddComponent<ShopCounter>().Configure("camp");
        Sign(t, new Vector3(-8.2f, 0f, -5.6f), "สมัครสมาชิกค่าย\n300 บาท", 0f);
        return camp;
    }

    // Boxing ring (เวทีมวย): 6x6 m, top at 0.6 m, ropes on three sides, steps at the open
    // front (-Z) where a FightSpot offers the given events (boxing.lua ids)
    public static Transform BoxingRing(Transform parent, Vector3 position, float yaw, string title, params string[] events)
    {
        var ring = PrimitiveKit.Group("BoxingRing_เวทีมวย", parent, position, yaw);
        var t = ring.transform;
        var baseMat = PrimitiveKit.Mat("RingBase", new Color(0.2f, 0.2f, 0.22f));
        var canvas = PrimitiveKit.Mat("RingCanvas", new Color(0.35f, 0.6f, 0.9f));
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var blue = PrimitiveKit.Mat("CornerBlue", new Color(0.15f, 0.3f, 0.85f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        PrimitiveKit.Box("RingBase", t, new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 6f), baseMat);
        PrimitiveKit.Box("RingCanvas", t, new Vector3(0f, 0.55f, 0f), new Vector3(5.8f, 0.1f, 5.8f), canvas);
        PrimitiveKit.Cylinder("CornerRed", t, new Vector3(-2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), red);
        PrimitiveKit.Cylinder("CornerBlue", t, new Vector3(2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), blue);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(-2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);

        // Ropes on three sides; the front (-Z) is left open as the entrance
        Material[] ropeColors = { red, white, blue };
        float[] ropeHeights = { 1.0f, 1.4f, 1.8f };
        for (int i = 0; i < 3; i++)
        {
            PrimitiveKit.Box("Rope", t, new Vector3(0f, ropeHeights[i], 2.8f), new Vector3(5.6f, 0.05f, 0.05f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(-2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
        }
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.1f, -3.6f), new Vector3(2f, 0.2f, 0.4f), baseMat);
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.2f, -3.2f), new Vector3(2f, 0.4f, 0.4f), baseMat);


        // Lighting truss over the ring: two posts and a bar with stage lights
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        foreach (float x in new[] { -3.4f, 3.4f })
            PrimitiveKit.Box("TrussPost", t, new Vector3(x, 2.3f, 0f), new Vector3(0.14f, 4.6f, 0.14f), steel);
        PrimitiveKit.Box("TrussBar", t, new Vector3(0f, 4.6f, 0f), new Vector3(7f, 0.16f, 0.16f), steel, collider: false);
        foreach (float x in new[] { -1.6f, 1.6f })
            StreetFurniture.RingLight(t, new Vector3(x, 4.36f, 0f));

        var centre = new GameObject("RingCentre");
        centre.transform.SetParent(t, false);
        centre.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var spot = new GameObject("FightSpot");
        spot.transform.SetParent(t, false);
        spot.transform.localPosition = new Vector3(0f, 1f, -4.3f);
        var zone = spot.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(3f, 2f, 1.6f);
        spot.AddComponent<FightSpot>().Configure(title, centre.transform, events);
        Sign(t, new Vector3(2.4f, 0f, -4.4f), title + "\nเดินมาที่บันไดเวที", 0f);
        return centre.transform;
    }

    // ── Small pieces ────────────────────────────────────────────────────────────
    public static GameObject BananaTree(Transform parent, Vector3 position, bool trainable)
    {
        var tree = PrimitiveKit.Group(trainable ? "Training_BananaTree" : "BananaTree", parent, position);
        if (trainable)
            tree.AddComponent<TrainingTarget>().Configure("banana", 1, 2);

        var trunkMat = PrimitiveKit.Mat("BananaTrunk", new Color(0.52f, 0.6f, 0.3f), 0.25f);
        var leafMat = PrimitiveKit.Mat("BananaLeaf", new Color(0.32f, 0.66f, 0.2f), 0.35f);
        var youngLeafMat = PrimitiveKit.Mat("BananaLeafYoung", new Color(0.5f, 0.78f, 0.28f), 0.35f);
        var fruitMat = PrimitiveKit.Mat("BananaFruit", new Color(0.62f, 0.75f, 0.2f), 0.4f);
        var flowerMat = PrimitiveKit.Mat("BananaFlower", new Color(0.5f, 0.12f, 0.2f), 0.45f);

        // Pivot at the base so the trunk bends when kicked; trunk must be the first child (PunchingBag.IsRooted)
        var pivot = PrimitiveKit.Group("Pivot", tree.transform, Vector3.zero);
        if (trainable) pivot.AddComponent<PunchingBag>();

        var trunk = MeshKit.Place("Trunk", pivot.transform, MeshKit.Lathe("BananaTrunk", new[]
        {
            new Vector2(0.24f, 0f), new Vector2(0.21f, 0.15f), new Vector2(0.19f, 0.6f), new Vector2(0.17f, 1.3f),
            new Vector2(0.15f, 1.9f), new Vector2(0.13f, 2.3f), new Vector2(0.1f, 2.45f),
        }, 14), trunkMat, new Vector3(0f, 0.01f, 0f)); // above the pivot: PunchingBag.IsRooted
        var collider = trunk.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.2f, 0f);
        collider.height = 2.4f;
        collider.radius = 0.2f;

        // Big arching leaves all around, a couple of young upright ones in the middle
        var leaf = MeshKit.Leaf("BananaLeaf", 2.3f, 0.75f, 0.55f, 1.5f);
        var young = MeshKit.Leaf("BananaLeafYoung", 1.6f, 0.45f, 0.9f, 0.4f);
        for (int i = 0; i < 8; i++)
            MeshKit.Place("Leaf", pivot.transform, leaf, leafMat, new Vector3(0f, 2.3f + (i % 2) * 0.12f, 0f),
                new Vector3(-28f - (i % 3) * 6f, i * 45f + (i % 2) * 17f, (i % 3 - 1) * 8f));
        MeshKit.Place("YoungLeaf", pivot.transform, young, youngLeafMat, new Vector3(0f, 2.4f, 0f), new Vector3(-62f, 30f, 0f));
        MeshKit.Place("YoungLeaf", pivot.transform, young, youngLeafMat, new Vector3(0f, 2.4f, 0f), new Vector3(-55f, 200f, 0f));

        // Hanging bunch (เครือกล้วย) with the purple banana flower (หัวปลี) at the end
        var stalkPath = MeshKit.Curve(new Vector3(0.1f, 2.3f, 0.1f), new Vector3(0.55f, 2.4f, 0.55f), new Vector3(0.55f, 1.35f, 0.55f), 8);
        MeshKit.Place("Stalk", pivot.transform, MeshKit.Sweep("BananaStalk", stalkPath, MeshKit.Taper(8, 0.05f, 0.035f), 6), trunkMat, Vector3.zero);
        var banana = MeshKit.Sweep("Banana", MeshKit.Curve(Vector3.zero, new Vector3(0.12f, 0.08f, 0f), new Vector3(0.18f, 0.24f, 0f), 6),
            new[] { 0.02f, 0.035f, 0.04f, 0.04f, 0.032f, 0.012f }, 6);
        for (int hand = 0; hand < 3; hand++)
        for (int k = 0; k < 6; k++)
        {
            float yaw = k * 60f + hand * 25f;
            Vector3 at = new Vector3(0.55f, 1.95f - hand * 0.17f, 0.55f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.05f, 0f, 0f);
            MeshKit.Place("Banana", pivot.transform, banana, fruitMat, at, new Vector3(0f, yaw, 0f));
        }
        MeshKit.Place("BananaFlower", pivot.transform, MeshKit.Lathe("BananaFlower", new[]
        {
            new Vector2(0.02f, 0.3f), new Vector2(0.07f, 0.22f), new Vector2(0.1f, 0.1f), new Vector2(0.06f, 0f), new Vector2(0f, -0.05f),
        }, 10), flowerMat, new Vector3(0.55f, 1.05f, 0.55f));
        return tree;
    }

    public static GameObject ShadeTree(Transform parent, Vector3 position, float size = 1f) =>
        BigTree(parent, position, "ShadeTree", size);

    // Broad tropical shade tree: flared trunk, three limbs and a faceted canopy of leaf clusters
    private static GameObject BigTree(Transform parent, Vector3 position, string name, float size)
    {
        var tree = PrimitiveKit.Group(name, parent, position);
        var t = tree.transform;
        var bark = PrimitiveKit.Mat("Bark", new Color(0.36f, 0.24f, 0.14f), 0.15f);
        var leaves = PrimitiveKit.Mat("Leaves", new Color(0.17f, 0.46f, 0.2f), 0.2f);
        var leavesLight = PrimitiveKit.Mat("LeavesLight", new Color(0.3f, 0.58f, 0.22f), 0.2f);
        var leavesDark = PrimitiveKit.Mat("LeavesDark", new Color(0.11f, 0.36f, 0.17f), 0.2f);

        MeshKit.Place("Trunk", t, MeshKit.Lathe("TreeTrunk", new[]
        {
            new Vector2(0.55f, 0f), new Vector2(0.38f, 0.15f), new Vector2(0.3f, 0.6f), new Vector2(0.26f, 1.6f),
            new Vector2(0.22f, 2.4f), new Vector2(0.17f, 2.9f),
        }, 12), bark, Vector3.zero, default, Vector3.one * size);
        var collider = tree.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.4f * size, 0f);
        collider.height = 2.8f * size;
        collider.radius = 0.3f * size;

        for (int i = 0; i < 3; i++)
        {
            float yaw = i * 120f + 20f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var limb = MeshKit.Curve(new Vector3(0f, 2.2f, 0f), new Vector3(0f, 3f, 0f) + dir * 0.4f, new Vector3(0f, 3.5f, 0f) + dir * 1.3f, 6);
            MeshKit.Place("Limb", t, MeshKit.Sweep("TreeLimb", limb, MeshKit.Taper(6, 0.16f, 0.07f), 6), bark, Vector3.zero, default, Vector3.one * size);
        }

        // Leaf clusters: a dome of faceted blobs in three greens
        var spots = new[]
        {
            new Vector4(0f, 4.3f, 0f, 2.6f), new Vector4(1.3f, 3.8f, 0.4f, 2f), new Vector4(-1.2f, 3.9f, 0.6f, 2.1f),
            new Vector4(0.3f, 3.7f, -1.3f, 2f), new Vector4(-0.6f, 3.5f, -0.9f, 1.7f), new Vector4(0.9f, 4.6f, -0.5f, 1.6f),
            new Vector4(-0.5f, 4.8f, 0.5f, 1.5f),
        };
        var mats = new[] { leaves, leavesLight, leavesDark };
        for (int i = 0; i < spots.Length; i++)
        {
            var sp = spots[i];
            MeshKit.Place("Leaves", t, MeshKit.Blob("Canopy" + (i % 4), 11 + i % 4, 0.22f, 1), mats[i % 3],
                new Vector3(sp.x, sp.y, sp.z) * size, new Vector3(0f, i * 47f, 0f), new Vector3(1f, 0.8f, 1f) * (sp.w * size));
        }
        return tree;
    }

    // Signpost (ป้ายบอกทาง): one lacquered post on a stone foot topped by a round medallion with a gold
    // rim and the place's picture on both faces (no lettering, so it stays small and tidy). Walking up to
    // it pops the detail card (SignPost → SignCardUI) with the name and the rest of the text.
    public static GameObject Sign(Transform parent, Vector3 position, string text, float yaw)
    {
        var sign = PrimitiveKit.Group("Sign", parent, position, yaw);
        var t = sign.transform;
        var lacquer = PrimitiveKit.Mat("SignLacquer", new Color(0.28f, 0.14f, 0.07f), 0.55f);
        var gold = PrimitiveKit.Mat("SignGold", new Color(1f, 0.78f, 0.3f), 0.8f, 0.85f);
        var stone = PrimitiveKit.Mat("SignStone", new Color(0.62f, 0.6f, 0.56f), 0.15f);
        var cream = PrimitiveKit.Mat("SignCream", new Color(0.98f, 0.93f, 0.8f), 0.2f);

        string[] lines = text.Split('\n');
        string title = lines[0];
        string sub = lines.Length > 1 ? string.Join("\n", lines, 1, lines.Length - 1) : "";

        const float centre = 1.75f, radius = 0.36f;
        PrimitiveKit.Box("Foot", t, new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.2f, 0.3f), stone);
        PrimitiveKit.Cylinder("Post", t, new Vector3(0f, (centre - radius) * 0.5f, 0f), new Vector3(0.1f, (centre - radius) * 0.5f, 0.1f), lacquer);
        PrimitiveKit.Cylinder("Ring", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.13f, 0.02f, 0.13f), gold, collider: false);
        PrimitiveKit.Cylinder("Collar", t, new Vector3(0f, centre - radius - 0.02f, 0f), new Vector3(0.16f, 0.04f, 0.16f), gold, collider: false);

        // Medallion: gold rim, dark lacquer disc, cream face, the picture on both sides
        PrimitiveKit.Cylinder("Rim", t, new Vector3(0f, centre, 0f), new Vector3(radius * 2f + 0.08f, 0.04f, radius * 2f + 0.08f), gold, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("Disc", t, new Vector3(0f, centre, 0f), new Vector3(radius * 2f, 0.05f, radius * 2f), lacquer, new Vector3(90f, 0f, 0f), collider: false);
        foreach (float side in new[] { -1f, 1f })
        {
            PrimitiveKit.Cylinder("Face", t, new Vector3(0f, centre, side * 0.052f), new Vector3(radius * 1.8f, 0.005f, radius * 1.8f), cream, new Vector3(90f, 0f, 0f), collider: false);
            var icon = PrimitiveKit.Create(PrimitiveType.Quad, "Icon", t, new Vector3(0f, centre, side * 0.058f), Vector3.one * radius * 1.45f,
                SignIcon(IconFor(text)), new Vector3(0f, side < 0 ? 0f : 180f, 0f), collider: false);
            icon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // Gold finial on top
        PrimitiveKit.Sphere("Finial", t, new Vector3(0f, centre + radius + 0.1f, 0f), Vector3.one * 0.1f, gold);
        PrimitiveKit.Box("Chofa", t, new Vector3(0f, centre + radius + 0.22f, 0f), new Vector3(0.035f, 0.16f, 0.035f), gold, collider: false);

        // Walk up to it and the detail card pops up on screen
        var zone = new GameObject("SignZone");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.4f, 2f, 3f);
        zone.AddComponent<SignPost>().Configure(title, sub);
        return sign;
    }

    // Which picture a sign shows, from what it says
    private static string IconFor(string text)
    {
        (string icon, string[] words)[] rules =
        {
            ("travel", new[] { "ป้ายรถ" }),
            ("school", new[] { "ห้องเรียน" }),
            ("barber", new[] { "ร้านตัดผม" }),
            ("tailor", new[] { "ร้านเสื้อผ้า" }),
            ("subj_craft", new[] { "ช่างซ่อม" }),
            ("subj_sports", new[] { "โค้ช" }),
            ("subj_logic", new[] { "ติวเตอร์" }),
            ("subj_computer", new[] { "คอมพิวเตอร์", "โปรแกรมเมอร์" }),
            ("boxing", new[] { "มวย", "กระสอบ", "ซ้อมเตะ" }),
            ("fair", new[] { "ประกวด", "งานวัด", "สอยดาว", "ก.ไก่" }),
            ("koknongna", new[] { "โคกหนองนา" }),
            ("farm", new[] { "เล้าไก่", "ยุ้งข้าว", "วัว", "ฟาร์ม", "นา" }),
            ("market", new[] { "ตลาด" }),
            ("music", new[] { "ผับ", "ดนตรี" }),
            ("shop", new[] { "รับซื้อ", "ร้าน" }),
            ("island", new[] { "เกาะ", "อ่าวมาหยา", "ชิงช้า", "สมบัติ", "สระบัว" }),
            ("fishing", new[] { "ท่าเรือ", "ตกปลา", "คลอง", "ปลา" }),
            ("sao", new[] { "ตำบล", "อบต" }),
            ("hospital", new[] { "รพ.", "อนามัย" }),
            ("school", new[] { "โรงเรียน" }),
            ("temple", new[] { "วัด" }),
        };
        foreach (var rule in rules)
            foreach (var w in rule.words)
                if (text.Contains(w)) return rule.icon;
        return "travel";
    }

    // Unlit, alpha-clipped picture material for a sign icon (saved per icon so it ships in the build)
    private static Material SignIcon(string icon)
    {
        string path = $"Assets/Art/Materials/SignIcon_{icon}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Branding/Icons/{icon}.png");
        material.SetTexture("_BaseMap", tex);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", 0.5f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        EditorUtility.SetDirty(material);
        return material;
    }

    // Two pillars and a beam with the place name, open in the middle
    private static void Gate(Transform parent, Vector3 position, string name, Material pillar, Material beam, Color textColor)
    {
        var gate = PrimitiveKit.Group("Gate", parent, position);
        PrimitiveKit.Box("PillarL", gate.transform, new Vector3(-2.2f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), pillar);
        PrimitiveKit.Box("PillarR", gate.transform, new Vector3(2.2f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), pillar);
        PrimitiveKit.Box("Beam", gate.transform, new Vector3(0f, 3.5f, 0f), new Vector3(5.4f, 0.8f, 0.3f), beam, collider: false);
        PrimitiveKit.Label(name, gate.transform, new Vector3(0f, 3.5f, -0.17f), 0.065f, textColor);
    }
}
