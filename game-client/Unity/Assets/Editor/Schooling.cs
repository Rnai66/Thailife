using UnityEngine;

// Places for the school and skilled jobs (education.lua): the classroom door at the school, job
// spots (a signpost with the job's picture and a trigger with JobDesk) and the motorbike repair shop.
public static class Schooling
{
    // Classroom door: walking in opens the classroom window (SchoolUI)
    public static void ClassroomDoor(Transform parent, Vector3 position, float yaw)
    {
        var door = PrimitiveKit.Group("Classroom_ห้องเรียน", parent, position, yaw);
        var zone = new GameObject("ClassZone");
        zone.transform.SetParent(door.transform, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.2f, 2f, 1.6f);
        zone.AddComponent<SchoolDesk>();
        Places.Sign(door.transform, new Vector3(2.4f, 0f, 0.2f), "ห้องเรียน\nคอมพิวเตอร์ · กีฬา · ช่าง · ตรรกะ\nเรียนเพื่อเพิ่มเลเวล", 0f);
    }

    // A job's workplace: signpost + the trigger where you start and finish shifts
    public static void JobSpot(Transform parent, Vector3 position, float yaw, string jobId, string sign)
    {
        var spot = PrimitiveKit.Group("JobSpot_" + jobId, parent, position, yaw);
        var zone = new GameObject("JobZone");
        zone.transform.SetParent(spot.transform, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 2f, 2f);
        zone.AddComponent<JobDesk>().Configure(jobId);
        Places.Sign(spot.transform, new Vector3(1.8f, 0f, 0.6f), sign, 0f);
    }

    // Motorbike repair shop (ร้านซ่อมมอเตอร์ไซค์): open garage with a tin roof, tyres and a bike on the stand
    public static void RepairShop(Transform parent, Vector3 position, float yaw)
    {
        var shop = PrimitiveKit.Group("RepairShop_ร้านซ่อมรถ", parent, position, yaw);
        var t = shop.transform;
        var concrete = PrimitiveKit.Mat("Concrete", new Color(0.7f, 0.7f, 0.68f));
        var wall = PrimitiveKit.Mat("GarageWall", new Color(0.95f, 0.85f, 0.45f));
        var tin = PrimitiveKit.Mat("RustyTin", new Color(0.62f, 0.4f, 0.28f), 0.3f);
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var rubber = PrimitiveKit.Mat("TyreRubber", new Color(0.1f, 0.1f, 0.11f), 0.35f);
        var red = PrimitiveKit.Mat("BikeRed", new Color(0.85f, 0.15f, 0.15f), 0.6f);

        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.05f, 0f), new Vector3(7f, 0.1f, 5f), concrete, collider: false);
        PrimitiveKit.Box("BackWall", t, new Vector3(0f, 1.6f, 2.4f), new Vector3(7f, 3.2f, 0.2f), wall);
        foreach (float x in new[] { -3.4f, 3.4f })
        {
            PrimitiveKit.Box("SideWall", t, new Vector3(x, 1.6f, 1.2f), new Vector3(0.2f, 3.2f, 2.6f), wall);
            PrimitiveKit.Cylinder("Post", t, new Vector3(x, 1.6f, -2.3f), new Vector3(0.12f, 1.6f, 0.12f), steel);
        }
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 3.35f, 0f), new Vector3(7.6f, 0.08f, 5.6f), tin, new Vector3(-6f, 0f, 0f), collider: false);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 3.0f, -2.45f), new Vector3(4.6f, 0.6f, 0.08f), PrimitiveKit.Mat("SignBlue", new Color(0.15f, 0.35f, 0.75f)), collider: false);
        PrimitiveKit.Label("ร้านซ่อมมอเตอร์ไซค์", t, new Vector3(0f, 3.0f, -2.51f), 0.05f, Color.white);

        // Stack of tyres, a workbench with tools, a motorbike up on its stand
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Cylinder("Tyre", t, new Vector3(-2.6f, 0.15f + i * 0.24f, 1.6f), new Vector3(0.7f, 0.11f, 0.7f), rubber, collider: i == 0);
        PrimitiveKit.Box("Bench", t, new Vector3(1.6f, 0.45f, 1.9f), new Vector3(2.6f, 0.9f, 0.7f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        PrimitiveKit.Box("Toolbox", t, new Vector3(1.0f, 1.05f, 1.9f), new Vector3(0.6f, 0.3f, 0.35f), red, collider: false);
        var bike = PrimitiveKit.Group("Motorbike", t, new Vector3(0.2f, 0f, 0.2f), 90f).transform;
        foreach (float z in new[] { -0.65f, 0.65f })
            PrimitiveKit.Cylinder("Wheel", bike, new Vector3(0f, 0.32f, z), new Vector3(0.62f, 0.05f, 0.62f), rubber, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Body", bike, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.35f, 1.1f), red, collider: false);
        PrimitiveKit.Box("Seat", bike, new Vector3(0f, 0.82f, -0.15f), new Vector3(0.3f, 0.1f, 0.6f), rubber, collider: false);
        PrimitiveKit.Cylinder("Handlebar", bike, new Vector3(0f, 0.95f, 0.5f), new Vector3(0.05f, 0.35f, 0.05f), steel, new Vector3(0f, 0f, 90f), collider: false);
        StreetFurniture.HangingLight(t, new Vector3(0f, 3.0f, 0.6f));

        JobSpot(t, new Vector3(0f, 0f, -3.4f), 0f, "repair", "ช่างซ่อมมอเตอร์ไซค์\nค่าจ้าง 260 ฿ · ต้องมี เลเวล 6 · ช่าง Lv 3");
    }
}
