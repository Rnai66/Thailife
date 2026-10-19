using System.Collections.Generic;
using UnityEngine;

// Turns the village stilt house into a room you can walk into and decorate (แต่งบ้าน).
// Done at runtime, so the scene doesn't need rebuilding: the solid "Walls" box is swapped for
// four thin walls with a doorway, the roof and walls hide while you're inside (so the camera can
// see in), and your furniture from HouseClient is placed on the floor.
public class HomeInterior : MonoBehaviour
{
    public const string HouseName = "StiltHouse_บ้านไม้ใต้ถุนสูง";

    // House-local measurements (VillageBuilder.BuildStiltHouse)
    private const float FloorY = 2.6f, WallTop = 5f, HalfX = 2.8f, HalfZ = 2.3f;
    private const float WallThickness = 0.12f, DoorHalfWidth = 0.75f, DoorTop = 4.8f;
    public const float BoundX = 2.5f, BoundZ = 2.0f; // same as house.lua

    public static HomeInterior Instance { get; private set; }

    public Transform Room { get; private set; }   // furniture origin: middle of the floor
    public Shader Shader { get; private set; }
    public bool PlayerInside { get; private set; }
    public IReadOnlyDictionary<int, GameObject> Pieces => pieces;

    private readonly Dictionary<int, GameObject> pieces = new Dictionary<int, GameObject>();
    private readonly List<Renderer> cutaway = new List<Renderer>();
    private Material wallMaterial;
    private bool frozen; // DecorateUI is carrying a piece: don't rebuild under it
    private HouseClient house;

    public static void Setup()
    {
        var go = GameObject.Find(HouseName);
        if (go != null && go.GetComponent<HomeInterior>() == null) go.AddComponent<HomeInterior>();
    }

    private void Awake()
    {
        Instance = this;

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

        foreach (Transform child in transform)
            if (child.name.StartsWith("Roof") || child.name.StartsWith("Finial") || child.name.StartsWith("Window"))
                cutaway.AddRange(child.GetComponentsInChildren<Renderer>());

        Room = new GameObject("Room").transform;
        Room.SetParent(transform, false);
        Room.localPosition = new Vector3(0f, FloorY, 0f);

        // Ceiling light: warm glow inside the room at night
        var ceiling = new GameObject("CeilingLight");
        ceiling.transform.SetParent(transform, false);
        ceiling.transform.localPosition = new Vector3(0f, WallTop - 0.2f, 0f);
        ceiling.AddComponent<Light>().enabled = false;
        ceiling.AddComponent<StreetLamp>().Configure(StreetLamp.Kind.Hanging);

        var zone = new GameObject("RoomZone");
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

    private void OnDestroy()
    {
        if (house != null) house.Changed -= OnHouseChanged;
        if (Instance == this) Instance = null;
        ContextButton.Hide(this);
    }

    private void AddWall(Vector3 position, Vector3 size)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "RoomWall";
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = size;
        var r = wall.GetComponent<Renderer>();
        r.sharedMaterial = wallMaterial;
        cutaway.Add(r);
    }

    // ── Inside / outside ────────────────────────────────────────────────────────
    public void SetInside(bool inside)
    {
        if (inside == PlayerInside) return;
        PlayerInside = inside;
        foreach (var r in cutaway) if (r != null) r.enabled = !inside;

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
        foreach (var p in state.placed)
        {
            var go = FurnitureFactory.Build(p.item, Shader);
            go.transform.SetParent(Room, false);
            go.transform.localPosition = new Vector3(p.x, 0f, p.z);
            go.transform.localRotation = Quaternion.Euler(0f, p.rot, 0f);
            pieces[p.id] = go;
        }
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
