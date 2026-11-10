using System.Collections.Generic;
using UnityEngine;

// The roads of a map as the traffic sees them (TrafficCar): every route some vehicle drives (its path in
// world space, with the stop lines and lights it obeys), and the places where one route runs along
// another in the same lane and direction — a "transfer", where a car may change over and carry on
// along the other one. Joining routes that way (the avenue's lanes, the side-street loop, the ring
// road and the slip roads between them) makes a network: cars no longer go round one loop for ever.
// A slip road (an open route) always hands its cars on at its far end. Built once per map, the first
// time a car asks, from the cars that are there.
// Junctions are found too: where one road meets another across (a side street at the avenue, a loop's
// end by the ring road) a car can turn into it along a curve, left or right; and where two roads run
// side by side the other way (the two carriageways of the ring) there are U-turn gaps (จุดกลับรถ)
// every 90 m or so. So every road joins every other and nothing goes round one loop for ever.
public static class TrafficNet
{
    public class Route
    {
        public Vector3[] world;     // points in world space
        public bool closed;
        public TrafficLight lights;
        public float[] stops;
        public int[] approaches;
        public float length;
        public readonly List<Transfer> transfers = new List<Transfer>();
    }

    public struct Transfer
    {
        public float from;          // metres along this route
        public int to;              // the other route
        public float at;            // metres along that one
        public bool end;            // the end of an open route: always taken
        public Kind kind;
        public Vector3[] curve;     // world points to drive between the two (a turn, a U-turn); null: same lane
    }

    public enum Kind { Lane, Turn, UTurn }

    // How often a car takes each kind when it passes one (a car on a trip takes only its plan's)
    public static float Chance(Kind k) => k == Kind.Lane ? 0.4f : k == Kind.Turn ? 0.35f : 0.1f;

    private const float Join = 0.6f;       // lanes this close (m) and...
    private const float SameWay = 0.97f;   // ...heading this alike are the same lane
    private const float MinRun = 3f;       // shared for at least this far to change over

    private static readonly List<Route> routes = new List<Route>();
    private static UnityEngine.SceneManagement.Scene builtFor;
    private static bool built;

    public static Route Get(int i) => i >= 0 && i < routes.Count ? routes[i] : null;

    // The route a car is on (registered when the map's network is built), or -1
    public static int Register(Vector3[] world, bool closed, TrafficLight lights, float[] stops, int[] approaches)
    {
        for (int i = 0; i < routes.Count; i++)
        {
            var r = routes[i];
            if (r.closed == closed && r.world.Length == world.Length && (r.world[0] - world[0]).sqrMagnitude < 0.01f
                && (r.world[world.Length - 1] - world[world.Length - 1]).sqrMagnitude < 0.01f)
            {
                if (r.stops == null && stops != null) { r.stops = stops; r.approaches = approaches; r.lights = lights; }
                return i;
            }
        }
        var route = new Route { world = world, closed = closed, lights = lights, stops = stops, approaches = approaches };
        route.length = Length(world, closed);
        routes.Add(route);
        built = false;
        return routes.Count - 1;
    }

    // A new map: forget the last one's roads
    public static void Reset(UnityEngine.SceneManagement.Scene scene)
    {
        if (builtFor == scene) return;
        builtFor = scene;
        routes.Clear();
        built = false;
    }

    public static void EnsureBuilt()
    {
        if (built) return;
        built = true;
        foreach (var r in routes) r.transfers.Clear();
        for (int a = 0; a < routes.Count; a++)
            for (int b = 0; b < routes.Count; b++)
                if (a != b) { FindTransfers(a, b); FindJunctions(a, b); }
        foreach (var r in routes) r.transfers.Sort((x, y) => x.from.CompareTo(y.from));
    }

    private static float Length(Vector3[] p, bool closed)
    {
        float l = 0f;
        for (int i = 1; i < p.Length; i++) l += Vector3.Distance(p[i - 1], p[i]);
        if (closed) l += Vector3.Distance(p[p.Length - 1], p[0]);
        return l;
    }

    public static Vector3 PointAt(Route r, float d, out Vector3 dir)
    {
        var p = r.world;
        int n = p.Length + (r.closed ? 1 : 0);
        d = r.closed ? Mathf.Repeat(d, r.length) : Mathf.Clamp(d, 0f, r.length);
        float run = 0f;
        for (int i = 1; i < n; i++)
        {
            Vector3 a = p[i - 1], b = p[i % p.Length];
            float seg = Vector3.Distance(a, b);
            if (run + seg >= d || i == n - 1)
            {
                dir = seg > 0f ? (b - a) / seg : Vector3.forward;
                return Vector3.Lerp(a, b, seg > 0f ? Mathf.Clamp01((d - run) / seg) : 0f);
            }
            run += seg;
        }
        dir = Vector3.forward;
        return p[0];
    }

