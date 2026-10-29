using System;
using UnityEngine;

// Cross-river ferry (เรือข้ามฟาก): walk to the end of the pier, pay the 10-baht fare and you step off
// at the pier on the other bank.
[RequireComponent(typeof(Collider))]
public class FerryCrossing : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private string message = "นั่งเรือหางยาวข้ามฟาก";

    private bool paying;

    public void Configure(Transform target, string text)
    {
        destination = target;
        message = text;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private async void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null || destination == null || paying) return;
        paying = true;
        try
        {
            int fare = await TravelClient.FerryAsync();
            if (player == null) return;
            player.Teleport(destination.position + Vector3.up, destination.rotation);
            UiKit.FloatAbovePlayer($"{message} · ค่าเรือ {fare} บาท", UiKit.GoldText);
        }
        catch (Exception e)
        {
            string why = e.Message.Contains("not enough baht") ? "เงินไม่พอค่าเรือ 10 บาท"
                : e.Message.Contains("offline") ? "ต้องออนไลน์ก่อนลงเรือ" : "ลงเรือไม่สำเร็จ ลองใหม่";
            UiKit.FloatAbovePlayer(why, new Color(1f, 0.5f, 0.4f));
        }
        finally { paying = false; }
    }
}
