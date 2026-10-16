using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Repair Scene (add missing parts)
// Safe to run any time: adds only what is missing — Nakama connection + match sync + career,
// mobile controls, the Thai village and training targets. Never deletes anything.
public static class RepairScene
{
    [MenuItem("Thai Life/Repair Scene (add missing parts)", priority = 0)]
    private static void Repair()
    {
        if (Object.FindAnyObjectByType<PlayerController>() == null)
        {
            EditorUtility.DisplayDialog("Repair Scene", "ไม่พบ Player ในฉากนี้ — เปิดฉาก Assets/Scenes/ThaiVillage.unity ก่อน", "OK");
            return;
        }

        NakamaSetup.Setup();
        MobileControlsSetup.EnsureMobileControls();
        VillageBuilder.EnsureVillage();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ThaiLife] Scene repaired. Save (Cmd+S) and press Play.");
    }
}
