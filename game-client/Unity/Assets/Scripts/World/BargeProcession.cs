using System.Collections.Generic;
using UnityEngine;

// ขบวนเรือพระราชพิธี on the Chao Phraya: the royal barges and their escorts glide down the river in
// their columns, every oarsman pulling in time — the oars sweep forward, dip and drive back together,
// the crews lean with the stroke. Day and night; after dark the escort column on the city side stands
// down so the dinner cruise has its lane (the royal barges and the far escorts sail on). A barge
// that reaches the end of the river comes round again at the start, so the procession never ends.
// Built by RoyalBarges (editor). The ferry waits while barges pass its crossing (Near).
public class BargeProcession : MonoBehaviour
{
    [SerializeField] private Transform[] barges;
    [SerializeField] private float[] lengths;
    [SerializeField] private float halfRun = 128f, speed = 1.6f, stroke = 2.6f;

    private class Barge
    {
        public Transform root, oarsL, oarsR, crew;
        public Vector3 crewRest;
        public float length, bob;
    }

    private static readonly List<BargeProcession> all = new List<BargeProcession>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => all.Clear();

    private readonly List<Barge> list = new List<Barge>();
    private bool shown = true, nightMode;
    private float nextCheck;

    public void Configure(Transform[] bargeRoots, float[] bargeLengths)
    {
        barges = bargeRoots;
        lengths = bargeLengths;
    }

    // The royal barge the live screens follow
    public static Transform Lead
    {
        get
        {
            foreach (var p in all)
                if (p.shown)
                    foreach (var b in p.list)
                        if (b.root != null && b.root.name.Contains("Suphannahong")) return b.root;
            return null;
        }
    }

    // Is any barge within `range` metres (along the river) of this point? The ferry waits for them.
    public static bool Near(Vector3 world, float range)
    {
        foreach (var p in all)
        {
            if (!p.shown) continue;
            float x = p.transform.InverseTransformPoint(world).x;
            foreach (var b in p.list)
                if (b.root.gameObject.activeSelf && Mathf.Abs(b.root.localPosition.x - x) < range + b.length * 0.5f) return true;
        }
        return false;
    }

    private void Awake()
    {
        if (barges == null) return;
        for (int i = 0; i < barges.Length; i++)
        {
            var root = barges[i];
            if (root == null) continue;
            var b = new Barge
            {
                root = root,
                oarsL = root.Find("OarsL"),
                oarsR = root.Find("OarsR"),
                crew = root.Find("Crew"),
                length = lengths != null && i < lengths.Length ? lengths[i] : 20f,
                bob = i * 1.7f,
            };
            if (b.crew != null) b.crewRest = b.crew.localPosition;
            list.Add(b);
        }
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 1f;
            bool night = DayNightCycle.IsNight;
            if (night != nightMode)
            {
                nightMode = night;
                foreach (var b in list) b.root.gameObject.SetActive(!night || b.root.localPosition.z >= 0f);
            }
        }

        float dt = Time.deltaTime, t = Time.time;
        float phase = t / stroke * Mathf.PI * 2f;
        // the stroke: the oars swing forward in the air, dip, and pull back through the water
        float sweep = Mathf.Cos(phase) * 0.32f;
        float dip = 4f + Mathf.Max(0f, Mathf.Sin(phase)) * 12f;
        // the boats surge a little on each pull
        float surge = 1f + Mathf.Max(0f, Mathf.Sin(phase)) * 0.35f;
        float loop = halfRun * 2f;
        foreach (var b in list)
        {
            if (!b.root.gameObject.activeSelf) continue;
            var p = b.root.localPosition;
            p.x += speed * surge * dt;
            if (p.x > halfRun) p.x -= loop;
            p.y = Mathf.Sin(t * 0.9f + b.bob) * 0.03f;
            b.root.localPosition = p;
            b.root.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f + b.bob) * 0.4f, 90f, Mathf.Sin(t * 0.8f + b.bob) * 0.6f);
            if (b.oarsL != null)
            {
                b.oarsL.localPosition = new Vector3(0f, 0f, sweep);
                b.oarsL.localRotation = Quaternion.Euler(0f, 0f, dip);
            }
            if (b.oarsR != null)
            {
                b.oarsR.localPosition = new Vector3(0f, 0f, sweep);
                b.oarsR.localRotation = Quaternion.Euler(0f, 0f, -dip);
            }
            if (b.crew != null) b.crew.localPosition = b.crewRest + new Vector3(0f, 0f, sweep * 0.25f);
        }
    }
}
