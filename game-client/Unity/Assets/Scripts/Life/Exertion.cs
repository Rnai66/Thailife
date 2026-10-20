using System;
using UnityEngine;

// Everyday exertion (ใช้แรง): walking, punching, kicking and jumping all cost energy (life.lua
// life_exert, rates in life_core.lua EXERT). Counted here, shown on the energy bar straight away, and
// reported to the server every few seconds. With no energy left you're exhausted and move slowly.
// Riding a motorbike/car costs less per metre; sitting in a boat or on a swing costs nothing.
[RequireComponent(typeof(PlayerController))]
public class Exertion : MonoBehaviour
{
    // life_core.lua M.EXERT
    private const float WalkPerMetre = 1f / 60f, Punch = 0.1f, Kick = 0.15f, JumpCost = 0.2f;
    private const float ReportEvery = 8f;
    private const float TiredSpeed = 0.55f;

    public static Exertion Instance { get; private set; }

    // Spent here but not yet in a server reply: LifeClient takes it off the energy it shows
    public static float Unconfirmed => Instance != null ? Instance.pendingCost + Instance.inFlightCost : 0f;

    private PlayerController player;
    private NakamaConnection connection;
    private Vector3 lastPosition;
    private float walkM, pendingCost, inFlightCost, nextReport;
    private int punches, kicks, jumps;
    private bool sending, exhausted;

    [Serializable] private class Report { public float walk_m; public int punches, kicks, jumps; }
    [Serializable] private class Reply { public int energy, max_energy; public bool sleeping; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        lastPosition = transform.position;
        nextReport = Time.time + ReportEvery;
    }

    public static void Attack(bool kick)
    {
        if (Instance == null) return;
        if (kick) Instance.kicks++; else Instance.punches++;
        Instance.pendingCost += kick ? Kick : Punch;
    }

    public static void Jumped()
    {
        if (Instance == null) return;
        Instance.jumps++;
        Instance.pendingCost += JumpCost;
    }

    private void Update()
    {
        // Distance walked on the ground under your own steam (not carried by a boat or swing, not teleports)
        Vector3 step = transform.position - lastPosition;
        lastPosition = transform.position;
        step.y = 0f;
        float d = step.magnitude;
        if (player.enabled && player.InputEnabled && player.IsGrounded && d > 0.001f && d < 2f)
        {
            float metres = d / Mathf.Max(1f, player.VehicleBoost);
            walkM += metres;
            pendingCost += metres * WalkPerMetre;
        }

        // Out of energy: exhausted, slow walk
        var life = LifeClient.Instance;
        bool empty = life != null && life.IsOnline && life.Energy < 1f;
        player.TiredSlow = empty ? TiredSpeed : 1f;
        if (empty && !exhausted)
            UiKit.FloatAbovePlayer("หมดแรง! กินอะไรหน่อยหรือนอนพัก", new Color(1f, 0.55f, 0.35f));
        exhausted = empty;

        if (Time.time >= nextReport && !sending) Send();
    }

    private async void Send()
    {
        nextReport = Time.time + ReportEvery;
        if (walkM < 0.5f && punches + kicks + jumps == 0) return;
        if (connection == null) connection = FindAnyObjectByType<NakamaConnection>();
        var conn = connection;
        if (conn == null || !conn.IsConnected) return;

        var report = new Report { walk_m = walkM, punches = punches, kicks = kicks, jumps = jumps };
        inFlightCost = pendingCost;
        walkM = 0f; punches = kicks = jumps = 0; pendingCost = 0f;
        sending = true;
        try
        {
            var result = await conn.Client.RpcAsync(conn.Session, "life_exert", JsonUtility.ToJson(report));
            var reply = JsonUtility.FromJson<Reply>(result.Payload);
            inFlightCost = 0f;
            LifeClient.Instance?.Apply(reply.energy, reply.max_energy, reply.sleeping);
        }
        catch (Exception e)
        {
            // Not counted: put it back for the next report
            walkM += report.walk_m; punches += report.punches; kicks += report.kicks; jumps += report.jumps;
            pendingCost += inFlightCost;
            inFlightCost = 0f;
            Debug.LogWarning($"[Exertion] Report failed: {e.Message}");
        }
        sending = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
