using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Local elections (election.lua): the current election's candidates and votes, registering at the
// อบต. office, campaigning and voting at the village hall, and the daily salary for office holders.
public static class ElectionClient
{
    [Serializable]
    public class Candidate
    {
        public string id, name;
        public int votes;
        public bool me, npc;
    }

    [Serializable]
    public class Position
    {
        public string id, name, @short, my_vote;
        public int level, fee, seats, salary, campaign_price;
        public Candidate[] candidates;
    }

    [Serializable]
    public class Winner
    {
        public string id, name;
        public int votes;
        public bool me;
    }

    [Serializable]
    public class Result
    {
        public string id, name;
        public Winner[] winners;
    }

    [Serializable]
    public class State
    {
        public int cycle, day, days, year, years, register_days, phase_ends_s, campaigns_left, level, politics, energy;
        public string phase, registered, office, office_name;
        public bool salary_today;
        public Position[] positions;
        public Result[] last_results;
        public string done;
        public int gained, earned;
        public bool bonus;
    }

    [Serializable] private class RegisterRequest { public string position; }
    [Serializable] private class VoteRequest { public string position, candidate; }

    public static State Current { get; private set; }
    public static float ReceivedAt { get; private set; }
    public static event Action<State> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }

    private static NakamaConnection Connection => UnityEngine.Object.FindAnyObjectByType<NakamaConnection>();
    public static bool Online => Connection != null && Connection.IsConnected;

    public static Task<State> GetAsync() => Call("election_get", "{}");
    public static Task<State> RegisterAsync(string position) =>
        Call("election_register", JsonUtility.ToJson(new RegisterRequest { position = position }));
    public static Task<State> CampaignAsync() => Call("election_campaign", "{}");
    public static Task<State> VoteAsync(string position, string candidate) =>
        Call("election_vote", JsonUtility.ToJson(new VoteRequest { position = position, candidate = candidate }));
    public static Task<State> SalaryAsync() => Call("election_salary", "{}");

    private static async Task<State> Call(string rpc, string payload)
    {
        var conn = Connection;
        if (conn == null || !conn.IsConnected) throw new InvalidOperationException("offline");
        var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        Current = s;
        ReceivedAt = Time.time;
        Changed?.Invoke(s);
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("offline") ? "ต้องออนไลน์ก่อน"
            : m.Contains("not enough baht") ? "เงินไม่พอ"
            : m.Contains("sleeping") ? "กำลังนอนอยู่"
            : m.Contains("energy") ? "พลังงานไม่พอ — พักก่อน"
            : m.Contains("level too low") ? "เลเวลยังไม่ถึงเกณฑ์ลงสมัคร"
            : m.Contains("registration closed") ? "ปิดรับสมัครแล้ว รอเลือกตั้งครั้งหน้า"
            : m.Contains("already registered") ? "ลงสมัครไปแล้วในการเลือกตั้งครั้งนี้"
            : m.Contains("not a candidate") ? "ต้องลงสมัครที่ อบต. ก่อนจึงหาเสียงได้"
            : m.Contains("campaigned enough") ? "วันนี้หาเสียงครบ 3 ครั้งแล้ว"
            : m.Contains("voting not open") ? "ยังไม่ถึงวันเลือกตั้ง"
            : m.Contains("already voted") ? "ลงคะแนนตำแหน่งนี้ไปแล้ว"
            : m.Contains("not in office") ? "ต้องชนะเลือกตั้งก่อนจึงรับเงินเดือนได้"
            : m.Contains("salary already") ? "วันนี้รับเงินเดือนแล้ว"
            : m.Contains("busy") ? "คนใช้สิทธิ์เยอะ ลองใหม่อีกครั้ง"
            : "ทำรายการไม่สำเร็จ ลองใหม่";
    }
}
