using UnityEngine;

// Sitting pose (ท่านั่ง) for a humanoid character without a sitting animation: after the animator has
// posed the body each frame, the thighs are swung forward and the knees bent so the character sits,
// and (optionally) the right arm reaches forward — e.g. the boatman holding the tiller.
public class SeatedPose : MonoBehaviour
{
    public bool HoldTiller;

    private Animator animator;
    private CharacterModel model;

    private void OnEnable()
    {
        model = GetComponent<CharacterModel>();
        if (model != null) model.Seated = true;
    }

    private void OnDisable()
    {
        if (model != null) model.Seated = false;
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        animator.SetFloat("Speed", 0f);
        Bend(HumanBodyBones.LeftUpperLeg, -85f);
        Bend(HumanBodyBones.RightUpperLeg, -85f);
        Bend(HumanBodyBones.LeftLowerLeg, 85f);
        Bend(HumanBodyBones.RightLowerLeg, 85f);
        if (HoldTiller)
        {
            Bend(HumanBodyBones.RightUpperArm, -55f, Vector3.forward, 20f);
            Bend(HumanBodyBones.RightLowerArm, -35f);
        }
    }

    // Rotate a bone about the character's own right axis (pitch), plus an optional extra twist
    private void Bend(HumanBodyBones bone, float pitch, Vector3 extraAxis = default, float extra = 0f)
    {
        var t = animator.GetBoneTransform(bone);
        if (t == null) return;
        var root = animator.transform;
        t.rotation = Quaternion.AngleAxis(pitch, root.right) * t.rotation;
        if (extra != 0f) t.rotation = Quaternion.AngleAxis(extra, root.TransformDirection(extraAxis)) * t.rotation;
    }
}
