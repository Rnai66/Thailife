using UnityEngine;
using UnityEngine.UI;

// Greetings window (ทักทาย), premium layout: ไหว้ and โบกมือ on top, preset phrases below.
// Everything is visible to the other players nearby (MatchSync).
public class EmoteUI : MonoBehaviour
{
    private static EmoteUI instance;

    private GameObject backdrop;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("EmoteUI").AddComponent<EmoteUI>();
        instance.backdrop.SetActive(true);
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        bool P = UiKit.Portrait; // upright: the same two columns, narrower
        var body = UiKit.PremiumWindow(transform, "EmoteCanvas", UiKit.WindowSize(new Vector2(1200f, 900f), 1150f), 55, "ท",
            out backdrop, out var title, out var subtitle, Close, out _);
        title.text = "ทักทาย";
        subtitle.text = "ผู้เล่นที่อยู่ใกล้ๆ จะเห็นท่าทางและข้อความของคุณ";

        UiKit.RoundButton(body, "ไหว้ สวัสดี", new Vector2(P ? -172f : -280f, P ? 400f : 250f), new Vector2(P ? 335f : 520f, 120f), UiKit.Gold, UiKit.GoldInk,
            () => Do(PlayerAction.Wai, QuickChat.Hello), 40);
        UiKit.RoundButton(body, "โบกมือ", new Vector2(P ? 172f : 280f, P ? 400f : 250f), new Vector2(P ? 335f : 520f, 120f), UiKit.Teak, UiKit.GoldText,
            () => Do(PlayerAction.Wave, -1), 40);

        UiKit.Label(body, 26, "ข้อความสั้น", new Vector2(P ? -190f : -430f, P ? 300f : 150f), new Vector2(300f, 40f), UiKit.Muted, TextAnchor.MiddleLeft);
        for (int i = 1; i < QuickChat.Phrases.Length; i++)
        {
            int phrase = i, slot = i - 1;
            var pos = new Vector2(slot % 2 == 0 ? (P ? -172f : -280f) : (P ? 172f : 280f), (P ? 225f : 75f) - (slot / 2) * 100f);
            var b = UiKit.Panel("Phrase", body, Color.white, pos, new Vector2(P ? 335f : 520f, 84f), shadow: true);
            b.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
            var button = b.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => Do(null, phrase));
            b.gameObject.AddComponent<PressScale>();
            UiKit.Label(b, 32, QuickChat.Phrases[i], Vector2.zero, new Vector2(P ? 315f : 500f, 76f), UiKit.Ink).fontStyle = FontStyle.Bold;
        }
        backdrop.SetActive(false);
    }

    private void Do(PlayerAction? action, int phrase)
    {
        Close();
        if (action.HasValue)
        {
            var actions = FindAnyObjectByType<PlayerActions>();
            if (actions != null) actions.Emote(action.Value);
        }
        if (phrase >= 0)
        {
            if (MatchSync.Instance != null) MatchSync.Instance.SendChat(phrase);
            else
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) SpeechBubble.Show(player.transform, QuickChat.Get(phrase));
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
