using UnityEngine;

// A barista behind the café bar (CafeSite): hands busy at the counter — tamping, pouring, wiping —
// with a turn now and then to the machine behind, posed over the idle animation.
public class CafeBarista : MonoBehaviour
{
    private int seed;
    private Animator animator;
    private Quaternion facing;
    private bool started;

    public void Configure(int randomSeed) => seed = randomSeed;

    private void LateUpdate()
    {
        if (!started)
        {
            facing = transform.localRotation;
            started = true;
        }
        // every so often turn round to the machine behind for a few seconds
        float cycle = (Time.time + seed * 3.7f) % 14f;
        float turn = cycle > 10f && cycle < 13f ? 1f : 0f;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, facing * Quaternion.Euler(0f, 180f * turn, 0f), Time.deltaTime * 3f);

        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        var root = transform;
        Vector3 f = root.forward, u = root.up, r = root.right;
        float s = root.lossyScale.y;
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        if (chest == null) return;
        // hands at the bar top, working in small circles
        float t = Time.time * 2.2f + seed;
        Vector3 work = root.position + u * 1.08f * s + f * 0.38f * s;
        Vector3 left = work - r * 0.16f * s + new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * 0.04f * s;
        Vector3 right = work + r * 0.14f * s + u * (0.05f + 0.04f * Mathf.Sin(t * 1.3f)) * s;
        Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + f * 0.35f - r * 0.15f, left);
        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + f * 0.35f + r * 0.15f, right);
        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head != null) head.rotation = Quaternion.AngleAxis(14f, r) * head.rotation;
    }

    private void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 upperDir, Vector3 handAt)
    {
        Aim(upper, lower, upperDir);
        var elbow = animator.GetBoneTransform(lower);
        if (elbow != null) Aim(lower, hand, handAt - elbow.position);
    }

    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir)
    {
        var a = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (a == null || c == null) return;
        Vector3 now = c.position - a.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        a.rotation = Quaternion.FromToRotation(now, dir) * a.rotation;
    }
}
