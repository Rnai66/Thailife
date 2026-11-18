using System.Collections.Generic;
using UnityEngine;

// Little pictures of each buildable piece for the catalogue (BuildUI): the piece is made far below the
// map (BuildKit), lit by a lamp of its own, filmed once from the front-left by a camera of its own into
// a small texture, then thrown away. Kept for the session.
public static class BuildThumbs
{
    private const int Size = 160;
    private static readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
    private static readonly Vector3 Studio = new Vector3(0f, -3000f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    public static Texture Get(string kind)
    {
        if (cache.TryGetValue(kind, out var rt) && rt != null && rt.IsCreated()) return rt;
        rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { name = "Thumb_" + kind, antiAliasing = 2 };
        rt.Create();

        var piece = BuildKit.Make(kind, 0, null);
        piece.transform.position = Studio;
        var renderers = piece.GetComponentsInChildren<Renderer>();
        var b = new Bounds(Studio + Vector3.up * 0.5f, Vector3.one * 0.5f);
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        var camGo = new GameObject("ThumbCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.93f, 0.88f, 0.76f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        var dir = new Vector3(-0.75f, 0.65f, -1f).normalized; // pieces face -Z: look at the front, a little from the left and above
        float dist = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
        camGo.transform.position = b.center + dir * dist;
        camGo.transform.LookAt(b.center);

        var lampGo = new GameObject("ThumbLamp");
        lampGo.transform.position = b.center + new Vector3(-2f, 4f, -4f) + dir * 2f;
        var lamp = lampGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = dist * 3f + 10f;
        lamp.intensity = 3f;

        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        // off at once: Destroy waits for the end of the frame, and the next picture is filmed in the same spot
        piece.SetActive(false);
        lampGo.SetActive(false);
        Object.Destroy(piece);
        Object.Destroy(camGo);
        Object.Destroy(lampGo);
        cache[kind] = rt;
        return rt;
    }
}
