using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// On the road in your own vehicle (FuelUI → ขับไปที่อื่น): the vehicle you set off in drives along a
// country road (in a little world of its own, far above the map, drawn by its own camera onto the
// screen) — past palms, paddies, stilt houses and power poles; it swerves round the ghosts standing
// in the road, slows through police checkpoints, overtakes slower traffic when the other lane is clear
// and gets out of the way of the tuk-tuks and cars coming the other way. Over it: where to, the route
// with a turning ดอกพิกุล on it, the kilometres and the time to go. The trip takes the game clock's time
// for the distance (a game hour is 90 real seconds), then the new map loads with you and the vehicle
// at its petrol station (ParkedCarSpawner).
public class DriveJourney : MonoBehaviour
{
    private const float Lane = 1.75f;          // lane centre from the middle of the road
    private const float OurLane = -Lane;       // Thailand drives on the left
    private const float OtherLane = Lane;
    private const float TileLength = 30f;
    private const int Tiles = 9;
    private static readonly Vector3 Origin = new Vector3(0f, 3000f, 0f);

    private enum Kind { Ghost, Slow, Oncoming, Checkpoint }

    private class Thing
    {
        public Kind kind;
        public Transform t;
        public float x, z, speed, baseSpeed, half, bob;
        public string name;
        public bool told;
        public PoliceOfficer officer;
    }

    private static DriveJourney running;

    private Location destination;
    private FuelClient.Drive drive;
    private string carId, carName;
    private Material template;
    private Look riderLook;
    private bool hasRider;

    // the little world
    private Transform world, car, camT;
    private Camera cam;
    private RenderTexture target;
    private readonly List<Camera> muted = new List<Camera>();
    private readonly List<Transform> tiles = new List<Transform>();
    private readonly List<Thing> things = new List<Thing>();
    private readonly List<Transform> scenery = new List<Transform>();
    private readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    private float carZ, carX = OurLane, laneTarget = OurLane, speed, cruise, yaw;
    private float nextEvent = 90f, nextScenery, nextPole, nextCheckpoint = 260f, nextOncoming;
    private Thing overtaking;
    private bool night;

    // the screen over it
    private Text leftText, timeText, speedText, eventText;
    private RectTransform fill, marker;
    private float lineW, eventUntil;

    public static bool Busy => running != null;

    public static void Begin(Location to, FuelClient.Drive d, DrivableCar car)
    {
        if (running != null || to == null || car == null) return;
        var go = new GameObject("DriveJourney");
        DontDestroyOnLoad(go);
        running = go.AddComponent<DriveJourney>();
        running.destination = to;
        running.drive = d;
        running.carId = car.CarId;
        running.carName = car.CarName;
        var player = FindAnyObjectByType<PlayerController>();
        running.template = player != null ? player.GetComponent<Renderer>()?.sharedMaterial : null;
        var model = player != null ? player.GetComponentInChildren<CharacterModel>() : null;
        if (model != null) { running.riderLook = model.CurrentLook; running.hasRider = true; }
        // the vehicle comes along: it's at the far end from now on, even if the app closes on the way
        // (after LeaveWithPlayer, which clears where it was parked)
        car.LeaveWithPlayer();
        ParkedCar.SendTo(to.Scene, running.carId, running.carName);
        running.night = DayNightCycle.Dark;
        running.BuildWorld();
        running.BuildScreen();
        running.StartCoroutine(running.Run());
    }

    // ── the little world ────────────────────────────────────────────────────────────────────────
    private Material Mat(Color c)
    {
        if (mats.TryGetValue(c, out var m) && m != null) return m;
        m = template != null ? new Material(template) : null;
        if (m != null) m.color = c;
        mats[c] = m;
        return m;
    }

    private Transform Part(PrimitiveType type, Transform parent, Vector3 at, Vector3 scale, Color color, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        var m = Mat(color);
        if (m != null) r.sharedMaterial = m;
        else r.material.color = color;
        return go.transform;
    }

    private Transform Group(string name, float x, float z)
    {
        var t = new GameObject(name).transform;
        t.SetParent(world, false);
        t.localPosition = new Vector3(x, 0f, z);
        return t;
    }