    // Nearest place on a route to p: distance off it, metres along it and its heading there
    private static float Nearest(Route r, Vector3 p, out float along, out Vector3 dir)
    {
        var pts = r.world;
        int n = pts.Length + (r.closed ? 1 : 0);
        float best = float.MaxValue, run = 0f;
        along = 0f;
        dir = Vector3.forward;
        for (int i = 1; i < n; i++)
        {
            Vector3 a = pts[i - 1], b = pts[i % pts.Length];
            float seg = Vector3.Distance(a, b);
            float t = seg > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, b - a) / (seg * seg)) : 0f;
            float d = Vector3.Distance(Vector3.Lerp(a, b, t), p);
            if (d < best) { best = d; along = run + t * seg; dir = seg > 0f ? (b - a) / seg : dir; }
            run += seg;
        }
        return best;
    }

    // One change-over on a planned trip: on route `route`, at `from` metres, onto `to` at `at`
    public struct Step
    {
        public int route, to;
        public float from, at;
        public Vector3[] curve;
        public Kind kind;
    }

    // Roads only for the map they were registered in (a map without traffic keeps the last town's list)
    public static int Count => builtFor == UnityEngine.SceneManagement.SceneManager.GetActiveScene() ? routes.Count : 0;

    // The speed limit on a road (km/h, SpeedSigns): the long loop round town is the ring road, the
    // middle ones avenues, short loops and slip roads side streets
    public static int LimitOf(Route r) => !r.closed || r.length < 250f ? 40 : r.length > 600f ? 80 : 60;

    public static float DistanceTo(Route r, Vector3 p) => Nearest(r, p, out _, out _);

    // The limit where p is (the nearest road within a lane or two), 0 off the roads
    public static int LimitAt(Vector3 p)
    {
        float best = 6f;
        int limit = 0;
        if (Count == 0) return 0;
        foreach (var r in routes)
        {
            float d = Nearest(r, p, out _, out _);
            if (d < best) { best = d; limit = LimitOf(r); }
        }
        return limit;
    }

    // The line a planned trip drives (for the ride map): along each road, round each turn, to the stop
    public static List<Vector3> Line(int from, float at, List<Step> steps, int goal, float goalAt, float every = 2f)
    {
        var line = new List<Vector3>();
        int route = from;
        float d = at;
        void Run(Route r, float a, float b)
        {
            float len = r.closed ? Mathf.Repeat(b - a, r.length) : Mathf.Max(0f, b - a);
            for (float x = 0f; x < len; x += every) line.Add(PointAt(r, a + x, out _));
            line.Add(PointAt(r, a + len, out _));
        }
        foreach (var st in steps)
        {
            var r = Get(route);
            if (r != null) Run(r, d, st.from);
            if (st.curve != null) line.AddRange(st.curve);
            route = st.to;
            d = st.at;
        }
        var g = Get(goal);
        if (g != null) Run(g, d, goalAt);
        return line;
    }

    public static float LineLength(List<Vector3> line)
    {
        float l = 0f;
        for (int i = 1; i < line.Count; i++) l += Vector3.Distance(line[i - 1], line[i]);
        return l;
    }

    // The area the roads cover (for the ride map)
    public static Bounds Extent()
    {
        var b = new Bounds();
        bool any = false;
        foreach (var r in routes)
            foreach (var p in r.world)
            {
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        return b;
    }

    // The place on the roads nearest p (where a passenger can be set down)
    public static bool NearestRoad(Vector3 p, out Vector3 road)
    {
        road = p;
        float best = float.MaxValue;
        foreach (var r in routes)
        {
            float off = Nearest(r, p, out float along, out _);
            if (off < best) { best = off; road = PointAt(r, along, out _); }
        }
        return best < float.MaxValue;
    }

    // How far a car on r at `a` metres drives to reach `b` metres (open routes can't go back)
    private static float Ahead(Route r, float a, float b, float least)
    {
        if (r.closed)
        {
            float d = Mathf.Repeat(b - a, r.length);
            return d < least ? d + r.length : d;
        }
        return b - a >= least ? b - a : float.MaxValue;
    }

    // A trip from route `from` (at metres along it) to the place on the roads nearest dest: the
    // change-overs to make in order (shortest drive, Dijkstra over the transfers), the route it ends on
    // and where on it to pull up. Null when the roads don't lead there.
    public static List<Step> Plan(int from, float at, Vector3 dest, out int goal, out float goalAt)
    {
        EnsureBuilt();
        goal = -1;
        goalAt = 0f;
        if (Get(from) == null) return null;
        int n = routes.Count;
        var off = new float[n];
        var along = new float[n];
        float bestOff = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            off[i] = Nearest(routes[i], dest, out along[i], out _);
            bestOff = Mathf.Min(bestOff, off[i]);
        }

        // nodes: 0 = where the car is now, then every place a change-over lands it
        var nodeRoute = new List<int> { from };
        var nodeAt = new List<float> { at };
        var landing = new Dictionary<(int, int), int>(); // (route, transfer index) -> node
        for (int r = 0; r < n; r++)
            for (int k = 0; k < routes[r].transfers.Count; k++)
            {
                var t = routes[r].transfers[k];
                landing[(r, k)] = nodeRoute.Count;
                nodeRoute.Add(t.to);
                nodeAt.Add(t.at);
            }
        int count = nodeRoute.Count;
        var cost = new float[count];
        var prev = new int[count];
        var via = new int[count];
        var done = new bool[count];
        for (int i = 0; i < count; i++) { cost[i] = float.MaxValue; prev[i] = -1; }
        cost[0] = 0f;
        float bestTotal = float.MaxValue;
        int bestNode = -1;
        for (int step = 0; step < count; step++)
        {
            int u = -1;
            for (int i = 0; i < count; i++)
                if (!done[i] && cost[i] < float.MaxValue && (u < 0 || cost[i] < cost[u])) u = i;
            if (u < 0 || cost[u] >= bestTotal) break;
            done[u] = true;
            int ru = nodeRoute[u];
            var route = routes[ru];
            // stop here? (a road nearly as close to the place as the closest one)
            if (off[ru] <= bestOff + 4f)
            {
                float d = Ahead(route, nodeAt[u], along[ru], u == 0 ? 2f : 0.2f);
                if (d < float.MaxValue && cost[u] + d < bestTotal) { bestTotal = cost[u] + d; bestNode = u; }
            }
            for (int k = 0; k < route.transfers.Count; k++)
            {
                var t = route.transfers[k];
                float d = Ahead(route, nodeAt[u], t.from, u == 0 ? 1.5f : 0.2f);
                if (d == float.MaxValue) continue;
                int v = landing[(ru, k)];
                float c = cost[u] + d + 8f + (t.curve != null ? t.curve.Length : 0f) // a little extra for each change, so it keeps to fewer
                          + (t.kind == Kind.UTurn ? UTurnCost : 0f);
                if (!done[v] && c < cost[v]) { cost[v] = c; prev[v] = u; via[v] = k; }
            }
        }
        if (bestNode < 0) return null;
        goal = nodeRoute[bestNode];
        goalAt = along[goal];
        var steps = new List<Step>();
        for (int v = bestNode; prev[v] >= 0; v = prev[v])
        {
            int ru = nodeRoute[prev[v]];
            var t = routes[ru].transfers[via[v]];
            steps.Insert(0, new Step { route = ru, from = t.from, to = t.to, at = t.at, curve = t.curve, kind = t.kind });
        }
        return steps;
    }

    private const float TurnReach = 7f;    // a road this close across (m) is a junction
    private const float TurnIn = 6f;       // the turn starts this far before it and ends this far into the other
    private const float Across = 0.5f;     // |cos| under this: across (roughly at right angles)
    private const float UTurnRoad = 300f;  // both carriageways at least this long (m) to have U-turn gaps
    private const float UTurnCost = 60f;   // planning cost of a U-turn on top of its length, so a car turns round only when it must

    private static float Ahead2(Route r, float d) => r.closed ? Mathf.Repeat(d, r.length) : d;

    // Turns and U-turns from closed route a onto closed route b (slip roads have their own transfers)
    private static void FindJunctions(int a, int b)
    {
        Route ra = routes[a], rb = routes[b];
        if (!ra.closed || !rb.closed) return;
        // turns: points of a near b across it, then points of b near a across (a loop's end cap by a road)
        float bestOff = float.MaxValue, bestD = 0f, bestAlong = 0f;
        bool inTurn = false;
        float uStart = -1f;
        bool longPair = ra.length > UTurnRoad && rb.length > UTurnRoad;
        for (float d = 0f; d <= ra.length; d += 1f)
        {
            var p = PointAt(ra, d, out var da);
            float off = Nearest(rb, p, out float along, out var db);
            float c = Vector3.Dot(Flat(da), Flat(db));
            bool turn = off < TurnReach && Mathf.Abs(c) < Across;
            if (turn && off < bestOff) { bestOff = off; bestD = d; bestAlong = along; }
            if (turn) inTurn = true;
            if (inTurn && (!turn || d + 1f > ra.length))
            {
                AddTurn(a, b, bestD - TurnIn, bestAlong + TurnIn);
                inTurn = false;
                bestOff = float.MaxValue;
            }
            // a U-turn only across a long road's median at street level: never on a bridge, a ramp or a soi
            bool uturn = longPair && off > 2.5f && off < 9f && c < -0.95f && p.y < 0.5f && PointAt(rb, along, out _).y < 0.5f;
            if (uturn && uStart < 0f) uStart = d;
            if (uStart >= 0f && (!uturn || d + 1f > ra.length))
            {
                float run = d - uStart;
                if (run > 30f)
                {
                    int n = Mathf.Max(1, Mathf.FloorToInt((run - 20f) / 90f));
                    for (int m = 0; m < n; m++)
                    {
                        float at = uStart + 10f + (run - 20f) * (m + 0.5f) / n;
                        Nearest(rb, PointAt(ra, at + 5f, out _), out float to, out _);
                        AddLink(a, b, at, to, Kind.UTurn, 5f);
                    }
                }
                uStart = -1f;
            }
        }
        inTurn = false;
        bestOff = float.MaxValue;
        for (float d = 0f; d <= rb.length; d += 1f)
        {
            var q = PointAt(rb, d, out var db);
            float off = Nearest(ra, q, out float along, out var da);
            bool turn = off < TurnReach && Mathf.Abs(Vector3.Dot(Flat(da), Flat(db))) < Across;
            if (turn && off < bestOff) { bestOff = off; bestD = d; bestAlong = along; }
            if (turn) inTurn = true;
            if (inTurn && (!turn || d + 1f > rb.length))
            {
                AddTurn(a, b, bestAlong - TurnIn, bestD + TurnIn);
                inTurn = false;
                bestOff = float.MaxValue;
            }
        }
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }

    private static void AddTurn(int a, int b, float from, float at)
    {
        Route ra = routes[a], rb = routes[b];
        from = Ahead2(ra, from);
        at = Ahead2(rb, at);
        var p0 = PointAt(ra, from, out var da);
        var p3 = PointAt(rb, at, out var db);
        if (Vector3.Distance(p0, p3) > 20f || Vector3.Dot(Flat(da), Flat(db)) < -0.7f) return;
        foreach (var t in ra.transfers) // one way into b round here is enough
        {
            float gap = Mathf.Abs(t.from - from);
            if (t.to == b && Mathf.Min(gap, ra.length - gap) < 12f) return;
        }
        AddLink(a, b, from, at, Kind.Turn, Vector3.Distance(p0, p3) * 0.45f);
    }

    // A change-over along a curve (cubic: leaving along a's heading, arriving along b's)
    private static void AddLink(int a, int b, float from, float at, Kind kind, float handle)
    {
        Route ra = routes[a], rb = routes[b];
        var p0 = PointAt(ra, from, out var da);
        var p3 = PointAt(rb, at, out var db);
        Vector3 p1 = p0 + da * handle, p2 = p3 - db * handle;
        int n = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(p0, p3) * 1.5f + handle), 8, 30);
        var curve = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, u = 1f - t;
            curve[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }
        ra.transfers.Add(new Transfer { from = from, to = b, at = at, kind = kind, curve = curve });
    }

    // Where route a runs in the same lane as route b: one change-over in the middle of each shared
    // stretch (at the very end for an open route that finishes on b)
    private static void FindTransfers(int a, int b)
    {
        Route ra = routes[a], rb = routes[b];
        float runStart = -1f;
        float step = 1f;
        for (float d = 0f; d <= ra.length + 0.01f; d += step)
        {
            var p = PointAt(ra, d, out var dirA);
            float off = Nearest(rb, p, out float along, out var dirB);
            bool same = off < Join && Vector3.Dot(dirA, dirB) > SameWay;
            bool last = d + step > ra.length + 0.01f;
            if (same && runStart < 0f) runStart = d;
            if (runStart >= 0f && (!same || last))
            {
                float runEnd = same ? d : d - step;
                if (runEnd - runStart >= MinRun)
                {
                    bool end = !ra.closed && last && same;
                    float from = end ? runEnd - 0.5f : (runStart + runEnd) * 0.5f;
                    Nearest(rb, PointAt(ra, from, out _), out float to, out _);
                    ra.transfers.Add(new Transfer { from = from, to = b, at = to, end = end });
                }
                runStart = -1f;
            }
        }
    }
}
