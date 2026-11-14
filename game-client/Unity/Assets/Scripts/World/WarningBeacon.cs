using UnityEngine;

// ไฟกระพริบ: police red-and-blue (or amber) lamps that flash in turn — on the checkpoint's barriers,
// its signs and the patrol car's light bar. Group A and group B take turns, each with an optional
// point light that only shines once it's dark (DayNightCycle.Dark) so the road around glows too.
// Set up by the Editor (Police.Checkpoint) with Configure.
public class WarningBeacon : MonoBehaviour
{
    [SerializeField] private Renderer[] groupA = new Renderer[0];
    [SerializeField] private Renderer[] groupB = new Renderer[0];
    [SerializeField] private Color colorA = new Color(1f, 0.1f, 0.08f);
    [SerializeField] private Color colorB = new Color(0.1f, 0.3f, 1f);
    [SerializeField] private Light lightA, lightB;
    [SerializeField] private float period = 0.9f;   // one red + one blue
    [SerializeField] private float lightIntensity = 6f;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private float offset;
    private int lastPhase = -1;

    public void Configure(Renderer[] a, Renderer[] b, Color ca, Color cb, Light la, Light lb, float seconds = 0.9f)
    {
        groupA = a; groupB = b; colorA = ca; colorB = cb; lightA = la; lightB = lb; period = seconds;
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        offset = Mathf.Abs(transform.position.GetHashCode() % 1000) / 1000f * period; // not all in step
    }

    private void Update()
    {
        // a double flash per colour: on-off-on, then the other side
        float t = Mathf.Repeat(Time.time + offset, period) / period;
        int phase = t < 0.5f ? (Mathf.Repeat(t, 0.25f) < 0.16f ? 1 : 0) : (Mathf.Repeat(t, 0.25f) < 0.16f ? 2 : 0);
        if (phase == lastPhase) return;
        lastPhase = phase;
        Set(groupA, colorA, phase == 1);
        Set(groupB, colorB, phase == 2);
        bool dark = DayNightCycle.Dark;
        if (lightA != null) { lightA.enabled = dark && phase == 1; lightA.intensity = lightIntensity; }
        if (lightB != null) { lightB.enabled = dark && phase == 2; lightB.intensity = lightIntensity; }
    }

    private void Set(Renderer[] group, Color c, bool on)
    {
        foreach (var r in group)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseId, on ? c : c * 0.25f);
            block.SetColor(EmissionId, on ? c * 6f : Color.black);
            r.SetPropertyBlock(block);
        }
    }
}
