using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Boxing v1 for the local player: health, landing punches/kicks, taking hits, knockout and respawn.
// Relayed matches trust clients, so the victim applies damage and clamps it to the attack's maximum.
// A server-authoritative match should replace this before anything competitive (GDD section 9).
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField, Range(0f, 1f)] private float blockDamageFactor = 0.2f;
    [SerializeField] private float punchRadius = 0.6f;
    [SerializeField] private float kickRadius = 0.7f;
    [SerializeField] private float punchKnockback = 3f;
    [SerializeField] private float kickKnockback = 6f;
    [SerializeField] private float knockoutSeconds = 3f;
    [SerializeField] private Vector3 respawnPoint = new Vector3(0f, 1f, 0f);

    public int MaxHealth => maxHealth;
    public int Health { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;

    private int maxHealth;
    public bool IsKnockedOut { get; private set; }

    // Raised whenever health changes (MatchSync broadcasts it, HealthHud shows it).
    public event Action<int> HealthChanged;

    private PlayerController controller;
    private PlayerActions actions;
    private readonly Collider[] overlap = new Collider[16];
    private readonly HashSet<IHittable> struck = new HashSet<IHittable>();

    public static PlayerCombat GetOrAdd(GameObject go) =>
        go.TryGetComponent(out PlayerCombat combat) ? combat : go.AddComponent<PlayerCombat>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        actions = GetComponent<PlayerActions>();
        maxHealth = baseMaxHealth;
        Health = maxHealth;

        if (GetComponent<HealthHud>() == null)
            gameObject.AddComponent<HealthHud>();
    }

    // Career level from the server: +5% damage and +5 max HP per level above 1.
    public void ApplyCareerLevel(int level)
    {
        level = Mathf.Max(1, level);
        DamageMultiplier = 1f + 0.05f * (level - 1);
        maxHealth = baseMaxHealth + 5 * (level - 1);
        if (!IsKnockedOut) SetHealth(maxHealth);
    }

    // Called by PlayerActions right after a punch/kick starts.
    public void PerformAttack(PlayerAction attack)
    {
        if (IsKnockedOut) return;

        bool kick = attack == PlayerAction.Kick;
        Vector3 point = transform.TransformPoint(kick ? ActionFeedback.KickOffset : ActionFeedback.PunchOffset);
        int count = Physics.OverlapSphereNonAlloc(point, kick ? kickRadius : punchRadius, overlap, ~0, QueryTriggerInteraction.Collide);

        struck.Clear();
        for (int i = 0; i < count; i++)
        {
            var target = overlap[i].GetComponentInParent<IHittable>();
            if (target == null || !struck.Add(target)) continue;

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

            target.ReceiveHit(new HitInfo
            {
                Attack = attack,
                Damage = Mathf.RoundToInt(HitInfo.BaseDamageFor(attack) * DamageMultiplier),
                Direction = direction.normalized,
            }, transform);
        }
    }

    // A hit from another player landed on us.
    public void ApplyHit(HitInfo hit)
    {
        if (IsKnockedOut) return;

        int damage = Mathf.Min(hit.Damage, HitInfo.MaxDamageFor(hit.Attack));
        if (damage <= 0) return;

        bool blocked = actions != null && actions.IsBlocking;
        if (blocked)
        {
            damage = Mathf.CeilToInt(damage * blockDamageFactor);
        }
        else
        {
            float force = hit.Attack == PlayerAction.Kick ? kickKnockback : punchKnockback;
            controller.AddImpulse(hit.Direction.normalized * force);
        }

        FloatingText.Spawn(transform.position + Vector3.up * 1.4f, blocked ? $"-{damage} BLOCK" : $"-{damage}",
            blocked ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.35f, 0.3f));

        SetHealth(Health - damage);
        if (Health <= 0)
            StartCoroutine(KnockOut());
    }

    private IEnumerator KnockOut()
    {
        IsKnockedOut = true;
        controller.InputEnabled = false;
        if (actions != null) actions.RefreshBlock();

        // Placeholder "lying down" until there is a knockdown animation
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);

        yield return new WaitForSeconds(knockoutSeconds);

        controller.Teleport(respawnPoint, Quaternion.identity);
        IsKnockedOut = false;
        controller.InputEnabled = true;
        if (actions != null) actions.RefreshBlock();
        SetHealth(maxHealth);
    }

    private void SetHealth(int value)
    {
        Health = Mathf.Clamp(value, 0, maxHealth);
        HealthChanged?.Invoke(Health);
    }
}
