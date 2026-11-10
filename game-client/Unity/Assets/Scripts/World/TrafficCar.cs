using System.Collections.Generic;
using UnityEngine;

// A vehicle in city traffic (รถเมล์ แท็กซี่ ตุ๊กตุ๊ก รถหรู ...): drives along a path of points in its
// parent's space, keeps its lane, stops at red lights (TrafficLight) on its stop lines, waits at bus
// stops, and brakes for whatever is in front — the car ahead, the player, the player's car. At the
// end of the path it carries on from the start (the ends are out past the edge of town).
// Where its route runs in the same lane as another one (TrafficNet) it may change over and carry on
// along that one — so the traffic spreads all over town instead of going round one loop; a slip road
// always hands it on at its end. Buses keep to their own route (their stops).
[RequireComponent(typeof(Rigidbody))]
public class TrafficCar : MonoBehaviour
{
    private const float Accel = 3.5f, Brake = 6f;

    [SerializeField] private Vector3[] path;            // parent-space points, in driving order
    [SerializeField] private bool loop;                 // drive from the last point back to the first (else jump)
    [SerializeField] private float topSpeed = 10f;
    [SerializeField] private float halfLength = 2.2f;
    [SerializeField] private float startAt;             // metres along the path at the start
    [SerializeField] private TrafficLight lights;
    [SerializeField] private float[] stopAt;            // stop lines: metres along the path...
    [SerializeField] private int[] stopFor;             // ...and the light approach each obeys
    [SerializeField] private float[] dwellAt;           // bus stops: metres along the path

    private float holdAt = -1f;                         // metres along the path to pull up at (TukTukHail), -1 none
    private int netRoute = -1;

    // Taking a passenger somewhere (GoTo): the change-overs still to make, and where to pull up on the last road
    private readonly System.Collections.Generic.List<TrafficNet.Step> plan = new System.Collections.Generic.List<TrafficNet.Step>();
    private bool navigating;
    // Driving a turn's curve between two roads: the road and where on it it comes out (-1 none)
    private int curveTo = -1;
    private float curveAt;
    private int goalRoute = -1;
    private float goalAt;
    private Vector3 goalWorld;
    // Wandering (no passenger): always on the way to somewhere random in town — every road and soi
    // gets driven, not just the loop it was put on. Buses (with stops) keep to their own route.
    private bool wandering;
    private float wanderRetry;
    // On a trip: the least it has had left to go, and when it last got closer (stuck going nowhere: plan again)
    private float bestLeft = float.MaxValue, progressAt;

    // Gridlock: cars waiting on each other in a ring (two loops crossing at a junction, cars spawned
    // on top of each other). One of them — the lowest id in the ring — eases through for a moment.
    private TrafficCar blocker, ghost;
    private float ghostUntil, blockedSince = -1f;
    // Squeezing out of a jam: for a moment it drives through cars that are standing still
    private float squeezeUntil;
    private static int made;
    private readonly int order = ++made;

    private float[] dist;
    private float length, s, speed, dwell, lastDwell = -1f;
    private Transform[] wheels;
    private float wheelRadius = 0.35f;

    public void Configure(Vector3[] points, bool closed, float maxSpeed, float halfLen, float start, TrafficLight light,
        float[] stops, int[] approaches, float[] busStops = null)
    {
        path = points;
        loop = closed;
        topSpeed = maxSpeed;
        halfLength = halfLen;
        startAt = start;
        lights = light;
        stopAt = stops;
        stopFor = approaches;
        dwellAt = busStops;
    }

    public bool IsStopped => speed < 0.05f && dwell <= 0f;

    // Pull up beside a point (someone hailing): at the nearest place on the route a little way ahead,
    // or as soon as it can if that place is behind or far off
    public void StopNear(Vector3 world)
    {
        if (dist == null) return;
        var local = transform.parent != null ? transform.parent.InverseTransformPoint(world) : world;
        float d = DistanceTo(path, local);
        float ahead = Mathf.Repeat(d - s, length);
        float need = speed * speed / (2f * Brake) + 0.5f;
        holdAt = ahead > need && ahead < 45f ? d : Mathf.Repeat(s + need, length);
    }

