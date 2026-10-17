using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Food bag (กระเป๋าอาหาร): eat snacks bought at the shops to restore energy.
// Opened from the "กิน" button next to the energy bar. Built at runtime.
public class FoodUI : MonoBehaviour
{
    private static FoodUI instance;

    private GameObject backdrop;
    private Text energyText;
    private Text messageText;
    private RectTransform list;
    private bool busy;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("FoodUI").AddComponent<FoodUI>();
        instance.backdrop.SetActive(true);
        instance.messageText.text = "";
        LifeClient.Instance?.Refresh();
        instance.Render();
    }

    private void Awake()
    {
        var card = UiKit.Window(transform, "FoodCanvas", new Vector2(1000f, 860f), 55, out backdrop);
        UiKit.Label(card, 50, "กระเป๋าอาหาร", new Vector2(0f, 370f), new Vector2(900f, 70f), UiKit.Brown);
        energyText = UiKit.Label(card, 32, "", new Vector2(0f, 305f), new Vector2(900f, 50f), UiKit.Green);
        list = UiKit.Container(card, new Vector2(0f, 220f));
        messageText = UiKit.Label(card, 32, "", new Vector2(0f, -300f), new Vector2(900f, 50f), new Color(0.6f, 0.2f, 0.05f));
        UiKit.Button(card, "ปิด", new Vector2(0f, -370f), new Vector2(280f, 74f), new Color(0.9f, 0.45f, 0.4f), () => backdrop.SetActive(false));
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
            energyText.text = "ต้องออนไลน์ก่อน";
            return;
        }

        energyText.text = $"พลังงาน {life.Energy:0}/{life.Current.max_energy}";
        int row = 0;
        if (life.Current.foods != null)
        {
            foreach (var food in life.Current.foods)
            {
                if (food.count <= 0) continue;
                var f = food;
                var r = UiKit.Rect("Food", list, new Color(1f, 1f, 1f, 0.6f));
                r.anchoredPosition = new Vector2(0f, -row * 90f);
                r.sizeDelta = new Vector2(860f, 80f);
                UiKit.Label(r, 32, $"{f.name} ×{f.count}   (พลังงาน +{f.energy})", new Vector2(-110f, 0f), new Vector2(600f, 70f), UiKit.Brown, TextAnchor.MiddleLeft);
                UiKit.Button(r, "กิน", new Vector2(340f, 0f), new Vector2(150f, 66f), new Color(0.45f, 0.85f, 0.45f), () => Eat(f)).interactable = !busy;
                row++;
            }
        }
        if (row == 0)
            UiKit.Label(list, 30, "ไม่มีของกินในกระเป๋า — ซื้อได้ที่ร้านค้าชุมชน หรือร้านข้าวมันไก่/ร้านกาแฟในเมือง",
                new Vector2(0f, -40f), new Vector2(880f, 100f), UiKit.Brown);
    }

    private async void Eat(LifeClient.Food food)
    {
        var life = LifeClient.Instance;
        if (busy || life == null) return;
        busy = true;
        try
        {
            var r = await life.EatAsync(food.id);
            messageText.text = $"กิน{r.ate} อร่อย! พลังงาน +{r.gained}";
            UiKit.FloatAbovePlayer($"+{r.gained} พลังงาน", new Color(1f, 0.85f, 0.3f));
        }
        catch (ApiResponseException e)
        {
            messageText.text = e.Message.Contains("full") ? "อิ่มแล้ว พลังงานเต็ม" : "กินไม่ได้ตอนนี้";
        }
        catch (Exception e)
        {
            messageText.text = "เกิดข้อผิดพลาด ลองใหม่";
            Debug.LogWarning($"[Food] {e.Message}");
        }
        busy = false;
        Render();
    }
}
