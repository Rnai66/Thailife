using System.Collections.Generic;
using UnityEngine;

// Turns your house (HomePlot you live in; the village stilt house by default) into a room you can walk
// into and decorate (แต่งบ้าน).
// Done at runtime, so the scene doesn't need rebuilding: the solid "Walls" box is swapped for
// four thin walls with a doorway, the roof and walls hide while you're inside (so the camera can
// see in), and your furniture from HouseClient is placed on the floor.
public class HomeInterior : MonoBehaviour
{
    public const string HouseName = "StiltHouse_บ้านไม้ใต้ถุนสูง";

    // House-local measurements (VillageBuilder.BuildStiltHouse; every HomePlot's room is the same size,
    // only the floor height differs)
    private const float HalfX = 2.8f, HalfZ = 2.3f;
    private const float WallThickness = 0.12f, DoorHalfWidth = 0.75f;
    private float FloorY = 2.6f, WallTop = 5f, DoorTop = 4.8f;
    private readonly List<GameObject> added = new List<GameObject>();
    public const float BoundX = 2.5f, BoundZ = 2.0f; // same as house.lua

    public static HomeInterior Instance { get; private set; }

    public Transform Room { get; private set; }   // furniture origin: middle of the floor
    public Shader Shader { get; private set; }
    public bool PlayerInside { get; private set; }
    public IReadOnlyDictionary<int, GameObject> Pieces => pieces;

    private readonly Dictionary<int, GameObject> pieces = new Dictionary<int, GameObject>();
    private readonly List<Renderer> cutaway = new List<Renderer>();
    private readonly List<GameObject> lowWalls = new List<GameObject>(); // shown while you're inside
    private GameObject exitMarker;
    private Material exitMaterial;

    // The middle of the doorway, on the floor (world space)
    public Vector3 DoorPoint => transform.TransformPoint(new Vector3(0f, FloorY, -HalfZ));
    private Material wallMaterial;
    private bool frozen; // DecorateUI is carrying a piece: don't rebuild under it
    private HouseClient house;
    private GameObject bulb;
    private Material bulbMaterial;
    private Light ceilingLight;
    private float nextLightCheck;

    // On the house you live in (moving house moves it)
    public static void Setup()
    {
        var plot = HomePlot.Mine();
        var go = plot != null ? plot.gameObject : (HomeClient.MyStyle == "stilt" ? GameObject.Find(HouseName) : null);
        if (Instance != null && (go == null || Instance.gameObject != go)) Destroy(Instance);
        if (go != null && go.GetComponent<HomeInterior>() == null) go.AddComponent<HomeInterior>();
    }

    private void Awake()
    {
        Instance = this;
        var plot = GetComponent<HomePlot>();
        if (plot != null)
        {
            FloorY = plot.FloorY;
            WallTop = FloorY + 2.4f;
            DoorTop = FloorY + 2.2f;
        }

        var walls = transform.Find("Walls");
        if (walls != null)
        {
            var r = walls.GetComponent<Renderer>();
            wallMaterial = r.sharedMaterial;
            r.enabled = false;
            var c = walls.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }
        var door = transform.Find("Door");
        if (door != null) door.gameObject.SetActive(false);
        Shader = wallMaterial != null ? wallMaterial.shader : Shader.Find("Universal Render Pipeline/Lit");
        if (wallMaterial == null) wallMaterial = new Material(Shader) { color = new Color(0.6f, 0.4f, 0.22f) };

        // Hollow walls; the doorway faces the porch (-Z)
        float h = WallTop - FloorY, cy = (WallTop + FloorY) * 0.5f, t = WallThickness;
        AddWall(new Vector3(0f, cy, HalfZ - t * 0.5f), new Vector3(HalfX * 2f, h, t));
        AddWall(new Vector3(-HalfX + t * 0.5f, cy, 0f), new Vector3(t, h, HalfZ * 2f));
        AddWall(new Vector3(HalfX - t * 0.5f, cy, 0f), new Vector3(t, h, HalfZ * 2f));
        float side = HalfX - DoorHalfWidth;
        AddWall(new Vector3(-(DoorHalfWidth + side * 0.5f), cy, -HalfZ + t * 0.5f), new Vector3(side, h, t));
        AddWall(new Vector3(DoorHalfWidth + side * 0.5f, cy, -HalfZ + t * 0.5f), new Vector3(side, h, t));
        AddWall(new Vector3(0f, (DoorTop + WallTop) * 0.5f, -HalfZ + t * 0.5f), new Vector3(DoorHalfWidth * 2f, WallTop - DoorTop, t));

        BuildExitMarker();

        foreach (Transform child in transform)
            if (child.name.StartsWith("Roof") || child.name.StartsWith("Finial") || child.name.StartsWith("Window"))
                cutaway.AddRange(child.GetComponentsInChildren<Renderer>());

        Room = new GameObject("Room").transform;
        added.Add(Room.gameObject);
        Room.SetParent(transform, false);
        Room.localPosition = new Vector3(0f, FloorY, 0f);

        // Ceiling bulb (หลอดไฟ) on a cord: lights the room whenever you're inside, and at night
        var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cord.name = "BulbCord";
        added.Add(cord);
        Destroy(cord.GetComponent<Collider>());
        cord.transform.SetParent(transform, false);
        cord.transform.localPosition = new Vector3(0f, WallTop - 0.25f, 0f);
        cord.transform.localScale = new Vector3(0.02f, 0.25f, 0.02f);
        cord.GetComponent<Renderer>().sharedMaterial = new Material(Shader) { color = new Color(0.1f, 0.1f, 0.1f) };
        bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "CeilingBulb";
        added.Add(bulb);
        Destroy(bulb.GetComponent<Collider>());
        bulb.transform.SetParent(transform, false);
        bulb.transform.localPosition = new Vector3(0f, WallTop - 0.6f, 0f);
        bulb.transform.localScale = Vector3.one * 0.18f;
        bulbMaterial = new Material(Shader) { color = new Color(1f, 0.95f, 0.8f) };
        bulbMaterial.EnableKeyword("_EMISSION");
        bulb.GetComponent<Renderer>().sharedMaterial = bulbMaterial;
        var lightGo = new GameObject("CeilingLight");
        added.Add(lightGo);
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, WallTop - 0.75f, 0f);
        ceilingLight = lightGo.AddComponent<Light>();
        ceilingLight.type = LightType.Point;
        ceilingLight.range = 6f;
        ceilingLight.intensity = 4f;
        ceilingLight.color = new Color(1f, 0.86f, 0.62f);
        ceilingLight.shadows = LightShadows.None;

