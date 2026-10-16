using UnityEngine;

// One landed attack. Sent over the network as 10 bytes: [attack][damage][dirX float][dirZ float].
public struct HitInfo
{
    public PlayerAction Attack;
    public int Damage;
    public Vector3 Direction; // horizontal, from attacker toward target

    public const int PunchDamage = 10;
    public const int KickDamage = 18;

    // Career levels raise damage up to +95% (level 20), so victims accept up to 2x the base.
    public const float MaxDamageMultiplier = 2f;

    public static int BaseDamageFor(PlayerAction attack) =>
        attack == PlayerAction.Kick ? KickDamage : attack == PlayerAction.Punch ? PunchDamage : 0;

    public static int MaxDamageFor(PlayerAction attack) =>
        Mathf.CeilToInt(BaseDamageFor(attack) * MaxDamageMultiplier);

    public byte[] ToBytes()
    {
        var bytes = new byte[10];
        bytes[0] = (byte)Attack;
        bytes[1] = (byte)Mathf.Clamp(Damage, 0, 255);
        System.BitConverter.GetBytes(Direction.x).CopyTo(bytes, 2);
        System.BitConverter.GetBytes(Direction.z).CopyTo(bytes, 6);
        return bytes;
    }

    public static bool TryParse(byte[] bytes, out HitInfo hit)
    {
        hit = default;
        if (bytes == null || bytes.Length < 10) return false;

        hit.Attack = (PlayerAction)bytes[0];
        hit.Damage = bytes[1];
        hit.Direction = new Vector3(System.BitConverter.ToSingle(bytes, 2), 0f, System.BitConverter.ToSingle(bytes, 6));
        return true;
    }
}

// Anything a punch or kick can land on: other players, the punching bag, later NPC opponents.
public interface IHittable
{
    Transform transform { get; }
    void ReceiveHit(HitInfo hit, Transform attacker);
}
