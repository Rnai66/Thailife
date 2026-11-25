using UnityEngine;

// The hotel in the City (built by the editor's HotelBiz), for the player who runs a hotel business
// (ธนาคาร → ธุรกิจ → โรงแรมเล็ก): standing in the lobby offers "บริหารโรงแรม" (HotelManageUI — staff,
// the takings, decorating the lobby, up to the penthouse on the roof to rest). The lobby itself — the
// name over the door, the furniture, receptionists and guests — is shown by the CafeSite (kind "hotel")
// on the same building. A second hotel business (ธนาคาร lets you run up to three businesses) is the
// hotel on the ring road (HotelBiz.BuildSecond, index 1): a ready-furnished lobby, managed the same way.
public class HotelSite : MonoBehaviour
{
    [SerializeField] private float halfW = 5.8f, halfD = 4.3f, height = 2.4f; // the lobby, metres
    [SerializeField] private int index;                                        // 0 = your first hotel, 1 = the second

    private Transform room, player;
    private BankClient bank;
    private bool hadButton;
    private int buttonStaff = -1;

    public static HotelSite Instance { get; private set; } // the first hotel (lobby decorated, penthouse)
    public static HotelSite Second { get; private set; }   // the second, on the ring road
    public static HotelSite Of(int which) => which == 1 ? Second : Instance;
    public int Index => index;
    public string Title => index == 1 ? Lang.T("โรงแรมของคุณ แห่งที่ 2") : Lang.T("โรงแรมของคุณ");

    // This player's hotel business for this building (their first or second hotel), or null
    public BankClient.Business Mine
    {
        get
        {
            if (bank == null) bank = BankClient.Instance;
            var s = bank != null ? bank.Current : null;
            if (s?.businesses == null) return null;
            int n = 0;
            foreach (var b in s.businesses)
                if (b.type == "hotel" && n++ == index) return b;
            return null;
        }
    }

    public void Configure(float lobbyHalfW, float lobbyHalfD, float lobbyHeight, int which = 0)
    {
        halfW = lobbyHalfW;
        halfD = lobbyHalfD;
        height = lobbyHeight;
        index = which;
    }

    private void Awake()
    {
        if (index == 1) Second = this;
        else Instance = this;
        room = transform.Find("Interior");
        if (room == null) room = transform;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Second == this) Second = null;
        if (hadButton) ContextButton.Hide(this);
    }

    // Is a world point inside the lobby?
    public bool Inside(Vector3 world)
    {
        var p = room.InverseTransformPoint(world) * room.lossyScale.x; // metres
        return Mathf.Abs(p.x) < halfW && Mathf.Abs(p.z) < halfD && p.y > -1f && p.y < height + 1f;
    }

    private void Update()
    {
        if (player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        var mine = Mine;
        bool offer = mine != null && Inside(player.position) && !HotelManageUI.IsOpen && !CafeUI.IsOpen && !ContextButton.TakenByOther(this);
        if (offer && (!ContextButton.IsShownBy(this) || buttonStaff != mine.staff))
        {
            buttonStaff = mine.staff;
            ContextButton.Show(this, "บริหารโรงแรม",
                $"{Title} · {Lang.T("พนักงาน")} {mine.staff} {Lang.T("คน")} · {Lang.T("กำไร/วัน")} ~{mine.per_day:N0} ฿",
                () => HotelManageUI.Open(this));
            hadButton = true;
        }
        else if (!offer && hadButton)
        {
            ContextButton.Hide(this);
            hadButton = false;
            buttonStaff = -1;
        }
    }
}
