using System;
using UnityEngine;

// โบกตุ๊กตุ๊ก / เรียกแท็กซี่: a tuk-tuk or taxi going round the town (a TrafficCar on a closed loop) can be waved down.
// Stand by the road as one comes past and press "โบกตุ๊กตุ๊ก": it pulls up beside you, you pay the
// flat fare (travel.lua tuktuk_ride) and sit on the back bench while it carries on round its route —
// the ring road, the station road, round the clock tower. Press "ลงรถ" when you want to get off; it
// pulls in and you step down on the kerb side.
[RequireComponent(typeof(TrafficCar))]
public class TukTukHail : MonoBehaviour
{
    private const float HailRange = 9f, BoardRange = 12f, GiveUpRange = 28f;
    private const string Pitch = "เที่ยวรอบเมือง · ค่ารถ 40 บาท\nกด \"ลงรถ\" ได้ทุกเมื่อ";
    private const string TaxiPitch = "แท็กซี่เที่ยวรอบเมือง · ค่าโดยสาร 60 บาท\nกด \"ลงรถ\" ได้ทุกเมื่อ";
    // the taxi's back seat, kerb side (CityTraffic.Taxi; the tuk-tuk's is TukTukRider.BenchSeat)
    private static readonly Vector3 TaxiSeat = new Vector3(-0.38f, 0.62f, -0.6f);

    [SerializeField] private bool taxi;

    public void Configure(bool isTaxi) => taxi = isTaxi;

    private enum State { Free, Coming, Paying, Riding, Leaving }

    private static TukTukHail offered, busy; // the one showing its button / the one you're dealing with

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { offered = null; busy = null; }

    private TrafficCar car;
    private TukTukRider crew;
    private PlayerController player;
    private SeatedPose pose;
    private Vector3 hips;
    private State state;
    private float nextCheck, quietUntil, waitSince;

    private void Awake()
    {
        car = GetComponent<TrafficCar>();
        crew = GetComponent<TukTukRider>();
    }

