using System;
using UnityEngine;

// Fades a window away by itself so nothing stays stuck on screen: when the player walks away from
// where it was opened (every PremiumWindow), or after a few seconds (result pop-ups, see
// UiKit.AutoFade). Closing goes through the window's own close action, so its cleanup still runs.
[RequireComponent(typeof(CanvasGroup))]
public class WindowAutoClose : MonoBehaviour
{
    private const float WalkAwayMeters = 3.5f;
    private const float FadeSeconds = 0.35f;

    private Action close;
    private float lifetime;
    private bool walkAway = true;
    private CanvasGroup group;
    private Transform player;
    private Vector3 openedAt;
    private float openedTime, fadeStart = -1f;

    public void Configure(Action onClose, float seconds = 0f, bool closeOnWalkAway = true)
    {
        close = onClose;
        lifetime = seconds;
        walkAway = closeOnWalkAway;
    }

    private void Awake() => group = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        group.alpha = 1f;
        fadeStart = -1f;
        openedTime = Time.unscaledTime;
        var pc = FindAnyObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;
        if (player != null) openedAt = player.position;
    }

    private void Update()
    {
        if (fadeStart < 0f)
        {
            bool expired = lifetime > 0f && Time.unscaledTime - openedTime > lifetime;
            bool left = walkAway && player != null && (player.position - openedAt).sqrMagnitude > WalkAwayMeters * WalkAwayMeters;
            if (expired || left) fadeStart = Time.unscaledTime;
            return;
        }
        float k = (Time.unscaledTime - fadeStart) / FadeSeconds;
        group.alpha = 1f - Mathf.Clamp01(k);
        if (k < 1f) return;
        fadeStart = -1f;
        group.alpha = 1f;
        if (close != null) close();
        else gameObject.SetActive(false);
    }
}
