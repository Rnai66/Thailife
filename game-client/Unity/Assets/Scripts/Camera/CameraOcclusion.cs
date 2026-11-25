using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Keeps the player in sight (CameraFollow adds it to the camera): anything standing between the camera
// and the player — a roof, an upper floor, a wall, the leaves of a tree, a sign — stops being drawn
// until it is out of the way again, so the view stays clear the way open-world games do it. Its shadow
// stays where it had one, so the ground still looks shaded under a hidden tree.
// The scene's meshes are sorted into an 8 m grid once a town has loaded (and again a little later, for
// what the town puts up at runtime); each check only looks at the cells along the line of sight.
public class CameraOcclusion : MonoBehaviour
{
    public Transform Target;

    private const float Cell = 8f;
    private const float Interval = 0.07f;      // seconds between checks
    private const float Pad = 0.35f;           // how wide the line of sight is, metres
    private const float MaxSize = 30f;         // bigger than this is ground, a merged block or the sky
    private const float LookHeight = 1.2f;     // where on the player the camera looks (CameraFollow)
    private const float Linger = 0.25f;        // stays hidden this long after it's clear, so it doesn't flicker

    private struct Hidden
    {
        public ShadowCastingMode mode;
        public float until;
    }

    private readonly Dictionary<long, List<Renderer>> grid = new Dictionary<long, List<Renderer>>();
    private readonly Dictionary<Renderer, Hidden> hidden = new Dictionary<Renderer, Hidden>();
    private readonly HashSet<Renderer> seen = new HashSet<Renderer>();
    private readonly List<Renderer> done = new List<Renderer>();
    private Scene gridScene;
    private float nextCheck, rebuildAt;
    private int rebuilds;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ShowAll();
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        rebuilds = 0;
        rebuildAt = Time.unscaledTime + 0.5f;
    }

    private void LateUpdate()
    {
        if (Target == null) return;
        var scene = SceneManager.GetActiveScene();
        if (scene != gridScene) { gridScene = scene; rebuilds = 0; rebuildAt = Time.unscaledTime + 0.5f; ShowAll(); grid.Clear(); }
        // sort the town's meshes now, then twice more as what it puts up at runtime appears
        if (rebuildAt > 0f && Time.unscaledTime >= rebuildAt)
        {
            Rebuild();
            rebuilds++;
            rebuildAt = rebuilds == 1 ? Time.unscaledTime + 4f : rebuilds == 2 ? Time.unscaledTime + 15f : 0f;
        }
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + Interval;
        Check();
    }

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

    private void Rebuild()
    {
        ShowAll();
        grid.Clear();
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r == null || !r.enabled) continue;
            var b = r.bounds;
            if (b.size.x > MaxSize || b.size.z > MaxSize || b.size.y < 0.3f) continue;
            if (Target != null && r.transform.IsChildOf(Target)) continue;
            int x0 = Mathf.FloorToInt(b.min.x / Cell), x1 = Mathf.FloorToInt(b.max.x / Cell);
            int z0 = Mathf.FloorToInt(b.min.z / Cell), z1 = Mathf.FloorToInt(b.max.z / Cell);
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                long k = Key(x, z);
                if (!grid.TryGetValue(k, out var list)) grid[k] = list = new List<Renderer>();
                list.Add(r);
            }
        }
    }

    private void Check()
    {
        Vector3 focus = Target.position + Vector3.up * LookHeight;
        Vector3 cam = transform.position;
        Vector3 toFocus = focus - cam;
        float dist = toFocus.magnitude;
        seen.Clear();
        if (dist > 0.6f && grid.Count > 0)
        {
            var ray = new Ray(cam, toFocus / dist);
            float reach = dist - 0.4f; // not what the player is standing right against
            int steps = Mathf.CeilToInt(dist / (Cell * 0.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(cam, focus, i / (float)steps);
                int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (!grid.TryGetValue(Key(cx + dx, cz + dz), out var list)) continue;
                    foreach (var r in list)
                    {
                        if (r == null || seen.Contains(r)) continue;
                        var b = r.bounds;
                        if (b.max.y < focus.y - 0.3f) continue;   // the ground, a floor, a low wall
                        if (b.Contains(focus)) continue;          // the car you're in, the room you stand in
                        b.Expand(Pad);
                        if (b.IntersectRay(ray, out float d) && d < reach) seen.Add(r);
                    }
                }
            }
        }

        float now = Time.unscaledTime;
        foreach (var r in seen)
        {
            if (hidden.TryGetValue(r, out var h)) { h.until = now + Linger; hidden[r] = h; continue; }
            if (!r.enabled || r.forceRenderingOff) continue; // switched off by someone else
            hidden[r] = new Hidden { mode = r.shadowCastingMode, until = now + Linger };
            if (r.shadowCastingMode == ShadowCastingMode.Off) r.forceRenderingOff = true;
            else r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        done.Clear();
        foreach (var kv in hidden) if (kv.Key == null || (!seen.Contains(kv.Key) && now >= kv.Value.until)) done.Add(kv.Key);
        foreach (var r in done) Restore(r);
    }

    private void Restore(Renderer r)
    {
        if (r != null && hidden.TryGetValue(r, out var h))
        {
            if (h.mode == ShadowCastingMode.Off) r.forceRenderingOff = false;
            else r.shadowCastingMode = h.mode;
        }
        hidden.Remove(r);
    }

    private void ShowAll()
    {
        done.Clear();
        done.AddRange(hidden.Keys);
        foreach (var r in done) Restore(r);
        hidden.Clear();
    }
}