    // Stop as soon as is comfortable (a passenger getting off)
    public void StopSoon()
    {
        if (dist == null) return;
        holdAt = Mathf.Repeat(s + speed * speed / (2f * Brake) + 0.5f, length);
    }

    public void Release() => holdAt = -1f;

    // Drive to the place on the roads nearest `world` (a passenger's destination) and pull up there.
    // False when the roads don't lead there from here.
    public bool GoTo(Vector3 world)
    {
        int from = curveTo >= 0 ? curveTo : netRoute; // mid-turn: from where the turn comes out
        if (dist == null || from < 0) return false;
        var steps = TrafficNet.Plan(from, curveTo >= 0 ? curveAt : s, world, out goalRoute, out goalAt);
        if (steps == null) return false;
        plan.Clear();
        plan.AddRange(steps);
        navigating = true;
        wandering = false;
        goalWorld = world;
        holdAt = -1f;
        bestLeft = float.MaxValue;
        progressAt = Time.time;
        return true;
    }

    // Off somewhere random on the roads (any road but this one): of a few places tried, one it can
    // get to driving on ahead — not one that means turning round straight away
    private void Wander()
    {
        wandering = false;
        plan.Clear();
        if (dwellAt != null || netRoute < 0 || curveTo >= 0 || Time.time < wanderRetry) return;
        TrafficNet.EnsureBuilt();
        int n = TrafficNet.Count;
        if (n < 2) { wanderRetry = Time.time + 5f; return; }
        List<TrafficNet.Step> best = null;
        int bestGoal = -1, bestTurns = int.MaxValue;
        float bestGoalAt = 0f, total = 0f;
        for (int i = 0; i < n; i++)
        {
            var g = TrafficNet.Get(i);
            if (g != null) total += g.length;
        }
        for (int tries = 0; tries < 6 && bestTurns > 0; tries++)
        {
            // a place anywhere on the roads, each metre as likely as any other: the long main roads get
            // their share instead of every car heading for the little streets in the middle of town
            int r = 0;
            for (float pick = Random.Range(0f, total); r < n - 1; r++)
            {
                var g = TrafficNet.Get(r);
                if ((pick -= g != null ? g.length : 0f) < 0f) break;
            }
            if (r == netRoute) continue;
            var route = TrafficNet.Get(r);
            if (route == null) continue;
            var dest = TrafficNet.PointAt(route, Random.Range(0f, route.length), out _);
            var steps = TrafficNet.Plan(netRoute, s, dest, out int goal, out float at);
            if (steps == null || steps.Count == 0) continue;
            int turns = 0;
            foreach (var st in steps) if (st.kind == TrafficNet.Kind.UTurn) turns++;
            if (turns < bestTurns) { best = steps; bestTurns = turns; bestGoal = goal; bestGoalAt = at; }
        }
        if (best == null) { wanderRetry = Time.time + 2f; return; }
        plan.AddRange(best);
        goalRoute = bestGoal;
        goalAt = bestGoalAt;
        wandering = true;
    }

    // The way it would drive to `world` from here (the ride map's preview), and how far that is
    public bool Preview(Vector3 world, out List<Vector3> line, out float metres)
    {
        line = null;
        metres = 0f;
        int from = curveTo >= 0 ? curveTo : netRoute;
        if (dist == null || from < 0) return false;
        float at = curveTo >= 0 ? curveAt : s;
        var steps = TrafficNet.Plan(from, at, world, out int goal, out float stopAt);
        if (steps == null) return false;
        line = TrafficNet.Line(from, at, steps, goal, stopAt);
        if (curveTo >= 0)
            for (float d = length; d > s; d -= 2f) line.Insert(0, At(d)); // the rest of the turn it is on
        metres = TrafficNet.LineLength(line);
        return true;
    }

