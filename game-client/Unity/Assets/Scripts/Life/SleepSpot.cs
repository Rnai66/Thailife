using System;
using UnityEngine;

// Sleeping mat (เสื่อ) on the stilt-house porch: lie down to restore energy quickly.
[RequireComponent(typeof(Collider))]
public class SleepSpot : MonoBehaviour
{
    private PlayerController sleeper;
    private bool busy;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

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
        ContextButton.Show(this, "นอนพัก", $"เสื่อบนชานบ้าน · {energy} · นอนแล้วฟื้นเร็ว", () => Sleep(player));
    }

    private async void Sleep(PlayerController player)
    {
        var life = LifeClient.Instance;
        if (busy || life == null || !life.IsOnline) return;
        busy = true;
        try
        {
            await life.SleepAsync();
            sleeper = player;
            player.InputEnabled = false;
            player.transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 90f); // lie down
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Life] Sleep failed: {e.Message}");
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
        player.transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

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
        if (sleeper != null) sleeper.InputEnabled = true;
        ContextButton.Hide(this);
    }
}
