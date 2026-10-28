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
    private readonly List<Renderer> frontWindows = new List<Renderer>();
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

        // Outside parts: anything above the ceiling, plus the ground-floor windows on the front face
        var root = shellRoot != null ? shellRoot : transform;
        float reach = halfX * (root == transform ? 1f : transform.localScale.x) + 0.4f;
        foreach (Transform child in root)
        {
            if (child == transform || child.name.StartsWith("Wall") || child.name == "Interior" || child.name.StartsWith("ShopUnit")) continue;
            var p = child.localPosition;
            if (root != transform && Mathf.Abs(p.x - shellCenterX) > reach) continue; // a neighbouring shop
            if (p.y >= ceilingY - 0.25f) shell.AddRange(child.GetComponentsInChildren<Renderer>());
            else if (p.z < -halfZ + 0.35f && IsWindow(child.name)) frontWindows.AddRange(child.GetComponentsInChildren<Renderer>());
        }

        var pc = FindAnyObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;
    }

    private static bool IsWindow(string n) =>
        n == "Window" || n == "WindowFrame" || n == "Mullion" || n == "Sill" || n == "Lintel" || n == "AirCon"
        || n == "Display" || n == "Grille" || n == "Plinth";

    private void Update()
    {
        if (player == null) return;
        var local = transform.InverseTransformPoint(player.position);
        bool now = Mathf.Abs(local.x) < halfX && Mathf.Abs(local.z) < halfZ && local.y < ceilingY + 1f;
        if (now != inside)
        {
            inside = now;
            foreach (var r in shell) if (r != null) r.enabled = !inside;
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
        foreach (var r in frontWindows) if (r != null) r.enabled = front;
        Set("WallBack", back);
        Set("WallLeft", left);
        Set("WallRight", right);
    }

    private void Set(string wall, bool show)
    {
        if (!walls.TryGetValue(wall, out var list)) return;
        foreach (var r in list) if (r != null && r.enabled != show) r.enabled = show;
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
        ShowWalls(true, true, true, true);
    }
}