    private void BuildWorld()
    {
        world = new GameObject("Road").transform;
        world.SetParent(transform, false);
        world.position = Origin;

        // the camera draws into a texture shown under the card (over the game's own screens)
        var camGo = new GameObject("JourneyCamera");
        camGo.transform.SetParent(transform, false);
        camT = camGo.transform;
        cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        // the haze the map uses (fog) fades the far road into the same colour as the sky
        cam.backgroundColor = RenderSettings.fog ? RenderSettings.fogColor : night ? new Color(0.06f, 0.08f, 0.15f) : new Color(0.62f, 0.8f, 0.97f);
        cam.fieldOfView = UiKit.Portrait ? 62f : 50f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 320f;
        cam.depth = 50f;
        target = new RenderTexture(Mathf.Max(64, Screen.width), Mathf.Max(64, Screen.height), 24) { antiAliasing = 2 };
        cam.targetTexture = target;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.renderShadows = true;
        foreach (var c in Camera.allCameras)
            if (c != cam && c.enabled) { c.enabled = false; muted.Add(c); }

        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(transform, false);
        sunGo.transform.rotation = night ? Quaternion.Euler(55f, 150f, 0f) : Quaternion.Euler(42f, -35f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = night ? new Color(0.55f, 0.62f, 0.9f) : new Color(1f, 0.95f, 0.85f);
        sun.intensity = night ? 0.5f : 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.6f;
        // the sky: soft clouds (or a moon) hanging far off in front of the camera
        for (int i = 0; i < 7; i++)
        {
            var sr = new GameObject("Cloud").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(camT, false);
            sr.sprite = PikulArt.Glow;
            sr.color = night ? new Color(0.5f, 0.55f, 0.75f, 0.25f) : new Color(1f, 1f, 1f, 0.85f);
            sr.transform.localPosition = new Vector3(Random.Range(-160f, 160f), Random.Range(45f, 95f), 290f);
            sr.transform.localScale = new Vector3(Random.Range(90f, 170f), Random.Range(26f, 44f), 1f);
        }
        if (night)
        {
            var moon = new GameObject("Moon").AddComponent<SpriteRenderer>();
            moon.transform.SetParent(camT, false);
            moon.sprite = UiKit.CircleSprite;
            moon.color = new Color(1f, 0.97f, 0.85f);
            moon.transform.localPosition = new Vector3(70f, 85f, 295f);
            moon.transform.localScale = Vector3.one * 16f;
        }

        // the road: tiles that leap ahead as they fall behind
        for (int i = 0; i < Tiles; i++) tiles.Add(RoadTile(i * TileLength));

        // the vehicle (with you on it, on two wheels)
        var carRoot = new GameObject("MyCar");
        carRoot.transform.SetParent(world, false);
        car = carRoot.transform;
        CarModels.Build(carId, car, template);
        bool bike = CarModels.TwoWheels(carId);
        if (bike && hasRider)
        {
            var rider = new GameObject("Rider");
            rider.transform.SetParent(car, false);
            rider.transform.localPosition = Vector3.up * 1f; // CharacterModel's feet are 1 m under it
            rider.AddComponent<CharacterModel>().SetLook(riderLook);
            rider.AddComponent<SeatedPose>().Ride = true;
        }
        carRoot.AddComponent<CarSway>().Configure(bike, spin: true);
        carRoot.AddComponent<CarHeadlights>().Configure(bike);
        cruise = (carId == "car_bicycle" ? 18f : carId == "car_motorbike" ? 60f : carId == "car_pickup" ? 80f : carId == "car_sedan" ? 85f : 90f) / 3.6f;
        speed = cruise * 0.7f;
        nextOncoming = 6f;
        PlaceCamera(1f);
    }

    private Transform RoadTile(float z)
    {
        var t = Group("Tile", 0f, z);
        var asphalt = night ? new Color(0.16f, 0.16f, 0.18f) : new Color(0.24f, 0.24f, 0.26f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, -0.05f, TileLength * 0.5f), new Vector3(Lane * 4f + 0.4f, 0.1f, TileLength + 0.05f), asphalt);
        foreach (float s in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(s * (Lane * 2f + 0.9f), -0.06f, TileLength * 0.5f), new Vector3(1.4f, 0.1f, TileLength), new Color(0.62f, 0.58f, 0.5f)); // shoulder
            Part(PrimitiveType.Cube, t, new Vector3(s * (Lane * 2f - 0.05f), 0.005f, TileLength * 0.5f), new Vector3(0.14f, 0.02f, TileLength), Color.white);
            var grass = Color.Lerp(new Color(0.33f, 0.58f, 0.22f), new Color(0.42f, 0.64f, 0.26f), Random.value);
            Part(PrimitiveType.Cube, t, new Vector3(s * 34f, -0.1f, TileLength * 0.5f), new Vector3(60f, 0.1f, TileLength + 0.05f), grass);
        }
        for (float d = 2f; d < TileLength; d += 7.5f)
            Part(PrimitiveType.Cube, t, new Vector3(0f, 0.005f, d), new Vector3(0.14f, 0.02f, 3f), Color.white);
        return t;
    }

    // ── scenery by the road ──
    private void SpawnScenery(float z)
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        float x = side * Random.Range(7.5f, 26f);
        float pick = Random.value;
        Transform t;
        if (pick < 0.32f) t = Palm(x, z);
        else if (pick < 0.55f) t = Tree(x, z);
        else if (pick < 0.7f) t = Paddy(side * Random.Range(14f, 30f), z);
        else if (pick < 0.82f) t = StiltHouse(side * Random.Range(12f, 24f), z, side);
        else t = Bush(x, z);
        scenery.Add(t);
    }

    private Transform Palm(float x, float z)
    {
        var t = Group("Palm", x, z);
        float h = Random.Range(5.5f, 8f);
        var lean = new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
        var trunk = Part(PrimitiveType.Cylinder, t, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.28f, h * 0.5f, 0.28f), new Color(0.5f, 0.4f, 0.3f), lean);
        var top = t.InverseTransformPoint(trunk.TransformPoint(Vector3.up));
        for (int i = 0; i < 7; i++)
            Part(PrimitiveType.Sphere, t, top + Quaternion.Euler(0f, i * 51f, 0f) * new Vector3(0f, -0.2f, 1.1f), new Vector3(0.7f, 0.18f, 2.6f), new Color(0.2f, 0.5f, 0.18f), new Vector3(25f, i * 51f, 0f));
        return t;
    }

    private Transform Tree(float x, float z)
    {
        var t = Group("Tree", x, z);
        float s = Random.Range(0.8f, 1.5f);
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 1.2f * s, 0f), new Vector3(0.35f, 1.2f * s, 0.35f), new Color(0.42f, 0.3f, 0.2f));
        var leaf = Random.value < 0.5f ? new Color(0.16f, 0.42f, 0.16f) : new Color(0.28f, 0.52f, 0.2f);
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 3.2f * s, 0f), new Vector3(3.6f, 3f, 3.6f) * s, leaf);
        Part(PrimitiveType.Sphere, t, new Vector3(0.8f * s, 2.6f * s, 0.5f * s), new Vector3(2.4f, 2f, 2.4f) * s, leaf);
        return t;
    }

    private Transform Bush(float x, float z)
    {
        var t = Group("Bush", x, z);
        var c = Random.value < 0.3f ? new Color(0.85f, 0.25f, 0.55f) : new Color(0.25f, 0.5f, 0.2f); // bougainvillea or green
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1.1f, 1.6f), c);
        return t;
    }

    private Transform Paddy(float x, float z)
    {
        var t = Group("Paddy", x, z);
        bool ripe = Random.value < 0.4f;
        var dike = new Color(0.5f, 0.42f, 0.3f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, -0.02f, 0f), new Vector3(16f, 0.08f, 12f), ripe ? new Color(0.85f, 0.75f, 0.3f) : new Color(0.45f, 0.75f, 0.3f));
        foreach (float s in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(s * 8f, 0.1f, 0f), new Vector3(0.5f, 0.25f, 12f), dike);
            Part(PrimitiveType.Cube, t, new Vector3(0f, 0.1f, s * 6f), new Vector3(16f, 0.25f, 0.5f), dike);
        }
        if (Random.value < 0.5f) // a scarecrow
        {
            var pole = new Color(0.45f, 0.35f, 0.2f);
            Part(PrimitiveType.Cylinder, t, new Vector3(2f, 0.9f, 1f), new Vector3(0.08f, 0.9f, 0.08f), pole);
            Part(PrimitiveType.Cube, t, new Vector3(2f, 1.4f, 1f), new Vector3(1.2f, 0.08f, 0.08f), pole);
            Part(PrimitiveType.Sphere, t, new Vector3(2f, 1.95f, 1f), new Vector3(0.7f, 0.25f, 0.7f), new Color(0.85f, 0.75f, 0.45f));
        }
        return t;
    }

    private Transform StiltHouse(float x, float z, float side)
    {
        var t = Group("House", x, z);
        t.localRotation = Quaternion.Euler(0f, side > 0f ? -90f : 90f, 0f);
        var wood = new Color(0.55f, 0.38f, 0.24f);
        var post = new Color(0.44f, 0.3f, 0.19f);
        foreach (float a in new[] { -2f, 2f })
            foreach (float b in new[] { -1.6f, 1.6f })
                Part(PrimitiveType.Cube, t, new Vector3(a, 1.1f, b), new Vector3(0.22f, 2.2f, 0.22f), post);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 3.2f, 0f), new Vector3(4.8f, 2f, 3.8f), wood);
        var roof = Random.value < 0.5f ? new Color(0.7f, 0.25f, 0.18f) : new Color(0.55f, 0.58f, 0.62f);
        foreach (float s in new[] { -1f, 1f })
            Part(PrimitiveType.Cube, t, new Vector3(0f, 4.75f, s * 1.1f), new Vector3(5.6f, 0.15f, 2.7f), roof, new Vector3(s * 35f, 0f, 0f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 3.2f, -1.92f), new Vector3(1f, 1.2f, 0.05f), new Color(0.3f, 0.2f, 0.12f)); // window
        return t;
    }

    // a power pole on the left verge, its lamp lit after dark
    private void SpawnPole(float z)
    {
        var t = Group("Pole", -(Lane * 2f + 2.2f), z);
        var grey = new Color(0.4f, 0.4f, 0.4f);
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 4f, 0f), new Vector3(0.22f, 4f, 0.22f), new Color(0.75f, 0.74f, 0.7f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 7.4f, 0f), new Vector3(0.14f, 0.14f, 1.6f), grey);
        Part(PrimitiveType.Cube, t, new Vector3(1.2f, 6.6f, 0f), new Vector3(2.4f, 0.1f, 0.1f), grey);
        Part(PrimitiveType.Cube, t, new Vector3(2.3f, 6.5f, 0f), new Vector3(0.5f, 0.12f, 0.25f), night ? new Color(1f, 0.92f, 0.7f) : new Color(0.85f, 0.85f, 0.8f));
        if (night)
        {
            var lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.transform.SetParent(t, false);
            lamp.transform.localPosition = new Vector3(2.3f, 6.3f, 0f);
            lamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lamp.type = LightType.Spot;
            lamp.spotAngle = 120f;
            lamp.range = 14f;
            lamp.intensity = 30f;
            lamp.color = new Color(1f, 0.82f, 0.55f);
        }
        scenery.Add(t);
    }

    // ── what happens on the road ──
    private static readonly (string id, string name)[] GhostKinds =
    {
        ("krasue", "ผีกระสือ"), ("krahang", "ผีกระหัง"), ("pop", "ผีปอบ"), ("tani", "ผีนางตานี"),
        ("takhian", "ผีนางตะเคียน"), ("phrai", "ผีพราย"), ("pret", "เปรต"), ("nak", "แม่นาก"),
    };

    private void SpawnEvent(float z)
    {
        bool bicycle = carId == "car_bicycle";
        float r = Random.value;
        if (z >= nextCheckpoint && r < 0.22f) { SpawnCheckpoint(z); nextCheckpoint = z + Random.Range(700f, 1100f); return; }
        if (r < 0.5f || bicycle) SpawnGhost(z, Random.value < 0.8f ? OurLane : OtherLane);
        else SpawnSlow(z);
    }

    private void SpawnGhost(float z, float lane)
    {
        var (id, name) = GhostKinds[Random.Range(0, GhostKinds.Length)];
        var t = Group("Ghost_" + id, lane, z);
        var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(t, false);
        glow.sprite = PikulArt.Glow;
        glow.color = new Color(0.55f, 1f, 0.7f, night ? 0.55f : 0.35f);
        glow.transform.localPosition = new Vector3(0f, 1.5f, 0.05f);
        glow.transform.localScale = Vector3.one * 4.2f;
        var art = new GameObject("Art").AddComponent<SpriteRenderer>();
        art.transform.SetParent(t, false);
        art.sprite = GhostArt.Get(id);
        art.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        art.transform.localScale = Vector3.one * 1.25f; // 256 px at 100 px/m: about 3.2 m
        things.Add(new Thing { kind = Kind.Ghost, t = t, x = lane, z = z, half = 0.8f, name = name, bob = Random.value * 6f });
    }

    private void SpawnSlow(float z)
    {
        var t = Group("Slow", OurLane, z);
        string name;
        float half;
        if (Random.value < 0.45f) { TukTuk(t); name = "ตุ๊กตุ๊ก"; half = 1.5f; }
        else
        {
            string id = Random.value < 0.5f ? "car_pickup" : "car_sedan";
            CarModels.Build(id, t, template);
            name = id == "car_pickup" ? "รถกระบะ" : "รถเก๋ง";
            half = 2.2f;
        }
        float v = Mathf.Min(cruise * 0.55f, Random.Range(7f, 11f));
        things.Add(new Thing { kind = Kind.Slow, t = t, x = OurLane, z = z, speed = v, baseSpeed = v, half = half, name = name });
    }

    private void SpawnOncoming(float z)
    {
        var t = Group("Oncoming", OtherLane, z);
        t.localRotation = Quaternion.Euler(0f, 180f, 0f);
        string name;
        float half;
        if (Random.value < 0.6f) { TukTuk(t); name = "ตุ๊กตุ๊ก"; half = 1.5f; }
        else
        {
            string[] ids = { "car_pickup", "car_sedan", "car_luxury" };
            CarModels.Build(ids[Random.Range(0, ids.Length)], t, template);
            name = "รถ";
            half = 2.2f;
        }
        t.gameObject.AddComponent<CarHeadlights>().Configure(false);
        float v = -Random.Range(11f, 16f);
        things.Add(new Thing { kind = Kind.Oncoming, t = t, x = OtherLane, z = z, speed = v, baseSpeed = v, half = half, name = name });
    }

    // a tuk-tuk: a bright cab up front, the bench behind, a striped canopy on top, three wheels
    private void TukTuk(Transform t)
    {
        Color[] paints = { new Color(0.1f, 0.35f, 0.8f), new Color(0.1f, 0.6f, 0.3f), new Color(0.85f, 0.15f, 0.12f), new Color(0.95f, 0.65f, 0.1f) };
        var paint = paints[Random.Range(0, paints.Length)];
        var tyre = new Color(0.1f, 0.1f, 0.11f);
        var chrome = new Color(0.75f, 0.77f, 0.8f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.55f, 0.75f), new Vector3(0.9f, 0.7f, 1.1f), paint);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.6f, -0.55f), new Vector3(1.4f, 0.75f, 1.5f), paint);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.05f, -0.75f), new Vector3(1.3f, 0.12f, 0.9f), new Color(0.3f, 0.2f, 0.15f)); // the bench
        foreach (float x in new[] { -0.68f, 0.68f })
            foreach (float z in new[] { -1.25f, 0.2f })
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.4f, z), new Vector3(0.06f, 0.9f, 0.06f), chrome);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.9f, -0.35f), new Vector3(1.55f, 0.12f, 2.5f), paint);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.82f, -0.35f), new Vector3(1.57f, 0.06f, 2.52f), Color.white);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.25f, 1.2f), new Vector3(1.1f, 0.65f, 0.05f), new Color(0.45f, 0.62f, 0.75f)); // windscreen
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 0.75f, 1.32f), new Vector3(0.22f, 0.22f, 0.1f), new Color(1f, 0.95f, 0.7f));
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 0.3f, 1.05f), new Vector3(0.55f, 0.08f, 0.55f), tyre, new Vector3(0f, 0f, 90f));
        foreach (float x in new[] { -0.72f, 0.72f })
            Part(PrimitiveType.Cylinder, t, new Vector3(x, 0.3f, -0.9f), new Vector3(0.55f, 0.08f, 0.55f), tyre, new Vector3(0f, 0f, 90f));
    }

    // ด่านตรวจ: cones down the edge and the middle, a sign, a tent and an officer by the road
    private void SpawnCheckpoint(float z)
    {
        var t = Group("Checkpoint", 0f, z);
        var cone = new Color(0.98f, 0.45f, 0.08f);
        for (float d = -12f; d <= 12f; d += 3f)
            foreach (float x in new[] { -(Lane * 2f - 0.3f), 0f })
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(x, 0.32f, d), new Vector3(0.26f, 0.3f, 0.26f), cone);
                Part(PrimitiveType.Cylinder, t, new Vector3(x, 0.42f, d), new Vector3(0.24f, 0.05f, 0.24f), Color.white);
            }
        var sign = new GameObject("Sign").transform;
        sign.SetParent(t, false);
        sign.localPosition = new Vector3(-(Lane * 2f + 1.2f), 0f, -22f);
        sign.localRotation = Quaternion.Euler(0f, 180f, 0f); // faces the traffic coming up to it
        Part(PrimitiveType.Cube, sign, new Vector3(0f, 1.3f, 0f), new Vector3(2f, 1.2f, 0.06f), new Color(0.98f, 0.82f, 0.1f));
        Part(PrimitiveType.Cube, sign, new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), Color.white);
        var label = SignText(sign, "ด่านตรวจ\nชะลอ 20", new Vector3(0f, 1.3f, 0.05f));
        label.color = new Color(0.1f, 0.1f, 0.1f);
        var tent = new GameObject("Tent").transform;
        tent.SetParent(t, false);
        tent.localPosition = new Vector3(-(Lane * 2f + 4f), 0f, 0f);
        foreach (float a in new[] { -1.4f, 1.4f })
            foreach (float b in new[] { -1.8f, 1.8f })
                Part(PrimitiveType.Cube, tent, new Vector3(a, 1.2f, b), new Vector3(0.08f, 2.4f, 0.08f), Color.white);
        Part(PrimitiveType.Cube, tent, new Vector3(0f, 2.5f, 0f), new Vector3(3.2f, 0.15f, 4f), new Color(0.36f, 0.25f, 0.16f));
        var officer = PoliceOfficer.Make(t, Origin + new Vector3(-(Lane * 2f + 1f), 0f, z + 2f), 90f, Random.Range(0, 9999));
        things.Add(new Thing { kind = Kind.Checkpoint, t = t, x = 0f, z = z, half = 12f, name = "ด่านตรวจ", officer = officer });
    }

    private static TextMesh SignText(Transform parent, string text, Vector3 at)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = ThaiFont.Display;
        go.GetComponent<MeshRenderer>().sharedMaterial = ThaiFont.Display.material;
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = 0.04f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        return mesh;
    }

    // ── driving ──
    // Nothing in `lane` between z0 and z1
    private bool LaneClear(float lane, float z0, float z1)
    {
        foreach (var th in things)
        {
            if (th.kind == Kind.Checkpoint || Mathf.Abs(th.x - lane) > 1.4f) continue;
            if (th.z + th.half > z0 && th.z - th.half < z1) return false;
        }
        return true;
    }

    // The other lane free long enough to get past `what`: nothing coming that would meet us on the way
    private bool PassClear(Thing what)
    {
        float gap = what.z - carZ + what.half + 8f;
        float closing = Mathf.Max(2f, cruise - what.speed);
        float time = Mathf.Min(7f, gap / closing + 1f);
        foreach (var th in things)
        {
            if (th == what || th.kind == Kind.Checkpoint || Mathf.Abs(th.x - OtherLane) > 1.4f) continue;
            float reach = th.kind == Kind.Oncoming ? gap + 12f - th.speed * time : gap + 6f;
            if (th.z > carZ - 6f && th.z - carZ < reach) return false;
        }
        return true;
    }

    private void Say(string text, Color color)
    {
        eventText.text = text;
        eventText.color = color;
        eventUntil = Time.unscaledTime + 2.2f;
    }

    private void Drive(float dt)
    {
        float want = cruise;

        // the things on the road move (oncoming traffic slows for us in its lane)
        foreach (var th in things)
        {
            if (th.kind == Kind.Oncoming)
            {
                bool usInWay = Mathf.Abs(carX - th.x) < 1.6f && th.z - carZ > 0f && th.z - carZ < 35f;
                th.speed = Mathf.MoveTowards(th.speed, usInWay ? 0f : th.baseSpeed, 14f * dt);
            }
            else if (th.kind == Kind.Slow)
            {
                // the slow car keeps its distance from whatever is in front of it too
                float lim = th.baseSpeed;
                foreach (var o in things)
                    if (o != th && o.kind != Kind.Oncoming && Mathf.Abs(o.x - th.x) < 1.4f && o.z > th.z && o.z - th.z < 16f)
                        lim = o.kind == Kind.Checkpoint ? Mathf.Min(lim, 5.5f) : o.kind == Kind.Ghost ? 0f : Mathf.Min(lim, o.speed);
                th.speed = Mathf.MoveTowards(th.speed, lim, 6f * dt);
            }
            th.z += th.speed * dt;
        }

        // police checkpoint: slow right down through it, the officer waves us on
        bool slowZone = false;
        foreach (var th in things)
        {
            if (th.kind != Kind.Checkpoint) continue;
            float from = th.z - th.half, to = th.z + th.half;
            if (carZ > from - 45f && carZ < to)
            {
                if (carZ > from - 35f) { want = Mathf.Min(want, 20f / 3.6f); slowZone = true; }
                if (!th.told) { th.told = true; Say("ด่านตรวจ · ชะลอความเร็ว 20 กม./ชม.", new Color(1f, 0.8f, 0.2f)); GameAudio.Play("whistle", null, 0.5f); }
                if (th.officer != null && carZ > from - 10f && carZ < from) th.officer.Wave();
            }
        }

        // what's in front of us in the lane we're heading for
        Thing ahead = null;
        float look = 14f + speed * 2.4f;
        foreach (var th in things)
        {
            if (th.kind == Kind.Checkpoint || Mathf.Abs(th.x - laneTarget) > 1.4f) continue;
            float d = th.z - th.half - carZ;
            if (d > -1f && d < look && (ahead == null || th.z < ahead.z)) ahead = th;
        }

        if (laneTarget == OurLane)
        {
            if (ahead != null && ahead.kind != Kind.Oncoming)
            {
                if (!slowZone && PassClear(ahead))
                {
                    laneTarget = OtherLane;
                    overtaking = ahead;
                    if (ahead.kind == Kind.Ghost) { Say($"{ahead.name}! หักหลบ", new Color(0.7f, 1f, 0.75f)); GameAudio.Play("ghost", null, 0.35f); }
                    else Say($"แซง{ahead.name}", Color.white);
                }
                else Follow(ahead, ref want);
            }
        }
        else
        {
            // back to our own side once past (and something coming: right away if there's room)
            bool past = overtaking == null || !things.Contains(overtaking) || overtaking.z + overtaking.half < carZ - 4.5f;
            bool danger = ahead != null && (ahead.kind == Kind.Oncoming || ahead.kind == Kind.Ghost);
            if ((past || danger) && LaneClear(OurLane, carZ - 5f, carZ + 6f))
            {
                if (!past && ahead.kind == Kind.Oncoming) Say($"{ahead.name}สวนมา! หลบเข้าเลน", new Color(1f, 0.6f, 0.4f));
                laneTarget = OurLane;
                overtaking = null;
            }
            else if (danger) Follow(ahead, ref want);
        }

        float rate = want < speed ? 10f : 3.2f;
        speed = Mathf.MoveTowards(speed, want, rate * dt);
        float before = carX;
        carX = Mathf.MoveTowards(carX, laneTarget, 2.8f * dt);
        float lateral = (carX - before) / Mathf.Max(0.0001f, dt);
        yaw = Mathf.Lerp(yaw, Mathf.Atan2(lateral, Mathf.Max(3f, speed)) * Mathf.Rad2Deg, 8f * dt);
        carZ += speed * dt;
        car.localPosition = new Vector3(carX, 0f, carZ);
        car.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // keep behind it: slow to its speed, stop short of something standing still
    private void Follow(Thing th, ref float want)
    {
        float gap = th.z - th.half - carZ - 3f;
        float limit = th.speed + Mathf.Max(0f, gap - 6f) * 0.7f;
        limit = Mathf.Min(limit, Mathf.Sqrt(2f * 7f * Mathf.Max(0f, gap - 2f)) + Mathf.Max(0f, th.speed));
        want = Mathf.Max(0f, Mathf.Min(want, limit));
    }

    private void Recycle()
    {
        // road tiles leap ahead
        foreach (var t in tiles)
            if (t.localPosition.z + TileLength < carZ - 25f) t.localPosition += new Vector3(0f, 0f, Tiles * TileLength);
        // scenery and traffic behind us go
        for (int i = scenery.Count - 1; i >= 0; i--)
            if (scenery[i].localPosition.z < carZ - 30f) { Destroy(scenery[i].gameObject); scenery.RemoveAt(i); }
        for (int i = things.Count - 1; i >= 0; i--)
            if (things[i].z + things[i].half < carZ - 30f)
            {
                if (things[i] == overtaking) overtaking = null;
                Destroy(things[i].t.gameObject);
                things.RemoveAt(i);
            }
        // new things ahead
        while (nextScenery < carZ + 230f) { SpawnScenery(nextScenery); nextScenery += Random.Range(4f, 10f); }
        while (nextPole < carZ + 230f) { SpawnPole(nextPole); nextPole += 32f; }
        while (nextEvent < carZ + 170f) { SpawnEvent(nextEvent); nextEvent += Random.Range(55f, 110f); }
        if (carZ + 240f > nextOncoming)
        {
            if (LaneClear(OtherLane, carZ + 180f, carZ + 270f)) SpawnOncoming(carZ + 250f);
            nextOncoming = carZ + 250f + Random.Range(40f, 140f);
        }
    }

    private void Animate(float t)
    {
        foreach (var th in things)
        {
            var p = new Vector3(th.x, 0f, th.z);
            if (th.kind == Kind.Ghost)
            {
                p.y = 0.25f + Mathf.Sin(t * 1.8f + th.bob) * 0.25f; // floating, drifting a little
                p.x += Mathf.Sin(t * 0.9f + th.bob) * 0.25f;
                var look = camT.position - th.t.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) th.t.rotation = Quaternion.LookRotation(-look);
            }
            th.t.localPosition = p;
        }
    }

    private void PlaceCamera(float k)
    {
        bool bike = CarModels.TwoWheels(carId);
        float back = bike ? 6.2f : 8.8f, up = bike ? 2.7f : 3.6f;
        var want = Origin + new Vector3(carX * 0.55f, up, carZ - back);
        camT.position = Vector3.Lerp(camT.position, want, k);
        var at = Origin + new Vector3(carX * 0.75f, 1.1f, carZ + 9f);
        camT.rotation = Quaternion.Slerp(camT.rotation, Quaternion.LookRotation(at - camT.position), k);
    }

    // ── the screen over it ──────────────────────────────────────────────────────────────────────
    private void BuildScreen()
    {
        var canvasGo = new GameObject("JourneyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        bool portrait = UiKit.Portrait;
        scaler.referenceResolution = portrait ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // the road, full screen (and it catches every tap meant for the map underneath)
        var viewGo = new GameObject("View", typeof(RectTransform), typeof(RawImage));
        viewGo.transform.SetParent(root, false);
        var view = (RectTransform)viewGo.transform;
        view.anchorMin = Vector2.zero;
        view.anchorMax = Vector2.one;
        view.offsetMin = view.offsetMax = Vector2.zero;
        viewGo.GetComponent<RawImage>().texture = target;

        // the card at the top
        float w = portrait ? 980f : 1240f, h = 300f;
        var card = UiKit.Panel("Card", root, new Color(0.13f, 0.08f, 0.05f, 0.86f), Vector2.zero, new Vector2(w, h), shadow: true);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
        card.anchoredPosition = new Vector2(0f, -(portrait ? 230f : 180f));
        card.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.75f, 0.3f, 0.9f);
        string to = destination.NameTh.Split('·')[0].Trim();
        string from = Locations.Current != null ? Locations.Current.NameTh.Split('·')[0].Trim() : "";
        UiKit.Cartoon(UiKit.Label(card, 44, $"กำลังขับไป {to}", new Vector2(0f, 105f), new Vector2(w - 60f, 60f), UiKit.GoldText), 3f);
        UiKit.Label(card, 24, $"{carName} · {drive.km:0} กม. · ใช้น้ำมัน {drive.litres:0.0} ลิตร · เหลือ {drive.left:0.0} ลิตร",
            new Vector2(0f, 58f), new Vector2(w - 60f, 36f), UiKit.HeaderSub);
        lineW = w - 240f;
        var track = UiKit.Panel("Track", card, new Color(1f, 1f, 1f, 0.15f), new Vector2(0f, 6f), new Vector2(lineW, 12f));
        fill = UiKit.Panel("Fill", track, UiKit.Gold, Vector2.zero, new Vector2(0f, 12f));
        fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
        foreach (var (x, name) in new[] { (-lineW * 0.5f, from), (lineW * 0.5f, to) })
        {
            var dot = UiKit.Rect("Stop", card, UiKit.Gold);
            dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
            dot.anchoredPosition = new Vector2(x, 6f);
            dot.sizeDelta = new Vector2(30f, 30f);
            UiKit.Label(card, 22, name, new Vector2(x, -30f), new Vector2(240f, 32f), UiKit.HeaderText);
        }
        marker = PikulSpinner.Make(card, new Vector2(-lineW * 0.5f, 6f), 64f);
        leftText = UiKit.Label(card, 30, "", new Vector2(-w * 0.25f, -100f), new Vector2(w * 0.45f, 46f), Color.white);
        timeText = UiKit.Label(card, 30, "", new Vector2(w * 0.25f, -100f), new Vector2(w * 0.45f, 46f), UiKit.GoldText);

        // what's happening on the road, under the card
        eventText = UiKit.Cartoon(UiKit.Label(root, 40, "", Vector2.zero, new Vector2(1000f, 60f), Color.white), 3f);
        var er = (RectTransform)eventText.transform;
        er.anchorMin = er.anchorMax = new Vector2(0.5f, 1f);
        er.anchoredPosition = new Vector2(0f, -(portrait ? 430f : 370f));

        // the speedometer
        var gauge = UiKit.Panel("Speed", root, new Color(0.1f, 0.08f, 0.06f, 0.8f), Vector2.zero, new Vector2(300f, 90f), shadow: true);
        gauge.anchorMin = gauge.anchorMax = new Vector2(0f, 0f);
        gauge.anchoredPosition = new Vector2(190f, portrait ? 160f : 110f);
        gauge.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.75f, 0.3f, 0.7f);
        speedText = UiKit.Cartoon(UiKit.Label(gauge, 40, "", Vector2.zero, new Vector2(280f, 80f), Color.white), 2.5f);
        var tip = UiKit.Label(root, 22, "ระหว่างทางเวลาในเกมเดินไปตามระยะทาง (1 ชม. ในเกม = 90 วินาที)", Vector2.zero, new Vector2(900f, 34f), new Color(1f, 1f, 1f, 0.85f));
        var tr = (RectTransform)tip.transform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
        tr.anchoredPosition = new Vector2(portrait ? 0f : 200f, portrait ? 60f : 50f);
        tip.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
    }

    private IEnumerator Run()
    {
        float total = Mathf.Max(3f, drive.seconds);
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            Recycle();
            Drive(dt);
            Animate(t);
            PlaceCamera(1f - Mathf.Exp(-6f * dt));

            float k = t / total;
            marker.anchoredPosition = new Vector2(-lineW * 0.5f + lineW * k, 6f);
            fill.sizeDelta = new Vector2(lineW * k, 12f);
            leftText.text = $"อีก {drive.km * (1f - k):0} กม.";
            timeText.text = "ถึงในอีก " + FuelClient.Hours(drive.hours * (1f - k));
            speedText.text = $"{Mathf.RoundToInt(speed * 3.6f)} กม./ชม.";
            var c = eventText.color;
            c.a = Mathf.Clamp01((eventUntil - Time.unscaledTime) / 0.4f);
            eventText.color = c;
            yield return null;
        }
        ParkedCar.Arriving = carId;
        Locations.TravelTo(destination, ParkedCarSpawner.DriveSpawn);
        yield return new WaitForSecondsRealtime(1.5f); // behind the loading screen
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (running == this) running = null;
        foreach (var c in muted) if (c != null) c.enabled = true;
        if (cam != null) cam.targetTexture = null;
        if (target != null) { target.Release(); Destroy(target); }
        foreach (var m in mats.Values) if (m != null) Destroy(m);
    }
}