    private float FlatDistance()
    {
        var d = player.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private void Update()
    {
        if (state == State.Riding) return;
        if (state == State.Leaving)
        {
            if (car.IsStopped) GetOff();
            return;
        }
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.2f;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        if (state == State.Coming)
        {
            float d = FlatDistance();
            if (d > GiveUpRange || Time.time - waitSince > 25f) { Cancel(); return; }
            if (car.IsStopped && d < BoardRange) Board();
            return;
        }
        if (state != State.Free) return;

        // Offer a ride when one comes close (the nearest one, and never over another button)
        bool can = busy == null && Time.time > quietUntil && player.enabled && player.InputEnabled;
        float dist = can ? FlatDistance() : float.MaxValue;
        if (can && dist < HailRange && !ContextButton.TakenByOther(this)
            && (offered == null || offered == this || offered.player == null || dist < offered.FlatDistance()))
        {
            if (offered != null && offered != this) ContextButton.Hide(offered);
            offered = this;
            ContextButton.Show(this, taxi ? "เรียกแท็กซี่" : "โบกตุ๊กตุ๊ก", taxi ? TaxiPitch : Pitch, Hail);
        }
        else if (offered == this && dist > HailRange + 2f)
        {
            ContextButton.Hide(this);
            offered = null;
        }
    }

    private void Hail()
    {
        if (busy != null || state != State.Free || player == null) return;
        busy = this;
        offered = null;
        state = State.Coming;
        waitSince = Time.time;
        car.StopNear(player.transform.position);
        ContextButton.Show(this, taxi ? "รอแท็กซี่..." : "รอตุ๊กตุ๊ก...", taxi ? "แท็กซี่กำลังจอดรับ" : "ตุ๊กตุ๊กกำลังจอดรับ", null, enabled: false);
    }

    private void Cancel()
    {
        car.Release();
        state = State.Free;
        if (busy == this) busy = null;
        ContextButton.Hide(this);
        quietUntil = Time.time + 3f;
    }

    private async void Board()
    {
        state = State.Paying;
        ContextButton.Show(this, "จ่ายค่ารถ...", null, null, enabled: false);
        try
        {
            int fare = await TravelClient.TukTukAsync(taxi);
            if (this == null || player == null) return;
            SitDown();
            UiKit.FloatAbovePlayer(taxi ? $"ขึ้นแท็กซี่ · ค่าโดยสาร {fare} บาท" : $"ขึ้นตุ๊กตุ๊ก · ค่ารถ {fare} บาท", UiKit.GoldText);
            GameAudio.Play("coins");
        }
        catch (Exception e)
        {
            if (this == null) return;
            string why = e.Message.Contains("not enough baht") ? (taxi ? "เงินไม่พอค่าแท็กซี่ 60 บาท" : "เงินไม่พอค่าตุ๊กตุ๊ก 40 บาท")
                : e.Message.Contains("offline") ? "ต้องออนไลน์ก่อนเรียกรถ" : "เรียกรถไม่สำเร็จ ลองใหม่";
            UiKit.FloatAbovePlayer(why, new Color(1f, 0.5f, 0.4f));
            Cancel();
        }
    }

    private void SitDown()
    {
        hips = SeatedPose.HipsOffset(player.transform); // measured standing
        player.InputEnabled = false;
        player.enabled = false;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // or the tuk-tuk would brake for its own passenger
        pose = player.gameObject.AddComponent<SeatedPose>();
        if (crew != null) crew.ShowPassengers(false);
        state = State.Riding;
        Seat();
        car.Release();
        ContextButton.Show(this, "ลงรถ", taxi ? "นั่งแท็กซี่เที่ยวรอบเมือง · กดเพื่อลงรถ" : "นั่งตุ๊กตุ๊กเที่ยวรอบเมือง · กดเพื่อลงรถ", AskToStop, highlight: true);
    }

    // Keep the player on the back bench as the tuk-tuk moves
    private void LateUpdate()
    {
        if (state == State.Riding || state == State.Leaving) Seat();
    }

    private void Seat()
    {
        if (player == null) return;
        var rot = transform.rotation;
        player.transform.SetPositionAndRotation(
            SeatedPose.RootFor(transform.TransformPoint((taxi ? TaxiSeat : TukTukRider.BenchSeat) + Vector3.up * 0.05f), rot, hips), rot);
    }

    private void AskToStop()
    {
        if (state != State.Riding) return;
        state = State.Leaving;
        car.StopSoon();
        ContextButton.Show(this, "กำลังจอด...", null, null, enabled: false);
    }

    // Step down on the kerb side (traffic keeps left: the left of the tuk-tuk)
    private void GetOff()
    {
        Vector3 feet = transform.position - transform.right * 1.7f;
        foreach (var hit in Physics.RaycastAll(feet + Vector3.up * 3f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(transform) && hit.point.y > feet.y - 1f) feet.y = Mathf.Max(feet.y, hit.point.y);
        Restore(feet);
        UiKit.FloatAbovePlayer("ลงรถแล้ว · ขอบคุณที่ใช้บริการ", UiKit.GoldText);
        Invoke(nameof(DriveOn), 1.2f);
    }

    private void DriveOn() => car.Release();

    private void Restore(Vector3 feet)
    {
        if (pose != null) Destroy(pose);
        pose = null;
        if (crew != null) crew.ShowPassengers(true);
        if (player != null)
        {
            player.enabled = true;
            player.InputEnabled = true;
            player.Teleport(feet + Vector3.up * 1.02f, Quaternion.LookRotation(transform.forward));
        }
        state = State.Free;
        if (busy == this) busy = null;
        ContextButton.Hide(this);
        quietUntil = Time.time + 5f;
    }

    private void OnDisable()
    {
        if (state == State.Riding || state == State.Leaving) Restore(transform.position - transform.right * 1.7f);
        else if (state != State.Free) Cancel();
        if (offered == this) { ContextButton.Hide(this); offered = null; }
    }
}
