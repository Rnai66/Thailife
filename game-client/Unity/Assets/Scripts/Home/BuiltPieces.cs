using System.Collections.Generic;
using UnityEngine;

// Shows what you've built (BuildClient) round the house you live in (HomePlot.Mine), and on the other
// sites you hold (your farm lot, your land: AnchorOf): each piece at its place relative to its site,
// standing on the ground. Rebuilt when the list changes, when a new map
// loads or when you move house. BuildUI hides one piece while it's being moved.
public class BuiltPieces : MonoBehaviour
{
    private static BuiltPieces instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; villageStilt = null; }

    private readonly List<Transform> containers = new List<Transform>();
    private readonly Transform[] anchors = new Transform[4];
    private int shownVersion = -1;
    private int hidden = -1;
    private float nextCheck;
    private readonly Dictionary<int, GameObject> shown = new Dictionary<int, GameObject>();

    // The house the pieces stand round (null when it isn't on this map)
    public static Transform Anchor
    {
        get
        {
            var c = BuildClient.Instance;
            if (c != null && c.Current != null && !string.IsNullOrEmpty(c.Current.anchor)) return AnchorFor(c.Current.anchor);
            var mine = HomePlot.Mine();
            return mine != null ? mine.transform : null;
        }
    }

    // A fixed house the pieces stand round instead of the owner's lot: "village_stilt" is the village's own
    // stilt house, where the game's own account built before there were house lots (null on other maps)
    private static Transform villageStilt;

    // Where a site's pieces are measured from: home (Anchor), your farm lot (FarmLots), your land (LandSite)
    public static Transform AnchorOf(string site)
    {
        switch (site)
        {
            case "home": return Anchor;
            case "farm": return FarmLots.Current != null && FarmLots.Current.MyLot() is FarmLot lot ? lot.transform : null;
            case "plain":
            case "city":
                foreach (var l in FindObjectsByType<LandSite>(FindObjectsSortMode.None)) if (l.Parcel == site) return l.transform;
                return null;
        }
        return null;
    }

    private static readonly string[] AllSites = { "home", "farm", "plain", "city" };
    public static Transform AnchorFor(string anchor)
    {
        if (anchor != "village_stilt") return null;
        if (villageStilt == null)
        {
            var go = GameObject.Find(HomeInterior.HouseName);
            villageStilt = go != null ? go.transform : null;
        }
        return villageStilt;
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
        if (client == null || client.Current == null) return;
        bool same = true;
        for (int i = 0; i < AllSites.Length; i++)
        {
            var a = AnchorOf(AllSites[i]);
            if (a != anchors[i]) { anchors[i] = a; same = false; }
        }
        if (same && shownVersion == client.Version && !containers.Exists(c => c == null)) return;
        shownVersion = client.Version;
        Rebuild(client.Current);
    }

    private void Rebuild(BuildClient.State state)
    {
        foreach (var c in containers) if (c != null) Destroy(c.gameObject);
        containers.Clear();
        shown.Clear();
        var bySite = new Dictionary<string, Transform>();
        foreach (var p in state.pieces)
        {
            string site = BuildClient.SiteOf(p);
            if (!bySite.TryGetValue(site, out var container))
            {
                var anchor = AnchorOf(site);
                container = null;
                if (anchor != null)
                {
                    container = new GameObject("BuiltPieces_สิ่งก่อสร้าง_" + site).transform;
                    container.SetParent(anchor, false); // goes with the map
                    containers.Add(container);
                }
                bySite[site] = container;
            }
            if (container == null) continue;
            var go = BuildKit.Make(p.kind, p.color, container);
            Place(go.transform, container.parent, p.x, p.z, p.yaw);
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
