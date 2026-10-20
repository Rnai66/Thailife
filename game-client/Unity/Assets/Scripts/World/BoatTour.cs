using System;
using System.Collections;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Beach job (beach_jobs.lua): take tourists round the bay in the longtail boat. Stand on the pier by
// the boat to start; you sit at the stern while the boat follows its route, and get the fare plus a
// tip when it docks again. The server times the trip, so it can't be skipped.
[RequireComponent(typeof(Collider))]
public class BoatTour : MonoBehaviour
{
    [SerializeField] private Transform boat;
    [SerializeField] private Transform seat;
    [SerializeField] private Vector3[] route;

    private const float TripSeconds = 40f;

    private PlayerController rider;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private bool busy;
    private SeatedPose pose;

    [Serializable] private class FinishResult { public int fare, tip, earned; public string done; }

    public void Configure(Transform boatRoot, Transform seatPoint, Vector3[] waypoints)
    {
        boat = boatRoot;
        seat = seatPoint;
        route = waypoints;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null && rider == null) Offer(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && rider == null) ContextButton.Hide(this);
    }

    private void Offer(PlayerController player, string message = null)
    {
        ContextButton.Show(this, "พานักท่องเที่ยวนั่งเรือ",
            message ?? "ขับเรือหางยาวรอบอ่าว · ใช้พลังงาน 10 · ค่าเรือ 180 บาท + ทิป", () => StartTour(player));
    }

    private static NakamaConnection Connection()
    {
        var conn = FindAnyObjectByType<NakamaConnection>();
        return conn != null && conn.IsConnected ? conn : null;
    }

    private async void StartTour(PlayerController player)
    {
        var conn = Connection();
        if (busy || rider != null || boat == null || route == null || route.Length < 2) return;
        if (conn == null) { Offer(player, "ต้องออนไลน์ก่อน"); return; }
        busy = true;
        try
        {
            await conn.Client.RpcAsync(conn.Session, "tour_start", "{}");
            LifeClient.Instance?.Refresh();
            ContextButton.Hide(this);
            rider = player;
            standPosition = player.transform.position;
            standRotation = player.transform.rotation;
            player.InputEnabled = false;
            player.enabled = false;
            pose = player.gameObject.AddComponent<SeatedPose>();
            pose.HoldTiller = true; // the boatman sits by the engine, hand on the tiller
            StartCoroutine(Ride(conn));
        }
        catch (ApiResponseException e)
        {
            Offer(player, e.Message.Contains("energy") ? "พลังงานไม่พอ — พักก่อนแล้วค่อยออกเรือ" : "ออกเรือไม่ได้ ลองใหม่");
        }
        catch (Exception)
        {
            Offer(player, "ออกเรือไม่ได้ ลองใหม่");
        }
        busy = false;
    }

    private IEnumerator Ride(NakamaConnection conn)
    {
        Vector3 dockPosition = boat.position;
        Quaternion dockRotation = boat.rotation;
        float length = 0f;
        for (int i = 1; i < route.Length; i++) length += Vector3.Distance(route[i - 1], route[i]);
        float speed = length / TripSeconds;
        GameAudio.Play("seagull", boat.position + Vector3.up * 3f, 0.5f);

        for (int i = 1; i < route.Length; i++)
        {
            Vector3 from = route[i - 1], to = route[i];
            float leg = Vector3.Distance(from, to), t = 0f;
            Quaternion heading = Quaternion.LookRotation(new Vector3(to.x - from.x, 0f, to.z - from.z), Vector3.up);
            while (t < leg)
            {
                t += speed * Time.deltaTime;
                boat.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / leg));
                boat.rotation = Quaternion.Slerp(boat.rotation, heading, Time.deltaTime * 2f);
                Seat();
                yield return null;
            }
        }
        boat.SetPositionAndRotation(dockPosition, dockRotation);
        Seat();
        yield return Finish(conn);
    }

    // Keep the player sitting at the stern while the boat moves
    private void Seat()
    {
        if (rider != null && seat != null) rider.transform.SetPositionAndRotation(seat.position, seat.rotation);
    }

    private IEnumerator Finish(NakamaConnection conn)
    {
        var call = conn.Client.RpcAsync(conn.Session, "tour_finish", "{}");
        while (!call.IsCompleted) yield return null;

        var player = rider;
        rider = null;
        if (pose != null) Destroy(pose);
        player.enabled = true;
        player.InputEnabled = true;
        player.Teleport(standPosition, standRotation);

        if (call.Status == TaskStatus.RanToCompletion)
        {
            var r = JsonUtility.FromJson<FinishResult>(call.Result.Payload);
            UiKit.FloatAbovePlayer($"+{r.earned} ฿ (ทิป {r.tip})", new Color(0.4f, 1f, 0.5f));
            GameAudio.Play("coins");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Offer(player, r.done);
        }
        else Offer(player, "ส่งนักท่องเที่ยวไม่สำเร็จ ลองใหม่");
    }

    private void OnDestroy()
    {
        if (pose != null) Destroy(pose);
        if (rider != null)
        {
            rider.enabled = true;
            rider.InputEnabled = true;
        }
        ContextButton.Hide(this);
    }
}
