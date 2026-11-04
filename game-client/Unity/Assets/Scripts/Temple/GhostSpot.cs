using UnityEngine;

// A haunted spot in the cemetery behind the temple or out in the community / tambon forest. At night
// (19:00–05:00) its ghost (`model`) drifts there until a หมอผี banishes it for the night (ExorciseUI).
// Anyone can see it; only a หมอผี can face it.
[RequireComponent(typeof(Collider))]
public class GhostSpot : MonoBehaviour
{
    [SerializeField] private string spot = "cem1";
    [SerializeField] private string ghost = "krasue";
    [SerializeField] private Transform model;

    private bool near, shown = true;
    private float nextCheck, phase;
    private Vector3 home;
    private Transform player;

    public string Spot => spot;
    public string Ghost => ghost;

    public void Configure(string spotId, string ghostId, Transform ghostModel)
    {
        spot = spotId;
        ghost = ghostId;
        model = ghostModel;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        if (model != null) home = model.localPosition;
        phase = Random.value * 10f;
        Show(false);
    }

    public static bool Night
    {
        get
        {
            float m = GameClock.MinuteOfDay;
            return m >= 1140f || m < 300f;
        }
    }

    public bool Out => Night && TempleClient.Instance != null && TempleClient.Instance.Current != null && !TempleClient.Instance.Banished(spot);

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 1f;
            Show(Out);
            if (near) Offer();
        }
        if (!shown || model == null) return;
        // drift: rise and fall, sway, and turn to stare at the player
        float t = Time.time + phase;
        model.localPosition = home + new Vector3(Mathf.Sin(t * 0.7f) * 0.25f, Mathf.Sin(t * 1.6f) * 0.18f, Mathf.Cos(t * 0.5f) * 0.2f);
        if (player == null) { var p = FindAnyObjectByType<PlayerController>(); if (p != null) player = p.transform; }
        if (player != null)
        {
            Vector3 to = player.position - model.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(-to) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 6f), Time.deltaTime * 2f);
        }
    }

    private void Show(bool on)
    {
        if (on == shown || model == null) return;
        shown = on;
        model.gameObject.SetActive(on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        near = true;
        if (Out) GameAudio.Play("ghost", transform.position);
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
        var t = TempleClient.Instance?.Current;
        if (t == null || !t.exorcist)
        {
            if (Out) ContextButton.Show(this, "ขนลุก…", "มีผีสิงอยู่แถวนี้ · ต้องเป็นหมอผีจึงปราบได้ (บวชจนพระเลเวล 30 แล้วสึกออกมา)", null, enabled: false);
            else ContextButton.Hide(this);
            return;
        }
        var info = TempleClient.Instance.GhostInfo(ghost);
        string name = info?.name ?? "ผี";
        if (!Night) { ContextButton.Show(this, "ปราบผี", $"ที่นี่มี{name}สิงอยู่ · ผีออกตอนกลางคืน 19:00-05:00", null, enabled: false); return; }
        if (TempleClient.Instance.Banished(spot)) { ContextButton.Show(this, "ปราบผี", $"ปราบ{name}ที่นี่ไปแล้วคืนนี้", null, enabled: false); return; }
        bool able = info == null || t.exorcist_level >= info.level;
        string hint = able ? $"{name} · ค่าปราบ {info?.baht ?? 0:N0} บาท · ใช้พลังงาน · ท่องคาถาแตะยันต์ให้ทัน"
                           : $"{name} ดุเกินไป · ต้องหมอผีเลเวล {info.level} (ตอนนี้ {t.exorcist_level})";
        ContextButton.Show(this, $"ปราบ{name}", hint, () => ExorciseUI.Show(this), able, highlight: able);
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
