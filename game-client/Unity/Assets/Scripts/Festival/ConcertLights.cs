using System.Collections.Generic;
using UnityEngine;

// The beach concert light show, in time with the band (124 bpm): moving heads sweep their beams
// over the crowd, colours chase and change every few bars, uplights and stage cans pulse on the
// beat, the LED screen flows through colours, lasers fan out over the audience and a strobe hits
// on the big moments. Runs only while the concert is on (it lives under "Attractions").
public class ConcertLights : MonoBehaviour
{
    private const float Bpm = 124f;

    private class Head
    {
        public Transform t;
        public Renderer beam;
        public Light light;
    }

    private readonly List<Head> heads = new List<Head>();
    private readonly List<Renderer> upBeams = new List<Renderer>();
    private readonly List<Transform> lasers = new List<Transform>();
    private readonly List<float> laserYaw = new List<float>();
    private readonly List<Renderer> laserRays = new List<Renderer>();
    private readonly List<Renderer> cans = new List<Renderer>();
    private readonly List<Light> washes = new List<Light>();
    private Renderer screen;
    private Light strobe;
    private MaterialPropertyBlock block;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            switch (child.name)
            {
                case "MovingHead":
                    heads.Add(new Head
                    {
                        t = child,
                        beam = child.Find("Beam")?.GetComponent<Renderer>(),
                        light = child.GetComponentInChildren<Light>(true),
                    });
                    break;
                case "Uplight":
                    var up = child.Find("Beam")?.GetComponent<Renderer>();
                    if (up != null) upBeams.Add(up);
                    break;
                case "LaserPivot":
                    lasers.Add(child);
                    laserYaw.Add(child.localEulerAngles.y);
                    var ray = child.GetComponentInChildren<Renderer>(true);
                    if (ray != null) laserRays.Add(ray);
                    break;
                case "StageCan":
                    var can = child.GetComponent<Renderer>();
                    if (can != null) cans.Add(can);
                    break;
                case "StageLight":
                    var wash = child.GetComponent<Light>();
                    if (wash != null) washes.Add(wash);
                    break;
                case "LedScreen":
                    screen = child.GetComponent<Renderer>();
                    break;
                case "Strobe":
                    strobe = child.GetComponent<Light>();
                    break;
            }
        }
    }

    private void Update()
    {
        float t = Time.time;
        float beats = t * Bpm / 60f;
        float pulse = Mathf.Exp(-(beats % 1f) * 5f);          // kicks on every beat
        int bar = Mathf.FloorToInt(beats / 4f);
        int mode = (bar / 2) % 3;                             // a new look every two bars
        bool drop = (bar % 8) == 7;                           // every eighth bar: strobe and full lasers
        float night = RenderSettings.ambientIntensity < 0.8f ? 1f : 0.55f;

        for (int i = 0; i < heads.Count; i++)
        {
            var h = heads[i];
            float pan = Mathf.Sin(t * 0.7f + i * 0.9f) * 40f;
            float tilt = 22f + Mathf.Sin(t * 1.1f + i * 0.6f) * 26f;
            if (mode == 2) { pan = (i % 2 == 0 ? 1f : -1f) * Mathf.Sin(t * 1.6f) * 45f; tilt = 30f; } // crossing beams
            h.t.localRotation = Quaternion.Euler(tilt, pan, 0f);

            Color c = Palette(t, i, mode);
            if (h.light != null)
            {
                h.light.color = c;
                h.light.intensity = 22f + 18f * pulse;
            }
            Tint(h.beam, BaseColor, c * (0.18f + 0.22f * pulse) * night);
        }

        for (int i = 0; i < upBeams.Count; i++)
        {
            Color c = Palette(t + 0.5f, i + 3, (mode + 1) % 3);
            bool on = mode != 1 || (Mathf.FloorToInt(beats) + i) % 2 == 0; // chase in mode 1
            Tint(upBeams[i], BaseColor, on ? c * (0.12f + 0.25f * pulse) * night : Color.black);
        }

        for (int i = 0; i < cans.Count; i++)
        {
            Color c = Palette(t, i, mode);
            bool on = (Mathf.FloorToInt(beats * 2f) % cans.Count) == i || mode == 0;
            Tint(cans[i], EmissionColor, c * (on ? 6f + 6f * pulse : 1f));
        }

        for (int i = 0; i < washes.Count; i++)
        {
            washes[i].color = Palette(t + 1f, i * 2, mode);
            washes[i].intensity = 5f + 6f * pulse;
        }

        if (screen != null)
        {
            Color c = Color.HSVToRGB((t * 0.05f) % 1f, 0.8f, 1f);
            Color flash = Color.HSVToRGB((t * 0.05f + 0.5f) % 1f, 0.7f, 1f);
            Tint(screen, EmissionColor, Color.Lerp(c, flash, pulse * 0.6f) * (2f + 2.5f * pulse));
        }

        // Lasers: sweep and dip over the crowd; on for the second half of every phrase and in drops
        bool lasersOn = drop || (bar % 4) >= 2;
        Color laser = Color.HSVToRGB(((bar / 2) * 0.33f + 0.3f) % 1f, 1f, 1f);
        for (int i = 0; i < lasers.Count; i++)
        {
            float yaw = laserYaw[i] + Mathf.Sin(t * 0.8f + i * 0.4f) * 10f;
            float dip = 6f + Mathf.Sin(t * 1.3f + i) * 5f;
            lasers[i].localRotation = Quaternion.Euler(dip, yaw, 0f);
        }
        foreach (var ray in laserRays)
            Tint(ray, BaseColor, lasersOn ? laser * (0.9f + 0.6f * pulse) * night : Color.black);

        if (strobe != null)
            strobe.intensity = drop && (beats * 4f % 1f) < 0.25f ? 45f : 0f;
    }

    // mode 0: everyone the same colour, slowly turning; 1: a rainbow across the rig; 2: two colours alternating
    private static Color Palette(float t, int i, int mode)
    {
        float hue = mode == 0 ? t * 0.06f
                  : mode == 1 ? t * 0.06f + i * 0.14f
                  : t * 0.04f + (i % 2) * 0.5f;
        return Color.HSVToRGB(hue % 1f, 1f, 1f);
    }

    private void Tint(Renderer r, int property, Color c)
    {
        if (r == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(property, c);
        r.SetPropertyBlock(block);
    }
}
