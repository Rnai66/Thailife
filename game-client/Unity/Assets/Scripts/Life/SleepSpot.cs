using System;
using UnityEngine;

// A place to lie down and restore energy quickly: the mat (เสื่อ) on the stilt-house porch,
// the rest hut (เถียงนา) at Kok Nong Na... With a `bed` point the player is moved onto it
// (e.g. up on the hut floor) and back to where they stood when they wake up.
[RequireComponent(typeof(Collider))]
public class SleepSpot : MonoBehaviour
{
    [SerializeField] private string place = "เสื่อบนชานบ้าน";
    [SerializeField] private string action = "นอนพัก";
    [SerializeField] private Transform bed;
    [SerializeField] private string paidSpot = "";  // e.g. "beach_chair": rent paid to the server per rest
    [SerializeField] private int price;

    private PlayerController sleeper;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private bool busy;

    public void Configure(string placeName, string actionLabel, Transform bedPoint)
    {
        place = placeName;
        action = actionLabel;
        bed = bedPoint;
    }

    // A rest spot you pay for each time (beach chairs); the server charges the price
    public void ConfigurePaid(string spotId, int rent)
    {
        paidSpot = spotId;
        price = rent;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    // Is the local player lying in any bed / chair / hammock right now?
    public static bool LocalSleeping
    {
        get
        {
            foreach (var s in FindObjectsByType<SleepSpot>(FindObjectsSortMode.None))
                if (s.sleeper != null) return true;
            return false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null) Offer(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && sleeper == null)
            ContextButton.Hide(this);
    }

    private void Update()
    {
        // While asleep, keep the status line counting up
        var life = LifeClient.Instance;
        if (sleeper != null && life != null && ContextButton.IsShownBy(this))
        {
            ContextButton.Show(this, "ตื่น", $"กำลังนอน... พลังงาน {life.Energy:0}/{life.Current?.max_energy ?? 100}", Wake);
            if (life.Energy >= (life.Current?.max_energy ?? 100)) Wake();
        }
    }

    private void Offer(PlayerController player)
    {
        var life = LifeClient.Instance;
        string energy = life != null && life.IsOnline ? $"พลังงาน {life.Energy:0}/{life.Current.max_energy}" : "ต้องออนไลน์ก่อน";
        string label = price > 0 ? $"{action} ({price} บาท)" : action;
        string cost = price > 0 ? $"ค่าเช่า {price} บาท · " : "";
        ContextButton.Show(this, label, $"{place} · {cost}{energy} · นอนแล้วฟื้นเร็ว", () => Sleep(player));
    }

    private async void Sleep(PlayerController player)
    {
        var life = LifeClient.Instance;
        if (busy || life == null || !life.IsOnline) return;
        busy = true;
        try
        {
            await life.SleepAsync(string.IsNullOrEmpty(paidSpot) ? null : paidSpot);
            if (price > 0)
            {
                UiKit.FloatAbovePlayer($"-{price} ฿", new Color(1f, 0.6f, 0.3f));
                GameAudio.Play("coins");
                if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            }
            sleeper = player;
            player.InputEnabled = false;
            standPosition = player.transform.position;
            standRotation = player.transform.rotation;
            if (bed != null)
            {
                player.Teleport(bed.position, bed.rotation);
                player.enabled = false; // no gravity while lying on a raised floor
            }
            player.transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 90f); // lie down
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Life] Sleep failed: {e.Message}");
            if (e.Message.Contains("not enough baht"))
                ContextButton.Show(this, action, $"เงินไม่พอจ่ายค่าเช่า {price} บาท", () => Sleep(player));
            else if (e.Message.Contains("no booking"))
                ContextButton.Show(this, action, "การจองห้องหมดเวลาแล้ว — จองใหม่ที่เคาน์เตอร์", () => Sleep(player));
        }
        busy = false;
    }

    private async void Wake()
    {
        var life = LifeClient.Instance;
        if (busy || sleeper == null) return;
        busy = true;

        var player = sleeper;
        sleeper = null;
        player.InputEnabled = true;
        if (bed != null)
        {
            player.enabled = true;
            player.Teleport(standPosition, standRotation); // climb back down
        }
        else player.transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

        try
        {
            if (life != null) await life.WakeAsync();
            FloatingText.Spawn(player.transform.position + Vector3.up * 2f, "สดชื่น!", new Color(0.5f, 0.9f, 1f));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Life] Wake failed: {e.Message}");
        }
        busy = false;
        Offer(player);
    }

    private void OnDestroy()
    {
        if (sleeper != null)
        {
            sleeper.InputEnabled = true;
            sleeper.enabled = true;
            _ = LifeClient.Instance?.WakeAsync(); // left the scene while asleep: wake on the server too
        }
        ContextButton.Hide(this);
    }
}
