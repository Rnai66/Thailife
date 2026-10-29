using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lists what stands on the City's pavements (anything with a collider over the walking strips),
// so buildings and props that block the footpath can be found and moved back.
public static class PavementCheck
{
    [MenuItem("Thai Life/Advanced/List City Pavement Obstacles")]
    public static void List()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/City.unity");
        var strips = new (string name, float z0, float z1)[] { ("north", -5.3f, -2.9f), ("south", -21.5f, -17.7f) };
        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c.isTrigger) continue;
            var b = c.bounds;
            if (b.max.y < 0.15f || b.min.y > 2.2f || Mathf.Abs(b.center.x) > 60f) continue; // ground, overhead
            foreach (var s in strips)
                if (b.max.z > s.z0 && b.min.z < s.z1)
                    sb.AppendLine($"{s.name}\t{Path(c.transform)}\tx {b.min.x:F1}..{b.max.x:F1}\tz {b.min.z:F1}..{b.max.z:F1}\ty {b.min.y:F1}..{b.max.y:F1}");
        }
        File.WriteAllText("Builds/pavement_obstacles.txt", sb.ToString());
        Debug.Log("[ThaiLife] Pavement obstacles written to Builds/pavement_obstacles.txt");
    }

    private static string Path(Transform t)
    {
        var parts = new System.Collections.Generic.List<string>();
        for (; t != null; t = t.parent) parts.Insert(0, t.name);
        return string.Join("/", parts.Skip(1).Take(3));
    }
}
