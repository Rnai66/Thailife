using UnityEngine;

// สระว่ายน้ำ: walk up to the pool and "ลงสระ" — the player slips into the water up to the shoulders and
// swims about with the stick (slower than walking, bobbing gently), kept inside the pool's edge;
// "ขึ้นจากสระ" climbs out onto the deck at the nearest side. The pool itself is drawn flat at ground
// level, so the body under the water line is simply out of sight below the deck.
[RequireComponent(typeof(BoxCollider))]
public class PoolSwim : MonoBehaviour
{
    [SerializeField] private Vector2 half = new Vector2(4.2f, 2f);   // the water, metres from the middle
    [SerializeField] private float waterY = 0.05f;
    [SerializeField] private string poolName = "สระว่ายน้ำ";

    private const float Sink = 1.15f;     // how far below the water line the feet go
    private const float SwimSpeed = 1.6f; // m/s
    private const float Edge = 0.35f;     // keep this far off the pool wall

    private PlayerController swimmer;
    private CharacterController body;
    private Vector3 heading;
    private float strokes;

    public static bool Swimming { get; private set; }

    public void Configure(Vector2 waterHalf, float surfaceY, string name)
    {
        half = waterHalf;
        waterY = surfaceY;
        poolName = name;
    }

    private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponent<PlayerController>();
        if (p != null && swimmer == null) Offer(p);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && swimmer == null) ContextButton.Hide(this);
    }

    private void Offer(PlayerController p) =>
        ContextButton.Show(this, "ลงสระ", $"{poolName} · ว่ายน้ำคลายร้อน ใช้จอยสติ๊กว่ายไปมาได้", () => GetIn(p));

    private void GetIn(PlayerController p)
    {
        if (swimmer != null || p == null || !p.InputEnabled) return;
        swimmer = p;
        body = p.GetComponent<CharacterController>();
        Swimming = true;
        p.enabled = false;
        if (body != null) body.enabled = false;
        var local = transform.InverseTransformPoint(p.transform.position);
        local.x = Mathf.Clamp(local.x, -half.x + Edge, half.x - Edge);
        local.z = Mathf.Clamp(local.z, -half.y + Edge, half.y - Edge);
        heading = p.transform.forward;
        Place(transform.TransformPoint(new Vector3(local.x, 0f, local.z)));
        GameAudio.Play("splash", null, 0.7f);
        ContextButton.Show(this, "ขึ้นจากสระ", "น้ำเย็นสบาย~ ว่ายไปมาได้ตามใจ", GetOut, highlight: true);
    }

    private void GetOut()
    {
        if (swimmer == null) return;
        var p = swimmer;
        // out over the nearest side, onto the deck
        var local = transform.InverseTransformPoint(p.transform.position);
        float toX = half.x - Mathf.Abs(local.x), toZ = half.y - Mathf.Abs(local.z);
        if (toX < toZ) local.x = Mathf.Sign(local.x == 0f ? 1f : local.x) * (half.x + 0.9f);
        else local.z = Mathf.Sign(local.z == 0f ? 1f : local.z) * (half.y + 0.9f);
        var outAt = transform.TransformPoint(new Vector3(local.x, 0f, local.z));
        outAt.y = transform.position.y + 0.1f;
        Release();
        p.Teleport(outAt, Quaternion.LookRotation(Vector3.ProjectOnPlane(outAt - transform.position, Vector3.up).normalized + Vector3.forward * 0.0001f));
        GameAudio.Play("splash", null, 0.4f);
        Offer(p);
    }

    private void Release()
    {
        if (swimmer != null)
        {
            if (body != null) body.enabled = true;
            swimmer.enabled = true;
        }
        swimmer = null;
        body = null;
        Swimming = false;
    }

    private void Place(Vector3 at)
    {
        float foot = body != null ? body.center.y - body.height * 0.5f : -1f;
        float bob = Mathf.Sin(Time.time * 2.2f) * 0.04f;
        at.y = transform.position.y + waterY - Sink - foot + bob;
        swimmer.transform.SetPositionAndRotation(at, Quaternion.LookRotation(heading, Vector3.up));
    }

    private void Update()
    {
        if (swimmer == null) return;
        if (!swimmer.InputEnabled) { GetOut(); return; } // knocked out, a cut-scene: out you get
        Vector2 stick = swimmer.PeekMoveInput();
        var cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 move = fwd * stick.y + right * stick.x;
        var pos = swimmer.transform.position;
        if (move.sqrMagnitude > 0.01f)
        {
            heading = Vector3.Slerp(heading, move.normalized, 1f - Mathf.Exp(-6f * Time.deltaTime));
            pos += move * SwimSpeed * Time.deltaTime;
            strokes += Time.deltaTime;
            if (strokes > 1.1f) { strokes = 0f; GameAudio.Play("splash", null, 0.15f); }
        }
        var local = transform.InverseTransformPoint(pos);
        local.x = Mathf.Clamp(local.x, -half.x + Edge, half.x - Edge);
        local.z = Mathf.Clamp(local.z, -half.y + Edge, half.y - Edge);
        Place(transform.TransformPoint(new Vector3(local.x, 0f, local.z)));
    }

    private void OnDisable()
    {
        Release();
        ContextButton.Hide(this);
    }
}
