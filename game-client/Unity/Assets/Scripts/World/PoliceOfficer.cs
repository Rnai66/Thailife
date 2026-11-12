using UnityEngine;

// A police officer (ตำรวจ) in the brown uniform with the traffic police's white cap: stands where it
// is put, turns to face whoever it is dealing with, waves cars on and blows its whistle. Made at
// runtime by TrafficPolice, SpeedTrap and PoliceCheckpoint (the Editor only builds the booths).
public class PoliceOfficer : MonoBehaviour
{
    private const int Uniform = 9; // Look.Outfits: ชุดตำรวจ

    private CharacterModel model;
    private float yaw, turnSpeed = 220f;
    private bool capped;

    // ground: the point on the ground it stands on
    public static PoliceOfficer Make(Transform parent, Vector3 ground, float facing, int seed = 0)
    {
        var go = new GameObject("PoliceOfficer_ตำรวจ");
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(ground + Vector3.up, Quaternion.Euler(0f, facing, 0f)); // CharacterModel's feet are 1 m down
        var officer = go.AddComponent<PoliceOfficer>();
        officer.Dress(seed);
        return officer;
    }

    private void Dress(int seed)
    {
        yaw = transform.eulerAngles.y;
        model = gameObject.AddComponent<CharacterModel>();
        var r = new System.Random(seed);
        model.SetLook(new Look { Gender = r.Next(4) == 0 ? 1 : 0, Face = r.Next(2), Hair = 1 + r.Next(3), HairColor = 0, Top = 0, Bottom = 0, Outfit = Uniform });
        if (!model.HasModel)
        {
            // no character files on this machine: a brown stand-in with a white cap
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.GetComponent<Renderer>().material.color = new Color(0.45f, 0.3f, 0.18f);
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(hat.GetComponent<Collider>());
            hat.transform.SetParent(transform, false);
            hat.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            hat.transform.localScale = new Vector3(0.42f, 0.06f, 0.42f);
            hat.GetComponent<Renderer>().material.color = Color.white;
            capped = true;
        }
    }

    public void Face(float worldYaw) => yaw = worldYaw;

    public void FaceTowards(Vector3 world)
    {
        var d = world - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) yaw = Quaternion.LookRotation(d).eulerAngles.y;
    }

    public void Wave()
    {
        if (model != null) model.Play(PlayerAction.Wave);
    }

    public void Whistle(bool stop = false) => GameAudio.Play(stop ? "whistle_long" : "whistle", transform.position, 0.9f, 45f);

    private void Update()
    {
        var want = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        if (!capped) PutOnCap();
    }

    // The white peaked cap of the traffic police, on the head bone once the model is in
    private void PutOnCap()
    {
        Transform head = null;
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name.EndsWith("Head") && !t.name.EndsWith("HeadTop_End")) { head = t; break; }
        if (head == null) return;
        capped = true;
        var white = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = Color.white };
        var black = new Material(white) { color = new Color(0.1f, 0.1f, 0.1f) };
        var cap = new GameObject("PoliceCap").transform;
        cap.SetParent(head, false);
        float s = 1f / Mathf.Max(0.0001f, head.lossyScale.y); // the bones may be scaled (Mixamo cm)
        cap.localScale = Vector3.one * s;
        cap.localPosition = new Vector3(0f, 0.17f * s, 0.01f * s);
        void Part(PrimitiveType type, Vector3 at, Vector3 size, Material m)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(cap, false);
            p.transform.localPosition = at;
            p.transform.localScale = size;
            p.GetComponent<Renderer>().sharedMaterial = m;
        }
        Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.24f, 0.04f, 0.26f), white);                 // crown
        Part(PrimitiveType.Cylinder, new Vector3(0f, -0.045f, 0f), new Vector3(0.2f, 0.025f, 0.21f), black); // band
        Part(PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.11f), new Vector3(0.16f, 0.012f, 0.08f), black);  // peak
    }
}
