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

    public static void AlmsRound(Transform parent, Vector3 from, Vector3 to, int monks)
    {
        var go = new GameObject("AlmsRound_พระบิณฑบาต");
        go.transform.SetParent(parent, false);
        go.AddComponent<MonkProcession>().Configure(from, to, monks);
    }
}
