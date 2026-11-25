using UnityEngine;

// People in the hotel's grounds (HotelGrounds): at the markers under "Spots" — "Staff" (the doorman,
// bartender, lifeguard, car-park attendant), "Sit" (loungers and bar stools, seat height in the
// marker's y), "Swim" (waist-deep in the pool) and "Stand" (guests on their feet). Only while the hotel
// is yours (HotelSite.Mine) and you're near: all the staff, and more of the guests the more staff
// you've hired. Guests move round once a game day.
public class HotelGuests : MonoBehaviour
{
    private Transform spots, people, player;
    private bool shown;
    private int shownDay = -1, shownStaff = -1;
    private float next;

    private void Awake()
    {
        spots = transform.Find("Spots");
        people = new GameObject("People").transform;
        people.SetParent(transform, false);
    }

    private void Update()
    {
        if (Time.time < next || spots == null) return;
        next = Time.time + 3f;
        if (player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        var mine = HotelSite.Instance != null ? HotelSite.Instance.Mine : null;
        bool want = mine != null && (player.position - new Vector3(80f, player.position.y, -36f)).sqrMagnitude < 70f * 70f;
        int staff = mine != null ? mine.staff : 0;
        if (want == shown && (!want || (shownDay == GameClock.Day && shownStaff == staff))) return;
        shown = want;
        shownDay = GameClock.Day;
        shownStaff = staff;
        Fill(want, staff);
    }

    private void Fill(bool on, int staff)
    {
        for (int i = people.childCount - 1; i >= 0; i--) Destroy(people.GetChild(i).gameObject);
        if (!on) return;
        int seed = GameClock.Day * 17;
        float busy = Mathf.Clamp01(0.35f + staff * 0.13f); // how many of the guests' places are taken
        for (int i = 0; i < spots.childCount; i++)
        {
            var s = spots.GetChild(i);
            bool always = s.name == "Staff";
            if (!always && Hash(seed + i * 31) > busy) continue;
            Person(s, seed + i * 7, s.name);
        }
    }

    private static float Hash(int n)
    {
        n = (n << 13) ^ n;
        return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
    }

    private void Person(Transform spot, int seed, string kind)
    {
        var go = new GameObject(kind);
        go.transform.SetParent(people, false);
        var p = people.InverseTransformPoint(spot.position);
        bool seated = kind == "Sit";
        go.transform.localPosition = seated ? new Vector3(p.x, 0f, p.z) : p;
        go.transform.rotation = spot.rotation;
        // A bare CharacterModel hangs its feet 1 unit below its origin: lift the standing ones onto the
        // marker (a swimmer's marker is under the water, so they stand in the pool up to the chest)
        if (!seated) go.transform.position += Vector3.up;
        var model = go.AddComponent<CharacterModel>();
        var look = Look.Random(seed);
        if (kind == "Staff") look.Top = 0;
        model.SetLook(look);
        if (seated)
        {
            model.Seated = true;
            go.AddComponent<CafeSitter>().Configure(p.y, seed);
        }
        else go.AddComponent<CafeBarista>().Configure(seed);
    }
}
