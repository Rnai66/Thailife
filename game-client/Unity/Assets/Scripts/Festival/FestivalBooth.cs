using System;
using System.Collections;
using Nakama;
using UnityEngine;

// A fair game or show (สอยดาว, บิงโก, ปาโป่ง, มหาลัย ก.ไก่, ชิงช้าสวรรค์, ม้าหมุน, หนังกลางแปลง, ลิเก).
// Rides seat the player on `seat` for a while; สอยดาว, ปาโป่ง and บิงโก open their own game screens;
// shows just roll on the server.
[RequireComponent(typeof(Collider))]
public class FestivalBooth : MonoBehaviour
{
    [SerializeField] private string game = "soidao";
    [SerializeField] private string title = "สอยดาว";
    [SerializeField] private int price = 20;
    [SerializeField] private Transform seat;
    [SerializeField] private float rideSeconds = 18f;

    private bool busy;

    public void Configure(string gameId, string name, int cost, Transform rideSeat = null, float seconds = 18f)
    {
        game = gameId;
        title = name;
        price = cost;
        seat = rideSeat;
        rideSeconds = seconds;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        string label = seat != null ? "ขึ้นเล่น" : price == 0 ? (game == "morlam" ? "ดูหมอลำ" : "นั่งดู") : game == "soidao" ? "สอยดาว" : "เล่น";
        ContextButton.Show(this, label, price > 0 ? $"{title} · {price} บาท" : $"{title} · ดูฟรี", Play);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }

    // Fair games with their own screen: they take the ticket money themselves when you play
    private bool OwnScreen()
    {
        switch (game)
        {
            case "soidao": SoiDaoUI.Show(title, price); return true;
            case "darts": DartsUI.Show(title, price); return true;
            case "bingo": BingoUI.Show(title, price); return true;
        }
        return false;
    }

    public static string Explain(Exception e, int price) =>
        e.Message.Contains("baht") ? $"เงินไม่พอ ({price} บาท)"
        : e.Message.Contains("watched") ? "ดูรอบนี้ไปแล้ว รองานหน้า"
        : e.Message.Contains("big") ? "มีเฉพาะงานใหญ่ในเมือง"
        : e.Message.Contains("no festival") ? "ยังไม่มีงานตอนนี้" : "เล่นไม่ได้ตอนนี้";

    private async void Play()
    {
        var f = FestivalClient.Instance;
        if (busy || f == null) return;
        if (OwnScreen()) return;
        busy = true;
        try
        {
            var r = await f.PlayAsync(game);
            if (r.card != null && r.card.Length > 0)
            {
                KokaiUI.Show(r); // watch the letters being called on the blackboard
                if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
                busy = false;
                return;
            }
            var color = r.won || r.gained > 0 ? new Color(1f, 0.85f, 0.3f) : new Color(0.85f, 0.85f, 0.85f);
            UiKit.FloatAbovePlayer($"{r.game_name}: {r.prize}" + (r.gained > 0 ? $" (+{r.gained} พลังงาน)" : ""), color);
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            if (seat != null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) StartCoroutine(Ride(player));
            }
        }
        catch (ApiResponseException e)
        {
            UiKit.FloatAbovePlayer(Explain(e, price), new Color(1f, 0.5f, 0.4f));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Festival] {e.Message}");
        }
        busy = false;
    }

    // Sit on the ride seat while it turns, then step off where you got on
    private IEnumerator Ride(PlayerController player)
    {
        ContextButton.Hide(this);
        Vector3 standPos = player.transform.position;
        Quaternion standRot = player.transform.rotation;
        player.InputEnabled = false;
        player.enabled = false;
        float end = Time.time + rideSeconds;
        while (Time.time < end && player != null)
        {
            player.transform.SetPositionAndRotation(seat.position + Vector3.up * 0.9f, Quaternion.Euler(0f, seat.eulerAngles.y, 0f));
            yield return null;
        }
        if (player == null) yield break;
        player.enabled = true;
        player.InputEnabled = true;
        player.Teleport(standPos, standRot);
    }
}
