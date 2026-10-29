using UnityEngine;

// Night-time dressing (the glowing outlines on the temples): its children only show after dark,
// following DayNightCycle's ambient like FloodLight.
public class NightOnly : MonoBehaviour
{
    private float nextCheck;
    private bool shown = true;

    private void Awake() => Show(false);

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 1f + Random.value;
        Show(RenderSettings.ambientIntensity < 0.8f);
    }

    private void Show(bool on)
    {
        if (on == shown) return;
        shown = on;
        foreach (Transform child in transform) child.gameObject.SetActive(on);
    }

    // Editor previews: show it as by night or by day
    public void Preview(bool night)
    {
        shown = !night;
        Show(night);
    }
}
