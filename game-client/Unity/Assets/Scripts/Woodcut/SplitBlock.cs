using System;
using System.Collections;
using UnityEngine;

// ตอผ่าฟืน: a chopping block by the footbridge with a pile of split wood. With an axe and logs in the
// bag: "ผ่าฟืน" splits up to a handful at a time (woodcut.lua wood_split) — each log stood on the
// block, the axe brought down over the head, and it cracks into three bundles of ฟืน.
[RequireComponent(typeof(Collider))]
public class SplitBlock : MonoBehaviour
{
    [SerializeField] private Transform block; // where the log stands

    private const int PerGo = 5;
    private bool near, busy;
    private GameObject log;

    public void Configure(Transform blockTop) => block = blockTop;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        near = true;
        Offer();
        if (WoodClient.Instance != null) Refresh();
    }

    private async void Refresh()
    {
        try { await WoodClient.Instance.RefreshAsync(); }
        catch (Exception) { return; }
        if (this != null && near && !busy) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        near = false;
        ContextButton.Hide(this);
    }

    private void Offer()
    {
        var s = WoodClient.Instance?.Current;
        if (s == null) { ContextButton.Show(this, "ผ่าฟืน", "ต้องออนไลน์ก่อน", null, enabled: false); return; }
        if (!s.has_axe) { ContextButton.Show(this, "ผ่าฟืน", "ต้องมีขวาน — ซื้อขวานผ่าฟืนที่ร้านค้าชุมชน", null, enabled: false); return; }
        if (s.logs <= 0) { ContextButton.Show(this, "ผ่าฟืน", $"ไม่มีท่อนไม้ — โค่นต้นไม้ในป่าก่อน · ฟืนในกระเป๋า {s.firewood} มัด", null, enabled: false); return; }
        int n = Mathf.Min(s.logs, Mathf.Min(PerGo, s.split_max));
        ContextButton.Show(this, "ผ่าฟืน", $"ท่อนไม้ {s.logs} ท่อน · ผ่า {n} ท่อน ได้ฟืน {n * s.split_gives} มัด (มัดละ {s.firewood_price} บาท) · พลังงาน {n * s.split_energy}",
            () => Split(n), highlight: true);
    }

    private async void Split(int n)
    {
        var client = WoodClient.Instance;
        if (busy || client == null) return;
        busy = true;
        ContextButton.Hide(this);
        float seconds = Mathf.Min(1.3f * n, 5.2f);
        int swings = Mathf.Max(1, Mathf.RoundToInt(seconds / 1.3f));
        float started = Time.time;
        ActivityPose.Do("split", seconds, face: block != null ? block.position : transform.position);
        StartCoroutine(Swings(seconds, swings));
        try
        {
            var s = await client.SplitAsync(n);
            float left = seconds - (Time.time - started);
            if (left > 0f) await System.Threading.Tasks.Task.Delay((int)(left * 1000f));
            if (this == null) return;
            UiKit.FloatAbovePlayer($"+{s.got} {s.name}", new Color(0.95f, 0.75f, 0.45f));
            LifeClient.Instance?.Refresh();
        }
        catch (Exception e)
        {
            ActivityPose.Clear();
            UiKit.FloatAbovePlayer(WoodClient.Explain(e), new Color(1f, 0.6f, 0.4f));
        }
        if (this == null) return;
        busy = false;
        if (near) Offer();
    }

    // A log on the block each swing; it cracks in two halves that fall either side
    private IEnumerator Swings(float seconds, int swings)
    {
        float per = seconds / swings;
        var at = block != null ? block.position : transform.position + Vector3.up * 0.5f;
        for (int i = 0; i < swings; i++)
        {
            log = Piece(at + Vector3.up * 0.2f, new Vector3(0.24f, 0.2f, 0.24f), PrimitiveType.Cylinder);
            yield return new WaitForSeconds(per * 0.62f);
            if (this == null) yield break;
            if (log != null) Destroy(log);
            GameAudio.Play("punch_heavy", at, 0.8f);
            foreach (float side in new[] { -1f, 1f })
            {
                var half = Piece(at + Vector3.up * 0.2f + transform.right * side * 0.07f, new Vector3(0.12f, 0.38f, 0.22f), PrimitiveType.Cube);
                var rb = half.AddComponent<Rigidbody>();
                rb.linearVelocity = transform.right * side * 1.2f + Vector3.up * 0.8f;
                rb.angularVelocity = transform.forward * side * -4f;
                Destroy(half, 2.5f);
            }
            yield return new WaitForSeconds(per * 0.38f);
        }
    }

    private static GameObject Piece(Vector3 at, Vector3 size, PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.position = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material.color = new Color(0.82f, 0.66f, 0.45f);
        return go;
    }

    private void OnDestroy()
    {
        ContextButton.Hide(this);
        if (log != null) Destroy(log);
    }
}
