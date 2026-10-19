using UnityEngine;

// The outdoor movie screen (หนังกลางแปลง): the "film" flickers through scenes
public class MovieScreen : MonoBehaviour
{
    private static readonly Color[] Scenes =
    {
        new Color(0.9f, 0.75f, 0.45f), new Color(0.35f, 0.55f, 0.85f), new Color(0.85f, 0.35f, 0.3f),
        new Color(0.4f, 0.7f, 0.4f), new Color(0.95f, 0.9f, 0.8f), new Color(0.2f, 0.2f, 0.3f),
    };

    private Material material;
    private float nextCut;
    private int scene;

    private void Awake()
    {
        var r = GetComponent<Renderer>();
        if (r != null) material = r.material; // own copy
    }

    private void Update()
    {
        if (material == null) return;
        if (Time.time >= nextCut)
        {
            scene = (scene + 1 + Random.Range(0, 3)) % Scenes.Length;
            nextCut = Time.time + Random.Range(1.5f, 4f);
        }
        float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.3f;
        var c = Scenes[scene] * flicker;
        material.color = c;
        material.SetColor("_EmissionColor", c * 1.2f);
    }
}
