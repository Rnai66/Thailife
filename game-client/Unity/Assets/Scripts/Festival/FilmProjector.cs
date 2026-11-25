using UnityEngine;

// The หนังกลางแปลง film projector (OutdoorCinema builds it): the two reels turn (the full one feeding,
// the take-up one winding faster), and the lamp's beam to the screen flickers the way an arc lamp
// does — faint in daylight, a bright cone through the dark once night falls. The lamp house glows too,
// and smoke from the burning carbons puffs out of its chimney and drifts up.
public class FilmProjector : MonoBehaviour
{
    [SerializeField] private Transform[] reels;
    [SerializeField] private float[] reelSpeed;
    [SerializeField] private Renderer[] beams;
    [SerializeField] private Color[] beamColors;
    [SerializeField] private Light lamp;
    [SerializeField] private Renderer[] smoke;
    [SerializeField] private Color smokeColor;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private float lampIntensity;
    private float seed;

    public void Configure(Transform[] reels, float[] reelSpeed, Renderer[] beams, Color[] beamColors, Light lamp,
        Renderer[] smoke = null, Color smokeColor = default)
    {
        this.reels = reels;
        this.reelSpeed = reelSpeed;
        this.beams = beams;
        this.beamColors = beamColors;
        this.lamp = lamp;
        this.smoke = smoke;
        this.smokeColor = smokeColor;
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        lampIntensity = lamp != null ? lamp.intensity : 0f;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null || (cam.transform.position - transform.position).sqrMagnitude > 120f * 120f) return;

        float dt = Time.deltaTime;
        if (reels != null)
            for (int i = 0; i < reels.Length; i++)
                if (reels[i] != null) reels[i].Rotate(0f, (reelSpeed != null && i < reelSpeed.Length ? reelSpeed[i] : 90f) * dt, 0f, Space.Self);

        // arc-lamp flicker: a slow wander, a quick shimmer and now and then a dip
        float t = Time.time + seed;
        float flicker = 0.86f + 0.1f * Mathf.PerlinNoise(t * 1.3f, 0.3f) + 0.06f * Mathf.PerlinNoise(t * 17f, 2.1f);
        if (Mathf.PerlinNoise(t * 3.1f, 7.7f) > 0.82f) flicker *= 0.7f;
        float dark = 1f - Mathf.Clamp01(DayNightCycle.Daylight);
        float strength = Mathf.Lerp(0.18f, 1f, dark) * flicker;

        if (beams != null)
            for (int i = 0; i < beams.Length; i++)
            {
                var r = beams[i];
                if (r == null) continue;
                var c = beamColors != null && i < beamColors.Length ? beamColors[i] : new Color(0.05f, 0.045f, 0.035f);
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, c * strength);
                r.SetPropertyBlock(block);
            }
        if (lamp != null) lamp.intensity = lampIntensity * (0.4f + 0.6f * dark) * flicker;

        // the puffs rise one after another, swelling and thinning out as they go
        if (smoke != null)
            for (int i = 0; i < smoke.Length; i++)
            {
                var r = smoke[i];
                if (r == null) continue;
                float k = Mathf.Repeat(Time.time * 0.3f + i / (float)smoke.Length, 1f);
                var p = r.transform;
                p.localPosition = new Vector3(Mathf.Sin(k * 5f + i) * 0.06f * k, k * 1.1f, Mathf.Cos(k * 4f + i * 2f) * 0.05f * k);
                p.localScale = Vector3.one * Mathf.Lerp(0.09f, 0.55f, k);
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, smokeColor * (Mathf.Sin(k * Mathf.PI) * Mathf.Lerp(0.6f, 1.3f, dark)));
                r.SetPropertyBlock(block);
            }
    }
}