    // Where it's driving to now (the rest of its trip), or null
    public List<Vector3> TripLine()
    {
        if (!navigating) return null;
        int from = curveTo >= 0 ? curveTo : netRoute;
        if (from < 0) return null;
        return TrafficNet.Line(from, curveTo >= 0 ? curveAt : s, plan, goalRoute, goalAt);
    }

    public void StopGoing()
    {
        navigating = false;
        wandering = false;
        plan.Clear();
    }

    public bool Navigating => navigating;

    // Pulled up at the destination
    public bool Arrived => navigating && plan.Count == 0 && holdAt >= 0f && IsStopped
        && (Mathf.Repeat(holdAt - s, length) < 1.5f || Mathf.Repeat(holdAt - s, length) > length - 3f);

    // Metres left to drive (roughly: the rest of the plan), for the passenger's display
    public float LeftToGo()
    {
        if (!navigating) return 0f;
        float total = curveTo >= 0 ? length - s : 0f, at = curveTo >= 0 ? curveAt : s;
        int route = curveTo >= 0 ? curveTo : netRoute;
        foreach (var st in plan)
        {
            var r = TrafficNet.Get(route);
            if (r != null) total += r.closed ? Mathf.Repeat(st.from - at, r.length) : Mathf.Max(0f, st.from - at);
            route = st.to;
            at = st.at;
        }
        var g = TrafficNet.Get(route);
        if (g != null) total += g.closed ? Mathf.Repeat(goalAt - at, g.length) : Mathf.Max(0f, goalAt - at);
        return total;
    }

