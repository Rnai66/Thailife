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
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("สร้างโลกทั้งหมด",
                "จะสร้างฉาก ตำบล และ เมือง ใหม่ แล้วสร้างผังหมู่บ้านใหม่ในฉาก ThaiVillage\n(Player/Network ในหมู่บ้านไม่ถูกลบ)",
                "สร้าง", "ยกเลิก"))
            return;

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TambonBuilder.BuildTambon(ask: false);
        TambonBuilder.BuildCity(ask: false);
        TambonBuilder.BuildBeach(ask: false);
        TambonBuilder.BuildIsland(ask: false);
        TambonBuilder.BuildBangkok(ask: false);
        ProvinceNakhonSi.Build(ask: false);
        ProvinceBuriram.Build(ask: false);
        ProvinceSongkhla.Build(ask: false);
        ProvinceChiangMai.Build(ask: false);
        ProvincePattaya.Build(ask: false);
        ProvinceKhonKaen.Build(ask: false);
        ProvinceUbon.Build(ask: false);

        var village = EditorSceneManager.OpenScene(VillageScene, OpenSceneMode.Single);
        VillageBuilder.Build(ask: false);
        EditorSceneManager.SaveScene(village);

        KeepEmission();
        Debug.Log("[ThaiLife] World built: ThaiVillage (หมู่บ้าน), Tambon (ตำบล), City (เมือง). Press Play in ThaiVillage.");
    }

    // Saving the scenes has been known to drop the _EMISSION keyword from the glow materials (lamps,
    // headlights, neon) so they stop glowing in the build: switch it back on for every material with an
    // emission colour, and save.
    [MenuItem("Thai Life/Fix Glow Materials", priority = 40)]
    public static void KeepEmission()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/Materials" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || !m.HasProperty("_EmissionColor") || m.IsKeywordEnabled("_EMISSION")) continue;
            if (m.GetColor("_EmissionColor").maxColorComponent < 0.01f) continue;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ThaiLife] Glow materials checked: {n} switched back on");
    }
}
