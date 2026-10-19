using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Runs one session in a ring: a fight (rounds against a BoxerNpc, KO or points) or pad work
// with the trainer (ครูฝึก calls "หมัด!" / "เตะ!", hit the right one in time). The server
// started it (boxing_start) and gets the result at the end (boxing_finish).
public class FightDirector : MonoBehaviour
{
    private const float HalfRing = 2.3f;
    private const float PadWindow = 1.0f;

    public static bool Busy => instance != null;
    private static FightDirector instance;

    private FightSpot spot;
    private BoxingClient.State session;
    private string eventId;
    private PlayerController player;
    private PlayerCombat combat;
    private PlayerActions actions;
    private BoxerNpc npc;
    private GameObject trainer;

    // HUD
    private GameObject hud;
    private Text banner, clock, leftName, rightName;
    private RectTransform leftFill, rightFill;

    private int landed;
    private bool playerDown, npcDown;
    private string padCall;
    private float padCallUntil;
    private int good, missed;

    public static void Begin(FightSpot spot, string eventId, BoxingClient.State session)
    {
        if (instance != null) return;
        instance = new GameObject("FightDirector").AddComponent<FightDirector>();
        instance.spot = spot;
        instance.eventId = eventId;
        instance.session = session;
        instance.StartCoroutine(session.opponent != null && session.opponent.hp > 0 ? instance.Fight() : instance.Pads());
    }

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController>();
        combat = player != null ? player.GetComponent<PlayerCombat>() : null;
        actions = player != null ? player.GetComponent<PlayerActions>() : null;
        BuildHud();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (combat != null)
        {
            combat.FightMode = false;
            combat.FightKnockout -= OnPlayerDown;
        }
        if (actions != null) actions.Performed -= OnPlayerAction;
        if (npc != null) Destroy(npc.gameObject);
        if (trainer != null) Destroy(trainer);
        if (hud != null) Destroy(hud);
    }

    private Transform Ring => spot.Ring;

    private void PlacePlayer(Vector3 local)
    {
        Vector3 world = Ring.TransformPoint(local + Vector3.up);
        Vector3 look = Ring.position - world;
        look.y = 0f;
        player.Teleport(world, Quaternion.LookRotation(look));
    }

    // ── Fight ───────────────────────────────────────────────────────────────────
    private IEnumerator Fight()
    {
        var o = session.opponent;
        combat.RestoreFull();
        combat.FightMode = true;
        combat.FightKnockout += OnPlayerDown;
        PlacePlayer(new Vector3(-1.6f, 0f, -1.6f));
        npc = BoxerNpc.Spawn(Ring, new Vector3(1.6f, 1f, 1.6f), HalfRing, o, combat);
        npc.KnockedOut += () => npcDown = true;
        int lastDamage = 0;
        npc.Changed += () => { if (npc.DamageTaken > lastDamage) { landed++; lastDamage = npc.DamageTaken; } UpdateBars(); };
        leftName.text = ProfileClient.Instance?.DisplayName ?? "คุณ";
        rightName.text = $"{o.name} (Lv {o.level})";
        UpdateBars();

        if (o.strong) yield return Banner("ระวัง! เจอตัวเก่ง", 2f);

        string result = "points";
        for (int round = 1; round <= session.rounds && result == "points"; round++)
        {
            npc.Active = false;
            yield return Banner($"ยกที่ {round}", 1.5f);
            yield return Banner("ชก!", 0.6f);
            npc.Active = true;
            float end = Time.time + session.round_s;
            while (Time.time < end)
            {
                clock.text = $"ยก {round}/{session.rounds} · {Mathf.CeilToInt(end - Time.time)}";
                UpdateBars();
                if (npcDown) { result = "ko_win"; break; }
                if (playerDown) { result = "ko_loss"; break; }
                yield return null;
            }
            npc.Active = false;
            if (result == "points" && round < session.rounds) yield return Banner("พักยก", 2.5f);
        }

        yield return Banner(result == "ko_win" ? "KO! ชนะน็อค!" : result == "ko_loss" ? "ถูกน็อค..." : "หมดเวลา — รอผลคะแนน", 2f);
        yield return Finish(result, npc.HitsLanded, npc.DamageTaken);
    }

    private void OnPlayerDown() => playerDown = true;

    private void UpdateBars()
    {
        if (combat != null) leftFill.anchorMax = new Vector2(Mathf.Clamp01((float)combat.Health / combat.MaxHealth), 1f);
        if (npc != null) rightFill.anchorMax = new Vector2(Mathf.Clamp01((float)npc.Hp / npc.MaxHp), 1f);
    }

    private void LateUpdate()
    {
        // Keep the player inside the ropes during the session
        if (player == null || Ring == null) return;
        Vector3 p = Ring.InverseTransformPoint(player.transform.position);
        if (Mathf.Abs(p.x) <= HalfRing && Mathf.Abs(p.z) <= HalfRing) return;
        p.x = Mathf.Clamp(p.x, -HalfRing, HalfRing);
        p.z = Mathf.Clamp(p.z, -HalfRing, HalfRing);
        player.Teleport(Ring.TransformPoint(p), player.transform.rotation);
    }

    // ── Pad work ────────────────────────────────────────────────────────────────
    private IEnumerator Pads()
    {
        PlacePlayer(new Vector3(0f, 0f, -1.4f));
        trainer = new GameObject("Trainer_ครูฝึก");
        trainer.transform.SetParent(Ring, false);
        trainer.transform.localPosition = new Vector3(0f, 1f, 0f);
        trainer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        trainer.AddComponent<CharacterModel>().SetSkin(1);
        actions.Performed += OnPlayerAction;
        leftName.text = "ซ้อมเป้าล่อ";
        rightName.text = "ครูฝึก";
        rightFill.parent.gameObject.SetActive(false);
        leftFill.parent.gameObject.SetActive(false);

        yield return Banner("ครูฝึก: ฟังคำสั่งแล้วออกอาวุธให้ทัน!", 2f);
        float end = Time.time + session.seconds;
        while (Time.time < end)
        {
            padCall = UnityEngine.Random.value < 0.6f ? "หมัด!" : "เตะ!";
            padCallUntil = Time.time + PadWindow;
            banner.text = padCall;
            banner.gameObject.SetActive(true);
            FloatingText.Spawn(trainer.transform.position + Vector3.up * 1.6f, padCall, new Color(1f, 0.9f, 0.4f));
            while (Time.time < padCallUntil) { Tick(end); yield return null; }
            if (padCall != null) { missed++; padCall = null; }
            banner.gameObject.SetActive(false);
            float pause = Time.time + 0.35f;
            while (Time.time < pause) { Tick(end); yield return null; }
        }
        yield return Banner($"ครบเวลา! เป๊ะ {good} ครั้ง", 1.5f);
        yield return Finish("pads", 0, 0);
    }

    private void Tick(float end) => clock.text = $"เป๊ะ {good} · พลาด {missed} · {Mathf.CeilToInt(end - Time.time)}";

    private void OnPlayerAction(PlayerAction action)
    {
        if (padCall == null || Time.time > padCallUntil) return;
        bool wanted = padCall == "หมัด!" ? action == PlayerAction.Punch : action == PlayerAction.Kick;
        if (action != PlayerAction.Punch && action != PlayerAction.Kick) return;
        bool close = Vector3.Distance(player.transform.position, trainer.transform.position) < 2.2f;
        if (wanted && close) good++; else missed++;
        FloatingText.Spawn(trainer.transform.position + Vector3.up * 1.2f, wanted && close ? "เป๊ะ!" : close ? "ผิด!" : "เข้าใกล้อีก!",
            wanted && close ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f));
        padCall = null;
        banner.gameObject.SetActive(false);
    }

    // ── End ─────────────────────────────────────────────────────────────────────
    private IEnumerator Finish(string result, int taken, int damage)
    {
        var boxing = BoxingClient.Instance;
        var task = boxing.FinishAsync(session.session, result, landed, taken, damage, good);
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            var e = task.Exception?.InnerException;
            BoxingUI.ShowMessage("ส่งผลไม่สำเร็จ", e is ApiResponseException api ? api.Message : "ลองใหม่อีกครั้ง");
            Debug.LogWarning($"[Boxing] finish failed: {e?.Message}");
        }
        else
        {
            BoxingUI.ShowResult(task.Result);
            if (task.Result.purse > 0)
            {
                UiKit.FloatAbovePlayer($"+{task.Result.purse} ฿", new Color(1f, 0.85f, 0.2f));
                if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            }
            CareerClient.Instance?.Reload();
        }

        yield return new WaitForSeconds(1f);
        combat.FightMode = false;
        combat.RestoreFull();
        player.Teleport(spot.transform.position + Vector3.up * 0.1f, spot.transform.rotation);
        LifeClient.Instance?.Refresh();
        Destroy(gameObject);
    }

    // ── HUD ─────────────────────────────────────────────────────────────────────
    private IEnumerator Banner(string text, float seconds)
    {
        banner.text = text;
        banner.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        banner.gameObject.SetActive(false);
    }

    private void BuildHud()
    {
        hud = new GameObject("FightHud", typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var top = UiKit.Panel("FightBar", hud.transform, UiKit.TeakDark, Vector2.zero, new Vector2(1100f, 120f), shadow: true);
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = new Vector2(0f, -200f);
        leftName = UiKit.Label(top, 26, "", new Vector2(-300f, 30f), new Vector2(440f, 40f), UiKit.HeaderText, TextAnchor.MiddleLeft);
        rightName = UiKit.Label(top, 26, "", new Vector2(300f, 30f), new Vector2(440f, 40f), UiKit.HeaderText, TextAnchor.MiddleRight);
        leftFill = Bar(top, new Vector2(-300f, -15f), new Color(0.3f, 0.8f, 0.4f));
        rightFill = Bar(top, new Vector2(300f, -15f), new Color(0.9f, 0.3f, 0.25f));
        clock = UiKit.Label(top, 28, "", new Vector2(0f, -40f), new Vector2(420f, 40f), UiKit.GoldText);
        clock.fontStyle = FontStyle.Bold;

        banner = UiKit.Label(hud.transform, 96, "", new Vector2(0f, 120f), new Vector2(1400f, 160f), new Color(1f, 0.85f, 0.3f));
        banner.fontStyle = FontStyle.Bold;
        banner.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        banner.gameObject.SetActive(false);
    }

    private static RectTransform Bar(RectTransform parent, Vector2 pos, Color color)
    {
        var track = UiKit.Panel("Track", parent, new Color(0f, 0f, 0f, 0.5f), pos, new Vector2(440f, 26f));
        var fill = UiKit.Rect("Fill", track, color);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        return fill;
    }
}
