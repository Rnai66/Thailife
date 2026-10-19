using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// The "premium" image for every scene, added at runtime so no scene needs editing:
// filmic tone mapping (ACES), a soft glow on bright things (gold, sky), richer colour,
// a gentle vignette, anti-aliasing, soft sun shadows and an atmospheric haze that darkens
// at night together with DayNightCycle.
public class ScenePolish : MonoBehaviour
{
    private static readonly Color DayFog = new Color(0.74f, 0.83f, 0.9f);
    private static readonly Color NightFog = new Color(0.08f, 0.1f, 0.17f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    private static void Apply()
    {
        if (FindAnyObjectByType<ScenePolish>() != null) return;
        var go = new GameObject("ScenePolish");
        go.AddComponent<ScenePolish>();

        foreach (var cam in Camera.allCameras)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
        }

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.65f);
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.3f);   // ACES darkens; bring the midtones back up
        color.contrast.Override(12f);
        color.saturation.Override(15f);
        profile.Add<WhiteBalance>(true).temperature.Override(6f); // a touch of warm tropical light
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.5f);
        volume.sharedProfile = profile;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 170f;

        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
        }
    }

    private void Update()
    {
        // Haze follows the day/night ambient (DayNightCycle sets ambientIntensity 0.5 at night .. 1 by day)
        float day = Mathf.InverseLerp(0.5f, 1f, RenderSettings.ambientIntensity);
        RenderSettings.fogColor = Color.Lerp(NightFog, DayFog, day);
    }
}
