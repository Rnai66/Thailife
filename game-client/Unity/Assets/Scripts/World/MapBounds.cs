using UnityEngine;
using UnityEngine.SceneManagement;

// ขอบแผนที่: every map gets invisible walls just inside the edges of its ground, so you can't walk off
// the world. If the player still ends up below the ground (pushed through by something, a glitch), they
// are put back on their feet at the nearest point inside the map.
public class MapBounds : MonoBehaviour
{
    private const float Inset = 0.6f, WallHeight = 40f, Thickness = 2f;

    private Bounds area;
    private float groundY;
    private PlayerController player;
    private float nextCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Build();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Build();

    private static void Build()
    {
        if (FindAnyObjectByType<MapBounds>() != null) return;
        // The map's own ground: the biggest thing called "Ground" (not, say, the fair ground's floor,
        // which is also called Ground and may be found first — walling you in on it)
        Renderer r = null;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.name != "Ground") continue;
            var candidate = t.GetComponent<Renderer>();
            if (candidate == null) continue;
            if (r == null || candidate.bounds.size.x * candidate.bounds.size.z > r.bounds.size.x * r.bounds.size.z) r = candidate;
        }
        if (r == null) return;
        var b = r.bounds;
        if (b.size.x < 20f || b.size.z < 20f) return;

        var edge = new GameObject("MapEdge_ขอบแผนที่").AddComponent<MapBounds>();
        edge.area = b;
        edge.groundY = b.max.y;
        float y = b.max.y + WallHeight * 0.5f - 1f;
        float x0 = b.min.x + Inset - Thickness * 0.5f, x1 = b.max.x - Inset + Thickness * 0.5f;
        float z0 = b.min.z + Inset - Thickness * 0.5f, z1 = b.max.z - Inset + Thickness * 0.5f;
        edge.Wall(new Vector3(b.center.x, y, z0), new Vector3(b.size.x, WallHeight, Thickness));
        edge.Wall(new Vector3(b.center.x, y, z1), new Vector3(b.size.x, WallHeight, Thickness));
        edge.Wall(new Vector3(x0, y, b.center.z), new Vector3(Thickness, WallHeight, b.size.z));
        edge.Wall(new Vector3(x1, y, b.center.z), new Vector3(Thickness, WallHeight, b.size.z));
    }

    private void Wall(Vector3 centre, Vector3 size)
    {
        var w = new GameObject("EdgeWall");
        w.transform.SetParent(transform, false);
        w.transform.position = centre;
        w.AddComponent<BoxCollider>().size = size;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.5f;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null || !player.enabled) return;
        var p = player.transform.position;
        bool outside = p.x < area.min.x || p.x > area.max.x || p.z < area.min.z || p.z > area.max.z;
        if (p.y > groundY - 6f && !outside) return;
        // fell through or out: back on the ground, inside the walls
        float m = Inset + 2f;
        var back = new Vector3(Mathf.Clamp(p.x, area.min.x + m, area.max.x - m), groundY + 2f, Mathf.Clamp(p.z, area.min.z + m, area.max.z - m));
        if (Physics.Raycast(back + Vector3.up * 60f, Vector3.down, out var hit, 120f, ~0, QueryTriggerInteraction.Ignore)) back.y = hit.point.y + 0.3f;
        player.Teleport(back, player.transform.rotation);
    }
}
