using UnityEngine;

// A village dancer (นางรำ / หนุ่มรำวง) on the รำวง stage: walks slowly round the flower table with the
// others, turning their hands (wave / wai) in time.
public class RamwongDancer : MonoBehaviour
{
    [SerializeField] private int look;
    [SerializeField] private Transform center;
    [SerializeField] private float radius = 2.3f;
    [SerializeField] private float phase;

    public const float DegreesPerSecond = 14f; // one lap in ~26 s, the pace of a รำวง round

    private CharacterModel model;
    private float nextMove;

    public void Configure(int lookIndex, Transform circleCenter, float circleRadius, float startDegrees)
    {
        look = lookIndex;
        center = circleCenter;
        radius = circleRadius;
        phase = startDegrees;
    }

    private void Start()
    {
        model = gameObject.AddComponent<CharacterModel>();
        model.SetSkin(look);
        nextMove = Time.time + Random.value * 1.5f;
    }

    private void Update()
    {
        if (center == null) return;
        float a = (phase + Time.time * DegreesPerSecond) * Mathf.Deg2Rad;
        // Feet on the stage: the character's origin is the middle of its 2 m capsule
        transform.position = center.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius + Vector3.up;
        transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
        if (model != null && Time.time >= nextMove)
        {
            nextMove = Time.time + Random.Range(1.4f, 2.2f);
            model.Play(Random.value < 0.6f ? PlayerAction.Wave : PlayerAction.Wai);
        }
    }
}
