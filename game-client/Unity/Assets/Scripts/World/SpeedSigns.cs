using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ป้ายจำกัดความเร็ว: every map with traffic gets the round red-ringed signs along its roads, on the
// kerb side (traffic keeps left) facing the drivers coming up — 80 on the ring road, 60 on the
// avenues, 40 in the side streets and slip roads (TrafficNet.LimitOf). Put up when the map loads
// from the roads the traffic drives, so no scene needs rebuilding; the speedometer
// (DrivableCar) shows the limit of the road you're on.
public class SpeedSigns : MonoBehaviour
{
    private const float Every = 110f, FirstAt = 25f, Apart = 35f;
    private static readonly float[] Offsets = { 2.9f, 5.2f, 8.6f };

    private static readonly Dictionary<int, Material> faces = new Dictionary<int, Material>();
    private static Material pole, back;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Make();
    }

    private static void OnLoaded(Scene s, LoadSceneMode m) => Make();

    private static void Make()
    {
        if (FindAnyObjectByType<SpeedSigns>() != null) return;
        new GameObject("SpeedSigns_ป้ายจำกัดความเร็ว").AddComponent<SpeedSigns>();
    }

    private IEnumerator Start()
    {
        yield return null; // the cars register their roads in Awake
        yield return null;
        if (TrafficNet.Count == 0) yield break;
        TrafficNet.EnsureBuilt();
        var put = new List<Vector3>();
        for (int i = 0; i < TrafficNet.Count; i++)
        {
            var r = TrafficNet.Get(i);
            if (r.length < 60f) continue;
            int limit = TrafficNet.LimitOf(r);
            for (float d = FirstAt; d < r.length - 10f; d += Every)
            {
                var p = TrafficNet.PointAt(r, d, out var f);
                f.y = 0f;
                if (f.sqrMagnitude < 0.01f) continue;
                f.Normalize();
                var left = new Vector3(-f.z, 0f, f.x);
                foreach (float off in Offsets)
                {
                    var at = p + left * off;
                    if (!Clear(at, put)) continue;
                    Sign(at, f, limit);
                    put.Add(at);
                    break;
                }
            }
        }
    }

    // Off every lane, not inside a building, not next to another sign
    private static bool Clear(Vector3 at, List<Vector3> put)
    {
        foreach (var q in put) if ((q - at).sqrMagnitude < Apart * Apart) return false;
        for (int i = 0; i < TrafficNet.Count; i++)
            if (TrafficNet.DistanceTo(TrafficNet.Get(i), at) < 2.4f) return false;
        if (Physics.CheckCapsule(at + Vector3.up * 0.6f, at + Vector3.up * 2.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) return false;
        return true;
    }

    private void Sign(Vector3 at, Vector3 facing, int limit)
    {
        // stand it on whatever ground is there (pavement, kerb)
        if (Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
        var g = new GameObject("SpeedLimit_" + limit);
        g.transform.SetParent(transform, false);
        g.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing));
        Part(PrimitiveType.Cylinder, g.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 1.2f, 0.08f), Quaternion.identity, PoleMat);
        // the face (a quad looking back at the traffic) on a grey disc
        Part(PrimitiveType.Cylinder, g.transform, new Vector3(0f, 2.55f, 0.03f), new Vector3(0.82f, 0.02f, 0.82f), Quaternion.Euler(90f, 0f, 0f), BackMat);
        Part(PrimitiveType.Quad, g.transform, new Vector3(0f, 2.55f, -0.005f), new Vector3(0.8f, 0.8f, 1f), Quaternion.identity, Face(limit));
        var col = g.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.2f, 0f);
        col.radius = 0.1f;
        col.height = 2.4f;
    }

    private static void Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
    private static Material PoleMat => pole != null ? pole : pole = new Material(Lit) { color = new Color(0.62f, 0.64f, 0.67f) };
    private static Material BackMat => back != null ? back : back = new Material(Lit) { color = new Color(0.5f, 0.52f, 0.55f) };

    // White disc, red ring, the number in black (drawn once per limit)
    private static Material Face(int limit)
    {
        if (faces.TryGetValue(limit, out var m) && m != null) return m;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "SpeedLimit" + limit };
        var px = new Color32[n * n];
        var white = new Color32(250, 250, 250, 255);
        var red = new Color32(214, 30, 36, 255);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                px[y * n + x] = d > 1f ? clear : d > 0.78f ? red : white;
            }
        Digits(px, n, limit.ToString());
        tex.SetPixels32(px);
        tex.Apply(true);
        m = new Material(Lit) { mainTexture = tex, color = Color.white };
        m.SetTexture("_BaseMap", tex);
        // cut out round the disc
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Smoothness", 0.3f);
        faces[limit] = m;
        return m;
    }

    // 3x5 pixel digits, scaled up, centred
    private static readonly string[] Font =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111",
    };

    private static void Digits(Color32[] px, int n, string text)
    {
        const int cell = 9, gap = 6;
        int w = text.Length * 3 * cell + (text.Length - 1) * gap, h = 5 * cell;
        int x0 = (n - w) / 2, y0 = (n - h) / 2;
        var ink = new Color32(20, 20, 20, 255);
        for (int c = 0; c < text.Length; c++)
        {
            string glyph = Font[text[c] - '0'];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                {
                    if (glyph[row * 3 + col] != '1') continue;
                    int gx = x0 + c * (3 * cell + gap) + col * cell, gy = y0 + (4 - row) * cell;
                    for (int y = gy; y < gy + cell; y++)
                        for (int x = gx; x < gx + cell; x++) px[y * n + x] = ink;
                }
        }
    }
}
