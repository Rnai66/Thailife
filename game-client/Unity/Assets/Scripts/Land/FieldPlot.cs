using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;

// One plot of your crop field (แปลงไร่) on your own land (land.lua): shows the crop and offers the
// next step — ไถ → ปลูก (the crop picked at the land board) → รดน้ำ → (รอโต) → เก็บเกี่ยว.
// Corn, cassava and sugarcane each have their own rows of plants that grow taller and ripen.
[RequireComponent(typeof(Collider))]
public class FieldPlot : MonoBehaviour
{
    [SerializeField] private int plotId = 1;
    [SerializeField] private Vector2 size = new Vector2(8f, 5.5f);

    private Transform soil;
    private Material soilMaterial, leafMaterial, headMaterial;
    private readonly Dictionary<string, Transform> crops = new Dictionary<string, Transform>();
    private bool playerInside, busy;

    private static readonly Color Soil = new Color(0.4f, 0.27f, 0.16f);
    private static readonly Color Young = new Color(0.55f, 0.85f, 0.32f);
    private static readonly Color Leaf = new Color(0.2f, 0.5f, 0.16f);
    private static readonly Color Dry = new Color(0.72f, 0.66f, 0.32f);
    private static readonly Color Ripe = new Color(0.98f, 0.8f, 0.2f);

    // Full-grown height of each crop (m)
    private static readonly Dictionary<string, float> Heights = new Dictionary<string, float> { { "corn", 2.2f }, { "cassava", 1.6f }, { "sugarcane", 2.8f } };

    public void Configure(int id, Vector2 plotSize)
    {
        plotId = id;
        size = plotSize;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        var template = player != null ? player.GetComponent<Renderer>() : null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Soil";
        Destroy(go.GetComponent<Collider>());
        soil = go.transform;
        soil.SetParent(transform, false);
        soil.localPosition = new Vector3(0f, -1f + 0.04f, 0f);
        soil.localScale = new Vector3(size.x, 0.08f, size.y);
        var r = go.GetComponent<Renderer>();
        soilMaterial = template != null ? new Material(template.sharedMaterial) : r.material;
        soilMaterial.color = Soil;
        r.sharedMaterial = soilMaterial;
        leafMaterial = new Material(soilMaterial);
        headMaterial = new Material(soilMaterial);
        soil.gameObject.SetActive(false);
    }

    private Transform CropRows(string crop)
    {
        if (crops.TryGetValue(crop, out var t) && t != null) return t;
        var (body, head) = Meshes(crop, size);
        t = new GameObject("Crop_" + crop).transform;
        t.SetParent(transform, false);
        t.localPosition = new Vector3(0f, -1f + 0.06f, 0f);
        Part(t, "Plants", body, leafMaterial);
        Part(t, "Heads", head, headMaterial);
        crops[crop] = t;
        return t;
    }

