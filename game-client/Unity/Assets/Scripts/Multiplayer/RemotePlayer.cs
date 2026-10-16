using Nakama;
using UnityEngine;

// Another player's avatar. Smoothly follows the latest position received from the network.
public class RemotePlayer : MonoBehaviour
{
    [SerializeField] private float smoothing = 12f;
    [SerializeField] private float snapDistance = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;

    public string UserId { get; private set; }

    public static RemotePlayer Create(IUserPresence presence, Transform template)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Remote_{presence.Username}";
        Destroy(go.GetComponent<Collider>()); // remotes don't block the local player (yet)

        // Same shader as the local player, different color per user
        var renderer = go.GetComponent<Renderer>();
        var templateRenderer = template != null ? template.GetComponent<Renderer>() : null;
        var material = templateRenderer != null ? new Material(templateRenderer.sharedMaterial) : renderer.material;
        material.color = ColorFor(presence.UserId);
        renderer.sharedMaterial = material;

        AddNameLabel(go.transform, presence.Username);

        go.AddComponent<ActionFeedback>();
        var remote = go.AddComponent<RemotePlayer>();
        remote.UserId = presence.UserId;
        go.SetActive(false); // hidden until the first position arrives
        return remote;
    }

    public void SetTarget(Vector3 position, float yaw)
    {
        targetPosition = position;
        targetRotation = Quaternion.Euler(0f, yaw, 0f);

        if (!hasTarget || (transform.position - position).sqrMagnitude > snapDistance * snapDistance)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            hasTarget = true;
            gameObject.SetActive(true);
        }
    }

    public void PlayAction(PlayerAction action)
    {
        GetComponent<ActionFeedback>().Play(action);
    }

    private void Update()
    {
        if (!hasTarget) return;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    private static Color ColorFor(string userId)
    {
        int hash = userId != null ? userId.GetHashCode() : 0;
        float hue = (hash & 0xFFFF) / 65535f;
        return Color.HSVToRGB(hue, 0.6f, 0.9f);
    }

    private static void AddNameLabel(Transform parent, string username)
    {
        var label = new GameObject("NameLabel");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        var text = label.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        text.text = username;
        text.characterSize = 0.08f;
        text.fontSize = 48;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;

        label.AddComponent<FaceCamera>();
    }
}

// Keeps a world-space label turned toward the main camera.
public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
