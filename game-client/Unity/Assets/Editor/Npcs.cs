using UnityEngine;

// Where the villagers (ชาวบ้าน NPC) appear: fair crowds, market shoppers, people out on the
// streets in daytime, and the monks' morning alms round.
public static class Npcs
{
    public const string FairTalk = "ไปเที่ยวงานกัน!|สนุกจัง|ซื้อลูกชิ้นปิ้งดีกว่า|ไปสอยดาวกัน|ชิงช้าสวรรค์สวยมาก|ลิเกเริ่มหรือยัง?";
    public const string MarketTalk = "ผักสดๆ จ้า|วันนี้ซื้ออะไรดีนะ|ราคาเท่าไหร่จ๊ะ|ลดหน่อยได้ไหม|อร่อยมาก";
    public const string VillageTalk = "สวัสดีจ้า|กินข้าวหรือยัง?|ไปไหนมา?|วันนี้อากาศดีนะ";
    public const string CityTalk = "รถติดจัง|ไปธนาคารก่อน|ร้านกาแฟเปิดหรือยัง?|สวัสดีครับ";

    public static void Crowd(Transform parent, Vector3 local, Vector2 size, int count, string festival, int open, int close, string talk, float yaw = 0f)
    {
        var go = PrimitiveKit.Group("Villagers_ชาวบ้าน", parent, local, yaw);
        go.AddComponent<VillagerSpawner>().Configure(size, count, festival, open, close, talk);
    }

    // Town crowd with foreign tourists among the locals
    public static void TouristCrowd(Transform parent, Vector3 local, Vector2 size, int count, int tourists, int open, int close, float yaw = 0f)
    {
        var go = PrimitiveKit.Group("Tourists_นักท่องเที่ยว", parent, local, yaw);
        go.AddComponent<VillagerSpawner>().Configure(size, count, "", open, close, CityTalk, tourists, TouristTalk);
    }

    public const string TouristTalk = "Hello! สวัสดีครับ|Sawasdee ka~|ต้มยำกุ้ง อร่อยมาก!|Where is the skytrain?|ตุ๊กตุ๊ก ไปวัดเท่าไหร่?|So hot today!|ขอบคุณค่ะ Khob khun ka|I love Thai food!|ถ่ายรูปให้หน่อยได้ไหมครับ|Amazing Thailand!|Pad Thai please~|ไปตลาดสดทางไหนคะ?";

    public static void AlmsRound(Transform parent, Vector3 from, Vector3 to, int monks)
    {
        var go = new GameObject("AlmsRound_พระบิณฑบาต");
        go.transform.SetParent(parent, false);
        go.AddComponent<MonkProcession>().Configure(from, to, monks);
    }

    // People who stay put and watch (Spectators): spots and facings in the group's own space
    public static Transform Watchers(Transform parent, string name, Vector3 local, float yaw, Vector3[] spots, float[] yaws,
        SpectatorPose.Kind[] kinds, string festival, int open, int close)
    {
        var g = PrimitiveKit.Group(name, parent, local, yaw);
        int seed = Mathf.Abs(Mathf.RoundToInt(parent.position.x * 13f + parent.position.z * 7f + local.x * 31f + local.z * 17f)) + 1;
        g.AddComponent<Spectators>().Configure(spots, yaws, kinds, festival, open, close, seed);
        return g.transform;
    }

    // The crowd round a boxing ring (Places.BoxingRing: 6x6 m, ropes on three sides, the entrance at -Z):
    // two rows behind it and one down each side, all facing the ring, most of them cheering.
    // `full` false: a few lookers-on behind it only (a training camp).
    public static void RingCrowd(Transform ring, bool full, string festival, int open, int close)
    {
        var spots = new System.Collections.Generic.List<Vector3>();
        var yaws = new System.Collections.Generic.List<float>();
        var kinds = new System.Collections.Generic.List<SpectatorPose.Kind>();
        void Add(float x, float z, int i)
        {
            spots.Add(new Vector3(x, 0f, z));
            yaws.Add(Mathf.Atan2(-x, -z) * Mathf.Rad2Deg);
            kinds.Add(!full || i % 4 == 3 ? SpectatorPose.Kind.Watch : SpectatorPose.Kind.Cheer);
        }
        int n = 0;
        for (float x = -2.8f; x <= 2.81f; x += 1.1f) Add(x + (n % 2) * 0.15f, 4.5f, n++);
        if (full)
        {
            for (float x = -2.3f; x <= 2.31f; x += 1.15f) Add(x, 5.6f, n++);
            foreach (float side in new[] { -1f, 1f })
                for (float z = -2.2f; z <= 2.61f; z += 1.2f) Add(side * 4.5f, z + (n % 2) * 0.1f, n++);
            foreach (float side in new[] { -1f, 1f }) Add(side * 4.3f, -3.6f, n++); // by the front corners
        }
        Watchers(ring, "RingCrowd_คนดูมวย", Vector3.zero, 0f, spots.ToArray(), yaws.ToArray(), kinds.ToArray(), festival, open, close);
    }
}
