using UnityEngine;

// ไหว้: palms pressed together before the chest, head and back bowed — after putting food in a monk's
// bowl (AlmsUI) while he gives the blessing. Plays for a few seconds over whatever the animator does,
// then removes itself. WaiPose.Play(player, seconds).
public class WaiPose : MonoBehaviour
{
    private Animator animator;
    private float start, until;

    public static void Play(GameObject who, float seconds)
    {
        var p = who.GetComponent<WaiPose>() ?? who.AddComponent<WaiPose>();
        p.start = Time.time;
        p.until = Time.time + seconds;
    }

    private void LateUpdate()
    {
        if (Time.time >= until) { Destroy(this); return; }
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        // ease in and out over a third of a second
        float k = Mathf.Clamp01(Mathf.Min(Time.time - start, until - Time.time) * 3f);
        var root = animator.transform;
        Vector3 f = root.forward, u = root.up, r = root.right;
        Bend(HumanBodyBones.Spine, 18f * k, r);
        Bend(HumanBodyBones.Head, 14f * k, r);
        var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) return;
        Vector3 palms = chest.position + f * 0.2f + u * 0.06f;
        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + 0.2f * f + 0.25f * r, palms + r * 0.02f, k);
        Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + 0.2f * f - 0.25f * r, palms - r * 0.02f, k);
    }

    private void Bend(HumanBodyBones bone, float angle, Vector3 axis)
    {
        var t = animator.GetBoneTransform(bone);
        if (t != null) t.rotation = Quaternion.AngleAxis(angle, axis) * t.rotation;
    }

    private void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 upperDir, Vector3 handAt, float k)
    {
        Aim(upper, lower, upperDir, k);
        var elbow = animator.GetBoneTransform(lower);
        if (elbow != null) Aim(lower, hand, handAt - elbow.position, k);
    }

    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir, float k)
    {
        var t = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (t == null || c == null) return;
        Vector3 now = c.position - t.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        t.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(now, dir), k) * t.rotation;
    }
}
