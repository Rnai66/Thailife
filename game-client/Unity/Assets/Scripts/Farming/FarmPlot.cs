using System;
using Nakama;
using UnityEngine;

// One rice paddy (แปลงนา). Shows the player's own crop and offers the next farming step:
// ไถนา → ดำนา → รดน้ำ → (รอข้าวโต) → เกี่ยวข้าว
[RequireComponent(typeof(Collider))]
public class FarmPlot : MonoBehaviour
{
    [SerializeField] private int plotId = 1;
    [SerializeField] private Vector2 size = new Vector2(7f, 6.5f);

    private Transform crop;
    private Renderer cropRenderer;
    private Material cropMaterial;
    private bool playerInside;
    private bool busy;

    private static readonly Color Soil = new Color(0.35f, 0.24f, 0.15f);
    private static readonly Color Seedling = new Color(0.45f, 0.8f, 0.3f);
    private static readonly Color Green = new Color(0.25f, 0.6f, 0.2f);
    private static readonly Color Golden = new Color(0.95f, 0.78f, 0.25f);

    public void Configure(int id, Vector2 plotSize)
    {
        plotId = id;
        size = plotSize;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        // Crop block covering the paddy; height and colour show the stage
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Crop";
        Destroy(go.GetComponent<Collider>());
        crop = go.transform;
        crop.SetParent(transform, false);
        cropRenderer = go.GetComponent<Renderer>();

        var player = FindAnyObjectByType<PlayerController>();
        var template = player != null ? player.GetComponent<Renderer>() : null;
        cropMaterial = template != null ? new Material(template.sharedMaterial) : cropRenderer.material;
        cropRenderer.sharedMaterial = cropMaterial;
        cropRenderer.enabled = false;
    }

    private void Update()
    {
        var farm = FarmClient.Instance;
        var plot = farm != null ? farm.GetPlot(plotId) : null;
        UpdateVisual(farm, plot);
        if (playerInside && !busy) Offer(farm, plot);
    }

    private void UpdateVisual(FarmClient farm, FarmClient.Plot plot)
    {
        if (crop == null) return;
        string stage = plot?.stage ?? "empty";
        if (stage == "empty")
        {
            cropRenderer.enabled = false;
            return;
        }

        float progress = farm.ProgressOf(plot);
        float height = stage == "plowed" ? 0.06f : Mathf.Lerp(0.15f, 0.8f, progress);
        Color color = stage == "plowed" ? Soil : stage == "ripe" ? Golden : Color.Lerp(Seedling, Green, progress);

        // Local space of the zone (which sits 1 m above the ground)
        crop.localPosition = new Vector3(0f, -1f + height * 0.5f, 0f);
        crop.localScale = new Vector3(size.x, height, size.y);
        cropMaterial.color = color;
        cropRenderer.enabled = true;
    }

    private void Offer(FarmClient farm, FarmClient.Plot plot)
    {
        if (farm == null || !farm.IsOnline || plot == null)
        {
            ContextButton.Show(this, "ทำนา", "ต้องออนไลน์ก่อนถึงจะทำนาได้", null, enabled: false);
            return;
        }

        int energy = Mathf.FloorToInt(LifeClient.Instance != null ? LifeClient.Instance.Energy : 0f);
        string header = $"แปลงนา {plotId} · พลังงาน {energy}";
        switch (plot.stage)
        {
            case "empty":
                ContextButton.Show(this, "ไถนา\n(พลังงาน 10)", $"{header} · ไถดินเตรียมดำนา", () => Act("plow"));
                break;
            case "plowed":
                var seed = farm.PickSeed();
                if (seed == null)
                    ContextButton.Show(this, "ไม่มี\nพันธุ์ข้าว", $"{header} · ซื้อพันธุ์ข้าวที่ร้านค้าชุมชนก่อน ({farm.SeedSummary()})", null, enabled: false);
                else
                    ContextButton.Show(this, $"ดำนา\n{seed.name}", $"{header} · พันธุ์ข้าวที่มี: {farm.SeedSummary()}", () => Act("plant", seed.id));
                break;
            case "growing":
                var left = TimeSpan.FromSeconds(farm.SecondsUntilRipe(plot));
                string timer = $"ข้าวสุกในอีก {(int)left.TotalMinutes}:{left.Seconds:00}";
                if (!plot.watered)
                    ContextButton.Show(this, "รดน้ำ\n(พลังงาน 5)", $"{header} · {plot.variety_name} · {timer} · รดน้ำแล้วได้ข้าวมากขึ้น", () => Act("water"));
                else
                    ContextButton.Show(this, "กำลังโต", $"{header} · {plot.variety_name} · {timer} · รดน้ำแล้ว", null, enabled: false);
                break;
            case "ripe":
                ContextButton.Show(this, "เกี่ยวข้าว\n(พลังงาน 10)", $"{header} · {plot.variety_name} สุกแล้ว! · ยุ้งข้าว {farm.Current.barn_total:0}/{farm.Current.barn_capacity:0} กก.", () => Act("harvest"), highlight: true);
                break;
        }
    }

    private async void Act(string action, string variety = null)
    {
        var farm = FarmClient.Instance;
        if (busy || farm == null) return;
        busy = true;
        try
        {
            var state = await farm.ActAsync(plotId, action, variety);
            var player = FindAnyObjectByType<PlayerController>();
            Vector3 at = (player != null ? player.transform.position : transform.position) + Vector3.up * 2f;
            if (action == "harvest")
                FloatingText.Spawn(at, $"ได้{state.harvested_name} {state.harvested_kg} กก. เข้ายุ้งแล้ว!", Golden);
            else
                FloatingText.Spawn(at, action == "plow" ? "ไถแล้ว" : action == "plant" ? "ดำนาแล้ว" : "รดน้ำแล้ว", new Color(0.6f, 0.9f, 0.5f));
        }
        catch (ApiResponseException e)
        {
            string why = e.Message.Contains("energy") ? "พลังงานไม่พอ — ไปนอนพักที่ชานบ้านก่อน"
                : e.Message.Contains("barn full") ? "ยุ้งข้าวเต็ม — ขายข้าวที่ร้านค้าชุมชน หรือซื้อการขยายยุ้ง"
                : e.Message.Contains("no seeds") ? "ไม่มีพันธุ์ข้าว — ซื้อที่ร้านค้าชุมชน"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่"
                : "ทำไม่ได้ตอนนี้";
            ContextButton.Show(this, "ตกลง", why, null, enabled: false);
            await System.Threading.Tasks.Task.Delay(1500);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Farm] {e.Message}");
        }
        busy = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        playerInside = false;
        ContextButton.Hide(this);
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
