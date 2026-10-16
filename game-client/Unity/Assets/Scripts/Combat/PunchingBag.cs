using UnityEngine;

// Swing-on-hit motion for training props (punching bag, banana tree trunk).
// Put this on a pivot: the top hook of a bag, or the base of a tree; the mesh/collider is a child.
// TrainingTarget on a parent receives the hit and calls Swing().
public class PunchingBag : MonoBehaviour
{
    [SerializeField] private float swingPerDamage = 1.5f;
    [SerializeField] private float stiffness = 40f;
    [SerializeField] private float damping = 4f;
    [SerializeField] private float maxTilt = 35f;

    private Vector2 tilt;      // degrees around X and Z
    private Vector2 velocity;

    public void Swing(HitInfo hit)
    {
        // Move the free end away from the attacker: the bottom of a hanging bag,
        // or the top of a rooted trunk (which needs the opposite rotation).
        float push = hit.Damage * swingPerDamage * 10f;
        float sign = IsRooted ? -1f : 1f;
        velocity.x -= sign * hit.Direction.z * push;
        velocity.y += sign * hit.Direction.x * push;
    }

    // Rooted props (trees) have their child above the pivot; hanging ones have it below.
    private bool IsRooted => transform.childCount > 0 && transform.GetChild(0).localPosition.y > 0f;

    private void Update()
    {
        // Damped spring back to rest
        velocity += (-stiffness * tilt - damping * velocity) * Time.deltaTime;
        tilt += velocity * Time.deltaTime;
        tilt = Vector2.ClampMagnitude(tilt, maxTilt);
        transform.localRotation = Quaternion.Euler(tilt.x, 0f, tilt.y);
    }
}
