using UnityEngine;

// เมนู "ติดอยู่?": if you're ever stuck — wedged in something, or left unable to move after a cut-scene
// (waking up, sitting, a ride) — this frees the controls and puts you back at this map's arrival point
// (the bus stop, the pier, ...), standing.
public static class StuckRescue
{
    public static void Rescue()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        foreach (var pose in player.GetComponents<SeatedPose>()) Object.Destroy(pose);
        player.enabled = true;
        player.InputEnabled = true;

        string want = Locations.Current?.SpawnId;
        SpawnPoint best = null;
        foreach (var s in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (best == null) best = s;
            if (s.Id == want) { best = s; break; }
        }
        if (best != null) player.Teleport(best.transform.position + Vector3.up, best.transform.rotation);
        else player.Teleport(new Vector3(0f, 1.5f, 0f), Quaternion.identity);
        ScreenToast.Show("กลับมาที่จุดเริ่มของแผนที่นี้แล้ว", new Color(0.3f, 0.85f, 0.45f));
    }
}
