using UnityEngine;

// The meditation mat before the Buddha in the ubosot: walk onto it to sit (MeditateUI). The player is
// set down sitting พับเพียบ on `seat` (the cushion: the hips rest on it, legs folded to the side), and
// the incense and candle flames (`flames`) burn while the player meditates with incense lit.
[RequireComponent(typeof(Collider))]
public class MeditationSpot : MonoBehaviour
{
    [SerializeField] private Transform seat;
    [SerializeField] private GameObject flames;

    private PlayerController sitter;
    private Vector3 standPosition;
    private Quaternion standRotation;

    public bool Occupied => sitter != null;

    public void Configure(Transform seatPoint, GameObject incenseFlames)
    {
        seat = seatPoint;
        flames = incenseFlames;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        if (flames != null) flames.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null && sitter == null) Offer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null || sitter != null) return;
        ContextButton.Hide(this);
        MeditateUI.Hide();
    }

    public void Offer()
    {
        var t = TempleClient.Instance?.Current;
        string hint = t == null ? "ต้องออนไลน์ก่อน"
            : $"บารมี Lv {t.barami} · นั่งสมาธิ {t.meditate_seconds} วินาที · ทำวัตรเช้า/เย็นได้สองเท่า";
        ContextButton.Show(this, "นั่งสมาธิ", hint, () => MeditateUI.Open(this));
    }

    // Down onto the mat, facing the Buddha
    public void Sit(bool incense)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        sitter = player;
        standPosition = player.transform.position;
        standRotation = player.transform.rotation;
        player.InputEnabled = false;
        if (seat != null)
        {
            // Lower the body so the hips (not the feet) are on the cushion
            var animator = player.GetComponentInChildren<Animator>();
            var hips = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            float hipHeight = hips != null ? hips.position.y - player.transform.position.y : 0.9f;
            player.Teleport(seat.position - Vector3.up * (hipHeight - 0.12f), seat.rotation);
            player.enabled = false; // hold still on the mat
        }
        var pose = player.GetComponent<SeatedPose>() ?? player.gameObject.AddComponent<SeatedPose>();
        pose.Kneel = true;
        if (flames != null) flames.SetActive(incense);
        ContextButton.Hide(this);
    }

    public void Stand()
    {
        if (flames != null) flames.SetActive(false);
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
