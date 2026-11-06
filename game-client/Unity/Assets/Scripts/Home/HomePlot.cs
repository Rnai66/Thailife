using System.Collections.Generic;
using UnityEngine;

// One house a player can live in (VillageBuilder / Homes): its style id (home.lua), the height of its
// room's floor, the porch where you arrive and its sleeping spot. The one you live in (HomeClient)
// becomes your walk-in room (HomeInterior) with your bed; the home shrine, the dog and the carport
// move over to it. The others are the neighbours' houses: you can't sleep in them.
// Every house's room is the same 5.6 x 4.6 m, door on its local -Z side, so furniture fits any of them.
public class HomePlot : MonoBehaviour
{
    [SerializeField] private string style = "stilt";
    [SerializeField] private float floorY = 2.6f;
    [SerializeField] private Transform porch;
    [SerializeField] private GameObject sleep;
    [SerializeField] private Transform carport;

    private static readonly List<HomePlot> all = new List<HomePlot>();

    // Where the shrine group, its dog and the car sat when the scene was built (the stilt house)
    private static Transform shrine, car;
    private static Vector3 shrineHome, carHome;
    private static Quaternion shrineRot, carRot;
    private static UnityEngine.SceneManagement.Scene rememberedScene; // the map they were found in

    public string Style => style;
    public float FloorY => floorY;
    public bool IsMine => style == HomeClient.MyStyle;

    public void Configure(string styleId, float floorHeight, Transform porchPoint, GameObject sleepZone, Transform carportPoint = null)
    {
        style = styleId;
        floorY = floorHeight;
        porch = porchPoint;
        sleep = sleepZone;
        carport = carportPoint;
    }

    public static HomePlot Mine()
    {
        foreach (var p in all) if (p != null && p.IsMine) return p;
        return null;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Start()
    {
        Apply();
        if (IsMine && Locations.PendingSpawnId == HomeClient.SpawnId)
        {
            Locations.PendingSpawnId = null;
            PutPlayerHere();
        }
    }

    public static void ApplyAll()
    {
        foreach (var p in all) if (p != null) p.Apply();
        HomeInterior.Setup();
    }

    private void Apply()
    {
        bool mine = IsMine;
        if (sleep != null)
        {
            sleep.SetActive(mine);
            sleep.name = mine ? "SleepZone" : "SleepZone_" + style; // the tutorial's arrow finds your own mat
        }
        if (mine) MoveBelongings();
    }

    // The home shrine (amulets, kuman, the dog and its kennel) and the car follow you to this house
    private void MoveBelongings()
    {
        if (rememberedScene != gameObject.scene)
        {
            rememberedScene = gameObject.scene;
            shrine = car = null;
            var s = GameObject.Find("HomeShrine_หิ้งพระ");
            if (s != null) { shrine = s.transform; shrineHome = shrine.position; shrineRot = shrine.rotation; }
            var c = GameObject.Find("Carport_ที่จอดรถใต้ถุน");
            if (c != null) { car = c.transform; carHome = car.position; carRot = car.rotation; }
        }
        bool stilt = style == "stilt";
        if (shrine != null)
        {
            // built for the stilt house (floor 2.6 m up): same place over this house's front door
            if (stilt) shrine.SetPositionAndRotation(shrineHome, shrineRot);
            else shrine.SetPositionAndRotation(transform.TransformPoint(new Vector3(0f, floorY - 2.6f, 0f)), transform.rotation);
            var dog = shrine.Find("Guardians/dog");
            if (dog != null) dog.localPosition = stilt ? Vector3.zero : new Vector3(-5.6f, 2.6f - floorY, 0.5f); // kennel in the side yard
        }
        if (car != null)
        {
            if (stilt || carport == null) car.SetPositionAndRotation(carHome, carRot);
            else car.SetPositionAndRotation(carport.position, carport.rotation);
        }
    }

    public void PutPlayerHere()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        var at = porch != null ? porch : transform;
        player.Teleport(at.position + Vector3.up, Quaternion.Euler(0f, at.eulerAngles.y, 0f));
    }
}
