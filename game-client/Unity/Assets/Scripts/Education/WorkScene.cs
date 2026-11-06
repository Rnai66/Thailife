using System.Collections.Generic;
using UnityEngine;

// ฉากทำงาน: while a job's shift runs (JobDesk), the workplace comes alive around the player instead of
// them just standing by a sign:
//   tutor       a whiteboard filling with working as you write, four pupils at their desks (hands go up)
//   office      the computer technician at the bench: an open PC (fan spinning), screwdriver in hand,
//               the monitor running its diagnostics
//   programmer  sitting at two monitors, code scrolling up the screen as you type
//   repair      bent over the motorbike in the shop with a wrench
//   coach       kids lined up between cones doing exercises, the coach leading with the whistle
//   doctor      a patient sitting on the bed while you listen with the stethoscope
// Everything is built here at run time without colliders, and goes away when the shift ends or the
// player steps away (the server keeps counting the time either way).
public class WorkScene : MonoBehaviour
{
    private static WorkScene current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => current = null;

    public static bool Active => current != null;
    public static string JobId => current != null ? current.job : null;

    private string job;
    private PlayerController player;
    private SeatedPose seat;
    private Vector3 standPosition;
    private Quaternion standRotation;

    // what moves
    private readonly List<CharacterModel> pupils = new List<CharacterModel>();
    private readonly List<float> nextGesture = new List<float>();
    private readonly List<Transform> codeLines = new List<Transform>();
    private readonly List<Transform> boardLines = new List<Transform>();
    private Transform fan, progress, screen;
    private Vector3 faceA, faceB; // tutor: the board / the class
    private float nextLine, phaseStart;
    private bool explaining;

    private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    // ── Start / stop ───────────────────────────────────────────────────────────
    public static void Begin(string jobId, Transform zone)
    {
        if (current != null && current.job == jobId) return;
        End();
        if (zone == null || ClassSeat.Seated) return;
        var p = FindAnyObjectByType<PlayerController>();
        if (p == null || !p.enabled) return;
        var spot = zone.parent != null ? zone.parent : zone;
        var go = new GameObject("WorkScene_" + jobId);
        go.transform.SetPositionAndRotation(new Vector3(spot.position.x, spot.position.y, spot.position.z), Quaternion.Euler(0f, spot.eulerAngles.y, 0f));
        current = go.AddComponent<WorkScene>();
        current.job = jobId;
        current.player = p;
        current.standPosition = p.transform.position;
        current.standRotation = p.transform.rotation;
        p.InputEnabled = false;
        current.Build(spot);
    }

    public static void End()
    {
        if (current == null) return;
        var w = current;
        current = null;
        ActivityPose.Clear();
        if (w.seat != null) Destroy(w.seat);
        if (w.player != null)
        {
            w.player.enabled = true;
            w.player.InputEnabled = true;
            w.player.Teleport(w.standPosition, w.standRotation);
        }
        Destroy(w.gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) End();
    }

    // ── Layout per job (local space of the job spot: +z = the spot's forward) ─────
    private void Build(Transform spot)
    {
        switch (job)
        {
            case "tutor": Tutor(); break;
            case "office": Desk(programmer: false); break;
            case "programmer": Desk(programmer: true); break;
            case "repair": Repair(spot); break;
            case "coach": Coach(); break;
            case "doctor": Doctor(); break;
            default: ActivityPose.Work("type"); break;
        }
    }

