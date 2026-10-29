using System.Collections.Generic;
using UnityEngine;

// เรือสำราญ dinner cruise on the Chao Phraya after dark: it sails up and down the river with its
// strings of bulbs chasing colours, the LED band and washes on the water changing in time with the
// ramwong band playing on board. By day it's moored out of sight (the boat is hidden).
public class NightCruise : MonoBehaviour
{
    private const float Bpm = 112f;

    [SerializeField] private float halfRun = 95f, speed = 3f;

    private Transform boat;
    private readonly List<Renderer> bulbs = new List<Renderer>();
    private readonly List<Renderer> bands = new List<Renderer>();
    private readonly List<Light> washes = new List<Light>();
    private AudioSource band;
    private MaterialPropertyBlock block;
    private float nextCheck;
    private bool night, preview;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // the lights are unlit

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        boat = transform.Find("Boat");
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Bulb" && child.TryGetComponent<Renderer>(out var b)) bulbs.Add(b);
            else if (child.name == "LedBand" && child.TryGetComponent<Renderer>(out var l)) bands.Add(l);
            else if (child.name == "Wash" && child.TryGetComponent<Light>(out var w)) washes.Add(w);
        }
        if (boat != null) boat.gameObject.SetActive(false);
        if (Application.isPlaying) SynthSounds.Prepare("ramwong");
    }

    private void Update()
    {
        if (!preview && Time.time >= nextCheck)
        {
            nextCheck = Time.time + 1f + Random.value;
            Show(RenderSettings.ambientIntensity < 0.8f);
        }
        if (!night || boat == null) return;
        Sail(Time.time);
        Lights(Time.time);
        Music();
    }

    private void Show(bool on)
    {
        night = on;
        if (boat != null) boat.gameObject.SetActive(on);
        if (!on && band != null) band.Stop();
    }

    // Up the river and back, turning round at each end
    private void Sail(float time)
    {
        float leg = halfRun * 2f / speed;
        float t = time % (leg * 2f);
        bool back = t >= leg;
        float k = (back ? t - leg : t) / leg;
        float x = Mathf.Lerp(-halfRun, halfRun, back ? 1f - k : k);
        boat.localPosition = new Vector3(x, Mathf.Sin(time * 0.9f) * 0.05f, 0f);
        boat.localRotation = Quaternion.Euler(0f, back ? -90f : 90f, Mathf.Sin(time * 0.6f) * 0.8f);
    }

    private void Lights(float t)
    {
        float beats = t * Bpm / 60f;
        float pulse = Mathf.Exp(-(beats % 1f) * 4f);
        int bar = Mathf.FloorToInt(beats / 4f);
        int mode = (bar / 2) % 3; // all one colour / a rainbow chase / two colours swapping
        for (int i = 0; i < bulbs.Count; i++)
        {
            float hue = mode == 0 ? t * 0.07f : mode == 1 ? t * 0.1f + i * 0.04f : t * 0.03f + ((i + Mathf.FloorToInt(beats)) % 2) * 0.5f;
            bool on = mode != 1 || (i + Mathf.FloorToInt(beats * 2f)) % 3 != 0;
            Tint(bulbs[i], Color.HSVToRGB(hue % 1f, 0.85f, 1f) * (on ? 3f + 2f * pulse : 0.3f));
        }
        Color led = Color.HSVToRGB((t * 0.05f) % 1f, 0.9f, 1f);
        foreach (var r in bands) Tint(r, led * (2.5f + 2.5f * pulse));
        for (int i = 0; i < washes.Count; i++)
        {
            washes[i].color = Color.HSVToRGB((t * 0.05f + i * 0.33f) % 1f, 0.8f, 1f);
            washes[i].intensity = 4f + 4f * pulse;
        }
    }

    // The band on board, heard as the boat goes by
    private void Music()
    {
        if (band == null)
        {
            var clip = SynthSounds.TryGet("ramwong");
            if (clip == null) return;
            band = boat.gameObject.AddComponent<AudioSource>();
            band.clip = clip;
            band.loop = true;
            band.spatialBlend = 1f;
            band.minDistance = 6f;
            band.maxDistance = 55f;
            band.rolloffMode = AudioRolloffMode.Linear;
        }
        band.volume = GameAudio.MusicVolume;
        if (!band.isPlaying) band.Play();
    }

    private void Tint(Renderer r, Color c)
    {
        r.GetPropertyBlock(block);
        block.SetColor(BaseColor, c);
        r.SetPropertyBlock(block);
    }

    // Editor previews: by night the boat shows, lit, opposite the temple
    public void Preview(bool on)
    {
        if (block == null) Awake();
        preview = true;
        night = on;
        if (boat == null) return;
        boat.gameObject.SetActive(on);
        boat.localPosition = new Vector3(-40f, 0f, 0f);
        boat.localRotation = Quaternion.Euler(0f, 90f, 0f);
        if (on) Lights(3.2f);
    }
}