    private static void Part(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ── Plant meshes: rows of simple plants (unit height), one mesh for stalks and leaves, one for ears/tufts ──
    private static readonly Dictionary<string, (Mesh, Mesh)> cache = new Dictionary<string, (Mesh, Mesh)>();
    private static Mesh cylinder, sphere, cube;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { cache.Clear(); cylinder = sphere = cube = null; }

    private static Mesh Primitive(PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        Destroy(go);
        return mesh;
    }

    private static (Mesh body, Mesh head) Meshes(string crop, Vector2 area)
    {
        string key = crop + area;
        if (cache.TryGetValue(key, out var cached) && cached.Item1 != null) return cached;
        if (cylinder == null) { cylinder = Primitive(PrimitiveType.Cylinder); sphere = Primitive(PrimitiveType.Sphere); cube = Primitive(PrimitiveType.Cube); }
        var body = new List<CombineInstance>();
        var head = new List<CombineInstance>();
        var rng = new System.Random(crop.GetHashCode() & 0xFFFF);
        float rowGap = crop == "cassava" ? 0.9f : 0.75f, plantGap = crop == "cassava" ? 0.8f : 0.5f;
        void Add(List<CombineInstance> list, Mesh m, Vector3 pos, Quaternion rot, Vector3 scale) =>
            list.Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, rot, scale) });

        for (float x = -area.x * 0.5f + 0.4f; x <= area.x * 0.5f - 0.4f; x += rowGap)
            for (float z = -area.y * 0.5f + 0.35f; z <= area.y * 0.5f - 0.35f; z += plantGap)
            {
                var p = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.1f, 0f, z + (float)(rng.NextDouble() - 0.5) * 0.1f);
                float yaw = (float)rng.NextDouble() * 360f;
                if (crop == "corn")
                {
                    Add(body, cylinder, p + Vector3.up * 0.5f, Quaternion.identity, new Vector3(0.05f, 0.5f, 0.05f));
                    for (int k = 0; k < 4; k++) // long leaves arching out of the stalk
                        Add(body, cube, p + Vector3.up * (0.3f + k * 0.17f), Quaternion.Euler(0f, yaw + k * 95f, 0f) * Quaternion.Euler(0f, 0f, 55f),
                            new Vector3(0.45f, 0.012f, 0.07f));
                    Add(head, sphere, p + new Vector3(0.06f, 0.62f, 0f), Quaternion.Euler(0f, 0f, -15f), new Vector3(0.08f, 0.2f, 0.08f));
                    Add(head, cube, p + Vector3.up * 1.03f, Quaternion.Euler(0f, yaw, 0f), new Vector3(0.12f, 0.08f, 0.02f)); // tassel
                }
                else if (crop == "cassava")
                {
                    Add(body, cylinder, p + Vector3.up * 0.4f, Quaternion.Euler(0f, 0f, 4f), new Vector3(0.035f, 0.4f, 0.035f));
                    for (int k = 0; k < 5; k++) // a crown of hand-shaped leaves
                        Add(body, sphere, p + Quaternion.Euler(0f, yaw + k * 72f, 0f) * new Vector3(0.22f, 0.85f, 0f), Quaternion.Euler(0f, yaw + k * 72f, 20f),
                            new Vector3(0.35f, 0.04f, 0.16f));
                    Add(head, sphere, p + Vector3.up * 0.05f, Quaternion.identity, new Vector3(0.25f, 0.1f, 0.25f)); // the root swelling at the base
                }
                else // sugarcane: three canes per hill with a tuft of leaves
                {
                    for (int k = 0; k < 3; k++)
                    {
                        var off = Quaternion.Euler(0f, yaw + k * 120f, 0f) * new Vector3(0.07f, 0f, 0f);
                        Add(body, cylinder, p + off + Vector3.up * 0.42f, Quaternion.Euler(0f, 0f, (k - 1) * 3f), new Vector3(0.05f, 0.42f, 0.05f));
                        for (int l = 0; l < 3; l++)
                            Add(head, cube, p + off + Vector3.up * 0.88f, Quaternion.Euler(0f, yaw + k * 120f + l * 60f, 0f) * Quaternion.Euler(0f, 0f, 40f),
                                new Vector3(0.4f, 0.01f, 0.05f));
                    }
                }
            }
        (Mesh, Mesh) result = (Combine(crop + "Plants", body), Combine(crop + "Heads", head));
        cache[key] = result;
        return result;
    }

    private static Mesh Combine(string name, List<CombineInstance> parts)
    {
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(parts.ToArray(), true, true);
        return mesh;
    }

    private void Update()
    {
        var land = LandClient.Instance;
        var plot = land != null ? land.GetPlot(plotId) : null;
        UpdateVisual(land, plot);
        if (playerInside && !busy) Offer(land, plot);
    }

    private void UpdateVisual(LandClient land, LandClient.Plot plot)
    {
        if (soil == null) return;
        string stage = plot?.stage ?? "empty";
        soil.gameObject.SetActive(stage != "empty");
        bool growing = stage == "growing" || stage == "ripe";
        foreach (var kv in crops) if (kv.Value != null) kv.Value.gameObject.SetActive(growing && kv.Key == plot.crop);
        if (!growing || string.IsNullOrEmpty(plot.crop)) return;

        var rows = CropRows(plot.crop);
        rows.gameObject.SetActive(true);
        float progress = land.ProgressOf(plot);
        float full = Heights.TryGetValue(plot.crop, out var h) ? h : 2f;
        rows.localScale = new Vector3(1f, Mathf.Lerp(0.12f, full, Mathf.SmoothStep(0f, 1f, progress)), 1f);
        leafMaterial.color = progress < 0.75f ? Color.Lerp(Young, Leaf, progress / 0.75f) : Color.Lerp(Leaf, plot.crop == "corn" ? Dry : Leaf, (progress - 0.75f) / 0.25f);
        var heads = rows.Find("Heads");
        if (heads != null) heads.gameObject.SetActive(plot.crop == "sugarcane" || progress > 0.5f);
        headMaterial.color = plot.crop == "sugarcane" ? leafMaterial.color
            : plot.crop == "cassava" ? new Color(0.55f, 0.36f, 0.22f) : Color.Lerp(Young, Ripe, Mathf.InverseLerp(0.5f, 1f, progress));
    }

    private void Offer(LandClient land, LandClient.Plot plot)
    {
        if (land == null || !land.IsOnline || plot == null)
        {
            ContextButton.Show(this, "ทำไร่", "ต้องออนไลน์ก่อนถึงจะทำไร่ได้", null, enabled: false);
            return;
        }
        int energy = Mathf.FloorToInt(LifeClient.Instance != null ? LifeClient.Instance.Energy : 0f);
        string header = $"แปลงไร่ {plotId} · พลังงาน {energy}";
        switch (plot.stage)
        {
            case "empty":
                ContextButton.Show(this, "ไถดิน\n(พลังงาน 10)", $"{header} · ไถเตรียมดินก่อนปลูก", () => Act("plow"));
                break;
            case "plowed":
                var crop = land.GetCrop(land.NextCrop);
                string name = crop != null ? crop.name : "พืช";
                ContextButton.Show(this, $"ปลูก{name}\n{(crop != null ? crop.seed_price : 0):N0} บาท",
                    $"{header} · เปลี่ยนพืชได้ที่ป้ายที่ดินหน้าไร่", () => Act("plant", land.NextCrop));
                break;
            case "growing":
                string timer = $"เก็บเกี่ยวได้ในอีก {GameClock.Countdown(land.SecondsUntilRipe(plot))}";
                if (!plot.watered)
                    ContextButton.Show(this, "รดน้ำ\n(พลังงาน 5)", $"{header} · {plot.crop_name} · {timer} · รดน้ำแล้วได้ผลผลิตมากขึ้น", () => Act("water"));
                else
                    ContextButton.Show(this, "กำลังโต", $"{header} · {plot.crop_name} · {timer} · รดน้ำแล้ว", null, enabled: false);
                break;
            case "ripe":
                ContextButton.Show(this, "เก็บเกี่ยว\n(พลังงาน 10)", $"{header} · {plot.crop_name} โตเต็มที่แล้ว!", () => Act("harvest"), highlight: true);
                break;
        }
    }

    private async void Act(string action, string crop = null)
    {
        var land = LandClient.Instance;
        if (busy || land == null) return;
        busy = true;
        string motion = action == "plow" ? "chop" : action == "plant" ? "plant" : action == "water" ? "pour" : "sweep";
        ActivityPose.Do(motion, 1.8f, transform.position);
        try
        {
            var state = await land.FieldAsync(plotId, action, crop);
            var player = FindAnyObjectByType<PlayerController>();
            Vector3 at = (player != null ? player.transform.position : transform.position) + Vector3.up * 2f;
            if (action == "harvest")
                FloatingText.Spawn(at, $"ได้{state.harvested_name} {state.harvested_kg:N0} กก. เข้าโรงเก็บแล้ว!", Ripe);
            else
                FloatingText.Spawn(at, action == "plow" ? "ไถแล้ว" : action == "plant" ? "ปลูกแล้ว" : "รดน้ำแล้ว", new Color(0.6f, 0.9f, 0.5f));
        }
        catch (ApiResponseException e)
        {
            string why = e.Message.Contains("energy") ? "พลังงานไม่พอ — พักก่อน"
                : e.Message.Contains("not enough baht") ? "เงินไม่พอซื้อพันธุ์"
                : e.Message.Contains("store full") ? "โรงเก็บผลผลิตเต็ม — ขายที่ป้ายที่ดินหน้าไร่ก่อน"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่"
                : "ทำไม่ได้ตอนนี้";
            ContextButton.Show(this, "ตกลง", why, null, enabled: false);
            await System.Threading.Tasks.Task.Delay(1500);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Field] {e.Message}");
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

    private void OnDisable()
    {
        playerInside = false;
        ContextButton.Hide(this);
    }
}
