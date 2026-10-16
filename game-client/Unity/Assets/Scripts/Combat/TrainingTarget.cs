using UnityEngine;

// A place to train for the boxer career: banana tree (tier 1), punching bag (tier 2), boxing camp (tier 3).
// Hits are reported to the server through CareerClient; the server decides the XP.
public class TrainingTarget : MonoBehaviour, IHittable
{
    [Tooltip("Must match a target id in game-server/nakama/modules/career.lua")]
    [SerializeField] private string targetId = "banana";
    [SerializeField] private int requiredLevel = 1;
    [Tooltip("Shown to the player only; the real value lives on the server")]
    [SerializeField] private int xpPerHit = 2;

    private static readonly Color XpColor = new Color(0.4f, 0.9f, 1f);
    private static readonly Color LockedColor = new Color(0.75f, 0.75f, 0.75f);

    public string TargetId => targetId;
    public int RequiredLevel => requiredLevel;

    public void Configure(string id, int level, int xp)
    {
        targetId = id;
        requiredLevel = level;
        xpPerHit = xp;
    }

    public void ReceiveHit(HitInfo hit, Transform attacker)
    {
        var swing = GetComponentInChildren<PunchingBag>();
        if (swing != null) swing.Swing(hit);

        Vector3 at = attacker.position + hit.Direction * 0.8f + Vector3.up * 0.6f;
        var career = CareerClient.Instance;

        if (career == null || !career.IsOnline)
        {
            FloatingText.Spawn(at, $"-{hit.Damage}", new Color(1f, 0.85f, 0.2f));
            return;
        }

        if (career.Level < requiredLevel)
        {
            FloatingText.Spawn(at, $"Lv {requiredLevel}+", LockedColor);
            return;
        }

        career.RecordHit(targetId);
        FloatingText.Spawn(at, $"+{xpPerHit} XP", XpColor);
    }
}
