using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Food bag (กระเป๋าอาหาร), premium layout: energy bar on top, a card per food with how much energy
// it restores and an "eat" button. Opened from the "กิน" button next to the energy bar.
public class FoodUI : MonoBehaviour
{
    private static FoodUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, energyText, toastText;
    private RectTransform energyFill, list;
    private Coroutine toast;
    private bool busy;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("FoodUI").AddComponent<FoodUI>();
        instance.backdrop.SetActive(true);
        LifeClient.Instance?.Refresh();
        instance.Render();
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "FoodCanvas", new Vector2(1300f, 860f), 55, "icon:food",
            out backdrop, out titleText, out subtitleText, Close, out _);
        titleText.text = "กระเป๋าอาหาร";
        subtitleText.text = "กินแล้วพลังงานเพิ่ม · ซื้อเพิ่มได้ที่ร้านค้าชุมชนและร้านอาหารในเมือง";

        var bar = UiKit.Panel("EnergyCard", body, Color.white, new Vector2(0f, 250f), new Vector2(1180f, 110f), shadow: true);
        bar.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(bar, 26, "พลังงาน", new Vector2(-470f, 0f), new Vector2(200f, 50f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        var track = UiKit.Panel("Track", bar, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(60f, 0f), new Vector2(760f, 30f));
        energyFill = UiKit.Panel("Fill", track, UiKit.Gold, Vector2.zero, new Vector2(0f, 30f));
        energyFill.anchorMin = energyFill.anchorMax = new Vector2(0f, 0.5f);
        energyFill.pivot = new Vector2(0f, 0.5f);
        energyText = UiKit.Label(bar, 30, "", new Vector2(510f, 0f), new Vector2(150f, 50f), UiKit.Price, TextAnchor.MiddleRight);
        energyText.fontStyle = FontStyle.Bold;

        list = UiKit.Container(body, new Vector2(0f, 90f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, -310f), 760f);

        backdrop.SetActive(false);
        if (LifeClient.Instance != null) LifeClient.Instance.Changed += _ => { if (backdrop.activeSelf) Render(); };
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Render()
    {
        UiKit.Clear(list);
        var life = LifeClient.Instance;
        if (life == null || !life.IsOnline)
        {
            energyText.text = "-";
            energyFill.sizeDelta = new Vector2(0f, 30f);
            UiKit.Label(list, 28, "ต้องออนไลน์ก่อน", Vector2.zero, new Vector2(900f, 60f), UiKit.Muted);
            return;
        }

        float max = life.Current.max_energy;
        energyText.text = $"{life.Energy:0}/{max:0}";
        energyFill.sizeDelta = new Vector2(760f * Mathf.Clamp01(life.Energy / max), 30f);

        int index = 0;
        if (life.Current.foods != null)
        {
            foreach (var food in life.Current.foods)
            {
                if (food.count <= 0) continue;
                AddFoodCard(food, index++);
            }
        }
        if (index == 0)
            UiKit.Label(list, 28, "ยังไม่มีของกินในกระเป๋า\nซื้อได้ที่ร้านค้าชุมชน (แท็บของกิน) หรือร้านข้าวมันไก่/ร้านกาแฟในเมือง",
                new Vector2(0f, -100f), new Vector2(1100f, 110f), UiKit.Muted);
    }

    // Three cards per row
    private void AddFoodCard(LifeClient.Food food, int index)
    {
        float x = -390f + (index % 3) * 390f;
        float y = -(index / 3) * 165f;
        var card = UiKit.Panel("Food", list, Color.white, new Vector2(x, y), new Vector2(370f, 150f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;

        var tile = UiKit.Panel("Icon", card, UiKit.Pill, new Vector2(-125f, 0f), new Vector2(90f, 90f));
        UiKit.Label(tile, 46, food.name.Substring(0, 1), Vector2.zero, new Vector2(86f, 86f), UiKit.Price).fontStyle = FontStyle.Bold;

        UiKit.Label(card, 26, food.name, new Vector2(45f, 42f), new Vector2(240f, 38f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.PillLabel(card, $"พลังงาน +{food.energy}", new Vector2(10f, 2f), new Vector2(170f, 34f), UiKit.OkTint, UiKit.OkGreen, 20);
        UiKit.Label(card, 22, $"มี {food.count}", new Vector2(130f, 2f), new Vector2(70f, 34f), UiKit.Muted, TextAnchor.MiddleRight);

        var f = food;
        UiKit.RoundButton(card, "กิน", new Vector2(45f, -45f), new Vector2(240f, 50f), UiKit.PayGreen, Color.white, () => Eat(f), 26)
            .interactable = !busy;
    }

    private async void Eat(LifeClient.Food food)
    {
        var life = LifeClient.Instance;
        if (busy || life == null) return;
        busy = true;
        try
        {
            var r = await life.EatAsync(food.id);
            Notify($"กิน{r.ate} อร่อย! พลังงาน +{r.gained}", UiKit.PayGreen);
            UiKit.FloatAbovePlayer($"+{r.gained} พลังงาน", new Color(1f, 0.85f, 0.3f));
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("full") ? "อิ่มแล้ว พลังงานเต็ม" : "กินไม่ได้ตอนนี้", UiKit.Price);
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Food] {e.Message}");
        }
        busy = false;
        Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
