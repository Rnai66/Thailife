using System;
using System.Collections;
using Nakama;
using UnityEngine;

// เวทีรำวงชาวบ้าน: buy a ticket (บัตรรำวง, 20 บาท a round) at the front of the stage and you go up and
// dance round the flower table with the dancers for one round (turning your hands as you go), then
// come back down. A round restores a little energy — dancing is fun.
[RequireComponent(typeof(Collider))]
public class RamwongStage : MonoBehaviour
{
    [SerializeField] private Transform center;   // the flower table, on the stage floor
    [SerializeField] private float radius = 2.3f;
    [SerializeField] private int price = 20;

    private const float RoundSeconds = 30f;

    private PlayerController dancer;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private float startAngle, endTime, nextGesture;
    private bool busy;

    public void Configure(Transform table, float circleRadius, int ticket)
    {
        center = table;
        radius = circleRadius;
        price = ticket;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null && dancer == null) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && dancer == null) ContextButton.Hide(this);
    }

    private void Offer(string message = null) =>
        ContextButton.Show(this, $"ซื้อบัตรรำวง\n{price} บาท", message ?? "ขึ้นเวทีไปรำวงรอบโต๊ะดอกไม้กับนางรำ 1 รอบ", Buy);

    private async void Buy()
    {
        var f = FestivalClient.Instance;
        if (busy || dancer != null || f == null || center == null) return;
        busy = true;
        try
        {
            var r = await f.PlayAsync("ramwong");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh();
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) StartCoroutine(Dance(player));
        }
        catch (ApiResponseException e) { Offer(FestivalBooth.Explain(e, price)); }
        catch (Exception) { Offer("ซื้อบัตรไม่สำเร็จ ลองใหม่"); }
        busy = false;
    }

    private IEnumerator Dance(PlayerController player)
    {
        dancer = player;
        standPosition = player.transform.position;
        standRotation = player.transform.rotation;
        player.InputEnabled = false;
        player.enabled = false;
        // Join the circle in step with the dancers, half way between two of them
        startAngle = Time.time * RamwongDancer.DegreesPerSecond + 30f;
        endTime = Time.time + RoundSeconds;
        ContextButton.Show(this, "ลงจากเวที", "รำวงกัน! วนรอบโต๊ะดอกไม้ 1 รอบ", Leave, true, highlight: true);
        UiKit.FloatAbovePlayer("รำวง!", new Color(1f, 0.6f, 0.8f));
        var model = player.GetComponent<CharacterModel>();
        while (dancer != null && Time.time < endTime)
        {
            float a = (startAngle + (Time.time - (endTime - RoundSeconds)) * RamwongDancer.DegreesPerSecond) * Mathf.Deg2Rad;
            var cc = player.GetComponent<CharacterController>();
            float foot = cc != null ? cc.center.y - cc.height * 0.5f : -1f;
            player.transform.SetPositionAndRotation(center.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius - Vector3.up * foot,
                Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a))));
            if (model != null && Time.time >= nextGesture)
            {
                nextGesture = Time.time + 1.6f;
                model.Play(UnityEngine.Random.value < 0.6f ? PlayerAction.Wave : PlayerAction.Wai);
            }
            yield return null;
        }
        Leave();
    }

    private void Leave()
    {
        if (dancer == null) return;
        var player = dancer;
        dancer = null;
        player.enabled = true;
        player.InputEnabled = true;
        player.Teleport(standPosition, standRotation);
        UiKit.FloatAbovePlayer("สนุกมาก! (+8 พลังงาน)", new Color(1f, 0.85f, 0.3f));
        Offer("รำอีกรอบไหม?");
    }

    private void OnDestroy()
    {
        if (dancer != null)
        {
            dancer.enabled = true;
            dancer.InputEnabled = true;
        }
        ContextButton.Hide(this);
    }
}
