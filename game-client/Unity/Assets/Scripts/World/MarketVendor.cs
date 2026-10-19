using UnityEngine;

// A stall keeper (แม่ค้า/พ่อค้า) standing behind a market table, idling. Uses one of the
// character looks from Setup Character; without them the stall is simply unattended.
public class MarketVendor : MonoBehaviour
{
    [SerializeField] private int look;

    public void Configure(int lookIndex) => look = lookIndex;

    private void Start()
    {
        int count = CharacterModel.SkinCount;
        if (count == 0) return;
        var prefab = Resources.Load<GameObject>($"Characters/Skin_{look % count}");
        if (prefab == null) return;
        var model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        var animator = model.GetComponentInChildren<Animator>();
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null)
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(CharacterModel.PrefabPath);
        animator.applyRootMotion = false;
        animator.SetFloat("Speed", 0f);
        animator.cullingMode = AnimatorCullingMode.CullCompletely; // nobody needs them animating off-screen
    }
}
