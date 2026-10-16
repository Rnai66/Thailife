using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Build Tambon Scene (ตำบล)
// Creates Assets/Scenes/Tambon.unity: the sub-district centre with the boxing camp (career tier 3)
// and a songthaew stop back to the village. Comes with its own Network and mobile controls.
public static class TambonBuilder
{
    private const string ScenePath = "Assets/Scenes/Tambon.unity";

    [MenuItem("Thai Life/Build Tambon Scene (ตำบล)")]
    private static void Build()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("ฉากตำบล", "Tambon.unity มีอยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?\n(ไม่กระทบฉากหมู่บ้าน)", "สร้างใหม่", "ยกเลิก"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = ThaiVillageSceneBuilder.CreateBaseScene();
        GameObject.Find("Ground").transform.localScale = new Vector3(6f, 1f, 6f); // 60 x 60 m

        NakamaSetup.Setup();
        MobileControlsSetup.EnsureMobileControls();

        var root = new GameObject("Tambon").transform;

        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var paint = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        PrimitiveKit.Box("Road", root, new Vector3(0f, 0.02f, -8f), new Vector3(60f, 0.04f, 4f), asphalt);
        for (float x = -28f; x <= 28f; x += 4f)
            PrimitiveKit.Box("Dash", root, new Vector3(x, 0.045f, -8f), new Vector3(1.6f, 0.01f, 0.15f), paint, collider: false);

        Places.BoxingCamp(root, new Vector3(0f, 0f, 9f), "ค่ายมวยตำบลหนองบัว");
        Places.BusStop(root, new Vector3(9f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(12f, 0f, -11f), "กลับหมู่บ้าน\n(ยืนใต้ป้ายรถ)", 180f);
        Places.Sign(root, new Vector3(-3f, 0f, -4.5f), "ตำบลหนองบัว", 0f);

        foreach (var pos in new[] { new Vector3(-14f, 0f, 4f), new Vector3(15f, 0f, 14f), new Vector3(-10f, 0f, 20f), new Vector3(-22f, 0f, -16f), new Vector3(22f, 0f, -18f) })
            Places.ShadeTree(root, pos);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        ThaiVillageSceneBuilder.AddSceneToBuildSettings(ScenePath);

        Debug.Log($"[ThaiLife] Built {ScenePath}. Open ThaiVillage again and travel here from the songthaew stop.");
    }
}
