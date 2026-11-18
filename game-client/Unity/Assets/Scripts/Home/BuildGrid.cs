using UnityEngine;

// The grid drawn on the ground while you build (BuildUI) or set out your café (CafeUI): thin lines a
// metre apart over the area you may use, with a brighter edge, so you can see where a piece will snap.
// One mesh, laid flat in the anchor's own space (x0..x1, z0..z1) just above the ground.
public class BuildGrid : MonoBehaviour
{
    private Mesh mesh;
    private string shownKey;

    public static BuildGrid Make(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>();
        var r = go.AddComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.AddComponent<BuildGrid>();
    }

    // Show the grid over (x0..x1, z0..z1) of `anchor`, at height y (world); step metres between lines
    public void Show(Transform anchor, float x0, float x1, float z0, float z1, float y, float step = 1f, float scale = 1f)
    {
        if (anchor == null) { Hide(); return; }
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(new Vector3(anchor.position.x, y + 0.04f, anchor.position.z), Quaternion.Euler(0f, anchor.eulerAngles.y, 0f));
        transform.localScale = Vector3.one * scale;
        string key = $"{x0:F2},{x1:F2},{z0:F2},{z1:F2},{step:F2}";
        if (key == shownKey && mesh != null) return;
        shownKey = key;
        Build(x0, x1, z0, z1, step);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Build(float x0, float x1, float z0, float z1, float step)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var cols = new System.Collections.Generic.List<Color>();
        var tris = new System.Collections.Generic.List<int>();
        var line = new Color(1f, 1f, 0.92f, 1f);
        var edge = new Color(1f, 0.82f, 0.25f, 1f);

        void Quad(Vector3 a, Vector3 b, float width, Color c)
        {
            var dir = (b - a).normalized;
            var side = new Vector3(-dir.z, 0f, dir.x) * (width * 0.5f);
            int i = verts.Count;
            verts.Add(a - side); verts.Add(a + side); verts.Add(b + side); verts.Add(b - side);
            for (int k = 0; k < 4; k++) cols.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        for (float x = Mathf.Ceil(x0 / step) * step; x <= x1 + 0.001f; x += step)
            Quad(new Vector3(x, 0f, z0), new Vector3(x, 0f, z1), 0.035f, line);
        for (float z = Mathf.Ceil(z0 / step) * step; z <= z1 + 0.001f; z += step)
            Quad(new Vector3(x0, 0f, z), new Vector3(x1, 0f, z), 0.035f, line);
        Quad(new Vector3(x0, 0.005f, z0), new Vector3(x1, 0.005f, z0), 0.09f, edge);
        Quad(new Vector3(x0, 0.005f, z1), new Vector3(x1, 0.005f, z1), 0.09f, edge);
        Quad(new Vector3(x0, 0.005f, z0), new Vector3(x0, 0.005f, z1), 0.09f, edge);
        Quad(new Vector3(x1, 0.005f, z0), new Vector3(x1, 0.005f, z1), 0.09f, edge);

        if (mesh == null) mesh = new Mesh { name = "BuildGrid" };
        mesh.Clear();
        mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        // one plain unlit colour (the edge stands out by being wider)
        GetComponent<MeshRenderer>().sharedMaterial = BuildKit.FlatMat(new Color(1f, 0.97f, 0.85f));
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
