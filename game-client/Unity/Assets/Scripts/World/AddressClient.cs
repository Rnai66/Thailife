using System;
using System.Threading.Tasks;
using UnityEngine;

// ที่อยู่ (profile.lua): the player's address. Its province is their home province — the "บ้านเกิด"
// town's place on the map, where trips and drives to other provinces are measured from.
public static class AddressClient
{
    [Serializable] public class Address { public string house = "", moo = "", village = "", tambon = "", amphoe = "", province = Provinces.Default; public bool set; }

    public static Address Last { get; private set; }

    public static async Task<Address> GetAsync() => Last = JsonUtility.FromJson<Address>(await Call("profile_get_address", "{}"));

    public static async Task<Address> SetAsync(Address a) => Last = JsonUtility.FromJson<Address>(await Call("profile_set_address", JsonUtility.ToJson(a)));

    // "บ้านเลขที่ 12 หมู่ 3 บ้านคอโค ต.… อ.… จ.…"
    public static string Line(Address a)
    {
        if (a == null) return "";
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(a.house)) parts.Add("บ้านเลขที่ " + a.house);
        if (!string.IsNullOrEmpty(a.moo)) parts.Add("หมู่ " + a.moo);
        if (!string.IsNullOrEmpty(a.village)) parts.Add(a.village);
        if (!string.IsNullOrEmpty(a.tambon)) parts.Add("ต." + a.tambon);
        if (!string.IsNullOrEmpty(a.amphoe)) parts.Add("อ." + a.amphoe);
        parts.Add("จ." + a.province);
        return string.Join(" ", parts);
    }

    private static async Task<string> Call(string rpc, string payload)
    {
        var conn = UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        return result.Payload;
    }
}