        var zone = new GameObject("RoomZone");
        added.Add(zone);
        zone.transform.SetParent(transform, false);
        zone.transform.localPosition = new Vector3(0f, FloorY + 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(HalfX * 2f - 0.4f, 2f, HalfZ * 2f - 0.4f);
        zone.AddComponent<RoomZone>().Interior = this;
    }

    private void Start()
    {
        house = HouseClient.Instance;
        if (house == null) return;
        house.Changed += OnHouseChanged;
        if (house.Current != null) Rebuild(house.Current);
    }

    private void Update()
    {
        if (exitMarker != null && exitMarker.activeSelf)
            exitMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.9f, 0.4f) * (1.2f + Mathf.Sin(Time.time * 4f) * 0.8f));
        if (ceilingLight == null || Time.time < nextLightCheck) return;
        nextLightCheck = Time.time + 0.5f;
        bool on = PlayerInside || RenderSettings.ambientIntensity < 0.8f;
        ceilingLight.enabled = on;
        bulbMaterial.SetColor("_EmissionColor", on ? new Color(1f, 0.85f, 0.55f) * 3f : Color.black);
    }

    private void OnDestroy()
    {
        if (house != null) house.Changed -= OnHouseChanged;
        if (Instance == this) Instance = null;
        ContextButton.Hide(this);
        // moved out: the house goes back to a plain closed house
        foreach (var go in added) if (go != null) Destroy(go);
        foreach (var go in pieces.Values) if (go != null) Destroy(go);
        foreach (var r in cutaway) if (r != null) r.enabled = true;
        var walls = transform.Find("Walls");
        if (walls != null)
        {
            var r = walls.GetComponent<Renderer>();
            if (r != null) r.enabled = true;
            var c = walls.GetComponent<Collider>();
            if (c != null) c.enabled = true;
        }
        var door = transform.Find("Door");
        if (door != null) door.gameObject.SetActive(true);
    }

    private void AddWall(Vector3 position, Vector3 size)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "RoomWall";
        added.Add(wall);
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = size;
        var r = wall.GetComponent<Renderer>();
        r.sharedMaterial = wallMaterial;
        cutaway.Add(r);

        // the same wall cut down to waist height: while you're inside the tall walls hide so the camera
        // can see in, but these stay, so the room keeps its shape and the doorway gap shows the way out
        if (position.y - size.y * 0.5f > FloorY + 0.1f) return; // the lintel over the door
        const float low = 0.85f;
        var stub = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stub.name = "RoomWallLow";
        Destroy(stub.GetComponent<Collider>());
        added.Add(stub);
        lowWalls.Add(stub);
        stub.transform.SetParent(transform, false);
        stub.transform.localPosition = new Vector3(position.x, FloorY + low * 0.5f, position.z);
        stub.transform.localScale = new Vector3(size.x, low, size.z);
        stub.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        stub.SetActive(false);
    }

    // Green chevrons on the floor at the doorway, pointing out, glowing gently while you're inside
    private void BuildExitMarker()
    {
        exitMarker = new GameObject("ExitMarker_ทางออก");
        added.Add(exitMarker);
        exitMarker.transform.SetParent(transform, false);
        exitMarker.transform.localPosition = new Vector3(0f, FloorY + 0.03f, -HalfZ + 0.55f);
        exitMaterial = new Material(Shader) { color = new Color(0.25f, 0.9f, 0.4f) };
        exitMaterial.EnableKeyword("_EMISSION");
        foreach (float z in new[] { 0.3f, -0.1f })
            foreach (float s in new[] { -1f, 1f })
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Chevron";
                Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(exitMarker.transform, false);
                bar.transform.localPosition = new Vector3(s * 0.15f, 0f, z - 0.15f);
                bar.transform.localRotation = Quaternion.Euler(0f, s * -45f, 0f);
                bar.transform.localScale = new Vector3(0.44f, 0.02f, 0.11f);
                bar.GetComponent<Renderer>().sharedMaterial = exitMaterial;
            }
        exitMarker.SetActive(false);
    }

    // ── Inside / outside ────────────────────────────────────────────────────────
    public void SetInside(bool inside)
    {
        if (inside == PlayerInside) return;
        PlayerInside = inside;
        foreach (var r in cutaway) if (r != null) r.enabled = !inside;
        foreach (var w in lowWalls) if (w != null) w.SetActive(inside);
        if (exitMarker != null) exitMarker.SetActive(inside);

        if (inside)
        {
            house?.Refresh();
            ShowPrompt();
        }
        else
        {
            ContextButton.Hide(this);
            DecorateUI.Close();
        }
    }

    public void ShowPrompt()
    {
        if (!PlayerInside || DecorateUI.IsOpen) return;
        bool online = house != null && house.IsOnline;
        ContextButton.Show(this, "แต่งบ้าน", online ? "จัดวางเฟอร์นิเจอร์ในบ้านของคุณ" : "ต้องออนไลน์ก่อน",
            () => { ContextButton.Hide(this); DecorateUI.Open(this); }, online);
    }

    // ── Furniture ───────────────────────────────────────────────────────────────
    public void Freeze(bool value)
    {
        frozen = value;
        if (!frozen && house?.Current != null) Rebuild(house.Current);
    }

    private void OnHouseChanged(HouseClient.State state)
    {
        if (!frozen) Rebuild(state);
        if (PlayerInside && !DecorateUI.IsOpen) ShowPrompt();
    }

    private void Rebuild(HouseClient.State state)
    {
        foreach (var go in pieces.Values) if (go != null) Destroy(go);
        pieces.Clear();
        bed = null;
        foreach (var p in state.placed)
        {
            var go = FurnitureFactory.Build(p.item, Shader);
            go.transform.SetParent(Room, false);
            go.transform.localPosition = new Vector3(p.x, 0f, p.z);
            go.transform.localRotation = Quaternion.Euler(0f, p.rot, 0f);
            pieces[p.id] = go;
            if (p.item == "bed" && bed == null) bed = go.transform;
        }
        // No bed bought yet: a rolled-out sleeping mat (ที่นอน) in the corner, so there's always somewhere to wake up
        if (bed == null && defaultMat == null)
        {
            defaultMat = FurnitureFactory.Build("mat", Shader);
            defaultMat.name = "SleepingMat_ที่นอน";
            defaultMat.transform.SetParent(Room, false);
            defaultMat.transform.localPosition = new Vector3(-1.6f, 0f, 0.9f);
            added.Add(defaultMat);
        }
        if (defaultMat != null) defaultMat.SetActive(bed == null);
    }

    private Transform bed;
    private GameObject defaultMat;

    // Where you lie when you wake up at home (the bed if you have one, else the mat) and where you stand up
    public bool WakePoint(out Vector3 lie, out Quaternion facing, out Vector3 stand)
    {
        Transform b = bed != null ? bed : defaultMat != null ? defaultMat.transform : null;
        lie = stand = Vector3.zero;
        facing = Quaternion.identity;
        if (b == null) return false;
        lie = b.TransformPoint(new Vector3(0f, bed != null ? 0.62f : 0.14f, 0.1f));
        facing = b.rotation * Quaternion.Euler(0f, 90f, 0f);
        // get up on the side towards the middle of the room
        Vector3 l = b.TransformPoint(new Vector3(-1.1f, 0f, 0f)), r = b.TransformPoint(new Vector3(1.1f, 0f, 0f));
        stand = (l - Room.position).sqrMagnitude < (r - Room.position).sqrMagnitude ? l : r;
        stand.y = Room.position.y;
        return true;
    }

    public GameObject SpawnPreview(string item)
    {
        var go = FurnitureFactory.Build(item, Shader);
        go.transform.SetParent(Room, false);
        return go;
    }

    // Where a piece would go if put down in front of the player (room-local, snapped, inside the walls)
    public Vector3 SpotInFront(Transform player, float distance)
    {
        Vector3 local = Room.InverseTransformPoint(player.position + player.forward * distance);
        float x = Mathf.Clamp(Mathf.Round(local.x * 10f) / 10f, -BoundX, BoundX);
        float z = Mathf.Clamp(Mathf.Round(local.z * 10f) / 10f, -BoundZ, BoundZ);
        return new Vector3(x, 0f, z);
    }
}
