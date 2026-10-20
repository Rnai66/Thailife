using System;
using System.Threading.Tasks;
using UnityEngine;

// School and skilled jobs (education.lua): data from the server and the RPC calls. The school window
// (SchoolUI) and the job desks (JobDesk) both use this.
public static class Education
{
    [Serializable]
    public class Subject
    {
        public string id, name, desc;
        public int level, xp, xp_to_next, fee, energy, main_xp, need_level;
        public int lib_energy, lib_xp, lib_main_xp; // self-study in the library
        public bool unlocked;
    }

    [Serializable]
    public class Job
    {
        public string id, name, place, missing;
        public int pay, energy, secs, shifts, cap;
        public bool unlocked;
    }

    [Serializable]
    public class Timer
    {
        public string id;   // empty = none (JsonUtility fills missing objects with defaults)
        public float remain, secs;
        public bool library;
        public bool Active => !string.IsNullOrEmpty(id);
    }

    [Serializable]
    public class Finished
    {
        public string subject, name;
        public int xp, main_xp, level, main_level;
        public bool subject_up, main_up, library;
    }

    [Serializable]
    public class Paid
    {
        public string job, name;
        public int pay;
    }

    [Serializable]
    public class State
    {
        public int main_level;
        public Subject[] subjects;
        public Job[] jobs;
        public Timer @class;
        public Timer job;
        public Finished finished;
        public Paid paid;

        public Job FindJob(string jobId)
        {
            if (jobs != null) foreach (var j in jobs) if (j.id == jobId) return j;
            return null;
        }
    }

    [Serializable] private class SubjectRequest { public string subject; public bool library; }
    [Serializable] private class JobRequest { public string job; }

    private static NakamaConnection Connection()
    {
        var conn = UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();
        return conn != null && conn.IsConnected ? conn : null;
    }

    public static bool Online => Connection() != null;

    private static async Task<State> Call(string rpc, string payload = "{}")
    {
        var conn = Connection();
        if (conn == null) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        return JsonUtility.FromJson<State>(result.Payload);
    }

    public static Task<State> GetAsync() => Call("edu_get");
    public static Task<State> StartClassAsync(string subject, bool library = false) =>
        Call("class_start", JsonUtility.ToJson(new SubjectRequest { subject = subject, library = library }));
    public static Task<State> FinishClassAsync() => Call("class_finish");
    public static Task<State> StartJobAsync(string job) => Call("job_start", JsonUtility.ToJson(new JobRequest { job = job }));
    public static Task<State> FinishJobAsync() => Call("job_finish");

    // Server error → short Thai message
    public static string Explain(Exception e)
    {
        string m = e.Message;
        int skill = m.IndexOf("skill: ", StringComparison.Ordinal);
        if (skill >= 0) return m.Substring(skill + 7);
        if (m.Contains("energy")) return "พลังงานไม่พอ — กินอะไรหน่อยหรือนอนพัก";
        if (m.Contains("baht")) return "เงินไม่พอ";
        if (m.Contains("sleeping")) return "กำลังนอนอยู่";
        if (m.Contains("level too low")) return "เลเวลยังไม่ถึง";
        if (m.Contains("no more shifts")) return "วันนี้ทำงานนี้ครบแล้ว — พรุ่งนี้มาใหม่";
        if (m.Contains("already in class") || m.Contains("in class")) return "กำลังเรียนอยู่";
        if (m.Contains("working")) return "กำลังทำงานอื่นอยู่";
        if (m.Contains("too early")) return "ยังไม่ครบเวลา";
        if (m.Contains("offline")) return "ต้องออนไลน์ก่อน";
        return "ทำไม่ได้ ลองใหม่";
    }

    // After XP changes: refresh the level shown on the HUD
    public static void RefreshLevel() => CareerClient.Instance?.Reload();
}