    private void Tutor()
    {
        var t = transform;
        // whiteboard on its stand, its back to the wall
        Box("Board", t, new Vector3(0f, 1.5f, -1.25f), new Vector3(2.1f, 1.05f, 0.05f), Mat("WB", new Color(0.97f, 0.97f, 0.97f)));
        Box("BoardFrame", t, new Vector3(0f, 1.5f, -1.28f), new Vector3(2.2f, 1.15f, 0.04f), Mat("WBFrame", new Color(0.55f, 0.57f, 0.6f)));
        Box("Tray", t, new Vector3(0f, 0.96f, -1.18f), new Vector3(1.6f, 0.03f, 0.1f), Mat("WBFrame", new Color(0.55f, 0.57f, 0.6f)));
        foreach (float x in new[] { -0.9f, 0.9f })
            Box("Leg", t, new Vector3(x, 0.5f, -1.3f), new Vector3(0.05f, 1f, 0.05f), Mat("WBFrame", new Color(0.55f, 0.57f, 0.6f)));
        // the working, hidden until written: rows of strokes (an equation, a box, more lines)
        var ink = Mat("Ink", new Color(0.15f, 0.25f, 0.7f));
        var red = Mat("InkRed", new Color(0.8f, 0.15f, 0.15f));
        for (int row = 0; row < 6; row++)
        {
            float y = 1.88f - row * 0.15f;
            int strokes = 3 + (row * 7) % 4;
            float x = -0.9f + (row % 3) * 0.08f;
            for (int i = 0; i < strokes; i++)
            {
                float w = 0.08f + ((row * 5 + i * 3) % 5) * 0.04f;
                var s = Box("Stroke", t, new Vector3(x + w * 0.5f, y, -1.22f), new Vector3(w, 0.025f, 0.005f), row == 3 && i == 1 ? red : ink);
                s.gameObject.SetActive(false);
                boardLines.Add(s);
                x += w + 0.06f;
            }
        }
        // four pupils at desks facing the board
        var wood = Mat("DeskWood", new Color(0.72f, 0.52f, 0.3f));
        var metal = Mat("DeskMetal", new Color(0.3f, 0.45f, 0.65f));
        int look = 0;
        foreach (float z in new[] { 1.1f, 2.15f })
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                Box("Desk", t, new Vector3(x, 0.62f, z), new Vector3(0.75f, 0.04f, 0.45f), wood);
                Box("DeskLeg", t, new Vector3(x, 0.31f, z), new Vector3(0.65f, 0.6f, 0.05f), metal);
                Box("Notebook", t, new Vector3(x + 0.08f, 0.645f, z + 0.02f), new Vector3(0.24f, 0.01f, 0.18f), Mat("Paper", new Color(0.98f, 0.97f, 0.92f)));
                Box("Stool", t, new Vector3(x, 0.36f, z + 0.42f), new Vector3(0.36f, 0.04f, 0.32f), metal);
                Box("StoolLeg", t, new Vector3(x, 0.18f, z + 0.42f), new Vector3(0.05f, 0.36f, 0.05f), metal);
                Sitter(t.TransformPoint(new Vector3(x, 0.38f, z + 0.45f)), t.rotation * Quaternion.Euler(0f, 180f, 0f), 2 + look++ % 5, 0.8f);
            }
        faceA = t.TransformPoint(new Vector3(0.45f, 0f, -2f));
        faceB = t.TransformPoint(new Vector3(0.45f, 0f, 2f));
        Stand(new Vector3(0.6f, 0f, -0.7f), faceA);
        ActivityPose.Work("write", faceA);
        phaseStart = Time.time;
    }

    private void Desk(bool programmer)
    {
        var t = transform;
        var wood = Mat("OfficeDesk", new Color(0.85f, 0.82f, 0.76f));
        var black = Mat("Plastic", new Color(0.1f, 0.1f, 0.12f));
        var grey = Mat("CaseGrey", new Color(0.6f, 0.62f, 0.65f));
        Box("Desk", t, new Vector3(0f, 0.74f, -0.75f), new Vector3(1.5f, 0.04f, 0.7f), wood);
        foreach (float x in new[] { -0.7f, 0.7f })
            Box("DeskLeg", t, new Vector3(x, 0.37f, -0.75f), new Vector3(0.05f, 0.72f, 0.6f), grey);
        Box("Keyboard", t, new Vector3(0f, 0.775f, -0.5f), new Vector3(0.45f, 0.02f, 0.15f), black);
        Box("Mouse", t, new Vector3(0.33f, 0.775f, -0.5f), new Vector3(0.06f, 0.025f, 0.1f), black);
        // chair
        Box("Seat", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.48f, 0.06f, 0.45f), black);
        Box("Back", t, new Vector3(0f, 0.8f, 0.24f), new Vector3(0.46f, 0.6f, 0.05f), black);
        Box("Post", t, new Vector3(0f, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.06f), grey);

        if (programmer)
        {
            foreach (float x in new[] { -0.3f, 0.32f })
            {
                var mon = Monitor(t, new Vector3(x, 0.76f, -0.92f), x < 0f ? 12f : -12f);
                CodeScreen(mon);
            }
            Box("Mug", t, new Vector3(0.6f, 0.81f, -0.6f), new Vector3(0.08f, 0.1f, 0.08f), Mat("MugWhite", new Color(0.95f, 0.95f, 0.92f)));
        }
        else
        {
            // the PC on the bench with its side panel off: board, RAM, a fan going round
            var pc = new GameObject("OpenPC").transform;
            pc.SetParent(t, false);
            pc.localPosition = new Vector3(-0.4f, 0.76f, -0.75f);
            Box("Case", pc, new Vector3(0f, 0.11f, 0f), new Vector3(0.48f, 0.22f, 0.42f), grey);
            Box("Board", pc, new Vector3(0f, 0.225f, 0f), new Vector3(0.42f, 0.01f, 0.36f), Mat("PCB", new Color(0.1f, 0.45f, 0.25f)));
            for (int i = 0; i < 2; i++)
                Box("RAM", pc, new Vector3(0.08f + i * 0.04f, 0.25f, -0.03f), new Vector3(0.015f, 0.04f, 0.2f), Mat("RAM", new Color(0.15f, 0.15f, 0.2f)));
            Box("CPU", pc, new Vector3(-0.08f, 0.24f, 0f), new Vector3(0.1f, 0.02f, 0.1f), grey);
            fan = Box("Fan", pc, new Vector3(-0.08f, 0.27f, 0f), new Vector3(0.12f, 0.01f, 0.025f), black);
            Box("FanBlade2", fan, Vector3.zero, new Vector3(0.2f, 1f, 4.8f), black);
            Box("SidePanel", t, new Vector3(-0.2f, 0.4f, -0.3f), new Vector3(0.42f, 0.8f, 0.02f), grey).localRotation = Quaternion.Euler(0f, 30f, -8f);
            Box("Screws", t, new Vector3(-0.05f, 0.765f, -0.42f), new Vector3(0.08f, 0.01f, 0.05f), Mat("Steel", new Color(0.7f, 0.72f, 0.75f)));
            var mon = Monitor(t, new Vector3(0.35f, 0.76f, -0.95f), -8f);
            DiagnosticScreen(mon);
        }
        Sit(new Vector3(0f, 0.48f, 0.02f), t.TransformPoint(new Vector3(0f, 0f, -2f)));
        ActivityPose.Work(programmer ? "type" : "repairpc");
    }

    private void Repair(Transform spot)
    {
        // the shop's own motorbike if this spot belongs to one, else one of our own in front of the spot
        Transform bike = null;
        if (spot.parent != null)
            foreach (var c in spot.parent.GetComponentsInChildren<Transform>())
                if (c.name == "Motorbike") { bike = c; break; }
        if (bike == null)
        {
            var t = transform;
            var red = Mat("BikeRed", new Color(0.85f, 0.15f, 0.15f));
            var rubber = Mat("Rubber", new Color(0.1f, 0.1f, 0.11f));
            bike = new GameObject("Motorbike").transform;
            bike.SetParent(t, false);
            bike.localPosition = new Vector3(0f, 0f, 1.3f);
            bike.localRotation = Quaternion.Euler(0f, 90f, 0f);
            foreach (float z in new[] { -0.65f, 0.65f })
                Cyl("Wheel", bike, new Vector3(0f, 0.32f, z), new Vector3(0.62f, 0.05f, 0.62f), rubber, new Vector3(0f, 0f, 90f));
            Box("Body", bike, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.35f, 1.1f), red);
        }
        // kneel at the bike's side (the side nearer to where the player stood)
        Vector3 side = bike.right;
        side.y = 0f;
        if (Vector3.Dot(standPosition - bike.position, side) < 0f) side = -side;
        Vector3 at = bike.position + side.normalized * 0.75f;
        // a few tools on a rag by the bike
        var rag = Box("Rag", transform, Vector3.zero, new Vector3(0.6f, 0.01f, 0.4f), Mat("Rag", new Color(0.35f, 0.4f, 0.55f)));
        rag.position = bike.position + side.normalized * 0.7f + bike.forward * 0.6f + Vector3.up * 0.01f;
        var tools = Box("SpareParts", transform, Vector3.zero, new Vector3(0.2f, 0.05f, 0.12f), Mat("Steel", new Color(0.7f, 0.72f, 0.75f)));
        tools.position = rag.position + Vector3.up * 0.03f;
        StandWorld(new Vector3(at.x, bike.position.y, at.z), bike.position);
        ActivityPose.Work("wrench", bike.position);
    }

    private void Coach()
    {
        var t = transform;
        var cone = Mat("Cone", new Color(1f, 0.45f, 0.1f));
        for (int i = 0; i < 4; i++)
        {
            float x = -1.8f + i * 1.2f;
            Cyl("Cone", t, new Vector3(x, 0.12f, 3f), new Vector3(0.18f, 0.12f, 0.18f), cone, Vector3.zero);
            var kid = Npc(t.TransformPoint(new Vector3(x, 0f, 2.3f)), t.rotation * Quaternion.Euler(0f, 180f, 0f), 2 + i, 0.78f);
            pupils.Add(kid);
            nextGesture.Add(Time.time + 0.6f + i * 0.25f);
        }
        Stand(new Vector3(0f, 0f, 0f), t.TransformPoint(new Vector3(0f, 0f, 3f)));
        ActivityPose.Work("whistle", t.TransformPoint(new Vector3(0f, 0f, 3f)));
    }

    private void Doctor()
    {
        var t = transform;
        var white = Mat("Sheet", new Color(0.95f, 0.96f, 0.98f));
        var steel = Mat("Steel", new Color(0.7f, 0.72f, 0.75f));
        Box("Bed", t, new Vector3(1.15f, 0.5f, 0.2f), new Vector3(0.8f, 0.12f, 1.9f), white);
        Box("BedFrame", t, new Vector3(1.15f, 0.25f, 0.2f), new Vector3(0.75f, 0.45f, 1.8f), steel);
        Box("Pillow", t, new Vector3(1.15f, 0.6f, 0.95f), new Vector3(0.5f, 0.08f, 0.3f), white);
        Cyl("IVStand", t, new Vector3(1.7f, 0.9f, 0.9f), new Vector3(0.03f, 0.9f, 0.03f), steel, Vector3.zero);
        Box("IVBag", t, new Vector3(1.7f, 1.7f, 0.9f), new Vector3(0.14f, 0.2f, 0.04f), Mat("IV", new Color(0.8f, 0.9f, 1f)));
        // the patient sits on the edge of the bed, facing the doctor
        Sitter(t.TransformPoint(new Vector3(1.0f, 0.56f, 0.1f)), t.rotation * Quaternion.Euler(0f, -90f, 0f), 4, 1f);
        Stand(new Vector3(0.35f, 0f, 0.1f), t.TransformPoint(new Vector3(1.0f, 0f, 0.1f)));
        ActivityPose.Work("examine", t.TransformPoint(new Vector3(1.0f, 0f, 0.1f)));
    }

    // ── Player placement ───────────────────────────────────────────────────────
    private void Stand(Vector3 local, Vector3 face) => StandWorld(transform.TransformPoint(local), face);

    private void StandWorld(Vector3 feet, Vector3 face)
    {
        var d = face - feet;
        d.y = 0f;
        var rot = d.sqrMagnitude > 0.001f ? Quaternion.LookRotation(d) : player.transform.rotation;
        player.Teleport(feet + Vector3.up * 1.02f, rot);
        player.enabled = false;
    }

    private void Sit(Vector3 localSeat, Vector3 face)
    {
        var seatAt = transform.TransformPoint(localSeat);
        var d = face - seatAt;
        d.y = 0f;
        var rot = Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : transform.forward);
        var hips = SeatedPose.HipsOffset(player.transform);
        player.Teleport(SeatedPose.RootFor(seatAt + Vector3.up * 0.06f, rot, hips), rot);
        player.enabled = false;
        seat = player.gameObject.AddComponent<SeatedPose>();
    }

    // ── People ─────────────────────────────────────────────────────────────────
    private CharacterModel Npc(Vector3 feet, Quaternion rot, int look, float scale)
    {
        var go = new GameObject("WorkNpc");
        go.transform.SetParent(transform, true);
        go.transform.SetPositionAndRotation(feet + Vector3.up * scale, rot);
        go.transform.localScale = Vector3.one * scale;
        var model = go.AddComponent<CharacterModel>();
        model.SetSkin(look % Mathf.Max(1, CharacterModel.SkinCount));
        return model;
    }

    private void Sitter(Vector3 seatTop, Quaternion rot, int look, float scale)
    {
        var model = Npc(seatTop, rot, look, scale);
        var hips = SeatedPose.HipsOffset(model.transform);
        model.transform.position = SeatedPose.RootFor(seatTop + Vector3.up * 0.05f, rot, hips);
        model.gameObject.AddComponent<SeatedPose>();
        pupils.Add(model);
        nextGesture.Add(Time.time + Random.Range(3f, 9f));
    }

    // ── Screens ────────────────────────────────────────────────────────────────
    private Transform Monitor(Transform t, Vector3 local, float yaw)
    {
        var black = Mat("Plastic", new Color(0.1f, 0.1f, 0.12f));
        var m = new GameObject("Monitor").transform;
        m.SetParent(t, false);
        m.localPosition = local;
        m.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box("Foot", m, new Vector3(0f, 0.01f, 0f), new Vector3(0.22f, 0.02f, 0.16f), black);
        Box("Neck", m, new Vector3(0f, 0.12f, 0.02f), new Vector3(0.04f, 0.22f, 0.03f), black);
        Box("Frame", m, new Vector3(0f, 0.36f, 0f), new Vector3(0.58f, 0.36f, 0.03f), black);
        // screen face towards +z (the chair)
        var s = Box("Screen", m, new Vector3(0f, 0.36f, 0.016f), new Vector3(0.54f, 0.32f, 0.004f), Mat("ScreenDark", new Color(0.08f, 0.1f, 0.16f), unlit: true));
        return s;
    }

    // Lines of "code": indented coloured bars that scroll up as the player types
    private void CodeScreen(Transform s)
    {
        screen = s;
        var colors = new[] { new Color(0.55f, 0.75f, 1f), new Color(0.95f, 0.6f, 0.3f), new Color(0.6f, 0.9f, 0.5f), new Color(0.85f, 0.85f, 0.85f), new Color(0.9f, 0.5f, 0.85f) };
        for (int i = 0; i < 10; i++)
        {
            var line = new GameObject("CodeLine").transform;
            line.SetParent(s, false);
            codeLines.Add(line);
            Fill(line, colors);
            line.localPosition = new Vector3(0f, 0.42f - i * 0.092f, 1.5f);
        }
    }

    private void Fill(Transform line, Color[] colors)
    {
        foreach (Transform c in line) Destroy(c.gameObject);
        float x = -0.44f + Random.Range(0, 4) * 0.06f;
        int tokens = Random.Range(1, 5);
        for (int i = 0; i < tokens && x < 0.42f; i++)
        {
            float w = Random.Range(0.06f, 0.24f);
            var c = colors[Random.Range(0, colors.Length)];
            var b = Box("Tok", line, new Vector3(x + w * 0.5f, 0f, 0f), new Vector3(w, 0.05f, 1f), Mat("Code" + c.GetHashCode(), c, unlit: true));
            b.localPosition = new Vector3(x + w * 0.5f, 0f, 0f);
            x += w + 0.035f;
        }
    }

    // Diagnostics: a list of checks ticking green and a progress bar
    private void DiagnosticScreen(Transform s)
    {
        screen = s;
        var green = Mat("DiagGreen", new Color(0.35f, 0.95f, 0.45f), unlit: true);
        var text = Mat("DiagText", new Color(0.8f, 0.85f, 0.9f), unlit: true);
        for (int i = 0; i < 5; i++)
        {
            var line = new GameObject("Check").transform;
            line.SetParent(s, false);
            line.localPosition = new Vector3(0f, 0.38f - i * 0.13f, 1.5f);
            Box("Label", line, new Vector3(-0.15f, 0f, 0f), new Vector3(0.5f + (i % 3) * 0.1f, 0.06f, 1f), text);
            var ok = Box("OK", line, new Vector3(0.38f, 0f, 0f), new Vector3(0.1f, 0.07f, 1f), green);
            ok.gameObject.SetActive(false);
            codeLines.Add(ok);
        }
        Box("BarBack", s, new Vector3(0f, -0.38f, 1.5f), new Vector3(0.9f, 0.08f, 1f), text);
        progress = Box("Bar", s, new Vector3(-0.45f, -0.38f, 2f), new Vector3(0.01f, 0.08f, 1f), green);
    }

    // ── Animation ──────────────────────────────────────────────────────────────
    private void Update()
    {
        if (player == null) { End(); return; }
        float now = Time.time;

        // pupils / kids / patient: a hand up now and then; the kids jump in turn
        for (int i = 0; i < pupils.Count; i++)
        {
            if (pupils[i] == null || now < nextGesture[i]) continue;
            if (job == "coach") { pupils[i].Play(PlayerAction.Jump); nextGesture[i] = now + 1.6f; }
            else { pupils[i].Play(Random.value < 0.75f ? PlayerAction.Wave : PlayerAction.Wai); nextGesture[i] = now + Random.Range(5f, 11f); }
        }

        if (fan != null) fan.Rotate(0f, 900f * Time.deltaTime, 0f, Space.Self);

        if (job == "tutor")
        {
            // write for a while (strokes appear one by one), then turn round and explain
            float phase = now - phaseStart;
            if (!explaining && phase > 7f) { explaining = true; phaseStart = now; ActivityPose.Work("explain", faceB); }
            else if (explaining && phase > 4f)
            {
                explaining = false;
                phaseStart = now;
                if (boardLines.TrueForAll(b => b.gameObject.activeSelf)) foreach (var b in boardLines) b.gameObject.SetActive(false); // wipe the board
                ActivityPose.Work("write", faceA);
            }
            if (!explaining && now >= nextLine)
            {
                nextLine = now + 0.35f;
                var hidden = boardLines.Find(b => !b.gameObject.activeSelf);
                if (hidden != null) hidden.gameObject.SetActive(true);
            }
        }
        else if (job == "programmer" && now >= nextLine && codeLines.Count > 0)
        {
            nextLine = now + Random.Range(0.18f, 0.45f);
            // every line moves up a row (per screen); the top one comes back at the bottom with new code
            var colors = new[] { new Color(0.55f, 0.75f, 1f), new Color(0.95f, 0.6f, 0.3f), new Color(0.6f, 0.9f, 0.5f), new Color(0.85f, 0.85f, 0.85f), new Color(0.9f, 0.5f, 0.85f) };
            foreach (var l in codeLines)
            {
                var p = l.localPosition;
                p.y += 0.092f;
                if (p.y > 0.43f) { p.y -= 0.092f * 10f; Fill(l, colors); }
                l.localPosition = p;
            }
        }
        else if (job == "office" && progress != null)
        {
            // the bar fills over ~12 s, the checks go green as it passes them, then it starts over
            float k = ((now - phaseStart) % 12f) / 12f;
            progress.localScale = new Vector3(Mathf.Max(0.01f, 0.9f * k), 0.08f, 1f);
            progress.localPosition = new Vector3(-0.45f + 0.45f * k, -0.38f, 2f);
            for (int i = 0; i < codeLines.Count; i++) codeLines[i].gameObject.SetActive(k > (i + 1) / (codeLines.Count + 1f));
        }
    }

    // ── Shapes ─────────────────────────────────────────────────────────────────
    private static Transform Box(string name, Transform parent, Vector3 local, Vector3 scale, Material m) =>
        Shape(PrimitiveType.Cube, name, parent, local, scale, m, Vector3.zero);

    private static Transform Cyl(string name, Transform parent, Vector3 local, Vector3 scale, Material m, Vector3 euler) =>
        Shape(PrimitiveType.Cylinder, name, parent, local, scale, m, euler);

    private static Transform Shape(PrimitiveType type, string name, Transform parent, Vector3 local, Vector3 scale, Material m, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        t.localRotation = Quaternion.Euler(euler);
        t.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return t;
    }

    private static Material Mat(string name, Color c, bool unlit = false)
    {
        if (mats.TryGetValue(name, out var m) && m != null) return m;
        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { name = name, color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        mats[name] = m;
        return m;
    }
}
