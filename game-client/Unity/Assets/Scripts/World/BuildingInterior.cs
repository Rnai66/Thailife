using System.Collections.Generic;
using UnityEngine;

// A building's ground floor you can walk into (built by Interiors in the Editor). While the player is
// inside, everything above the ceiling (upper floors, roof, signs, canopy) and the walls on the
// camera's side hide so the camera can see into the room, and a ceiling light comes on.
// Front faces local -Z; the room spans ±halfX, ±halfZ from this transform, floor to ceilingY.
public class BuildingInterior : MonoBehaviour
{
    [SerializeField] private float halfX = 4f, halfZ = 4f, ceilingY = 3.2f;
    [SerializeField] private Transform shellRoot; // where the outside parts live (this, or the shophouse row)
    [SerializeField] private float shellCenterX;  // this unit's x in shellRoot (shophouse rows)

    private readonly List<Renderer> shell = new List<Renderer>();
    private readonly List<Renderer> labels = new List<Renderer>(); // TextMesh in the room: drawn through walls, so only while inside
    private readonly Dictionary<string, List<Renderer>> windows = new Dictionary<string, List<Renderer>>();
    private readonly Dictionary<string, Renderer[]> walls = new Dictionary<string, Renderer[]>();
    private Transform player, cam;
    private Light lamp;
    private bool inside;

    public void Configure(float roomHalfX, float roomHalfZ, float ceiling, Transform outsideRoot = null, float outsideCenterX = 0f)
    {
        halfX = roomHalfX;
        halfZ = roomHalfZ;
        ceilingY = ceiling;
        shellRoot = outsideRoot;
        shellCenterX = outsideCenterX;
    }

    private void Start()
    {
        foreach (var name in new[] { "WallFront", "WallBack", "WallLeft", "WallRight" })
        {
            var w = transform.Find(name);
            if (w != null) walls[name] = w.GetComponentsInChildren<Renderer>();
        }

        // Outside parts: anything above the ceiling, plus the windows and doors set into each wall
        // (measured in this room's own space, so it works for shell roots with other scales/offsets)
        var root = shellRoot != null ? shellRoot : transform;
        foreach (Transform child in root)
        {
            if (child == transform || child.name.StartsWith("Wall") || child.name == "Interior" || child.name.StartsWith("ShopUnit")) continue;
            var p = transform.InverseTransformPoint(child.position);
            if (root != transform && Mathf.Abs(p.x) > halfX + 0.4f) continue; // a neighbouring shop
            if (p.y >= ceilingY - 0.25f) shell.AddRange(child.GetComponentsInChildren<Renderer>());
            else if (HighParts(child)) { }
            else if (IsWindow(child.name))
            {
                string wall = p.z < -halfZ + 0.35f ? "WallFront" : p.z > halfZ - 0.35f ? "WallBack"
                    : p.x < -halfX + 0.35f ? "WallLeft" : p.x > halfX - 0.35f ? "WallRight" : null;
                if (wall == null) continue;
                if (!windows.TryGetValue(wall, out var list)) windows[wall] = list = new List<Renderer>();
                list.AddRange(child.GetComponentsInChildren<Renderer>());
            }
        }

        var room = transform.Find("Interior");
        if (room != null)
            foreach (var t in room.GetComponentsInChildren<TextMesh>(true))
            {
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                labels.Add(r);
                r.enabled = false;
            }

        var pc = FindAnyObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;
    }

    // A part whose pivot sits below the ceiling but which is mostly above it (a gable end set on the
    // wall top, a skirt roof leaning over the eaves): hide the high pieces of it with the roof
    private bool HighParts(Transform child)
    {
        if (IsWindow(child.name)) return false;
        bool any = false;
        foreach (var r in child.GetComponentsInChildren<Renderer>())
        {
            var c = transform.InverseTransformPoint(r.bounds.center);
            var top = transform.InverseTransformPoint(r.bounds.max);
            if (c.y >= ceilingY - 0.5f || (c.y >= ceilingY - 1.2f && top.y >= ceilingY + 0.3f && !IsWindow(r.name)))
            {
                shell.Add(r);
                any = true;
            }
        }
        return any;
    }

    private static bool IsWindow(string n) =>
        n == "Window" || n == "WindowFrame" || n == "Mullion" || n == "Sill" || n == "Lintel" || n == "AirCon"
        || n == "Display" || n == "Grille" || n == "Plinth" || n == "Stripe" || n == "DoorFrame" || n == "DoorJamb" || n == "DoorLeaf" || n == "DoorArch";

    private void Update()
    {
        if (player == null) return;
        var local = transform.InverseTransformPoint(player.position);
        bool now = Mathf.Abs(local.x) < halfX && Mathf.Abs(local.z) < halfZ && local.y < ceilingY + 1f;
        if (now != inside)
        {
            inside = now;
            foreach (var r in shell) if (r != null) r.enabled = !inside;
            foreach (var r in labels) if (r != null) r.enabled = inside;
            if (lamp == null && inside) MakeLamp();
            if (lamp != null) lamp.enabled = inside;
            if (!inside) ShowWalls(true, true, true, true);
        }
        if (!inside) return;

        // Hide the walls between the camera and the room
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) return;
        var c = transform.InverseTransformPoint(cam.position);
        ShowWalls(c.z >= -halfZ, c.z <= halfZ, c.x >= -halfX, c.x <= halfX);
    }

    private void ShowWalls(bool front, bool back, bool left, bool right)
    {
        Set("WallFront", front);
        Set("WallBack", back);
        Set("WallLeft", left);
        Set("WallRight", right);
    }

    private void Set(string wall, bool show)
    {
        if (walls.TryGetValue(wall, out var list))
            foreach (var r in list) if (r != null && r.enabled != show) r.enabled = show;
        if (windows.TryGetValue(wall, out var glass))
            foreach (var r in glass) if (r != null && r.enabled != show) r.enabled = show;
    }

    private void MakeLamp()
    {
        var go = new GameObject("RoomLight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, ceilingY - 0.4f, 0f);
        lamp = go.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.96f, 0.9f);
        lamp.intensity = 1.6f;
        lamp.range = Mathf.Max(halfX, halfZ) * 2.6f * transform.lossyScale.x;
        lamp.shadows = LightShadows.None;
    }

    private void OnDisable()
    {
        if (!inside) return;
        inside = false;
        foreach (var r in shell) if (r != null) r.enabled = true;
        foreach (var r in labels) if (r != null) r.enabled = false;
        ShowWalls(true, true, true, true);
    }
}
