using UnityEngine;

// People who stay put and watch: the crowd round a boxing ring (standing, cheering), the audience on
// the mats at the หนังกลางแปลง (sitting cross-legged), the projectionist at his machine, worshippers
// kneeling with their hands together before a Buddha. Each one is a villager model (CharacterModel)
// posed by SpectatorPose. They come out only when you are near (and, if set, during a festival or
// between opening hours), and a spot that turns out to be blocked by a wall or a stall is skipped.
public class Spectators : MonoBehaviour
{
    [SerializeField] private Vector3[] spots;       // local, on the ground
    [SerializeField] private float[] yaws;          // local, degrees
    [SerializeField] private int[] poses;           // SpectatorPose.Kind
    [SerializeField] private string festivalScale = "";
    [SerializeField] private int openHour, closeHour = 24;
    [SerializeField] private int seed = 1;
    [SerializeField] private string faithPlace = ""; // "church" / "mosque": out only during its service or prayer time (FaithClient)
    [SerializeField] private bool menOnly;           // the men's rows in the mosque

    private const float Near = 60f, Far = 75f;
    private GameObject crowd;
    private float nextCheck;

    public void Configure(Vector3[] at, float[] facing, SpectatorPose.Kind[] kinds, string festival, int open, int close, int randomSeed)
    {
        spots = at;
        yaws = facing;
        poses = new int[kinds.Length];
        for (int i = 0; i < kinds.Length; i++) poses[i] = (int)kinds[i];
        festivalScale = festival ?? "";
        openHour = open;
        closeHour = close;
        seed = randomSeed;
    }

    // Out only while the church's service / the mosque's prayer is on (faith.lua via FaithClient)
    public void DuringFaith(string place, bool men)
    {
        faithPlace = place ?? "";
        menOnly = men;
    }

    private bool ShouldBeOut()
    {
        if (faithPlace != "" && FaithClient.Instance?.Of(faithPlace)?.now == null) return false;
        if (festivalScale != "")
        {
            var f = FestivalClient.Instance;
            if (f == null || !f.OpenFor(festivalScale)) return false;
        }
        int h = GameClock.Hour;
        return openHour <= closeHour ? h >= openHour && h < closeHour : h >= openHour || h < closeHour;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 1.5f + (seed % 7) * 0.05f;
        var cam = Camera.main;
        float d = cam != null ? Vector3.Distance(cam.transform.position, transform.position) : float.MaxValue;
        bool want = ShouldBeOut() && (crowd != null ? d < Far : d < Near);
        if (want && crowd == null) Spawn();
        else if (!want && crowd != null) { Destroy(crowd); crowd = null; }
    }

    private void OnDisable()
    {
        if (crowd != null) { Destroy(crowd); crowd = null; }
    }

    private void Spawn()
    {
        crowd = new GameObject("People");
        crowd.transform.SetParent(transform, false);
        if (spots == null) return;
        for (int i = 0; i < spots.Length; i++)
        {
            var kind = poses != null && i < poses.Length ? (SpectatorPose.Kind)poses[i] : SpectatorPose.Kind.Watch;
            Vector3 ground = transform.TransformPoint(spots[i]);
            // a wall, a stall or a post where this one would stand: leave the spot empty
            if (Physics.CheckBox(ground + Vector3.up * 0.95f, new Vector3(0.22f, 0.7f, 0.22f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                continue;
            var go = new GameObject("Spectator");
            go.transform.SetParent(crowd.transform, false);
            go.transform.localPosition = spots[i];
            go.transform.localRotation = Quaternion.Euler(0f, yaws != null && i < yaws.Length ? yaws[i] : 0f, 0f);
            // life size even inside a scaled building (the church and mosque are built at 0.75)
            float parentScale = Mathf.Max(0.01f, crowd.transform.lossyScale.y);
            go.transform.localScale = Vector3.one / parentScale;
            bool standing = kind == SpectatorPose.Kind.Watch || kind == SpectatorPose.Kind.Cheer || kind == SpectatorPose.Kind.Projectionist;
            // a bare CharacterModel hangs its feet 1 unit below its origin: lift the standing ones
            if (standing) go.transform.position += go.transform.up * go.transform.lossyScale.y;
            var model = go.AddComponent<CharacterModel>();
            var look = Look.Random(seed * 131 + i * 17);
            if (menOnly) { look.Gender = 0; look.Outfit = 0; }
            model.SetLook(look);
            model.Seated = true; // stays in the idle pose (no walk cycle when it is lifted into place)
            go.AddComponent<SpectatorPose>().Configure(kind, ground.y, seed + i * 3);
        }
    }
}
