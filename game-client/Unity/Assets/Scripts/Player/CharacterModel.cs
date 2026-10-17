using UnityEngine;

// Swaps the placeholder capsule for the animated character (Resources/Characters/ThaiCharacter,
// made by Thai Life > Setup Character). Works for the local player and RemotePlayer avatars:
// walk/run speed is measured from how the object moves, actions come from ActionFeedback.
// Without the prefab (fresh clone, files not downloaded yet) the capsule simply stays.
public class CharacterModel : MonoBehaviour
{
    public const string PrefabPath = "Characters/ThaiCharacter";

    private static GameObject prefab;
    private static bool looked;

    private Animator animator;
    private Vector3 lastPosition;
    private float speed;

    public bool HasModel => animator != null;

    public static void Attach(GameObject host)
    {
        if (host.GetComponent<CharacterModel>() == null) host.AddComponent<CharacterModel>();
    }

    private void Awake()
    {
        if (!looked)
        {
            prefab = Resources.Load<GameObject>(PrefabPath);
            looked = true;
        }
        if (prefab == null) { enabled = false; return; }

        // Stand the feet on the bottom of the capsule
        float foot = -1f;
        var cc = GetComponent<CharacterController>();
        var capsule = GetComponent<CapsuleCollider>();
        if (cc != null) foot = cc.center.y - cc.height * 0.5f;
        else if (capsule != null) foot = capsule.center.y - capsule.height * 0.5f;

        var model = Instantiate(prefab, transform);
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
        }
        if (animator == null || animator.runtimeAnimatorController == null)
            Debug.LogWarning("[Character] Model has no Animator controller — run Thai Life > Setup Character (not in Play mode)");

        var placeholder = GetComponent<MeshRenderer>();
        if (placeholder != null) placeholder.enabled = false;
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
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
        }
        return true;
    }
}
