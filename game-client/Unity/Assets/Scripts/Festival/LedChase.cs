using UnityEngine;

// Strings of LED pixels round the outdoor cinema (and anywhere that wants a light show): each bulb is
// a renderer, and they run a pattern — a rainbow chasing along the string, two colours swapping,
// a pulse to the beat, or a fill that climbs up a pixel tube and drops. Brightest at night.
// Set up by the Editor (OutdoorCinema) with Configure.
public class LedChase : MonoBehaviour
{
    public enum Mode { Rainbow, Swap, Pulse, Climb }

    [SerializeField] private Renderer[] bulbs = new Renderer[0];
    [SerializeField] private Mode mode = Mode.Rainbow;
    [SerializeField] private Color colorA = Color.green, colorB = Color.magenta;
    [SerializeField] private float speed = 1f;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private float offset, next;

    public void Configure(Renderer[] lights, Mode pattern, Color a, Color b, float rate = 1f)
    {
        bulbs = lights; mode = pattern; colorA = a; colorB = b; speed = rate;
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        offset = Mathf.Abs(transform.position.GetHashCode() % 997) / 97f;
    }

    private void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 1f / 20f; // 20 steps a second is plenty for bulbs
        var cam = Camera.main;
        if (cam != null && (cam.transform.position - transform.position).sqrMagnitude > 120f * 120f) return;
        float t = Time.time * speed + offset;
        float bright = DayNightCycle.Dark ? 5f : 2.2f;
        int n = bulbs.Length;
        for (int i = 0; i < n; i++)
        {
            var r = bulbs[i];
            if (r == null) continue;
            Color c;
            float level = 1f;
            switch (mode)
            {
                case Mode.Rainbow:
                    c = Color.HSVToRGB(Mathf.Repeat(i / 14f - t * 0.35f, 1f), 0.85f, 1f);
                    level = Mathf.Repeat(i - t * 6f, 6f) < 1.2f ? 1.6f : 0.75f; // a bright head running along
                    break;
                case Mode.Swap:
                    c = ((i + (int)(t * 3f)) % 2 == 0) ? colorA : colorB;
                    break;
                case Mode.Pulse:
                    c = colorA;
                    level = 0.35f + 0.65f * Mathf.Pow(1f - Mathf.Repeat(t * 2.2f, 1f), 2f); // a thump on each beat
                    break;
                default: // Climb: fills up the tube, flashes, drops
                    float fill = Mathf.Repeat(t * 0.8f, 1.3f) * n;
                    c = Color.Lerp(colorA, colorB, i / Mathf.Max(1f, n - 1f));
                    level = i < fill ? (fill > n ? (Mathf.Repeat(t * 8f, 1f) < 0.5f ? 1.5f : 0.2f) : 1f) : 0.12f;
                    break;
            }
            r.GetPropertyBlock(block);
            block.SetColor(BaseId, c * Mathf.Min(1f, level));
            block.SetColor(EmissionId, c * bright * level);
            r.SetPropertyBlock(block);
        }
    }
}
