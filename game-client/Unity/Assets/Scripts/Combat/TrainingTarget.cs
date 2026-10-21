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
    private static readonly Color TiredColor = new Color(1f, 0.55f, 0.35f);

    private static float nextNudge;

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
            FloatingText.Spawn(at, $"ต้องเลเวล {requiredLevel} ก่อน (ตอนนี้ Lv {career.Level})", LockedColor);
            return;
        }

        // Camp bags also need a paid membership (bought at the camp's front desk)
        var wallet = WalletClient.Instance;
        if (targetId == "camp" && (wallet == null || !wallet.IsCampMember))
        {
            FloatingText.Spawn(at, "สมัครสมาชิกค่ายก่อน", LockedColor);
            return;
        }

        // Training costs energy (พลังงาน): too tired or asleep = the hit doesn't count
        var life = LifeClient.Instance;
        float cost = CareerClient.EnergyPerHit(targetId);
        if (life != null && life.IsInjured)
        {
            FloatingText.Spawn(at, "บาดเจ็บ! ไปรักษาก่อน", TiredColor);
            return;
        }
        if (life != null && life.IsOnline && life.IsSleeping)
        {
            FloatingText.Spawn(at, "กำลังนอนพักอยู่", TiredColor);
            if (Time.time > nextNudge) { nextNudge = Time.time + 3f; life.Refresh(); } // wakes a leftover nap if we're not in bed
            return;
        }
        if (life != null && life.IsOnline && life.Energy < cost)
        {
            FloatingText.Spawn(at, "เหนื่อยแล้ว!", TiredColor);
            return;
        }

        career.RecordHit(targetId);
        life?.PredictSpend(cost);
        FloatingText.Spawn(at, $"+{xpPerHit} XP", XpColor);
    }
}
