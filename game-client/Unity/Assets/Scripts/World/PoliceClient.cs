using System;
using System.Threading.Tasks;
using UnityEngine;

// ใบสั่ง (police.lua): ask for the ticket the police just wrote — speeding past a speed trap or
// running a checkpoint. The server checks it and takes the fine (or lets you off with a warning).
public static class PoliceClient
{
    [Serializable] public class Ticket { public string kind; public int speed, limit, fine, paid; public bool warning; }
    [Serializable] private class FineRequest { public string kind; public int speed, limit; }

    public static async Task<Ticket> FineAsync(string kind, int speed, int limit)
    {
        var conn = UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, "police_fine",
            JsonUtility.ToJson(new FineRequest { kind = kind, speed = speed, limit = limit }));
        var t = JsonUtility.FromJson<Ticket>(result.Payload);
        if (t.paid > 0 && WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        return t;
    }

    // The police stopped you: get the ticket and show it (offline or too soon after the last one:
    // just a telling-off)
    public static async void Stop(string kind, int speed, int limit)
    {
        try
        {
            var t = await FineAsync(kind, speed, limit);
            PoliceTicketUI.Show(t);
        }
        catch (Exception e)
        {
            Debug.Log("[Police] " + e.Message);
            PoliceTicketUI.Show(new Ticket { kind = kind, speed = speed, limit = limit, warning = true });
        }
    }
}
