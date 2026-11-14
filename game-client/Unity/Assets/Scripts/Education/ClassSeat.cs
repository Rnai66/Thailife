using UnityEngine;

// Sitting down in class (นั่งเรียน): while a class or a library book runs, the player sits on the
// nearest free chair of that room — a classroom "Chair" (facing away from its "ChairBack", towards the
// blackboard) or a library "Stool" (facing the nearest "ReadingTable") — and stands back up where they
// were when it ends or the window is closed.
public static class ClassSeat
{
    private static PlayerController sitter;
    private static SeatedPose pose;
    private static Vector3 standPosition;
    private static Quaternion standRotation;

    public static bool Seated => sitter != null;

    // Standing up puts the player back on the desk zone; the window closed by hand stays closed
    public static bool JustClosed => Time.time < quietUntil;
    private static float quietUntil;

    // `room` = the school building the desk zone belongs to; `near` = the desk zone
    public static void Sit(Transform room, Vector3 near, bool writing = false)
    {
        if (sitter != null || room == null) return;
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Transform best = null;
        float bestDist = 9f * 9f;
        foreach (var t in room.GetComponentsInChildren<Transform>())
        {
            if (t.name != "Chair" && t.name != "Stool") continue;
            float d = (t.position - near).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t; }
        }
        if (best == null) return;

        // Which way to face
        Vector3 facing = Vector3.zero;
        string lookFrom = best.name == "Chair" ? "ChairBack" : "ReadingTable";
        float closest = float.MaxValue;
        foreach (var t in best.parent.GetComponentsInChildren<Transform>())
        {
            if (t.name != lookFrom) continue;
            float d = (t.position - best.position).sqrMagnitude;
            if (d >= closest) continue;
            closest = d;
            facing = best.name == "Chair" ? best.position - t.position : t.position - best.position;
        }
        facing.y = 0f;
        if (facing.sqrMagnitude < 1e-4f) facing = -player.transform.forward;

        // Hips just above the seat's top
        var r = best.GetComponent<Renderer>();
        float top = r != null ? r.bounds.max.y : best.position.y + 0.03f;
        var rot = Quaternion.LookRotation(facing.normalized, Vector3.up);
        Vector3 hips = SeatedPose.HipsOffset(player.transform);

        sitter = player;
        standPosition = player.transform.position;
        standRotation = player.transform.rotation;
        player.InputEnabled = false;
        player.Teleport(SeatedPose.RootFor(new Vector3(best.position.x, top + 0.08f, best.position.z) - facing.normalized * 0.08f, rot, hips), rot);
        player.enabled = false;
        pose = player.gameObject.AddComponent<SeatedPose>();
        pose.Writing = writing;
    }

    public static void Stand(bool closing = false)
    {
        if (closing && sitter != null) quietUntil = Time.time + 1f;
        var player = sitter;
        sitter = null;
        if (pose != null) Object.Destroy(pose);
        pose = null;
        if (player == null) return;
        player.enabled = true;
        player.InputEnabled = true;
        player.Teleport(standPosition, standRotation);
    }
}
