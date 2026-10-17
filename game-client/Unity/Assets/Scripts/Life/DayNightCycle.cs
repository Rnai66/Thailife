using UnityEngine;

// Moves the sun with GameClock: sunrise 06:00, noon overhead, sunset 18:00, a dim blue moonlight at night.
[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.25f;
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.84f);
    [SerializeField] private Color duskColor = new Color(1f, 0.6f, 0.35f);
    [SerializeField] private Color nightColor = new Color(0.45f, 0.55f, 0.85f);

    private Light sun;
    private Color dayAmbient;

    private void Awake()
    {
        sun = GetComponent<Light>();
        dayAmbient = RenderSettings.ambientLight;
        RenderSettings.sun = sun;
    }

    private void Update()
    {
        // 06:00 → 0°, 12:00 → 90°, 18:00 → 180°
        float minutes = GameClock.MinuteOfDay;
        float sunAngle = (minutes / GameClock.MinutesPerDay) * 360f - 90f;
        float height = Mathf.Sin(sunAngle * Mathf.Deg2Rad); // -1 night .. 1 noon

        if (height > 0f)
        {
            transform.rotation = Quaternion.Euler(Mathf.Max(8f, sunAngle), -30f, 0f);
            float dusk = 1f - Mathf.Clamp01(height * 3f); // warm light near sunrise/sunset
            sun.color = Color.Lerp(dayColor, duskColor, dusk);
            sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, Mathf.Clamp01(height * 2f));
        }
        else
        {
            // Moon: light from high above so the village stays readable
            transform.rotation = Quaternion.Euler(60f, 150f, 0f);
            sun.color = nightColor;
            sun.intensity = nightIntensity;
        }

        float daylight = Mathf.Clamp01(height * 2f + 0.3f);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.15f, 0.18f, 0.3f), dayAmbient, daylight);
        RenderSettings.ambientIntensity = Mathf.Lerp(0.5f, 1f, daylight);
    }
}
