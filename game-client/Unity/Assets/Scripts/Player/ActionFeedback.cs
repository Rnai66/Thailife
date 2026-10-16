using System.Collections;
using UnityEngine;

// Placeholder visuals for actions until real animations exist.
// The shapes mark where a punch/kick would land, which is also where hitboxes will go later.
// Used by both the local player and RemotePlayer avatars.
public class ActionFeedback : MonoBehaviour
{
    public static readonly Vector3 PunchOffset = new Vector3(0.25f, 0.4f, 0.8f);
    public static readonly Vector3 KickOffset = new Vector3(0f, -0.5f, 0.9f);
    private static readonly Vector3 ShieldOffset = new Vector3(0f, 0.2f, 0.6f);

    private Material punchMaterial;
    private Material kickMaterial;
    private GameObject shield;

    public void Play(PlayerAction action)
    {
        bool canAnimate = isActiveAndEnabled; // coroutines can't start on hidden avatars

        switch (action)
        {
            case PlayerAction.Punch:
                if (!canAnimate) break;
                StartCoroutine(Flash(PrimitiveType.Sphere, PunchOffset, Vector3.one * 0.35f, PunchMaterial, 0.15f));
                break;
            case PlayerAction.Kick:
                if (!canAnimate) break;
                StartCoroutine(Flash(PrimitiveType.Cube, KickOffset, new Vector3(0.3f, 0.3f, 0.6f), KickMaterial, 0.2f));
                break;
            case PlayerAction.BlockStart:
                Shield.SetActive(true);
                break;
            case PlayerAction.BlockEnd:
                if (shield != null) shield.SetActive(false);
                break;
            // Jump needs no extra visual: the position change is already visible
        }
    }

    private IEnumerator Flash(PrimitiveType shape, Vector3 localOffset, Vector3 scale, Material material, float duration)
    {
        var go = CreateShape(shape, "Fx_" + shape, localOffset, scale, material);
        yield return new WaitForSeconds(duration);
        Destroy(go);
    }

    private GameObject Shield
    {
        get
        {
            if (shield == null)
                shield = CreateShape(PrimitiveType.Cube, "Fx_Shield", ShieldOffset, new Vector3(1.1f, 1.2f, 0.12f),
                    CreateMaterial(new Color(0.3f, 0.6f, 1f)));
            return shield;
        }
    }

    private Material PunchMaterial => punchMaterial ??= CreateMaterial(new Color(1f, 0.85f, 0.2f));
    private Material KickMaterial => kickMaterial ??= CreateMaterial(new Color(1f, 0.35f, 0.2f));

    private GameObject CreateShape(PrimitiveType shape, string name, Vector3 localOffset, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>()); // visual only — must not push the CharacterController
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private Material CreateMaterial(Color color)
    {
        // Reuse this object's shader so it renders correctly under URP
        var own = GetComponent<Renderer>();
        var material = own != null && own.sharedMaterial != null
            ? new Material(own.sharedMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        return material;
    }

    private void OnDestroy()
    {
        if (punchMaterial != null) Destroy(punchMaterial);
        if (kickMaterial != null) Destroy(kickMaterial);
    }
}
