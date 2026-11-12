using System.Collections.Generic;
using UnityEngine;

// The level-up seen round the player in the world (LevelUpFX does the screen): a golden ring of light
// spreading on the ground, a soft pillar of light, a warm flash, and ดอกพิกุล spiralling up round them.
// Follows the player while it plays.
public class LevelUpAura : MonoBehaviour
{
    private const float Duration = 2.6f;

    private Transform target, ring, pillar;
    private SpriteRenderer ringSr, pillarSr;
    private Light glow;
    private readonly List<(Transform t, SpriteRenderer sr, float angle, float speed, float rise, float size)> petals = new List<(Transform, SpriteRenderer, float, float, float, float)>();
    private float age;

    public static void Burst(Transform player)
    {
        if (player == null) return;
        var go = new GameObject("LevelUpAura");
        go.AddComponent<LevelUpAura>().Build(player);
    }

    private SpriteRenderer Sprite(string name, UnityEngine.Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;
        return sr;
    }

    private void Build(Transform player)
    {
        target = player;
        transform.position = Feet();
        ringSr = Sprite("Ring", PikulArt.Rays, new Color(1f, 0.85f, 0.35f, 0.9f));
        ring = ringSr.transform;
        ring.localPosition = Vector3.up * 0.05f;
        ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
        pillarSr = Sprite("Pillar", PikulArt.Glow, new Color(1f, 0.9f, 0.55f, 0.6f));
        pillar = pillarSr.transform;
        for (int i = 0; i < 16; i++)
        {
            var sr = Sprite("Pikul", PikulArt.Pikul, Color.white);
            petals.Add((sr.transform, sr, i * Mathf.PI * 2f / 16f, Random.Range(2.2f, 3.4f), Random.Range(2.2f, 3.6f), Random.Range(0.18f, 0.3f)));
        }
        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.up * 1.4f;
        glow = lightGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.82f, 0.45f);
        glow.range = 7f;
        glow.shadows = LightShadows.None;
    }

    // the ground under the player (CharacterController's foot, or 1 m under the model's root)
    private Vector3 Feet()
    {
        if (target == null) return transform.position;
        var cc = target.GetComponent<CharacterController>();
        float down = cc != null ? cc.height * 0.5f - cc.center.y : 1f;
        return target.position + Vector3.down * down;
    }

    private void LateUpdate()
    {
        age += Time.deltaTime;
        if (target != null) transform.position = Feet();
        float k = age / Duration, fade = Mathf.Clamp01((Duration - age) / 0.8f);
        var cam = Camera.main;

        float r = Mathf.Lerp(0.6f, 4.2f, Mathf.Sqrt(Mathf.Clamp01(age / 1.2f)));
        ring.localScale = Vector3.one * r;
        ring.localRotation = Quaternion.Euler(90f, age * 60f, 0f);
        ringSr.color = new Color(1f, 0.85f, 0.35f, 0.9f * Mathf.Clamp01(1f - age / 1.4f));

        // a tall soft column of light, turned to the camera round the upright
        pillar.localPosition = Vector3.up * 1.6f;
        pillar.localScale = new Vector3(1.6f, 5.5f * Mathf.Clamp01(age / 0.3f), 1f);
        if (cam != null)
        {
            var d = cam.transform.position - pillar.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) pillar.rotation = Quaternion.LookRotation(-d);
        }
        pillarSr.color = new Color(1f, 0.9f, 0.55f, 0.55f * fade);

        foreach (var p in petals)
        {
            float a = p.angle + age * p.speed;
            float h = p.rise * Mathf.Clamp01(age / (Duration * 0.85f));
            float rad = 0.9f + 0.25f * Mathf.Sin(age * 3f + p.angle);
            p.t.localPosition = new Vector3(Mathf.Cos(a) * rad, 0.2f + h, Mathf.Sin(a) * rad);
            p.t.localScale = Vector3.one * p.size * Mathf.Clamp01(age / 0.25f);
            if (cam != null) p.t.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, age * 200f + p.angle * 57f);
            p.sr.color = new Color(1f, 1f, 1f, fade);
        }
        glow.intensity = 9f * Mathf.Clamp01(1f - age / 1.6f);

        if (k >= 1f) Destroy(gameObject);
    }
}
