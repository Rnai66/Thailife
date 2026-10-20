using System;
using UnityEngine;

// A skilled job's workplace (education.lua JOBS): walk up to see the job, its pay and what it needs.
// "เริ่มงาน" starts a timed shift (the server counts the time); come back when it's done for the wage.
// Locked until your main level and subject levels are high enough — learn them at the school.
[RequireComponent(typeof(Collider))]
public class JobDesk : MonoBehaviour
{
    [SerializeField] private string jobId = "repair";

    private bool inside, busy;
    private Education.State state;
    private float endsAt, nextTick;

    public void Configure(string job) => jobId = job;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        inside = true;
        Load();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        inside = false;
        ContextButton.Hide(this);
    }

    private async void Load()
    {
        if (!Education.Online) { ContextButton.Show(this, "งาน", "ต้องออนไลน์ก่อน", () => { }, false); return; }
        try { Show(await Education.GetAsync()); }
        catch (Exception e) { ContextButton.Show(this, "งาน", Education.Explain(e), Load, true); }
    }

    private void Show(Education.State s, string note = null)
    {
        state = s;
        if (!inside) return;
        var job = s.FindJob(jobId);
        if (job == null) return;
        bool mine = s.job != null && s.job.Active && s.job.id == jobId;
        bool other = s.job != null && s.job.Active && !mine;
        bool inClass = s.@class != null && s.@class.Active;

        if (mine)
        {
            endsAt = Time.unscaledTime + s.job.remain;
            Tick();
            return;
        }
        string head = $"{job.name} · ค่าจ้าง {job.pay} ฿";
        if (!job.unlocked)
            ContextButton.Show(this, "ยังทำไม่ได้", $"{head}\n{job.missing} (เรียนที่โรงเรียน)", () => { }, false);
        else if (other || inClass)
            ContextButton.Show(this, "ไม่ว่าง", $"{head}\n{(inClass ? "กำลังเรียนอยู่" : "กำลังทำงานอื่นอยู่")}", () => { }, false);
        else if (job.shifts >= job.cap)
            ContextButton.Show(this, "ครบแล้ววันนี้", $"{head}\nทำครบ {job.cap} กะแล้ว พรุ่งนี้มาใหม่", () => { }, false);
        else
            ContextButton.Show(this, "เริ่มงาน", note ?? $"{head}\nใช้พลังงาน {job.energy} · {job.secs} วินาที · วันนี้ {job.shifts}/{job.cap} กะ", StartShift);
    }

    // While a shift runs: count down on the button, then let the player collect the wage
    private void Tick()
    {
        float remain = Mathf.Max(0f, endsAt - Time.unscaledTime);
        var job = state?.FindJob(jobId);
        string name = job != null ? job.name : "งาน";
        if (remain > 0f)
            ContextButton.Show(this, $"ทำงาน... {Mathf.CeilToInt(remain)} วิ", $"{name}\nรอให้ครบเวลาแล้วรับค่าจ้าง", () => { }, false);
        else
            ContextButton.Show(this, "รับค่าจ้าง", $"{name} เสร็จแล้ว!", FinishShift, true, highlight: true);
    }

    private void Update()
    {
        if (!inside || state == null || state.job == null || !state.job.Active || state.job.id != jobId) return;
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.5f;
        Tick();
    }

    private async void StartShift()
    {
        if (busy) return;
        busy = true;
        try
        {
            Show(await Education.StartJobAsync(jobId));
            LifeClient.Instance?.Refresh();
            GameAudio.Play("bell", null, 0.5f);
        }
        catch (Exception e)
        {
            if (state != null) Show(state, Education.Explain(e));
        }
        busy = false;
    }

    private async void FinishShift()
    {
        if (busy) return;
        busy = true;
        try
        {
            var s = await Education.FinishJobAsync();
            UiKit.FloatAbovePlayer($"+{s.paid.pay} ฿ ค่าจ้าง{s.paid.name}", new Color(0.4f, 1f, 0.5f));
            GameAudio.Play("coins");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Show(s);
        }
        catch (Exception e)
        {
            ContextButton.Show(this, "รับค่าจ้าง", Education.Explain(e), FinishShift);
        }
        busy = false;
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
