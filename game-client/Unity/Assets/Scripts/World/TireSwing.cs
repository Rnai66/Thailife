using UnityEngine;

// Tire swing on a leaning palm (ชิงช้ายางรถ): sit in the tyre and swing out over the water.
// The rope pivot rocks back and forth and the player rides the seat; the button gets you off.
[RequireComponent(typeof(Collider))]
public class TireSwing : MonoBehaviour
{
    [SerializeField] private Transform pivot;   // top of the rope, rotates about its local X
    [SerializeField] private Transform seat;    // where the rider's feet go (moves with the pivot)
    [SerializeField] private float amplitude = 32f;

    private PlayerController rider;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private SeatedPose pose;
    private float swingTime, swingEnergy;

    public void Configure(Transform ropePivot, Transform seatPoint)
    {
        pivot = ropePivot;
        seat = seatPoint;
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

    private void Offer(PlayerController player) =>
        ContextButton.Show(this, "นั่งชิงช้า", "ชิงช้ายางรถใต้ต้นมะพร้าว แกว่งออกไปเหนือน้ำทะเล", () => Sit(player));

    private void Sit(PlayerController player)
    {
        if (rider != null || pivot == null || seat == null) return;
        rider = player;
        standPosition = player.transform.position;
        standRotation = player.transform.rotation;
        player.InputEnabled = false;
        player.enabled = false;
        pose = player.gameObject.AddComponent<SeatedPose>();
        swingTime = 0f;
        swingEnergy = 0f;
        GameAudio.Play("whoosh", null, 0.5f);
        ContextButton.Show(this, "ลงจากชิงช้า", "แกว่งไกวรับลมทะเล~", GetOff, highlight: true);
    }

    private void GetOff()
    {
        if (rider == null) return;
        if (pose != null) Destroy(pose);
        rider.enabled = true;
        rider.InputEnabled = true;
        rider.Teleport(standPosition, standRotation);
        var player = rider;
        rider = null;
        Offer(player);
    }

    private void Update()
    {
        if (pivot == null) return;
        if (rider != null)
        {
            // Pump up to full swing over a few seconds
            swingEnergy = Mathf.MoveTowards(swingEnergy, 1f, Time.deltaTime * 0.3f);
            swingTime += Time.deltaTime;
            pivot.localRotation = Quaternion.Euler(Mathf.Sin(swingTime * 1.9f) * amplitude * swingEnergy, 0f, 0f);
        }
        else
        {
            // Settle back to rest, swaying a little in the breeze
            swingEnergy = Mathf.MoveTowards(swingEnergy, 0f, Time.deltaTime * 0.5f);
            swingTime += Time.deltaTime;
            float breeze = Mathf.Sin(Time.time * 0.8f) * 3f;
            pivot.localRotation = Quaternion.Euler(Mathf.Sin(swingTime * 1.9f) * amplitude * swingEnergy + breeze, 0f, 0f);
        }
    }

    private void LateUpdate()
    {
        if (rider == null || seat == null) return;
        var cc = rider.GetComponent<CharacterController>();
        float foot = cc != null ? cc.center.y - cc.height * 0.5f : -1f;
        rider.transform.SetPositionAndRotation(seat.position - Vector3.up * foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(seat.forward, Vector3.up), Vector3.up));
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
