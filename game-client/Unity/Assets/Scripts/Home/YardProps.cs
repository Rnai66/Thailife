using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The village home yard's own things that you may move round in โหมดก่อสร้าง (BuildUI): the two banana
// trees you kick, the punching bag, the two dragon jars, the rice barn and the chicken coop. Each one
// is found by name round the village's stilt house when a map loads, remembered where the map put it,
// and shifted by what you saved (build.lua build_prop: dx, dz from there, turned to yaw). Only you see
// your yard moved. Made next to BuildClient.
public class YardProps : MonoBehaviour
{
    public class Item
    {
        public string name, label;
        public Transform t;
        public Vector3 home;    // where the map put it
        public float homeYaw;
        public float size;      // about how wide, for picking it with a finger
    }

    private static YardProps instance;
    private readonly List<Item> items = new List<Item>();
    private Scene foundIn;
    private int appliedVersion = -1;
    private float nextCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; Held = null; }

    // The one being dragged right now (BuildUI): left where the finger puts it
    public static string Held { get; set; }

    public static IReadOnlyList<Item> All => instance != null ? instance.items : (IReadOnlyList<Item>)new Item[0];

    public static Item Get(string name)
    {
        if (instance == null || string.IsNullOrEmpty(name)) return null;
        foreach (var i in instance.items) if (i.name == name && i.t != null) return i;
        return null;
    }

    public static void Reapply()
    {
        if (instance != null) instance.appliedVersion = -1;
    }

    private void Awake() => instance = this;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.5f;
        var scene = SceneManager.GetActiveScene();
        if (scene != foundIn || items.Exists(i => i.t == null))
        {
            foundIn = scene;
            Find();
            appliedVersion = -1;
        }
        var client = BuildClient.Instance;
        if (client?.Current == null || items.Count == 0 || appliedVersion == client.Version) return;
        appliedVersion = client.Version;
        foreach (var i in items)
        {
            if (i.name == Held) continue;
            var p = client.GetProp(i.name);
            Put(i, p != null ? p.dx : 0f, p != null ? p.dz : 0f, p != null ? p.yaw : i.homeYaw);
        }
    }

    // Stand one at (dx, dz) from where the map put it, turned to yaw (degrees, world)
    public static void Put(Item i, float dx, float dz, float yaw)
    {
        if (i?.t == null) return;
        i.t.SetPositionAndRotation(i.home + new Vector3(dx, 0f, dz), Quaternion.Euler(0f, yaw, 0f));
    }

    private void Find()
    {
        items.Clear();
        var house = GameObject.Find(HomeInterior.HouseName);
        if (house == null) return;
        Vector3 at = house.transform.position;
        bool Near(Transform t) => t != null && (t.position - at).sqrMagnitude < 30f * 30f;

        var bananas = new List<Transform>();
        Transform bag = null;
        foreach (var target in FindObjectsByType<TrainingTarget>(FindObjectsSortMode.None))
        {
            if (!Near(target.transform)) continue;
            if (target.name == "Training_BananaTree") bananas.Add(target.transform);
            else if (target.name == "Training_PunchingBag") bag = target.transform;
        }
        bananas.Sort(Order);
        for (int n = 0; n < bananas.Count && n < 2; n++) Add("banana" + (n + 1), "ต้นกล้วย", bananas[n], 1.2f);
        Add("bag", "กระสอบทราย", bag, 1f);

        var jars = new List<Transform>();
        foreach (var t in house.GetComponentsInChildren<Transform>())
            if (t.name == "WaterJar_โอ่งมังกร") jars.Add(t);
        jars.Sort(Order);
        for (int n = 0; n < jars.Count && n < 2; n++) Add("jar" + (n + 1), "โอ่งมังกร", jars[n], 0.9f);

        Transform granary = null, coop = null;
        foreach (var root in house.scene.GetRootGameObjects())
        {
            var g = root.name == "Granary_ยุ้งข้าว" ? root.transform : root.transform.Find("Granary_ยุ้งข้าว");
            var c = root.name == "ChickenCoop_เล้าไก่" ? root.transform : root.transform.Find("ChickenCoop_เล้าไก่");
            if (Near(g) && granary == null) granary = g;
            if (Near(c) && coop == null) coop = c;
        }
        Add("granary", "ยุ้งข้าว", granary, 3.2f);
        Add("coop", "เล้าไก่", coop, 3.2f);
    }

    private static int Order(Transform a, Transform b)
    {
        int c = a.position.x.CompareTo(b.position.x);
        return c != 0 ? c : a.position.z.CompareTo(b.position.z);
    }

    private void Add(string name, string label, Transform t, float size)
    {
        if (t == null) return;
        items.Add(new Item { name = name, label = label, t = t, home = t.position, homeYaw = t.eulerAngles.y, size = size });
    }
}