    // Distance along a path to the point nearest p (for placing stop lines by position)
    public static float DistanceTo(Vector3[] points, Vector3 p)
    {
        float best = float.MaxValue, at = 0f, run = 0f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            var a = points[i]; var b = points[i + 1];
            float seg = Vector3.Distance(a, b);
            float t = seg > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, b - a) / (seg * seg)) : 0f;
            float d = Vector3.Distance(Vector3.Lerp(a, b, t), p);
            if (d < best) { best = d; at = run + t * seg; }
            run += seg;
        }
        return at;
    }

    private void Awake()
    {
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        if (path == null || path.Length < 2) { enabled = false; return; }
        // A scene saves a null array as an empty one: no bus stops means not a bus (or it would keep
        // to its own road for ever: no wandering, no taking a passenger anywhere)
        if (dwellAt != null && dwellAt.Length == 0) dwellAt = null;
        if (stopAt != null && stopAt.Length == 0) { stopAt = null; stopFor = null; }
        Measure();
        s = Mathf.Repeat(startAt, length);
        TrafficNet.Reset(gameObject.scene);
        var world = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++) world[i] = transform.parent != null ? transform.parent.TransformPoint(path[i]) : path[i];
        netRoute = TrafficNet.Register(world, loop, lights, stopAt, stopFor);
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name == "Wheel") // a group turned on its side: the tyre and a hub that shows it turning
            {
                list.Add(t);
                if (t.childCount > 0) wheelRadius = t.GetChild(0).lossyScale.x * 0.5f;
            }
        wheels = list.ToArray();
        speed = topSpeed * 0.6f;
        // Body roll, dive and bounce on its springs (this script spins the wheels itself)
        if (GetComponent<CarSway>() == null)
        {
            string kind = name.ToLowerInvariant();
            bool bike = kind.Contains("motor") || kind.Contains("bike") || kind.Contains("cycle");
            gameObject.AddComponent<CarSway>().Configure(bike, spin: false, pivot: kind.Contains("bus") ? 1f : 0.5f);
            if (GetComponent<CarHeadlights>() == null) gameObject.AddComponent<CarHeadlights>().Configure(bike);
        }
        Place();
    }

    private void Measure()
    {
        int n = path.Length + (loop ? 1 : 0);
        dist = new float[n];
        for (int i = 1; i < n; i++) dist[i] = dist[i - 1] + Vector3.Distance(path[(i - 1) % path.Length], path[i % path.Length]);
        length = dist[n - 1];
    }

    // Moving from `before` to `after` metres along: change over to another route on the way?
    private bool TryChange(float before, float after)
    {
        if (netRoute < 0 || dwellAt != null || holdAt >= 0f) return false;
        TrafficNet.EnsureBuilt();
        var r = TrafficNet.Get(netRoute);
        if (r == null) return false;
        foreach (var t in r.transfers)
        {
            bool passed = after <= length ? before < t.from && t.from <= after
                                         : before < t.from || t.from <= after - length;
            if (!passed) continue;
            bool offPlan = false;
            if (navigating || wandering)
            {
                // on a trip: only the change-over that's next on the plan (or the end of a slip road,
                // which must be taken: then find the way again from where it leads)
                bool next = plan.Count > 0 && plan[0].to == t.to && Mathf.Abs(plan[0].from - t.from) <= 0.01f;
                if (!next && !t.end) continue;
                if (next) plan.RemoveAt(0);
                else offPlan = true;
            }
            else if (!(t.end || Random.value < TrafficNet.Chance(t.kind))) continue;
            var to = TrafficNet.Get(t.to);
            if (to == null) continue;
            float over = Mathf.Repeat(after - t.from, length);
            if (t.curve != null)
            {
                // round the corner first, then onto the other road where the curve comes out
                Follow(t.curve, false, null, null, null);
                s = Mathf.Min(over, length - 0.01f);
                curveTo = t.to;
                curveAt = t.at;
                netRoute = -1;
                if (offPlan) plan.Clear();
                return true;
            }
            Join(t.to, t.at + over);
            if (offPlan) Replan();
            return true;
        }
        return false;
    }

    // Lost the plan (had to leave the road it was on): find the way again
    private void Replan()
    {
        if (navigating) { if (!GoTo(goalWorld)) StopGoing(); }
        else Wander();
    }

    // Drive along these world points from now on
    private void Follow(Vector3[] world, bool closed, TrafficLight light, float[] stops, int[] approaches)
    {
        var local = new Vector3[world.Length];
        for (int i = 0; i < local.Length; i++) local[i] = transform.parent != null ? transform.parent.InverseTransformPoint(world[i]) : world[i];
        path = local;
        loop = closed;
        lights = light;
        stopAt = stops;
        stopFor = approaches;
        Measure();
    }

    // Onto route `index` of the network, `at` metres along it
    private void Join(int index, float at)
    {
        var to = TrafficNet.Get(index);
        if (to == null) return;
        Follow(to.world, to.closed, to.lights, to.stops, to.approaches);
        s = to.closed ? Mathf.Repeat(at, length) : Mathf.Min(at, length - 0.01f);
        netRoute = index;
        curveTo = -1;
        if (holdAt >= 0f) holdAt = Mathf.Repeat(s + 1f, length); // was pulling up on the turn: just round it
    }

    private Vector3 At(float d)
    {
        d = curveTo >= 0 || !loop ? Mathf.Clamp(d, 0f, length) : Mathf.Repeat(d, length);
        int i = 1;
        while (i < dist.Length - 1 && dist[i] < d) i++;
        float seg = dist[i] - dist[i - 1];
        float t = seg > 0f ? (d - dist[i - 1]) / seg : 0f;
        var local = Vector3.Lerp(path[(i - 1) % path.Length], path[i % path.Length], t);
        return transform.parent != null ? transform.parent.TransformPoint(local) : local;
    }

    // A moving average along the road: the corners of the route's polyline get rounded off, so the
    // car sweeps round a bend instead of snapping from one straight to the next
    private Vector3 Smoothed(float d)
    {
        float w = Mathf.Clamp(halfLength * 0.6f, 1.2f, 3f);
        return (At(d - 2f * w) + 2f * At(d - w) + 3f * At(d) + 2f * At(d + w) + At(d + 2f * w)) / 9f;
    }

    private void Place()
    {
        var p = Smoothed(s);
        var ahead = Smoothed(s + 1.2f) - Smoothed(s - 1.2f); // nose along the curve (and up a bridge ramp)
        var rot = ahead.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(ahead) : transform.rotation;
        bool jump = (p - transform.position).sqrMagnitude > 25f;
        if (jump) transform.SetPositionAndRotation(p, rot); // wrapped round to the start
        else
        {
            transform.position = p;
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
        }
    }

    // Free road ahead (metres) before something we must not hit
    private float Clearance()
    {
        float look = 4f + speed * 1.2f;
        var origin = transform.position + Vector3.up * 0.9f;
        float best = look;
        TrafficCar nearest = null;
        foreach (var hit in Physics.SphereCastAll(origin, 0.8f, transform.forward, look + halfLength, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            var car = hit.collider.GetComponentInParent<TrafficCar>();
            if (car != null)
            {
                if (car == ghost && Time.time < ghostUntil) continue;
                if (Time.time < squeezeUntil && car.speed < 0.5f) continue;
                // already overlapping (the cast starts inside it): only what is ahead of us counts
                if (hit.distance <= 0f && Vector3.Dot(car.transform.position - transform.position, transform.forward) <= 0f) continue;
            }
            bool blocks = car != null
                       || hit.collider.GetComponentInParent<PlayerController>() != null
                       || hit.collider.GetComponentInParent<DrivableCar>() != null
                       || hit.collider is CharacterController
                       || hit.collider.GetComponent<TrafficBlock>() != null; // a lowered level-crossing gate
            if (!blocks) continue;
            float d = Mathf.Max(0f, hit.distance - halfLength);
            if (d < best) { best = d; nearest = car; }
        }
        blocker = best < 1.6f ? nearest : null;
        return best;
    }

    // Waited long enough on the car in front? Not if it is a queue held up at the front by something
    // that isn't a car (a red light, a stop, a fare, the player, a level-crossing gate): yes after 2 s
    // when it's across our way (at a junction, round a roundabout) rather than going our way, and
    // after 6 s when the queue comes back round on itself (cars at a junction all waiting on each other)
    private bool GiveUpWaiting(float waited)
    {
        if (blocker == null) return false;
        if (waited > 2f && Vector3.Dot(transform.forward, blocker.transform.forward) < 0.8f) return true;
        if (waited < 6f) return false;
        var h = blocker;
        for (int i = 0; i < 24 && h != null; i++)
        {
            if (h.blocker == null) return false;
            h = h.blocker;
        }
        return true;
    }

    // Is the queue we are stuck in a closed ring, with us as its lowest id?
    private bool GridlockedHere()
    {
        int lowest = order;
        var c = blocker;
        for (int i = 0; i < 16 && c != null; i++)
        {
            if (c == this) return lowest == order;
            lowest = Mathf.Min(lowest, c.order);
            c = c.blocker;
        }
        return false;
    }

    private float ToStopLine()
    {
        float best = float.MaxValue;
        if (stopAt == null || lights == null) return best;
        for (int i = 0; i < stopAt.Length; i++)
        {
            float ahead = Mathf.Repeat(stopAt[i] - (s + halfLength), length);
            if (ahead > 40f) continue;
            var lamp = lights.State(stopFor[i]);
            if (lamp == TrafficLight.Lamp.Green) continue;
            // On yellow, go on if too close to stop in comfort
            if (lamp == TrafficLight.Lamp.Yellow && ahead < speed * speed / (2f * Brake) + 0.5f) continue;
            best = Mathf.Min(best, ahead);
        }
        return best;
    }

    // Top speed, eased off for a bend in the next few metres
    // the sharper the bend coming up, the slower (looking further ahead the faster it goes)
    private float Cruise()
    {
        float look = 6f + speed * 0.9f;
        var a = At(s + 1f) - At(s);
        a.y = 0f;
        float bend = 0f;
        foreach (float f in new[] { 0.4f, 0.7f, 1f })
        {
            var b = At(s + look * f + 1f) - At(s + look * f);
            b.y = 0f;
            bend = Mathf.Max(bend, Vector3.Angle(a, b));
        }
        return Mathf.Lerp(topSpeed, Mathf.Min(topSpeed, 4f), Mathf.InverseLerp(12f, 85f, bend));
    }

    private void Update()
    {
        if (dwell > 0f) { dwell -= Time.deltaTime; return; }

        // Bus stops: pull in and wait
        if (dwellAt != null)
            foreach (float d in dwellAt)
            {
                float ahead = Mathf.Repeat(d - s, length);
                if (ahead < 0.3f && lastDwell != d) { dwell = 6f; lastDwell = d; speed = 0f; return; }
                if (ahead > 0.3f && ahead < 30f && lastDwell == d) lastDwell = -1f;
            }

        float room = Mathf.Min(Clearance() - 1.5f, ToStopLine() - 0.3f);
        if (dwellAt != null)
            foreach (float d in dwellAt)
            {
                float ahead = Mathf.Repeat(d - s, length);
                if (ahead < 25f && lastDwell != d) room = Mathf.Min(room, ahead + 0.5f);
            }
        // On a trip, on the last road: pull up at the destination once it's coming up
        if (navigating && plan.Count == 0 && holdAt < 0f && netRoute == goalRoute)
        {
            float toGo = loop ? Mathf.Repeat(goalAt - s, length) : goalAt - s;
            if (toGo >= 0f && toGo < 60f) holdAt = goalAt;
        }
        if (holdAt >= 0f)
        {
            float ahead = Mathf.Repeat(holdAt - s, length);
            if (ahead > length - 3f) ahead = 0f; // just past it: stay put
            room = Mathf.Min(room, ahead);
        }
        float want = room <= 0f ? 0f : Mathf.Min(Cruise(), Mathf.Sqrt(2f * Brake * room));
        // gentle: ease off and pull away smoothly (harder only when something's close in front)
        float decel = room < 3f ? Brake * 2f : Brake;
        float accel = Accel * Mathf.Lerp(1f, 0.45f, speed / Mathf.Max(1f, topSpeed));
        speed = want < speed ? Mathf.Max(want, speed - decel * Time.deltaTime) : Mathf.Min(want, speed + accel * Time.deltaTime);
        if (room <= 0f) speed = 0f;
        if (blocker != null && speed < 0.05f)
        {
            if (blockedSince < 0f) blockedSince = Time.time;
            else if (Time.time - blockedSince > 3f && GridlockedHere())
            {
                ghost = blocker;
                ghostUntil = Time.time + 4f;
                blockedSince = -1f;
            }
            else if (GiveUpWaiting(Time.time - blockedSince))
            {
                ghost = blocker;
                ghostUntil = Time.time + 4f;
                if (Time.time - blockedSince > 6f) squeezeUntil = Time.time + 3f;
                blockedSince = -1f;
            }
        }
        else blockedSince = -1f;
        // no passenger: always off somewhere (a new place each time it gets to the last one)
        if (dwellAt == null && holdAt < 0f && curveTo < 0 && netRoute >= 0)
        {
            if (!navigating && (!wandering || plan.Count == 0)) Wander();
            else if (navigating && plan.Count == 0 && netRoute != goalRoute && Time.time > wanderRetry)
            {
                wanderRetry = Time.time + 2f;
                Replan();
            }
            else if (navigating)
            {
                float left = LeftToGo();
                if (left < bestLeft - 5f) { bestLeft = left; progressAt = Time.time; }
                else if (Time.time - progressAt > 40f) Replan(); // GoTo resets the watch
            }
        }
        float next = s + speed * Time.deltaTime;
        if (curveTo >= 0)
        {
            if (next >= length) Join(curveTo, curveAt + (next - length));
            else s = next;
        }
        else if (!TryChange(s, next)) s = Mathf.Repeat(next, length);
        Place();

        float spin = speed * Time.deltaTime / Mathf.Max(0.1f, wheelRadius) * Mathf.Rad2Deg;
        foreach (var w in wheels) w.Rotate(0f, spin, 0f, Space.Self);
    }
}
