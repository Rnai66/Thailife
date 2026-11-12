using UnityEngine;

// A lamp (power-pole street lamp, lamp post, solar lamp or a light hanging under a roof):
// its light only shines at night (follows DayNightCycle's ambient). A wide cone pointing
// straight down puts a warm pool of light on the ground; the numbers are applied here so
// tuning them never needs the world rebuilt.
public class StreetLamp : MonoBehaviour
{
    public enum Kind { PoleStreet, Post, Solar, Hanging, Ring, Orange }

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
            case Kind.PoleStreet: Set(140f, 24f, 40f, new Color(1f, 0.82f, 0.58f)); break;
            case Kind.Post:       Set(140f, 16f, 18f, new Color(1f, 0.86f, 0.62f)); break;
            case Kind.Solar:      Set(135f, 15f, 14f, new Color(0.88f, 0.94f, 1f)); break; // cool white LED
            case Kind.Hanging:    Set(145f, 12f, 10f, new Color(1f, 0.84f, 0.58f)); break;
            case Kind.Ring:       Set(110f, 9f, 16f, new Color(1f, 0.96f, 0.88f)); break; // bright stage light
            case Kind.Orange:     Set(140f, 11f, 7f, new Color(1f, 0.55f, 0.15f)); break; // sodium-orange (cow pen)
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
        if (kind == Kind.Ring) { lamp.enabled = true; nextCheck = float.MaxValue; return; } // rings are lit day and night
        nextCheck = Time.time + 1f + Random.value; // spread the checks out
        lamp.enabled = DayNightCycle.Dark;
    }
}
