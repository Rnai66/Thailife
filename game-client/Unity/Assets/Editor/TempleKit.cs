using UnityEngine;

// Temple life for the temples of the towns and provinces (the village temple has its own, BuildVillage):
// a ศาลาบุญ beside each one — an open sala with a small gilded Buddha on its altar, the abbot receiving
// visitors (บวช · สึก · หมาวัด · กุมารทอง), two monks chanting on their mats, villagers kneeling before
// them, an incense urn and candles in front with people kneeling to pray, the ต้นกฐิน/ผ้าป่า money tree
// (ทำบุญ), a little amulet stall with its monk (พระเครื่อง), and a temple dog.
// DressAll runs once a scene's content is built (TambonBuilder.BuildLocation): it finds every temple by
// name and puts the kit on the nearest clear, flat patch of ground round it (no wall, stall, tree or
// building in the way), facing the way the temple faces.
public static partial class TempleLife
{
    // the kit's footprint in its own space: x ±3.4, z -5..3.1 (front at -Z)
    private static readonly Vector3 KitHalf = new Vector3(3.4f, 1.75f, 4.05f);
    private static readonly Vector3 KitCentre = new Vector3(0f, 0f, -0.95f);
    private static System.Collections.Generic.List<Bounds> paved;

    // Not Buddhist temples with monks: the Khmer stone sanctuaries, the Brahmin shrine, the wooden sanctuary
    private static readonly string[] NotMonastery = { "ปราสาทหิน", "ปราสาทเมืองต่ำ", "หอพระอิศวร", "ปราสาทสัจธรรม" };

