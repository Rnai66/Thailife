using UnityEngine;
using UnityEngine.UI;

// Result card for each quest the auto mode finishes (การ์ดผลงาน): "ภารกิจสำเร็จ!", what was done with
// its picture, the baht it paid — and for fishing, a fish card for every fish that went into the bag.
// It pops up in the middle of the screen without blocking anything (auto carries on walking), and
// slides away after a few seconds or on a tap.
public class AutoQuestCard : MonoBehaviour
{
    private const float Duration = 4.2f;

    private static AutoQuestCard current;

    private CanvasGroup group;
    private RectTransform card;
    private float start;
    private bool closing;

    public static void Show(QuestClient.Quest quest)
    {
        if (quest == null) return;
        if (current != null) Destroy(current.gameObject);
        current = new GameObject("AutoQuestCard").AddComponent<AutoQuestCard>();
        DontDestroyOnLoad(current.gameObject);
        current.Build(quest);
    }

    private static string Icon(string kind) => kind switch
    {
        "fish" => "fishing", "sell_produce" => "market", "plant" => "seed_hommali", "harvest" => "sell_rice_hommali",
        "feed" => "chicken_feed", "sell_eggs" => "chicken_hen", "music" => "music", "train" => "boxing",
        "eat" => "food", "shop" => "shop", "study" => "school", _ => "community",
    };

    private void Build(QuestClient.Quest quest)
    {
        start = Time.unscaledTime;
        var canvasGo = new GameObject("CardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        var catches = quest.@catch ?? new QuestClient.FishCatch[0];
        bool fish = catches.Length > 0;
        float cardW = 400f, cardH = 250f, gap = 24f;
        Vector2 size = fish ? new Vector2(Mathf.Max(760f, catches.Length * (cardW + gap) + 60f), 560f) : new Vector2(760f, 420f);

        card = UiKit.WoodPlate(canvasGo.transform, new Vector2(0f, 60f), size);
        var tap = card.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => { if (Time.unscaledTime - start > 0.4f) Close(); });

        float top = size.y * 0.5f;
        var title = UiKit.Cartoon(UiKit.Label(card, 58, "ภารกิจสำเร็จ!", new Vector2(0f, top - 60f), new Vector2(size.x - 40f, 80f), UiKit.GoldText), 4f);
        title.raycastTarget = false;
        UiKit.Label(card, 30, quest.title, new Vector2(0f, top - 118f), new Vector2(size.x - 60f, 44f), UiKit.WoodInk).raycastTarget = false;

        if (fish)
        {
            float x0 = -(catches.Length - 1) * (cardW + gap) * 0.5f;
            for (int i = 0; i < catches.Length; i++)
            {
                var c = catches[i];
                FishCard.Make(card, c.id, c.name, c.kg, new Vector2(x0 + i * (cardW + gap), -10f), new Vector2(cardW, cardH));
            }
            UiKit.Label(card, 26, "ปลาเข้าข้องแล้ว · นำไปขายที่ร้านค้าได้", new Vector2(0f, -170f), new Vector2(size.x - 60f, 40f), UiKit.HeaderSub).raycastTarget = false;
        }
        else
        {
            var icon = UiKit.IconTile(card, Icon(quest.kind), new Vector2(0f, -30f), new Vector2(170f, 170f));
            icon.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        }

        // the pay
        var pay = UiKit.Panel("Pay", card, UiKit.PayGreen, new Vector2(0f, -top + 62f), new Vector2(300f, 66f));
        pay.GetComponent<Image>().raycastTarget = false;
        UiKit.Cartoon(UiKit.Label(pay, 40, $"+฿{quest.reward}", Vector2.zero, new Vector2(300f, 66f), Color.white), 2.5f).raycastTarget = false;

        card.localScale = Vector3.one * 0.6f;
        GameAudio.Play(fish ? "bell" : "coins", null, 0.6f);
    }

    private void Update()
    {
        float k = Time.unscaledTime - start;
        if (closing) return;
        group.alpha = Mathf.Clamp01(k / 0.2f);
        float pop = Mathf.Clamp01(k / 0.4f);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        card.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f + c3 * Mathf.Pow(pop - 1f, 3f) + c1 * Mathf.Pow(pop - 1f, 2f));
        if (k > Duration) Close();
    }

    private void Close()
    {
        if (closing) return;
        closing = true;
        StartCoroutine(Fade());
    }

    private System.Collections.IEnumerator Fade()
    {
        Vector2 from = card.anchoredPosition;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.35f;
            card.anchoredPosition = from + new Vector2(0f, 160f * t / 0.35f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
