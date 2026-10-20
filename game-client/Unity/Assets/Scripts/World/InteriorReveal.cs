using UnityEngine;

// Walk-in building: while the player is inside, the upper floor and roof stop drawing (they still
// cast their shadow) so the third-person camera can see into the rooms; they come back on leaving.
[RequireComponent(typeof(Collider))]
public class InteriorReveal : MonoBehaviour
{
    [SerializeField] private Transform hide; // upper storey + roof

    private Renderer[] renderers;
    private UnityEngine.Rendering.ShadowCastingMode[] modes;
    private int inside;

    public void Configure(Transform upper) => hide = upper;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake()
    {
        if (hide == null) return;
        renderers = hide.GetComponentsInChildren<Renderer>();
        modes = new UnityEngine.Rendering.ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) modes[i] = renderers[i].shadowCastingMode;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (inside++ == 0) Set(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (--inside <= 0) { inside = 0; Set(false); }
    }

    private void Set(bool reveal)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (renderers[i] is MeshRenderer && renderers[i].GetComponent<TextMesh>() != null)
                renderers[i].enabled = !reveal; // text can't be shadows-only
            else
                renderers[i].shadowCastingMode = reveal ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : modes[i];
        }
    }

    private void OnDisable() => Set(false);
}
