using UnityEngine;

// Someone at the controls of a farm machine (TambonFarm): seated on its seat with SeatedPose, hands on
// the wheel or the handlebars. Uses the character looks from Setup Character; without them it runs empty.
public class FarmDriver : MonoBehaviour
{
    [SerializeField] private Vector3 seat;   // the seat top in the machine's space, facing +Z
    [SerializeField] private int look;
    [SerializeField] private bool steer = true;

    public void Configure(Vector3 seatTop, int lookIndex, bool holdControls = true)
    {
        seat = seatTop;
        look = lookIndex;
        steer = holdControls;
    }

    private void Start()
    {
        if (CharacterModel.SkinCount == 0) return;
        var go = new GameObject("Driver");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = seat + Vector3.up;
        go.transform.localRotation = Quaternion.identity;
        var model = go.AddComponent<CharacterModel>();
        model.SetSkin(look % Mathf.Max(1, CharacterModel.SkinCount));
        var hips = SeatedPose.HipsOffset(go.transform);
        go.transform.position = SeatedPose.RootFor(transform.TransformPoint(seat + Vector3.up * 0.05f), transform.rotation, hips);
        go.AddComponent<SeatedPose>().HoldTiller = steer;
        var animator = go.GetComponentInChildren<Animator>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.CullCompletely;
    }
}
