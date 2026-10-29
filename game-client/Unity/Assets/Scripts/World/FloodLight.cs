using UnityEngine;

// A floodlight on a landmark (วัดอรุณ glowing gold after dark): its light only shines at night,
// following DayNightCycle's ambient like StreetLamp.
[RequireComponent(typeof(Light))]
public class FloodLight : MonoBehaviour
{
    private Light lamp;
    private float nextCheck;

    private void Awake()
    {
        lamp = GetComponent<Light>();
        lamp.enabled = false;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 1f + Random.value; // spread the checks out
        lamp.enabled = RenderSettings.ambientIntensity < 0.8f;
    }

    // Editor previews: show it as by night or by day
    public void Preview(bool night) => GetComponent<Light>().enabled = night;
}
