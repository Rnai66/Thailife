using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Graphics quality (คุณภาพภาพ) for slower phones: render scale, anti-aliasing, shadows and frame
// rate, saved on the device and changed in เมนู → ตั้งค่า. Also writes the average frame rate to the
// log every 30 s, so performance on a real iPhone shows up in game.log.
public class GraphicsQuality : MonoBehaviour
{
    private const string Key = "gfx.quality";
    public static readonly string[] Names = { "ประหยัด", "กลาง", "สวยสุด" };

    private static GraphicsQuality instance;
    private float frames, elapsed;

    public static int Level
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(Key, 2), 0, 2);
        set { PlayerPrefs.SetInt(Key, Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); Apply(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GraphicsQuality");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GraphicsQuality>();
        Apply();
    }

    public static void Apply()
    {
        int level = Level;
#if !UNITY_EDITOR // in the Editor this would rewrite the project's pipeline asset; phones only
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = level == 0 ? 0.75f : level == 1 ? 0.9f : 1f;
            urp.msaaSampleCount = level == 0 ? 1 : level == 1 ? 2 : 4;
            urp.shadowDistance = level == 0 ? 0f : level == 1 ? 35f : 60f;
        }
#endif
        Application.targetFrameRate = level == 0 ? 30 : 60;
    }

    private void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < 30f) return;
        Debug.Log($"[Perf] {frames / elapsed:0.0} fps · {SceneManager.GetActiveScene().name} · quality {Names[Level]}");
        frames = 0f;
        elapsed = 0f;
    }
}
