using System;
using UnityEngine;

// The opponent in a ring fight: walks in, throws punches and kicks with a cooldown, blocks
// sometimes, stays inside the ropes. Uses the same animated characters as the players.
public class BoxerNpc : MonoBehaviour, IHittable
{
    private const float Reach = 1.35f;
    private const float WalkSpeed = 2.2f;

    public int MaxHp { get; private set; }
    public int Hp { get; private set; }
    public int DamageTaken { get; private set; }
    public int HitsLanded { get; private set; } // on the player
    public bool Down => Hp <= 0;
    public bool Active { get; set; }

    public event Action Changed;
    public event Action KnockedOut;

    private float power, speed;
    private PlayerCombat target;
    private Transform ring;
    private float halfSize;
    private CharacterModel model;
    private float nextAttack, blockUntil, stunUntil;

    public static BoxerNpc Spawn(Transform ring, Vector3 localPosition, float halfSize, BoxingClient.Opponent o, PlayerCombat target)
    {
        var go = new GameObject("Boxer_" + o.name);
        go.transform.SetParent(ring, false);
        go.transform.localPosition = localPosition;
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.35f;
        var npc = go.AddComponent<BoxerNpc>();
        npc.ring = ring;
        npc.halfSize = halfSize;
        npc.target = target;
        npc.MaxHp = npc.Hp = o.hp;
        npc.power = o.power;
        npc.speed = Mathf.Clamp01(o.speed);
        npc.model = go.AddComponent<CharacterModel>();
        npc.model.SetSkin(o.skin);
        return npc;
    }

    private void Update()
    {
        if (!Active || Down || target == null) return;
        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8f * Time.deltaTime);
        if (Time.time < stunUntil || target.IsKnockedOut) return;

        float dist = to.magnitude;
        if (dist > Reach * 0.9f)
        {
            // Step in (a little slower when it's a weaker fighter)
            transform.position += to.normalized * WalkSpeed * Mathf.Lerp(0.6f, 1f, speed) * Time.deltaTime;
        }
        else if (Time.time >= nextAttack && Time.time >= blockUntil)
        {
            Attack(to.normalized);
        }

        // Stay inside the ropes
        Vector3 p = ring.InverseTransformPoint(transform.position);
        p.x = Mathf.Clamp(p.x, -halfSize, halfSize);
        p.z = Mathf.Clamp(p.z, -halfSize, halfSize);
        transform.position = ring.TransformPoint(p);
    }

    private void Attack(Vector3 dir)
    {
        bool kick = UnityEngine.Random.value < 0.3f;
        var action = kick ? PlayerAction.Kick : PlayerAction.Punch;
        model.Play(action);
        nextAttack = Time.time + Mathf.Lerp(1.6f, 0.7f, speed) + UnityEngine.Random.value * 0.4f;

        // The strike lands a moment later, if the player is still in reach
        StartCoroutine(Land(action, dir, kick ? 0.35f : 0.2f));
    }

    private System.Collections.IEnumerator Land(PlayerAction action, Vector3 dir, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Down || target == null || target.IsKnockedOut) yield break;
        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude > Reach + 0.3f) yield break; // stepped back: miss
        HitsLanded++;
        target.ApplyHit(new HitInfo
        {
            Attack = action,
            Damage = Mathf.RoundToInt(HitInfo.BaseDamageFor(action) * Mathf.Min(power, HitInfo.MaxDamageMultiplier)),
            Direction = to.normalized,
        });
    }

    public void ReceiveHit(HitInfo hit, Transform attacker)
    {
        if (!Active || Down) return;
        bool blocked = Time.time < blockUntil;
        int damage = blocked ? Mathf.CeilToInt(hit.Damage * 0.3f) : hit.Damage;
        Hp = Mathf.Max(0, Hp - damage);
        DamageTaken += damage;
        FloatingText.Spawn(transform.position + Vector3.up * 1.4f, blocked ? $"-{damage} BLOCK" : $"-{damage}",
            blocked ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.85f, 0.2f));
        if (!blocked) stunUntil = Time.time + 0.25f;

        // Better fighters guard more after taking a shot
        if (!blocked && UnityEngine.Random.value < 0.15f + speed * 0.3f)
        {
            blockUntil = Time.time + 0.8f;
            model.Play(PlayerAction.BlockStart);
            Invoke(nameof(Unblock), 0.8f);
        }
        Changed?.Invoke();
        if (Down)
        {
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);
            KnockedOut?.Invoke();
        }
    }

    private void Unblock() => model.Play(PlayerAction.BlockEnd);
}
