using UnityEngine;

// Where you pray in the church (a kneeler at the front pew) or the mosque (a place in the first row,
// facing the mihrab): walk onto it and FaithUI opens — pray (อธิษฐาน / ละหมาด), light a candle (church),
// or give. While praying the player is set down on `seat` facing the way it faces.
[RequireComponent(typeof(Collider))]
public class PrayerSpot : MonoBehaviour
{
    [SerializeField] private string place = "church";
    [SerializeField] private Transform seat;
    [SerializeField] private GameObject candles;

    private PlayerController sitter;
    private Vector3 standPosition;
    private Quaternion standRotation;

    public string Place => place;

    public void Configure(string which, Transform seatPoint, GameObject candleFlames)
    {
        place = which;
        seat = seatPoint;
        candles = candleFlames;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        if (candles != null) candles.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && sitter == null) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null || sitter != null) return;
        ContextButton.Hide(this);
        FaithUI.Hide();
    }

    public void Offer()
    {
        var p = FaithClient.Instance?.Of(place);
        bool mosque = place == "mosque";
        string hint = p == null ? "ต้องออนไลน์ก่อน"
            : p.now != null ? $"ตอนนี้: {p.now}" + (p.now_done ? " (ร่วมแล้ว)" : " · ร่วมได้ XP เพิ่ม")
            : p.next != null ? $"ครั้งหน้า: {p.next}" : "";
        ContextButton.Show(this, mosque ? "ละหมาด · บริจาค" : "อธิษฐาน · ถวายทรัพย์", hint, () => FaithUI.Open(this));
    }

    public void Sit(bool candle)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        sitter = player;
        standPosition = player.transform.position;
        standRotation = player.transform.rotation;
        player.InputEnabled = false;
        if (seat != null)
        {
            var animator = player.GetComponentInChildren<Animator>();
            var hips = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            float hipHeight = hips != null ? hips.position.y - player.transform.position.y : 0.9f;
            player.Teleport(seat.position - Vector3.up * (hipHeight - 0.12f), seat.rotation);
            player.enabled = false;
        }
        var pose = player.GetComponent<SeatedPose>() ?? player.gameObject.AddComponent<SeatedPose>();
        pose.Kneel = true;
        if (candles != null) candles.SetActive(candle);
        ContextButton.Hide(this);
    }

    public void Stand()
    {
        if (candles != null) candles.SetActive(false);
        var player = sitter;
        sitter = null;
        if (player == null) return;
        var pose = player.GetComponent<SeatedPose>();
        if (pose != null) Destroy(pose);
        player.enabled = true;
        player.InputEnabled = true;
        if (seat != null) player.Teleport(standPosition, standRotation);
        Offer();
    }

    private void OnDestroy()
    {
        if (sitter != null)
        {
            sitter.enabled = true;
            sitter.InputEnabled = true;
            var pose = sitter.GetComponent<SeatedPose>();
            if (pose != null) Destroy(pose);
        }
        ContextButton.Hide(this);
    }
}
