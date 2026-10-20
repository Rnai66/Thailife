using System;
using System.Collections;
using Nakama;
using UnityEngine;

// Island treasure chest (หีบสมบัติ, island.lua): walk up to it and open it once per game day for a
// random find. The lid swings open and gold glints inside while the reward floats up.
[RequireComponent(typeof(Collider))]
public class TreasureChest : MonoBehaviour
{
    [SerializeField] private string chestId = "cave";
    [SerializeField] private Transform lid;
    [SerializeField] private GameObject gold;

    private bool busy;

    [Serializable] private class Result { public int coins; public string found; }

    public void Configure(string id, Transform lidHinge, GameObject goldInside)
    {
        chestId = id;
        lid = lidHinge;
        gold = goldInside;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        if (gold != null) gold.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }

    private void Offer(string message = null) =>
        ContextButton.Show(this, "เปิดหีบสมบัติ", message ?? "หีบเก่าของนักเดินเรือ · เปิดได้วันละครั้ง", Open);

    private async void Open()
    {
        if (busy) return;
        var conn = FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected) { Offer("ต้องออนไลน์ก่อน"); return; }
        busy = true;
        try
        {
            var rpc = await conn.Client.RpcAsync(conn.Session, "island_treasure", $"{{\"chest\":\"{chestId}\"}}");
            var r = JsonUtility.FromJson<Result>(rpc.Payload);
            StartCoroutine(Reveal());
            UiKit.FloatAbovePlayer($"{r.found}  +{r.coins} ฿", new Color(1f, 0.85f, 0.3f));
            GameAudio.Play("coins");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Offer("ได้ " + r.found + " · พรุ่งนี้มาเปิดใหม่ได้");
        }
        catch (ApiResponseException e)
        {
            Offer(e.Message.Contains("already") ? "หีบนี้เปิดไปแล้ววันนี้ — มาใหม่พรุ่งนี้" : "เปิดหีบไม่ได้ ลองใหม่");
        }
        catch (Exception)
        {
            Offer("เปิดหีบไม่ได้ ลองใหม่");
        }
        busy = false;
    }

    // Lid swings open, the gold shows for a while, then the lid closes again
    private IEnumerator Reveal()
    {
        if (lid == null) yield break;
        if (gold != null) gold.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime * 2.5f)
        {
            lid.localRotation = Quaternion.Euler(-Mathf.SmoothStep(0f, 105f, t), 0f, 0f);
            yield return null;
        }
        yield return new WaitForSeconds(4f);
        for (float t = 0f; t < 1f; t += Time.deltaTime * 2f)
        {
            lid.localRotation = Quaternion.Euler(-Mathf.SmoothStep(105f, 0f, t), 0f, 0f);
            yield return null;
        }
        if (gold != null) gold.SetActive(false);
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
