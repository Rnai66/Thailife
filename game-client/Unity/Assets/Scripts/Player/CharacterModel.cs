using UnityEngine;

// Swaps the placeholder capsule for an animated character (Resources/Characters/Skin_N, made by
// Thai Life > Setup Character). Works for the local player and RemotePlayer avatars:
// walk/run speed is measured from how the object moves, actions come from ActionFeedback.
// Without the prefabs (fresh clone, files not downloaded yet) the capsule simply stays.
public class CharacterModel : MonoBehaviour
{
    public const string PrefabPath = "Characters/ThaiCharacter";
    private const int MaxSkins = 8;

    private static int skinCount = -1;

    private Animator animator;
    private GameObject model;
    private Vector3 lastPosition;
    private float speed;
    private int skin = -1;
    private string lookCode;
    private ProfileClient profile;

    public Look CurrentLook { get; private set; }

    public bool HasModel => animator != null;

    // How many looks Setup Character made (Skin_0, Skin_1, ...)
    public static int SkinCount
    {
        get
        {
            if (skinCount < 0)
            {
                skinCount = 0;
                while (skinCount < MaxSkins && Resources.Load<GameObject>(SkinPath(skinCount)) != null) skinCount++;
            }
            return skinCount;
        }
    }

    // Domain reload is off in this project (Enter Play Mode Options), so statics survive between
    // Play sessions: count again every time, e.g. after running Setup Character
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => skinCount = -1;

    private static string SkinPath(int index) => $"Characters/Skin_{index}";

    public static void Attach(GameObject host)
    {
        if (host.GetComponent<CharacterModel>() == null) host.AddComponent<CharacterModel>();
    }

    private void Awake()
    {
        if (model == null) SetSkin(0);
        lastPosition = transform.position;
    }

    private void Start()
    {
        // The local player follows the look saved in the profile
        if (GetComponent<PlayerController>() == null) return;
        profile = ProfileClient.Instance;
        if (profile == null) return;
        profile.LookChanged += SetLook;
        SetLook(profile.Look);
    }

    private void OnDestroy()
    {
        if (profile != null) profile.LookChanged -= SetLook;
    }

    // NPCs: one of the body models as it comes
    public void SetSkin(int index) => SetLook(Look.FromSkin(index));

    // Body model plus hairstyle and clothes colours (CharacterLook)
    public void SetLook(Look look)
    {
        if (look.Code == lookCode && model != null) return;
        int index = look.Skin;
        var prefab = Resources.Load<GameObject>(SkinPath(index))
                     ?? Resources.Load<GameObject>(SkinPath(0))
                     ?? Resources.Load<GameObject>(PrefabPath);
        if (prefab == null) return; // keep the capsule
        skin = index;
        lookCode = look.Code;
        CurrentLook = look;

        bool blocking = animator != null && animator.GetBool("Block");
        if (model != null) Destroy(model);

        // Stand the feet on the bottom of the capsule
        float foot = -1f;
        var cc = GetComponent<CharacterController>();
        var capsule = GetComponent<CapsuleCollider>();
        if (cc != null) foot = cc.center.y - cc.height * 0.5f;
        else if (capsule != null) foot = capsule.center.y - capsule.height * 0.5f;

        model = Instantiate(prefab, transform);
        model.name = "Model";
        model.transform.localPosition = new Vector3(0f, foot, 0f);
        model.transform.localRotation = Quaternion.identity;

        animator = model.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(PrefabPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (blocking) animator.SetBool("Block", true);
        }
        if (animator == null || animator.runtimeAnimatorController == null)
            Debug.LogWarning("[Character] Model has no Animator controller — run Thai Life > Setup Character (not in Play mode)");

        CharacterLook.Apply(model, skin, look);

        var placeholder = GetComponent<MeshRenderer>();
        if (placeholder != null) placeholder.enabled = false;
    }

    // Sitting (on a boat that moves under us): stay in the idle pose instead of walking
    public bool Seated { get; set; }

    private void Update()
    {
        if (animator == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        if (Seated)
        {
            speed = 0f;
            animator.SetFloat("Speed", 0f);
            return;
        }
        delta.y = 0f;
        float measured = Mathf.Min(delta.magnitude / dt, 8f); // teleports/snaps don't spike the run
        speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-10f * dt));
        animator.SetFloat("Speed", speed < 0.05f ? 0f : speed);
    }

    // Returns false when there is no model (ActionFeedback then shows its placeholder shapes)
    public bool Play(PlayerAction action)
    {
        if (animator == null || !isActiveAndEnabled) return false;
        switch (action)
        {
            case PlayerAction.Punch: animator.SetTrigger("Punch"); break;
            case PlayerAction.Kick: animator.SetTrigger("Kick"); break;
            case PlayerAction.Jump: animator.SetTrigger("Jump"); break;
            case PlayerAction.BlockStart: animator.SetBool("Block", true); break;
            case PlayerAction.BlockEnd: animator.SetBool("Block", false); break;
            case PlayerAction.Wai: Trigger("Wai"); break;
            case PlayerAction.Wave: Trigger("Wave"); break;
        }
        return true;
    }

    // Wai/Wave clips are optional in Setup Character
    private void Trigger(string name)
    {
        foreach (var p in animator.parameters)
            if (p.name == name) { animator.SetTrigger(name); return; }
    }
}
