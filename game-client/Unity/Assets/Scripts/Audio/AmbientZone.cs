using UnityEngine;

// A place with its own sound (วัดมีเสียงพระสวด, ร้านค้ามีเสียงประตู, โคกหนองนามีเสียงควาย…):
// it plays once as soon as you walk in, then only now and then while you stay.
// Added at runtime by GameAudio, so the scenes don't need rebuilding.
public class AmbientZone : MonoBehaviour
{
    private string[] sounds;
    private float radius, minGap, maxGap, volume;
    private Bounds area;
    private bool inside;
    private float lastPlayed = -999f, nextPlay, nextCheck;

    public void Configure(Bounds bounds, float range, float gapMin, float gapMax, float loudness, params string[] clips)
    {
        area = bounds;
        radius = range;
        minGap = gapMin;
        maxGap = gapMax;
        volume = loudness;
        sounds = clips;
    }

    private void Update()
    {
        if (Time.time < nextCheck || sounds == null || sounds.Length == 0) return;
        nextCheck = Time.time + 0.5f;
        var player = GameAudio.Player;
        if (player == null) return;

        Vector3 p = player.position;
        Vector3 closest = area.ClosestPoint(p);
        bool nowInside = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(closest.x, 0f, closest.z)) <= radius;

        if (nowInside && !inside && Time.time - lastPlayed > 20f)
            PlayNow(closest, p);           // the "you've arrived" sound
        else if (nowInside && Time.time >= nextPlay)
            PlayNow(closest, p);           // occasional repeat, not too often
        inside = nowInside;
    }

    private void PlayNow(Vector3 closest, Vector3 player)
    {
        var pen = GetComponent<CowPen>();
        if (pen != null && !pen.HasCows) return; // an empty pen stays quiet
        lastPlayed = Time.time;
        nextPlay = Time.time + Random.Range(minGap, maxGap);
        // Sounds come from the place itself, but no further than a few metres away so they're heard
        Vector3 from = Vector3.MoveTowards(player, new Vector3(closest.x, player.y + 1.5f, closest.z), 8f);
        GameAudio.Play(sounds[Random.Range(0, sounds.Length)], from, volume, radius + 15f);
    }
}
