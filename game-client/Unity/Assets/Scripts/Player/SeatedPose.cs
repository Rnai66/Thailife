using UnityEngine;

// Sitting pose (ท่านั่ง) for a humanoid character without a sitting animation: after the animator has
// posed the body each frame, the thighs are swung forward and the knees bent so the character sits,
// and (optionally) the right arm reaches forward — e.g. the boatman holding the tiller. Ride = astride
// a bicycle or motorbike: knees apart round the tank, feet on the pegs, both hands on the grips.
// Kneel = พับเพียบ, the way monks and laypeople sit on the floor before the Buddha: legs folded to one
// side (knees forward, feet tucked back to the right), hands resting together in the lap.
public class SeatedPose : MonoBehaviour
{
    public bool HoldTiller;
    public bool Ride;
    public bool Kneel;

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
        if (Kneel)
        {
            Bend(HumanBodyBones.LeftUpperLeg, -88f, Vector3.up, -22f);
            Bend(HumanBodyBones.RightUpperLeg, -88f, Vector3.up, -10f);
            Bend(HumanBodyBones.LeftLowerLeg, 155f, Vector3.up, 55f);
            Bend(HumanBodyBones.RightLowerLeg, 160f, Vector3.up, 45f);
            Bend(HumanBodyBones.LeftUpperArm, -25f, Vector3.forward, 14f);
            Bend(HumanBodyBones.RightUpperArm, -25f, Vector3.forward, -14f);
            Bend(HumanBodyBones.LeftLowerArm, -70f, Vector3.up, 25f);
            Bend(HumanBodyBones.RightLowerArm, -70f, Vector3.up, -25f);
            return;
        }
        if (Ride)
        {
            Bend(HumanBodyBones.LeftUpperLeg, -70f, Vector3.forward, -12f);
            Bend(HumanBodyBones.RightUpperLeg, -70f, Vector3.forward, 12f);
            Bend(HumanBodyBones.LeftLowerLeg, 75f);
            Bend(HumanBodyBones.RightLowerLeg, 75f);
            Bend(HumanBodyBones.LeftUpperArm, -55f, Vector3.forward, 10f);
            Bend(HumanBodyBones.RightUpperArm, -55f, Vector3.forward, -10f);
            Bend(HumanBodyBones.LeftLowerArm, -25f);
            Bend(HumanBodyBones.RightLowerArm, -25f);
            return;
        }
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
