using UnityEngine;

// Small runtime animations for the fairground.

// Turns around a local axis (ferris wheel, carousel). Children named "Cabin" stay upright.
public class Spinner : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.right;
    [SerializeField] private float degreesPerSecond = 12f;

    public void Configure(Vector3 localAxis, float speed)
    {
        axis = localAxis;
        degreesPerSecond = speed;
    }

    private void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        foreach (Transform child in transform)
            if (child.name == "Cabin") child.rotation = Quaternion.Euler(0f, transform.parent != null ? transform.parent.eulerAngles.y : 0f, 0f);
    }
}

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

// Likay (ลิเก) performer: stands on the stage and dances (wave / wai) now and then
public class LikayDancer : MonoBehaviour
{
    [SerializeField] private int look;

    private CharacterModel model;
    private float nextMove;

    public void Configure(int lookIndex) => look = lookIndex;

    private void Start()
    {
        model = gameObject.AddComponent<CharacterModel>();
        model.SetSkin(look);
        nextMove = Time.time + Random.value * 2f;
    }

    private void Update()
    {
        if (model == null || Time.time < nextMove) return;
        nextMove = Time.time + Random.Range(2.5f, 4.5f);
        model.Play(Random.value < 0.6f ? PlayerAction.Wave : PlayerAction.Wai);
        transform.Rotate(0f, Random.Range(-40f, 40f), 0f);
    }
}
