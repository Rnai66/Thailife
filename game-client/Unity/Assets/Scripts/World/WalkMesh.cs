using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// The walkable map (NavMesh) the ออโต้ walks by: built while the game runs, from the scene's solid
// colliders, only round where the character is going (built in the background, a few seconds the first
// time; the map grows to take in later trips). Paths keep to the ground: round walls, in and out through
// gates and doors, never up onto a roof. One per scene; gone with the scene.
public class WalkMesh : MonoBehaviour
{
    private static WalkMesh instance;

    private NavMeshData data;
    private NavMeshDataInstance handle;
    private AsyncOperation building;
    private Bounds area, pending;
    private bool has;
    private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
    private readonly List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
    private readonly NavMeshPath path = new NavMeshPath();

    private const float Margin = 60f, MaxSize = 520f;

    public static WalkMesh Get()
    {
        if (instance == null) instance = new GameObject("WalkMesh").AddComponent<WalkMesh>();
        return instance;
    }

    public bool Busy => building != null && !building.isDone;

    private void OnDestroy()
    {
        if (handle.valid) handle.Remove();
        if (instance == this) instance = null;
    }

    // Make sure the map takes in both points (starts a background build when it doesn't yet)
    public void Cover(Vector3 a, Vector3 b)
    {
        if (has && Inside(area, a) && Inside(area, b)) return;
        if (Busy) return;
        var want = new Bounds(a, Vector3.zero);
        want.Encapsulate(b);
        want.Expand(new Vector3(Margin * 2f, 0f, Margin * 2f));
        // keep what was mapped before if the whole thing stays a sensible size
        if (has)
        {
            var both = want;
            both.Encapsulate(area);
            if (both.size.x <= MaxSize && both.size.z <= MaxSize) want = both;
        }
        want.size = new Vector3(Mathf.Min(want.size.x, MaxSize * 1.4f), 0f, Mathf.Min(want.size.z, MaxSize * 1.4f));
        float midY = (a.y + b.y) * 0.5f;
        want.center = new Vector3(want.center.x, midY, want.center.z);
        want.size = new Vector3(want.size.x, Mathf.Abs(a.y - b.y) + 80f, want.size.z);
        Build(want);
    }

    private static bool Inside(Bounds b, Vector3 p) =>
        p.x > b.min.x + 4f && p.x < b.max.x - 4f && p.z > b.min.z + 4f && p.z < b.max.z - 4f;

    private void Build(Bounds bounds)
    {
        sources.Clear();
        NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
        // only the fixed world: no trigger zones, no characters, nothing that moves by physics
        sources.RemoveAll(s =>
        {
            var c = s.component as Collider;
            if (c == null) return false;
            if (c.isTrigger || c is CharacterController) return true;
            var body = c.attachedRigidbody;
            return body != null && !body.isKinematic;
        });

        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.4f;
        settings.agentHeight = 1.9f;
        settings.agentClimb = 0.35f;  // what the character steps up without jumping
        settings.agentSlope = 45f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.14f;
        settings.overrideTileSize = true;
        settings.tileSize = 128;
        settings.minRegionArea = 3f;

        if (data == null)
        {
            data = new NavMeshData(settings.agentTypeID);
            handle = NavMesh.AddNavMeshData(data);
        }
        pending = bounds;
        building = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
        building.completed += _ => { area = pending; has = true; };
    }

    // The way from `from` to `to` as corner points along the ground; false = no map here yet / no way
    public bool Path(Vector3 from, Vector3 to, List<Vector3> corners)
    {
        corners.Clear();
        if (!has) return false;
        if (!NavMesh.SamplePosition(from, out var start, 2.5f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(to, out var end, 2.5f, NavMesh.AllAreas)
            && !NavMesh.SamplePosition(to, out end, 7f, NavMesh.AllAreas)) return false;
        if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)) return false;
        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2) return false;
        corners.AddRange(path.corners);
        return true;
    }
}
