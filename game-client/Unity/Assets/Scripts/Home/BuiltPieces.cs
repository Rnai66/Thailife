using System.Collections.Generic;
using UnityEngine;

// Shows what you've built (BuildClient) round the house you live in (HomePlot.Mine): each piece at its
// place relative to the house, standing on the ground. Rebuilt when the list changes, when a new map
// loads or when you move house. BuildUI hides one piece while it's being moved.
public class BuiltPieces : MonoBehaviour
{
    private static BuiltPieces instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private Transform container, anchor;
    private int shownVersion = -1;
    private int hidden = -1;
    private float nextCheck;
    private readonly Dictionary<int, GameObject> shown = new Dictionary<int, GameObject>();

    // The house the pieces stand round (null when it isn't on this map)
    public static Transform Anchor
    {
        get
        {
            var mine = HomePlot.Mine();
            return mine != null ? mine.transform : null;
        }
    }

    public static GameObject Shown(int id) => instance != null && instance.shown.TryGetValue(id, out var go) ? go : null;

    // Hide one piece (being moved) or none (-1)
    public static void Hide(int id)
    {
        if (instance == null) return;
        instance.hidden = id;
        foreach (var kv in instance.shown) if (kv.Value != null) kv.Value.SetActive(kv.Key != id);
    }

    public static void Refresh()
    {
        if (instance != null) instance.shownVersion = -1;
    }

    private void Awake() => instance = this;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.5f;
        var client = BuildClient.Instance;
        var a = Anchor;
        if (client == null || client.Current == null) return;
        if (a == anchor && container != null && shownVersion == client.Version) return;
        anchor = a;
        shownVersion = client.Version;
        Rebuild(client.Current);
    }

    private void Rebuild(BuildClient.State state)
    {
        if (container != null) Destroy(container.gameObject);
        shown.Clear();
        if (anchor == null) return;
        container = new GameObject("BuiltPieces_สิ่งก่อสร้าง").transform;
        container.SetParent(anchor, false); // goes with the map
        foreach (var p in state.pieces)
        {
            var go = BuildKit.Make(p.kind, p.color, container);
            Place(go.transform, anchor, p.x, p.z, p.yaw);
            go.name = "Built_" + p.id;
            if (p.id == hidden) go.SetActive(false);
            shown[p.id] = go;
        }
    }

    // Put a piece at (x, z, yaw) relative to the house, standing on whatever ground is there
    public static void Place(Transform piece, Transform house, float x, float z, float yaw)
    {
        var world = house.TransformPoint(new Vector3(x, 0f, z));
        world.y = GroundAt(world, house.position.y);
        piece.position = world;
        piece.rotation = house.rotation * Quaternion.Euler(0f, yaw, 0f);
    }

    // The ground under a point: the highest surface below about knee height above the house's base
    // (so not a roof or a table), ignoring built pieces and the player
    public static float GroundAt(Vector3 world, float baseY)
    {
        float best = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(new Vector3(world.x, baseY + 30f, world.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.point.y > baseY + 0.6f) continue;
            if (hit.collider.GetComponentInParent<BuiltPieces>() != null || hit.collider.name.StartsWith("Built_")
                || hit.collider.transform.parent != null && hit.collider.transform.parent.name.StartsWith("BuiltPieces")) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            best = Mathf.Max(best, hit.point.y);
        }
        return float.IsNegativeInfinity(best) ? baseY : best;
    }
}