    public static void DressAll(Transform root)
    {
        Physics.SyncTransforms();
        paved = PavedAreas(root);
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string name = t.name;
            bool temple = name.StartsWith("AttrTemple_") || name == "CityTemple_วัด" || name.StartsWith("WatArun_") || name.StartsWith("WatPhraKaew_");
            if (!temple) continue;
            bool skip = false;
            foreach (var s in NotMonastery) if (name.Contains(s)) skip = true;
            if (skip) continue;
            if (t.Find("TempleLife_ศาลาบุญ") != null) continue;
            if (FindSpot(t, out var at, out var rot))
            {
                Kit(t, at, rot, ++n);
                Physics.SyncTransforms();
            }
            else Debug.LogWarning($"[ThaiLife] No room for the temple life kit at {name}");
        }
        if (n > 0) Debug.Log($"[ThaiLife] Temple life added at {n} temple(s) in {root.name}");
    }

    // Put the kit at a chosen spot in the temple's own space (a temple whose grounds are laid out for it)
    public static void KitAt(Transform temple, Vector3 local, float yaw)
    {
        Kit(temple, temple.TransformPoint(local), temple.rotation * Quaternion.Euler(0f, yaw, 0f), 1);
    }

    // Road, pavement, paths and water have no colliders: find them by name so the kit never lands on them
    private static readonly System.Text.RegularExpressions.Regex Paved = new System.Text.RegularExpressions.Regex(
        "Road|Street|Lane|Asphalt|Kerb|Curb|Pavement|Crossing|Zebra|Rail|Track|Water|River|Sea|Pool|Path|Parking|Runway|Pier|Stair|Step",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static System.Collections.Generic.List<Bounds> PavedAreas(Transform root)
    {
        var list = new System.Collections.Generic.List<Bounds>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (Paved.IsMatch(r.name) && r.bounds.size.y < 3f) list.Add(r.bounds);
        return list;
    }

    private static bool GroundLike(Collider c, float baseY) =>
        c.name == "Ground" || (c.bounds.size.x > 60f && c.bounds.size.z > 60f) || c.bounds.max.y < baseY + 0.22f;

    private static bool FindSpot(Transform temple, out Vector3 at, out Quaternion rot)
    {
        rot = Quaternion.Euler(0f, temple.eulerAngles.y, 0f);
        float baseY = temple.position.y;
        var candidates = new System.Collections.Generic.List<Vector2>();
        for (float x = -45f; x <= 45f; x += 1f)
            for (float z = -45f; z <= 45f; z += 1f)
                if (x * x + z * z <= 45f * 45f) candidates.Add(new Vector2(x, z));
        candidates.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
        var hits = new Collider[32];
        foreach (var c in candidates)
        {
            at = temple.position + rot * new Vector3(c.x, 0f, c.y);
            var centre = at + rot * KitCentre + Vector3.up * (KitHalf.y + 0.25f);
            int count = Physics.OverlapBoxNonAlloc(centre, KitHalf + new Vector3(0.3f, 0f, 0.3f), hits, rot, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            for (int i = 0; i < count && !blocked; i++) blocked = !GroundLike(hits[i], baseY);
            if (blocked) continue;
            // nothing paved under it: no road, pavement, path, steps or water
            var area = new Bounds(at + rot * KitCentre, Vector3.zero);
            foreach (var k in new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) })
                area.Encapsulate(at + rot * (KitCentre + Vector3.Scale(k, KitHalf + new Vector3(0.4f, 0f, 0.4f))));
            area.Expand(new Vector3(0f, 1f, 0f));
            bool onPaving = false;
            if (paved != null)
                foreach (var b in paved)
                    if (b.Intersects(area)) { onPaving = true; break; }
            if (onPaving) continue;
            // flat ground under the corners and the middle, at the temple's own level
            bool flat = true;
            foreach (var k in new[] { new Vector3(0f, 0f, 0f), new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) })
            {
                var p = at + rot * (KitCentre + Vector3.Scale(k, KitHalf)) + Vector3.up * 3.8f;
                if (Physics.Raycast(p, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!GroundLike(hit.collider, baseY) || Mathf.Abs(hit.point.y - baseY) > 0.3f) { flat = false; break; }
                }
            }
            if (!flat) continue;
            return true;
        }
        at = default;
        return false;
    }

    private static void Kit(Transform temple, Vector3 at, Quaternion rot, int seed)
    {
        var g = PrimitiveKit.Group("TempleLife_ศาลาบุญ", temple, Vector3.zero).transform;
        g.SetPositionAndRotation(at, rot);
        var wood = PrimitiveKit.Mat("WoodPlank", new Color(0.55f, 0.35f, 0.2f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.3f, 0.18f, 0.1f));
        var white = PrimitiveKit.Mat("TempleWhite", new Color(0.95f, 0.94f, 0.9f), 0.35f);
        var orange = PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f), 0.6f);
        var green = PrimitiveKit.Mat("TempleGreen", new Color(0.15f, 0.45f, 0.3f), 0.6f);
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f), 0.7f, 0.85f);
        var lacquer = PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f);
        var mat = PrimitiveKit.Mat("MeditationMat", new Color(0.85f, 0.7f, 0.4f));

        // ── the sala ──
        const float floorTop = 0.35f;
        PrimitiveKit.Box("SalaFloor", g, new Vector3(0f, floorTop * 0.5f, 1f), new Vector3(5f, floorTop, 3.4f), wood);
        PrimitiveKit.Box("SalaStep", g, new Vector3(0f, 0.09f, -0.9f), new Vector3(1.6f, 0.18f, 0.4f), wood);
        foreach (var p in new[] { new Vector2(-2.3f, -0.55f), new Vector2(2.3f, -0.55f), new Vector2(-2.3f, 2.55f), new Vector2(2.3f, 2.55f), new Vector2(0f, 2.55f) })
            PrimitiveKit.Cylinder("Post", g, new Vector3(p.x, floorTop + 1.32f, p.y), new Vector3(0.16f, 1.32f, 0.16f), white);
        PrimitiveKit.Box("RoofFront", g, new Vector3(0f, 3.38f, 0f), new Vector3(5.8f, 0.1f, 2.35f), orange, new Vector3(-30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", g, new Vector3(0f, 3.38f, 2f), new Vector3(5.8f, 0.1f, 2.35f), green, new Vector3(30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Ridge", g, new Vector3(0f, 3.97f, 1f), new Vector3(5.9f, 0.1f, 0.12f), gold, collider: false);
        foreach (float sx in new[] { -1f, 1f }) // ช่อฟ้า
            PrimitiveKit.Box("Chofa", g, new Vector3(sx * 2.95f, 4.2f, 1f), new Vector3(0.08f, 0.5f, 0.08f), gold, new Vector3(0f, 0f, sx * -25f), collider: false);
        PrimitiveKit.Box("BackScreen", g, new Vector3(0f, floorTop + 1.1f, 2.62f), new Vector3(4.8f, 2.2f, 0.06f), PrimitiveKit.Mat("SalaMural", new Color(0.72f, 0.2f, 0.12f), 0.4f), collider: false);

        // the altar and its Buddha, flowers either side
        PrimitiveKit.Box("Altar", g, new Vector3(0f, floorTop + 0.3f, 2.25f), new Vector3(1.6f, 0.6f, 0.6f), lacquer, collider: false);
        PrimitiveKit.Box("AltarTop", g, new Vector3(0f, floorTop + 0.62f, 2.25f), new Vector3(1.7f, 0.04f, 0.68f), gold, collider: false);
        BuddhaStatue.Build(g, new Vector3(0f, floorTop + 0.72f, 2.3f), 0.42f, detail: false);
        var lotus = PrimitiveKit.Mat("LotusPink", new Color(0.95f, 0.55f, 0.7f), 0.4f);
        foreach (float sx in new[] { -0.65f, 0.65f })
        {
            PrimitiveKit.Cylinder("Vase", g, new Vector3(sx, floorTop + 0.76f, 2.05f), new Vector3(0.12f, 0.12f, 0.12f), gold, collider: false);
            PrimitiveKit.Sphere("Lotus", g, new Vector3(sx, floorTop + 0.98f, 2.05f), new Vector3(0.16f, 0.2f, 0.16f), lotus);
        }

        // the abbot receiving visitors (front left), two monks chanting on their mats (right)
        PrimitiveKit.Box("AbbotMat", g, new Vector3(-1.3f, floorTop + 0.01f, 0.75f), new Vector3(1.1f, 0.02f, 0.9f), mat, collider: false);
        SeatedMonk(g, new Vector3(-1.3f, floorTop + 0.02f, 0.8f), 0f, abbot: true);
        foreach (float x in new[] { 0.6f, 1.6f })
        {
            PrimitiveKit.Box("MonkMat", g, new Vector3(x, floorTop + 0.01f, 1.55f), new Vector3(0.85f, 0.02f, 0.8f), mat, collider: false);
            SeatedMonk(g, new Vector3(x, floorTop + 0.02f, 1.6f), 0f);
        }
        var abbot = PrimitiveKit.Group("AbbotCounter", g, new Vector3(-1.3f, 1f, -1.25f));
        var abox = abbot.AddComponent<BoxCollider>();
        abox.isTrigger = true;
        abox.size = new Vector3(2f, 2f, 1.6f);
        abbot.AddComponent<TempleCounter>().Configure(TempleUI.Abbot);
        Places.Sign(g, new Vector3(-3f, 0f, -0.9f), "กราบเจ้าอาวาส\nบวช · สึก · หมาวัด · กุมารทอง", 0f);

        // a temple without a walk-in ubosot: meditate here in the sala, before its Buddha
        bool hasUbosot = false;
        foreach (var c in temple.GetComponentsInChildren<Transform>(true)) if (c.name == "Ubosot_โบสถ์") { hasUbosot = true; break; }
        if (!hasUbosot) Meditation(g, new Vector3(-0.35f, floorTop, 0.6f));

        // ── the incense urn and candles in front ──
        var urn = PrimitiveKit.Group("IncenseStand_กระถางธูป", g, new Vector3(0f, 0f, -2.3f)).transform;
        PrimitiveKit.Box("Stand", urn, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.5f), lacquer);
        PrimitiveKit.Cylinder("Urn", urn, new Vector3(0f, 0.82f, 0f), new Vector3(0.36f, 0.12f, 0.36f), gold, collider: false);
        PrimitiveKit.Cylinder("Ash", urn, new Vector3(0f, 0.945f, 0f), new Vector3(0.3f, 0.005f, 0.3f), PrimitiveKit.Mat("IncenseAsh", new Color(0.7f, 0.68f, 0.64f)), collider: false);
        var stick = PrimitiveKit.Mat("IncenseStick", new Color(0.55f, 0.15f, 0.1f));
        var ember = PrimitiveKit.Glow("IncenseEmber", new Color(1f, 0.35f, 0.1f), 4f);
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.035f, z = ((i * 5) % 3 - 1) * 0.04f;
            PrimitiveKit.Box("Incense", urn, new Vector3(x, 1.08f, z), new Vector3(0.01f, 0.26f, 0.01f), stick, new Vector3(0f, 0f, (i - 3) * 5f), collider: false);
            PrimitiveKit.Sphere("Ember", urn, new Vector3(x - (i - 3) * 0.011f, 1.21f, z), Vector3.one * 0.018f, ember);
        }
        var candle = PrimitiveKit.Mat("CandleYellow", new Color(1f, 0.9f, 0.5f));
        var flame = PrimitiveKit.Glow("CandleGlow", new Color(1f, 0.7f, 0.35f), 2f);
        foreach (float sx in new[] { -0.35f, 0.35f })
        {
            PrimitiveKit.Cylinder("Candle", urn, new Vector3(sx, 0.85f, 0.05f), new Vector3(0.05f, 0.14f, 0.05f), candle, collider: false);
            PrimitiveKit.Sphere("Flame", urn, new Vector3(sx, 1.03f, 0.05f), new Vector3(0.04f, 0.08f, 0.04f), flame);
        }
        PrimitiveKit.Sphere("Garland", urn, new Vector3(0f, 0.72f, -0.26f), new Vector3(0.3f, 0.06f, 0.08f), PrimitiveKit.Mat("JasmineGarland", new Color(0.97f, 0.97f, 0.92f)));

        // ── merit: the money tree on the left, the amulet stall on the right ──
        MeritTree(g, new Vector3(-2.5f, 0f, -2.6f), 0f);
        var stall = PrimitiveKit.Group("AmuletStall_พระเครื่อง", g, new Vector3(2.5f, 0f, -2.4f)).transform;
        var velvet = PrimitiveKit.Mat("AmuletVelvet", new Color(0.55f, 0.06f, 0.08f), 0.3f);
        PrimitiveKit.Box("Counter", stall, new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.8f, 0.55f), dark);
        PrimitiveKit.Box("Velvet", stall, new Vector3(0f, 0.81f, 0f), new Vector3(1.42f, 0.02f, 0.48f), velvet, collider: false);
        PrimitiveKit.Box("Glass", stall, new Vector3(0f, 0.93f, 0f), new Vector3(1.5f, 0.22f, 0.55f), PrimitiveKit.Mat("DisplayGlass", new Color(0.8f, 0.92f, 0.95f), 0.9f), collider: false);
        var frames = new[] { gold, PrimitiveKit.Mat("SilverFrame", new Color(0.8f, 0.82f, 0.85f), 0.7f, 0.8f), PrimitiveKit.Mat("ClayAmulet", new Color(0.62f, 0.5f, 0.38f)) };
        for (int c = 0; c < 6; c++)
            PrimitiveKit.Cylinder("Amulet", stall, new Vector3(-0.55f + c * 0.22f, 0.83f, (c % 2) * 0.12f - 0.06f), new Vector3(0.12f, 0.01f, 0.16f), frames[c % 3], collider: false);
        PrimitiveKit.Box("Stool", stall, new Vector3(0f, 0.15f, 0.75f), new Vector3(0.6f, 0.3f, 0.5f), dark, collider: false);
        SeatedMonk(stall, new Vector3(0f, 0.3f, 0.78f), 0f);
        var amulets = PrimitiveKit.Group("AmuletCounter", stall, new Vector3(0f, 1f, -1f));
        var mbox = amulets.AddComponent<BoxCollider>();
        mbox.isTrigger = true;
        mbox.size = new Vector3(1.8f, 2f, 1.4f);
        amulets.AddComponent<TempleCounter>().Configure(TempleUI.Amulets);
        PrimitiveKit.Label("พระเครื่อง", stall, new Vector3(0f, 1.15f, -0.29f), 0.03f, new Color(1f, 0.85f, 0.4f));

        // ── people: two kneeling before the monks, two at the incense urn (in the day) ──
        Npcs.Watchers(g, "Worshippers_คนไหว้พระ", Vector3.zero, 0f,
            new[] { new Vector3(0.6f, floorTop, 0.2f), new Vector3(1.6f, floorTop, 0.25f), new Vector3(-0.38f, 0f, -3.05f), new Vector3(0.38f, 0f, -3.1f) },
            new[] { 0f, 4f, -3f, 3f },
            new[] { SpectatorPose.Kind.Kneel, SpectatorPose.Kind.Kneel, SpectatorPose.Kind.Kneel, SpectatorPose.Kind.Kneel },
            "", 6, 20);

        // and a temple dog lazing about out front
        TempleDog(g, new Vector3(0f, 0f, -4.3f), new Vector2(2f, 0.5f), seed % 2 == 0 ? new Color(0.78f, 0.55f, 0.3f) : new Color(0.2f, 0.18f, 0.16f), seed + 3);
    }
}
