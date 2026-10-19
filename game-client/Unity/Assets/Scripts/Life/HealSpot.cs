using System;
using Nakama;
using UnityEngine;

// The counter at the health station (รพ.สต.) or the hospital (โรงพยาบาล): treats an injury
// from a knockout. The hospital costs more and also restores full energy.
[RequireComponent(typeof(Collider))]
public class HealSpot : MonoBehaviour
{
    [SerializeField] private string place = "station"; // life.lua CLINICS id
    [SerializeField] private string title = "รพ.สต.";
    [SerializeField] private int price = 50;

    private bool busy;

    public string Place => place;

    public void Configure(string clinic, string name, int cost)
    {
        place = clinic;
        title = name;
        price = cost;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }

    private void Offer()
    {
        var life = LifeClient.Instance;
        bool injured = life != null && life.IsInjured;
        string extra = place == "hospital" ? " · พลังงานเต็ม" : "";
        ContextButton.Show(this, "รักษาตัว",
            injured ? $"{title} · ค่ารักษา {price} บาท{extra}" : $"{title} · สุขภาพแข็งแรงดี ไม่ต้องรักษา",
            Treat, injured, highlight: injured);
    }

    private async void Treat()
    {
        var life = LifeClient.Instance;
        if (busy || life == null) return;
        busy = true;
        try
        {
            var r = await life.HealAsync(place);
            UiKit.FloatAbovePlayer($"หายแล้ว! จ่าย {r.paid} บาท", new Color(0.5f, 1f, 0.6f));
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        }
        catch (ApiResponseException e)
        {
            UiKit.FloatAbovePlayer(e.Message.Contains("baht") ? $"เงินไม่พอ (ค่ารักษา {price} บาท)" : "รักษาไม่ได้ตอนนี้", new Color(1f, 0.45f, 0.4f));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Heal] {e.Message}");
        }
        busy = false;
        Offer();
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
