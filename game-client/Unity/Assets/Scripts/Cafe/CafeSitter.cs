using UnityEngine;

// A customer sitting in the café (CafeSite): lowered onto the seat, thighs forward, shins down, a
// little lean over the table, hands on the lap or round a cup — posed over the idle animation.
public class CafeSitter : MonoBehaviour
{
    private float seatHeight;
    private int seed;
    private Animator animator;
    private bool lowered;

    public void Configure(float seat, int randomSeed)
    {
        seatHeight = seat;
        seed = randomSeed;
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        var root = transform;
        Vector3 f = root.forward, u = root.up, r = root.right;
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null) return;
        if (!lowered)
        {
            // once: bring the hips down to the seat
            root.position += u * (root.parent.TransformPoint(Vector3.up * (seatHeight + 0.06f)).y - root.parent.TransformPoint(Vector3.zero).y
                                  - (hips.position.y - root.position.y));
            lowered = true;
            return;
        }
        float s = root.lossyScale.y;
        float sway = Mathf.Sin(Time.time * 0.7f + seed) * 3f;
        Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, f - r * 0.08f, -u + f * 0.15f);
        Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, f + r * 0.08f, -u + f * 0.15f);
        Bend(HumanBodyBones.Spine, 8f + sway, r);
        Bend(HumanBodyBones.Head, 6f, r);
        // hands together in front of the belly (on the lap, round a cup)
        Vector3 hands = hips.position + f * 0.32f * s + u * 0.18f * s;
        Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + f * 0.3f - r * 0.1f, hands - r * 0.06f * s);
        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + f * 0.3f + r * 0.1f, hands + r * 0.06f * s);
    }

    private void Leg(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot, Vector3 thigh, Vector3 shin)
    {
        Aim(upper, lower, thigh);
        Aim(lower, foot, shin);
    }

    private void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 upperDir, Vector3 handAt)
    {
        Aim(upper, lower, upperDir);
        var elbow = animator.GetBoneTransform(lower);
        if (elbow != null) Aim(lower, hand, handAt - elbow.position);
    }

    private void Bend(HumanBodyBones bone, float angle, Vector3 axis)
    {
        var t = animator.GetBoneTransform(bone);
        if (t != null) t.rotation = Quaternion.AngleAxis(angle, axis) * t.rotation;
    }

    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir)
    {
        var t = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (t == null || c == null) return;
        Vector3 now = c.position - t.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        t.rotation = Quaternion.FromToRotation(now, dir) * t.rotation;
    }
}
