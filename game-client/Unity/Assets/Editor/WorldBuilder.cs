using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Build Whole World (all scenes)
// Rebuilds the tambon and city scenes, then rebuilds the village layout inside ThaiVillage.unity
// (keeping the player, network and anything else outside the "Village" object) and saves everything.
public static class WorldBuilder
{
    private const string VillageScene = "Assets/Scenes/ThaiVillage.unity";

    [MenuItem("Thai Life/Build Whole World (all scenes)", priority = 1)]
    private static void BuildAll()
    {
        if (!EditorUtility.DisplayDialog("สร้างโลกทั้งหมด",
                "จะสร้างฉาก ตำบล และ เมือง ใหม่ แล้วสร้างผังหมู่บ้านใหม่ในฉาก ThaiVillage\n(Player/Network ในหมู่บ้านไม่ถูกลบ)",
                "สร้าง", "ยกเลิก"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TambonBuilder.BuildTambon(ask: false);
        TambonBuilder.BuildCity(ask: false);
        TambonBuilder.BuildBeach(ask: false);

        var village = EditorSceneManager.OpenScene(VillageScene, OpenSceneMode.Single);
        VillageBuilder.Build(ask: false);
        EditorSceneManager.SaveScene(village);

        Debug.Log("[ThaiLife] World built: ThaiVillage (หมู่บ้าน), Tambon (ตำบล), City (เมือง). Press Play in ThaiVillage.");
    }
}
