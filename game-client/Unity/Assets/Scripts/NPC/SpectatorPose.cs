using UnityEngine;

// How one of the Spectators holds himself, posed over the idle animation every frame:
//  Watch          stands and looks on
//  Cheer          stands; now and then both fists go up and pump (a boxing crowd)
//  SitGround      sits cross-legged on a mat, hands on the knees (the หนังกลางแปลง audience)
//  Kneel          sits back on the heels, hands together (ไหว้), bowing down now and then (กราบ)
//  Projectionist  stands at the projector, now and then reaching up to the reels
//  Pew            sits on a church pew, hands clasped, head bowed in prayer
//  Sujud          sits back on the heels in a prayer row, hands on the thighs, going down to the floor in
//                 prostration (สุญูด) now and then
public class SpectatorPose : MonoBehaviour
{
    public enum Kind { Watch, Cheer, SitGround, Kneel, Projectionist, Pew, Sujud }

    [SerializeField] private Kind kind;
    [SerializeField] private float groundY;
    [SerializeField] private int seed;

    private Animator animator;
    private bool lowered;

    public void Configure(Kind how, float ground, int randomSeed)
    {
        kind = how;
        groundY = ground;
        seed = randomSeed;
    }

    private bool Sitting => kind == Kind.SitGround || kind == Kind.Kneel || kind == Kind.Pew || kind == Kind.Sujud;

    private void LateUpdate()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
        }
        var root = transform;
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null) return;
        float s = root.lossyScale.y;
        Vector3 f = root.forward, u = root.up, r = root.right;

        if (Sitting && !lowered)
        {
            // once: bring the hips down to the mat (cross-legged) or onto the heels (kneeling)
            float hipHeight = (kind == Kind.SitGround ? 0.14f : kind == Kind.Pew ? 0.55f : 0.36f) * s;
            root.position += Vector3.up * (groundY + hipHeight - hips.position.y);
            lowered = true;
            return;
        }

        float t = Time.time + seed * 1.37f;
        switch (kind)
        {
            case Kind.SitGround:
            {
                Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, f * 0.75f - r * 0.65f - u * 0.12f, r * 0.9f + f * 0.25f - u * 0.1f);
                Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, f * 0.75f + r * 0.65f - u * 0.12f, -r * 0.9f + f * 0.15f - u * 0.1f);
                Bend(HumanBodyBones.Spine, 6f + Mathf.Sin(t * 0.5f) * 2f, r);
                Vector3 lap = hips.position + f * 0.28f * s + u * 0.08f * s;
                Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + f * 0.4f - r * 0.2f, lap - r * 0.2f * s);
                Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + f * 0.4f + r * 0.2f, lap + r * 0.2f * s);
                break;
            }
            case Kind.Kneel:
            {
                Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, f - u * 0.6f - r * 0.08f, -f - u * 0.05f);
                Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, f - u * 0.6f + r * 0.08f, -f - u * 0.05f);
                // a slow กราบ every ~9 s: bow down and come up again
                float cycle = Mathf.Repeat(t, 9f);
                float bow = cycle < 2.4f ? Mathf.Sin(cycle / 2.4f * Mathf.PI) : 0f;
                Bend(HumanBodyBones.Spine, 4f + bow * 38f, r);
                Bend(HumanBodyBones.Chest, bow * 18f, r);
                Bend(HumanBodyBones.Head, 8f + bow * 10f, r);
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? hips;
                Vector3 wai = chest.position + chest.forward * 0.2f * s + chest.up * 0.05f * s;
                Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + f * 0.5f - r * 0.15f, wai - r * 0.02f * s);
                Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + f * 0.5f + r * 0.15f, wai + r * 0.02f * s);
                break;
            }
            case Kind.Pew:
            {
                Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, f - r * 0.06f, -u + f * 0.12f);
                Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, f + r * 0.06f, -u + f * 0.12f);
                Bend(HumanBodyBones.Spine, 10f, r);
                Bend(HumanBodyBones.Head, 18f + Mathf.Sin(t * 0.3f) * 3f, r);
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? hips;
                Vector3 clasp = chest.position + f * 0.25f * s - u * 0.12f * s;
                Arm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -u + f * 0.45f - r * 0.12f, clasp - r * 0.02f * s);
                Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, -u + f * 0.45f + r * 0.12f, clasp + r * 0.02f * s);
                break;
            }
            case Kind.Sujud:
            {
                Leg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, f - u * 0.6f - r * 0.1f, -f - u * 0.05f);
                Leg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, f - u * 0.6f + r * 0.1f, -f - u * 0.05f);
                // everyone in the row together: down for ~3 s in every 10
                float cycle = Mathf.Repeat(Time.time, 10f);
                float down = cycle < 3.4f ? Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, cycle / 0.8f)) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, (3.4f - cycle) / 0.8f)) : 0f;
                Bend(HumanBodyBones.Spine, 4f + down * 55f, r);
                Bend(HumanBodyBones.Chest, down * 25f, r);
                Bend(HumanBodyBones.Head, 6f + down * 10f, r);
                Vector3 thighs = hips.position + f * 0.3f * s + u * 0.12f * s;
                Vector3 floor = hips.position + f * 0.62f * s - u * 0.3f * s;
                foreach (float side in new[] { -1f, 1f })
                {
                    var upper = side < 0f ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
                    var lower = side < 0f ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
                    var hand = side < 0f ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
                    Vector3 at = Vector3.Lerp(thighs, floor, down) + r * side * 0.16f * s;
                    Arm(upper, lower, hand, Vector3.Lerp(-u + f * 0.3f, f - u * 0.4f, down) + r * side * 0.1f, at);
                }
                break;
            }
            case Kind.Cheer:
            {
                // cheer in bursts of a few seconds, each in his own time
                float burst = Mathf.Repeat(t * 0.23f, 1f);
                if (burst > 0.45f) { Bend(HumanBodyBones.Head, -4f, r); break; }
                float pump = 0.5f + 0.5f * Mathf.Sin(t * 9f);
                foreach (float side in new[] { -1f, 1f })
                {
                    var upper = side < 0f ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
                    var lower = side < 0f ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
                    var hand = side < 0f ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
                    var shoulder = animator.GetBoneTransform(upper);
                    if (shoulder == null) continue;
                    Vector3 up = u + r * side * 0.35f + f * 0.15f;
                    Arm(upper, lower, hand, up, shoulder.position + (up.normalized * 0.3f + u * (0.2f + 0.15f * pump)) * s);
                }
                Bend(HumanBodyBones.Head, -8f, r);
                break;
            }
            case Kind.Projectionist:
            {
                float cycle = Mathf.Repeat(t, 7f);
                if (cycle < 2.2f) // reach up to the reels and thread the film
                {
                    var shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    if (shoulder != null)
                        Arm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, f + u * 0.4f, shoulder.position + (f * 0.45f + u * (0.3f + 0.05f * Mathf.Sin(t * 6f))) * s);
                    Bend(HumanBodyBones.Head, -10f, r);
                }
                break;
            }
            default: // Watch
                Bend(HumanBodyBones.Head, Mathf.Sin(t * 0.4f) * 8f, u);
                break;
        }
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
        var b = animator.GetBoneTransform(bone);
        if (b != null) b.rotation = Quaternion.AngleAxis(angle, axis) * b.rotation;
    }

    private void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 dir)
    {
        var b = animator.GetBoneTransform(bone);
        var c = animator.GetBoneTransform(child);
        if (b == null || c == null) return;
        Vector3 now = c.position - b.position;
        if (now.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return;
        b.rotation = Quaternion.FromToRotation(now, dir) * b.rotation;
    }
}
