using UnityEngine;

// Sitting pose (ท่านั่ง) for a humanoid character without a sitting animation: after the animator has
// posed the body each frame, the thighs are swung forward and the knees bent so the character sits,
// and (optionally) the right arm reaches forward — e.g. the boatman holding the tiller. Ride = astride
// a bicycle or motorbike: knees apart round the tank, feet on the pegs, both hands on the grips.
// Kneel = พับเพียบ, the way monks and laypeople sit on the floor before the Buddha: legs folded to one
// side (knees forward, feet tucked back to the right), hands resting together in the lap.
// CrossLegged = ขัดสมาธิ for meditation: knees out to the sides, the right shin crossed in front of the
// left, hands one on the other in the lap. It is posed by aiming each bone at where it should point
// (not by fixed angles), so it comes out the same whatever way the model's bones are set up.
public class SeatedPose : MonoBehaviour
{
    public bool HoldTiller;
    public bool Ride;
    public bool Kneel;
    public bool CrossLegged;
    public bool Writing; // นั่งเรียน: forearms on the desk, the right hand writing, head dipping to the book now and then

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
        if (CrossLegged)
        {
            PoseCrossLegged();
            return;
        }
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
        if (Writing)
        {
            float w = Time.time;
            float scribble = Mathf.Sin(w * 9f) * 4f + Mathf.Sin(w * 2.3f) * 3f;
            bool look = Mathf.Repeat(w, 7f) < 4.5f; // head down to the book, then up to the board
            Bend(HumanBodyBones.LeftUpperArm, -40f, Vector3.forward, 18f);
            Bend(HumanBodyBones.RightUpperArm, -42f + scribble * 0.4f, Vector3.forward, -16f);
            Bend(HumanBodyBones.LeftLowerArm, -55f, Vector3.up, 35f);
            Bend(HumanBodyBones.RightLowerArm, -50f + scribble, Vector3.up, -30f);
            Bend(HumanBodyBones.Spine, 8f);
            Bend(HumanBodyBones.Head, look ? 22f : 2f);
        }
        if (HoldTiller)
        {
            Bend(HumanBodyBones.RightUpperArm, -55f, Vector3.forward, 20f);
            Bend(HumanBodyBones.RightLowerArm, -35f);
        }
    }

    private void PoseCrossLegged()
    {
        var root = animator.transform;
        Vector3 r = root.right, f = root.forward, u = root.up;
        // thighs out to the sides and a little forward; shins back across in front of the hips,
        // the right one on top (a touch higher and further forward)
        Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
            -0.8f * r + 0.58f * f - 0.06f * u, 0.97f * r - 0.12f * f - 0.3f * u);
        Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
            0.8f * r + 0.58f * f + 0.02f * u, -0.95f * r + 0.05f * f - 0.18f * u);

        // arms hang relaxed, forearms come in so the hands rest in the lap, right hand on the left
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Vector3 lap = (hips != null ? hips.position : root.position) + 0.2f * f;
        Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + 0.3f * f - 0.12f * r, lap + 0.05f * u - 0.02f * r);
        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + 0.3f * f + 0.12f * r, lap + 0.09f * u + 0.02f * r);
    }

    private void Leg(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot, HumanBodyBones toes, Vector3 thighDir, Vector3 shinDir)
    {
        var thigh = animator.GetBoneTransform(upper);
        var knee = animator.GetBoneTransform(lower);
        if (thigh == null || knee == null) return;
        var root = animator.transform;
        thighDir.Normalize();
        shinDir.Normalize();
        // swing the thigh, then turn it about its own length so the knee bends the way a knee does:
        // the kneecap ends up on the outside of the bend, opposite the shin
        var swing = Quaternion.FromToRotation(knee.position - thigh.position, thighDir);
        thigh.rotation = swing * thigh.rotation;
        Vector3 kneecap = Vector3.ProjectOnPlane(swing * root.forward, thighDir);
        Vector3 want = -Vector3.ProjectOnPlane(shinDir, thighDir);
        if (kneecap.sqrMagnitude > 1e-4f && want.sqrMagnitude > 1e-4f)
            thigh.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(kneecap, want, thighDir), thighDir) * thigh.rotation;
        Aim(lower, foot, shinDir);
        // the foot lies relaxed, toes carrying on from the shin and turned down to the floor
        Aim(foot, toes, shinDir - 0.5f * root.up);
    }

    private void Arm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 upperDir, Vector3 handAt)
    {
        Aim(upper, lower, upperDir);
        var elbow = animator.GetBoneTransform(lower);
        if (elbow != null) Aim(lower, hand, handAt - elbow.position);
    }

    // Turn a bone so its child lies in `dir` from it
    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir)
    {
        var t = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (t == null || c == null) return;
        Vector3 now = c.position - t.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        t.rotation = Quaternion.FromToRotation(now, dir) * t.rotation;
    }

    // Where the hips sit from the character's origin, in its own space (measure while standing)
    public static Vector3 HipsOffset(Transform character)
    {
        var animator = character.GetComponentInChildren<Animator>();
        var hips = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
        return hips != null ? Quaternion.Inverse(character.rotation) * (hips.position - character.position) : new Vector3(0f, 0.9f, 0f);
    }

    // Origin that puts the hips at `hipsAt` with the character turned to `rotation`
    public static Vector3 RootFor(Vector3 hipsAt, Quaternion rotation, Vector3 hipsOffset) => hipsAt - rotation * hipsOffset;

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
