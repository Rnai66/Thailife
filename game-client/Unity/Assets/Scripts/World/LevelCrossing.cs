using UnityEngine;

// จุดตัดทางรถไฟ: where a road crosses the railway (ThaiRailway, ring road). When the town's train
// (SkyTrain on the line) comes near, the red lamps flash in turn, the red-and-white barrier arms come
// down across the road and traffic waits at the gates (their TrafficBlock colliders switch on); once
// the last carriage is clear the arms go up again.
public class LevelCrossing : MonoBehaviour
{
    [SerializeField] private Transform line;          // the railway (the train runs along its local X)
    [SerializeField] private float crossX;            // where the road crosses, along the line
    [SerializeField] private float trainLength = 76f; // locomotive + carriages behind the lead
    [SerializeField] private Transform[] arms;        // pivots: rotate about local Z, 85 = up, 0 = down
    [SerializeField] private GameObject[] lampsA, lampsB;
    [SerializeField] private Collider[] blocks;

    private const float Up = 85f, ArmSpeed = 40f;
    private SkyTrain train;
    private float angle = Up;

    public void Configure(Transform railway, float x, Transform[] barrierArms, GameObject[] flashA, GameObject[] flashB, Collider[] gates)
    {
        line = railway;
        crossX = x;
        arms = barrierArms;
        lampsA = flashA;
        lampsB = flashB;
        blocks = gates;
    }

    private void Update()
    {
        if (train == null)
        {
            if (line == null) return;
            train = line.GetComponentInChildren<SkyTrain>();
            if (train == null) return;
        }
        float lead = train.transform.localPosition.x; // the train runs towards +X
        bool coming = lead > crossX - 40f && lead - trainLength < crossX + 8f;

        angle = Mathf.MoveTowards(angle, coming ? 0f : Up, ArmSpeed * Time.deltaTime);
        if (arms != null)
            foreach (var a in arms) if (a != null) a.localRotation = Quaternion.Euler(0f, 0f, angle);

        bool flash = coming || angle < Up - 1f;
        bool tick = Mathf.Repeat(Time.time * 1.6f, 1f) < 0.5f;
        Set(lampsA, flash && tick);
        Set(lampsB, flash && !tick);
        if (blocks != null)
            foreach (var b in blocks) if (b != null) b.enabled = coming || angle < 60f;
    }

    private static void Set(GameObject[] lamps, bool on)
    {
        if (lamps == null) return;
        foreach (var l in lamps) if (l != null && l.activeSelf != on) l.SetActive(on);
    }
}
