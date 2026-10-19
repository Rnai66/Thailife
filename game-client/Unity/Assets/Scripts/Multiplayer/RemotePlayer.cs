using Nakama;
using UnityEngine;

// Another player's avatar. Smoothly follows the latest position received from the network.
// Punches/kicks that land on it are forwarded to that player through MatchSync.
public class RemotePlayer : MonoBehaviour, IHittable
{
    [SerializeField] private float smoothing = 12f;
    [SerializeField] private float snapDistance = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;
    private MatchSync owner;
    private TextMesh nameLabel;
    private int health = -1;

    public IUserPresence Presence { get; private set; }
    public string DisplayName { get; private set; }
    public string UserId => Presence.UserId;
    public bool IsKnockedOut => health == 0;

    public static RemotePlayer Create(IUserPresence presence, Transform template, MatchSync owner)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Remote_{presence.Username}";
        // Trigger: punches can find it, but it doesn't block the local player's movement (yet)
        go.GetComponent<Collider>().isTrigger = true;

        // Same shader as the local player, different color per user
        var renderer = go.GetComponent<Renderer>();
        var templateRenderer = template != null ? template.GetComponent<Renderer>() : null;
        var material = templateRenderer != null ? new Material(templateRenderer.sharedMaterial) : renderer.material;
        material.color = ColorFor(presence.UserId);
        renderer.sharedMaterial = material;

        go.AddComponent<ActionFeedback>();
        go.AddComponent<CharacterModel>();
        var remote = go.AddComponent<RemotePlayer>();
        remote.Presence = presence;
        remote.DisplayName = presence.Username; // replaced by the character name once looked up
        remote.owner = owner;
        remote.nameLabel = AddNameLabel(go.transform, presence.Username);
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

    public void ReceiveHit(HitInfo hit, Transform attacker)
    {
        if (IsKnockedOut || owner == null) return;
        owner.SendHit(this, hit); // the victim applies the damage and broadcasts their new health
    }

    public void SetHealth(int value)
    {
        if (health > value && gameObject.activeInHierarchy)
        {
            Color color = value == 0 ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.35f, 0.3f);
            FloatingText.Spawn(transform.position + Vector3.up * 1.6f, value == 0 ? "KO!" : $"-{health - value}", color);
        }

        health = value;
        RefreshLabel();
    }

    public void SetSkin(int skin)
    {
        var model = GetComponent<CharacterModel>();
        if (model != null) model.SetSkin(skin);
    }

    public void SetDisplayName(string name)
    {
        DisplayName = name;
        gameObject.name = $"Remote_{name}";
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        nameLabel.text = health >= 0 ? $"{DisplayName}\nHP {health}" : DisplayName;
    }

    private void Update()
    {
        if (!hasTarget) return;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        // Lie down while knocked out (placeholder for a knockdown animation)
        Quaternion pose = IsKnockedOut ? targetRotation * Quaternion.Euler(0f, 0f, 90f) : targetRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, pose, t);
    }

    private static Color ColorFor(string userId)
    {
        int hash = userId != null ? userId.GetHashCode() : 0;
        float hue = (hash & 0xFFFF) / 65535f;
        return Color.HSVToRGB(hue, 0.6f, 0.9f);
    }

    private static TextMesh AddNameLabel(Transform parent, string username)
    {
        var label = new GameObject("NameLabel");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        var text = label.AddComponent<TextMesh>();
        var font = ThaiFont.Font;
        text.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        text.text = username;
        text.characterSize = 0.08f;
        text.fontSize = 48;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;

        label.AddComponent<FaceCamera>();
        return text;
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
