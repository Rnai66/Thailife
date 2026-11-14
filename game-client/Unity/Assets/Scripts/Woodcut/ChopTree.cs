using System;
using System.Collections;
using UnityEngine;

// A tree you can fell with the axe (woodcut.lua): a big forest tree or a tangle of overgrown scrub
// (ไม้รก) in the forest behind the tambon canal. Walk up → "โค่นต้นไม้"/"ถางไม้รก": a few swings of
// the axe, the tree creaks and falls away from you, and the logs go in your bag. It leaves a stump
// (`stump`) until it has grown back. `standing` is the whole tree that shows while it's there.
[RequireComponent(typeof(Collider))]
public class ChopTree : MonoBehaviour
{
    [SerializeField] private string tree = "wt1";
    [SerializeField] private string kind = "tree";
    [SerializeField] private GameObject standing, stump;

    private bool near, busy, falling;
    private float nextCheck;

    public void Configure(string treeId, string kindId, GameObject standingModel, GameObject stumpModel)
    {
        tree = treeId;
        kind = kindId;
        standing = standingModel;
        stump = stumpModel;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private float ReadyIn => WoodClient.Instance != null ? WoodClient.Instance.ReadyIn(tree) : -1f;

    private void Update()
    {
        if (Time.time < nextCheck || falling) return;
        nextCheck = Time.time + 1f;
        bool up = ReadyIn <= 0f; // unknown (offline) shows it standing
        if (standing != null && standing.activeSelf != up)
        {
            standing.SetActive(up);
            standing.transform.localRotation = Quaternion.identity;
        }
        if (stump != null) stump.SetActive(!up);
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
        if (busy) return;
        var client = WoodClient.Instance;
        var k = client?.KindOf(kind);
        if (k == null) { ContextButton.Show(this, kind == "tree" ? "โค่นต้นไม้" : "ถางไม้รก", "ต้องออนไลน์ก่อน", null, enabled: false); return; }
        float r = ReadyIn;
        if (r > 0f)
        {
            ContextButton.Show(this, k.verb, $"โค่นไปแล้ว · {k.name}ขึ้นใหม่อีกราว {Mathf.CeilToInt(r / 60f)} นาที", null, enabled: false);
            return;
        }
        var s = client.Current;
        if (!s.has_axe)
        {
            ContextButton.Show(this, k.verb, "ต้องมีขวาน — ซื้อขวานผ่าฟืนที่ร้านค้าชุมชน", null, enabled: false);
            return;
        }
        ContextButton.Show(this, k.verb, $"{k.name} · ได้ท่อนไม้ {k.min}-{k.max} ท่อน · ใช้พลังงาน {k.energy}", Chop, highlight: true);
    }

    private async void Chop()
    {
        var client = WoodClient.Instance;
        if (busy || client == null) return;
        busy = true;
        ContextButton.Hide(this);
        float seconds = kind == "tree" ? 3.6f : 2.4f;
        float started = Time.time;
        ActivityPose.Do("fell", seconds, face: transform.position);
        StartCoroutine(Thunks(seconds, kind == "tree" ? 3 : 2));
        try
        {
            var s = await client.ChopAsync(tree);
            float left = seconds - (Time.time - started);
            if (left > 0f) await System.Threading.Tasks.Task.Delay((int)(left * 1000f));
            if (this == null) return;
            UiKit.FloatAbovePlayer($"+{s.got} {s.name}", new Color(0.85f, 0.65f, 0.4f));
            StartCoroutine(Fall());
            LifeClient.Instance?.Refresh();
        }
        catch (Exception e)
        {
            ActivityPose.Clear();
            UiKit.FloatAbovePlayer(WoodClient.Explain(e), new Color(1f, 0.6f, 0.4f));
        }
        if (this == null) return;
        busy = false;
    }

    // The axe biting into the wood once per swing, chips flying
    private IEnumerator Thunks(float seconds, int swings)
    {
        float per = seconds / swings;
        for (int i = 0; i < swings; i++)
        {
            yield return new WaitForSeconds(per * 0.68f);
            if (this == null) yield break;
            GameAudio.Play("punch_heavy", transform.position + Vector3.up, 0.7f);
            Chips(transform.position + Vector3.up * (kind == "tree" ? 0.8f : 0.5f));
            yield return new WaitForSeconds(per * 0.32f);
        }
    }

    private static void Chips(Vector3 at)
    {
        for (int i = 0; i < 5; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(c.GetComponent<Collider>());
            c.transform.position = at;
            c.transform.localScale = new Vector3(0.06f, 0.02f, 0.04f);
            c.GetComponent<Renderer>().material.color = new Color(0.85f, 0.72f, 0.5f);
            var rb = c.AddComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(1f, 2.5f), UnityEngine.Random.Range(-1.5f, 1.5f));
            Destroy(c, 1.2f);
        }
    }

    // The tree tips over away from the player, then lies there a moment before the stump shows
    private IEnumerator Fall()
    {
        if (standing == null) yield break;
        falling = true;
        var player = FindAnyObjectByType<PlayerController>();
        Vector3 away = player != null ? transform.position - player.transform.position : transform.forward;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = transform.forward;
        Vector3 axis = Vector3.Cross(Vector3.up, away.normalized);
        var t = standing.transform;
        Quaternion start = t.rotation;
        if (stump != null) stump.SetActive(true);
        GameAudio.Play("punch_heavy", transform.position, 1f);
        float dur = kind == "tree" ? 1.6f : 0.6f;
        for (float u = 0f; u < 1f; u += Time.deltaTime / dur)
        {
            float a = 88f * u * u; // slow at first, then crashing down
            t.rotation = Quaternion.AngleAxis(a, axis) * start;
            yield return null;
        }
        t.rotation = Quaternion.AngleAxis(88f, axis) * start;
        GameAudio.Play("punch_heavy", transform.position + away.normalized * 3f, 0.9f);
        yield return new WaitForSeconds(1.5f);
        standing.SetActive(false);
        t.rotation = start;
        falling = false;
        nextCheck = 0f;
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
