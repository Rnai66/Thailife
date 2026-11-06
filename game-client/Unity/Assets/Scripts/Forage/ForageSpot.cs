using System;
using UnityEngine;

// A place in the forest behind the tambon canal where something wild grows (forage.lua): mushrooms by
// a termite mound, bamboo shoots in a bamboo clump, a red ant nest up a tree, ผักหวานป่า. Walk up and
// pick it; it vanishes until it has grown back. `crop` is the bit that shows while it's ready.
[RequireComponent(typeof(Collider))]
public class ForageSpot : MonoBehaviour
{
    [SerializeField] private string spot = "fg1";
    [SerializeField] private string kind = "mushroom";
    [SerializeField] private GameObject crop;

    private bool near, busy;
    private float nextCheck;

    public void Configure(string spotId, string kindId, GameObject cropModel)
    {
        spot = spotId;
        kind = kindId;
        crop = cropModel;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private float ReadyIn => ForageClient.Instance != null ? ForageClient.Instance.ReadyIn(spot) : -1f;

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 1f;
        float r = ReadyIn;
        if (crop != null) crop.SetActive(r <= 0f); // unknown (offline) shows it; picking needs the server anyway
        if (near) Offer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        near = true;
        Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        near = false;
        ContextButton.Hide(this);
    }

    private void Offer()
    {
        var client = ForageClient.Instance;
        var k = client?.KindOf(kind);
        if (k == null) { ContextButton.Show(this, "หาของป่า", "ต้องออนไลน์ก่อน", null, enabled: false); return; }
        float r = ReadyIn;
        if (r > 0f)
        {
            int min = Mathf.CeilToInt(r / 60f);
            ContextButton.Show(this, k.verb, $"เก็บไปแล้ว · {k.name}ขึ้นใหม่อีกราว {min} นาที", null, enabled: false);
            return;
        }
        ContextButton.Show(this, k.verb, $"{k.name} · ขายร้านค้าชุมชน {k.price} บาท · ใช้พลังงาน {client.Current.energy}", Pick, !busy, highlight: true);
    }

    private async void Pick()
    {
        var client = ForageClient.Instance;
        if (busy || client == null) return;
        busy = true;
        try
        {
            var s = await client.PickAsync(spot);
            GameAudio.Play("pickup", transform.position);
            UiKit.FloatAbovePlayer($"+{s.got} {s.name}", new Color(0.6f, 1f, 0.5f));
            if (crop != null) crop.SetActive(false);
            LifeClient.Instance?.Refresh();
        }
        catch (Exception e)
        {
            UiKit.FloatAbovePlayer(ForageClient.Explain(e), new Color(1f, 0.6f, 0.4f));
        }
        busy = false;
        if (near) Offer();
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
