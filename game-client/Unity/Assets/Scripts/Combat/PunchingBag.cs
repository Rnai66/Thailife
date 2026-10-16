using UnityEngine;

// Training target (กระสอบทราย). Swings when hit and shows damage numbers; local only, not synced.
// Put this on a pivot at the top of the bag; the bag mesh/collider hangs below as a child.
public class PunchingBag : MonoBehaviour, IHittable
{
    [SerializeField] private float swingPerDamage = 1.5f;
    [SerializeField] private float stiffness = 40f;
    [SerializeField] private float damping = 4f;

    private Vector2 tilt;      // degrees around X and Z
    private Vector2 velocity;
    private int totalDamage;
    private int hits;

    public void ReceiveHit(HitInfo hit, Transform attacker)
    {
        // Push the bottom of the bag away from the attacker
        float push = hit.Damage * swingPerDamage * 10f;
        velocity.x -= hit.Direction.z * push;
        velocity.y += hit.Direction.x * push;

        totalDamage += hit.Damage;
        hits++;

        Vector3 hitPoint = attacker.position + hit.Direction * 0.8f + Vector3.up * 0.5f;
        FloatingText.Spawn(hitPoint, $"-{hit.Damage}", hit.Attack == PlayerAction.Kick
            ? new Color(1f, 0.45f, 0.2f)
            : new Color(1f, 0.85f, 0.2f));

        Debug.Log($"[Training] {hit.Attack} {hit.Damage} dmg — {hits} hits, {totalDamage} total");
    }

    private void Update()
    {
        // Damped spring back to hanging straight
        velocity += (-stiffness * tilt - damping * velocity) * Time.deltaTime;
        tilt += velocity * Time.deltaTime;
        tilt = Vector2.ClampMagnitude(tilt, 35f);
        transform.localRotation = Quaternion.Euler(tilt.x, 0f, tilt.y);
    }
}
