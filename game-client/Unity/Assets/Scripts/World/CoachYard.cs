using System.Collections;
using UnityEngine;

// ลานจอดรถทัวร์ บขส.: the coaches at the terminal's bays come and go. Every half minute or so one
// pulls out — forward out of its bay, a left turn onto the yard lane and away down it, off on its
// trip — or one comes in along the lane, stops past an empty bay and backs into it. All in the
// terminal's own space (TerminalBuilder): bays along local X with the coaches' noses to local -Z,
// the yard lane along local X in front of them.
public class CoachYard : MonoBehaviour
{
    [SerializeField] private Transform[] coaches;
    [SerializeField] private float[] bayX;
    [SerializeField] private float bayZ = 1f, laneZ = -8.3f, farX = 75f, turn = 5f;
    [SerializeField] private float gapMin = 20f, gapMax = 40f;

    private const float Speed = 5f, Reverse = 2.2f;
    private bool[] parked;

    public void Configure(Transform[] coachList, float[] bays, float bayDepth, float lane, float far)
    {
        coaches = coachList;
        bayX = bays;
        bayZ = bayDepth;
        laneZ = lane;
        farX = far;
    }

    private IEnumerator Start()
    {
        if (coaches == null || bayX == null || coaches.Length != bayX.Length || coaches.Length == 0) yield break;
        parked = new bool[coaches.Length];
        for (int i = 0; i < parked.Length; i++)
        {
            parked[i] = coaches[i] != null;
            if (parked[i]) Pose(coaches[i], new Vector3(bayX[i], 0f, bayZ), Vector3.back);
        }
        yield return new WaitForSeconds(Random.Range(5f, 12f));
        while (true)
        {
            int empty = Pick(false), full = Pick(true);
            if (empty >= 0 && (full < 0 || Random.value < 0.5f)) yield return Arrive(empty);
            else if (full >= 0) yield return Depart(full);
            yield return new WaitForSeconds(Random.Range(gapMin, gapMax));
        }
    }

    private int Pick(bool isParked)
    {
        int n = 0, chosen = -1;
        for (int i = 0; i < parked.Length; i++)
            if (coaches[i] != null && parked[i] == isParked && Random.Range(0, ++n) == 0) chosen = i;
        return chosen;
    }

    // Forward out of the bay, round to the left, away down the lane
    private IEnumerator Depart(int i)
    {
        var c = coaches[i];
        parked[i] = false;
        float bx = bayX[i], mid = laneZ + turn;
        yield return Line(c, new Vector3(bx, 0f, bayZ), new Vector3(bx, 0f, mid), false, Speed * 0.5f);
        yield return Arc(c, new Vector3(bx + turn, 0f, mid), new Vector3(-turn, 0f, 0f), new Vector3(0f, 0f, -turn), false, Speed * 0.6f);
        yield return Line(c, new Vector3(bx + turn, 0f, laneZ), new Vector3(farX, 0f, laneZ), false, Speed);
        c.gameObject.SetActive(false);
    }

    // Up the lane, past the bay, then back into it
    private IEnumerator Arrive(int i)
    {
        var c = coaches[i];
        float bx = bayX[i], mid = laneZ + turn;
        c.gameObject.SetActive(true);
        yield return Line(c, new Vector3(farX, 0f, laneZ), new Vector3(bx - turn, 0f, laneZ), false, Speed);
        yield return new WaitForSeconds(1.2f);
        yield return Arc(c, new Vector3(bx - turn, 0f, mid), new Vector3(0f, 0f, -turn), new Vector3(turn, 0f, 0f), true, Reverse);
        yield return Line(c, new Vector3(bx, 0f, mid), new Vector3(bx, 0f, bayZ), true, Reverse);
        Pose(c, new Vector3(bx, 0f, bayZ), Vector3.back);
        parked[i] = true;
    }

    private IEnumerator Line(Transform c, Vector3 from, Vector3 to, bool backwards, float speed)
    {
        float len = Vector3.Distance(from, to);
        var dir = (to - from).normalized;
        for (float d = 0f; d < len; d += speed * Time.deltaTime)
        {
            Pose(c, Vector3.Lerp(from, to, d / len), backwards ? -dir : dir);
            yield return null;
        }
        Pose(c, to, backwards ? -dir : dir);
    }

    // A quarter turn round `centre`, from centre+a to centre+b (the shorter way)
    private IEnumerator Arc(Transform c, Vector3 centre, Vector3 a, Vector3 b, bool backwards, float speed)
    {
        float sweep = Vector3.SignedAngle(a, b, Vector3.up);
        float len = Mathf.Abs(sweep) * Mathf.Deg2Rad * a.magnitude;
        var last = centre + a;
        for (float d = 0f; d < len; d += speed * Time.deltaTime)
        {
            var p = centre + Quaternion.Euler(0f, sweep * d / len, 0f) * a;
            var dir = p - last;
            if (dir.sqrMagnitude > 0.000001f) Pose(c, p, backwards ? -dir : dir);
            last = p;
            yield return null;
        }
    }

    private void Pose(Transform c, Vector3 local, Vector3 forward)
    {
        forward.y = 0f;
        c.position = transform.TransformPoint(local);
        if (forward.sqrMagnitude > 0.0001f) c.rotation = transform.rotation * Quaternion.LookRotation(forward.normalized);
    }
}
