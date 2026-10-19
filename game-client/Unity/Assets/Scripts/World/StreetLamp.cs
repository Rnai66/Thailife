using UnityEngine;

// A lamp (power-pole street lamp, lamp post, solar lamp or a light hanging under a roof):
// its light only shines at night (follows DayNightCycle's ambient). A wide cone pointing
// straight down puts a warm pool of light on the ground; the numbers are applied here so
// tuning them never needs the world rebuilt.
public class StreetLamp : MonoBehaviour
{
    public enum Kind { PoleStreet, Post, Solar, Hanging }

    [SerializeField] private Kind kind = Kind.PoleStreet;

    private Light lamp;
    private float nextCheck;

    public void Configure(Kind k)
    {
        kind = k;
        Setup();
    }

    private void Awake() => Setup();

    private void Setup()
    {
        lamp = GetComponentInChildren<Light>(true);
        if (lamp == null) return;
        lamp.type = LightType.Spot;
        lamp.transform.rotation = Quaternion.LookRotation(Vector3.down, transform.forward);
        lamp.shadows = LightShadows.None;
        switch (kind)
        {
            case Kind.PoleStreet: Set(135f, 18f, 22f, new Color(1f, 0.8f, 0.55f)); break;
            case Kind.Post:       Set(130f, 12f, 9f, new Color(1f, 0.85f, 0.6f)); break;
            case Kind.Solar:      Set(125f, 11f, 7f, new Color(0.85f, 0.92f, 1f)); break; // cool white LED
            case Kind.Hanging:    Set(140f, 9f, 5f, new Color(1f, 0.82f, 0.55f)); break;
        }
    }

    private void Set(float angle, float range, float intensity, Color color)
    {
        lamp.spotAngle = angle;
        lamp.innerSpotAngle = angle * 0.5f;
        lamp.range = range;
        lamp.intensity = intensity;
        lamp.color = color;
    }

    private void Update()
    {
        if (lamp == null || Time.time < nextCheck) return;
        nextCheck = Time.time + 1f + Random.value; // spread the checks out
        lamp.enabled = RenderSettings.ambientIntensity < 0.8f;
    }
}
