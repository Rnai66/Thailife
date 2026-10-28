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

    private Transform crop;          // mud block shown after ploughing
    private Renderer cropRenderer;
    private Material cropMaterial;
    private Transform rice;          // rows of rice clumps (leaves + ears)
    private Renderer leaves, ears;
    private Material leafMaterial, earMaterial;
    private bool playerInside;
    private bool busy;

    private static readonly Color Soil = new Color(0.35f, 0.24f, 0.15f);
    private static readonly Color Seedling = new Color(0.55f, 0.85f, 0.32f);
    private static readonly Color Green = new Color(0.22f, 0.52f, 0.16f);
    private static readonly Color Straw = new Color(0.8f, 0.68f, 0.3f);
    private static readonly Color EarGreen = new Color(0.62f, 0.74f, 0.3f);
    private static readonly Color Golden = new Color(0.95f, 0.76f, 0.24f);

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

        // Rice planted in rows (ดำนาเป็นแถว): one mesh of leaf blades, one of drooping ears
        rice = new GameObject("Rice").transform;
        rice.SetParent(transform, false);
        rice.localPosition = new Vector3(0f, -1f, 0f);
        leafMaterial = new Material(cropMaterial);
        leafMaterial.SetFloat("_Cull", 0f); // blades are single sheets: show both faces
        earMaterial = new Material(leafMaterial);
        leaves = MakePart("Leaves", RiceMeshes(size).leaves, leafMaterial);
        ears = MakePart("Ears", RiceMeshes(size).ears, earMaterial);
        rice.gameObject.SetActive(false);
    }

    private Renderer MakePart(string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(rice, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    // ── Rice clump meshes (built once per paddy size, shared by all paddies) ──────────
    private static readonly System.Collections.Generic.Dictionary<Vector2, (Mesh leaves, Mesh ears)> meshCache =
        new System.Collections.Generic.Dictionary<Vector2, (Mesh, Mesh)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => meshCache.Clear();

    private static (Mesh leaves, Mesh ears) RiceMeshes(Vector2 area)
    {
        if (meshCache.TryGetValue(area, out var cached) && cached.leaves != null) return cached;
        var rng = new System.Random(17);
        var lv = new System.Collections.Generic.List<Vector3>(); var lt = new System.Collections.Generic.List<int>();
        var ev = new System.Collections.Generic.List<Vector3>(); var et = new System.Collections.Generic.List<int>();
        const float rowGap = 0.45f, hillGap = 0.4f, margin = 0.12f; // dense rows right up to the dikes
        for (float x = -area.x * 0.5f + margin; x <= area.x * 0.5f - margin; x += rowGap)
            for (float z = -area.y * 0.5f + margin; z <= area.y * 0.5f - margin; z += hillGap)
            {
                var basePos = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.06f, 0.02f, z + (float)(rng.NextDouble() - 0.5) * 0.06f);
                int blades = 7;
                for (int b = 0; b < blades; b++)
                {
                    float ang = (b / (float)blades + (float)rng.NextDouble() * 0.1f) * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    float lean = 0.12f + (float)rng.NextDouble() * 0.16f, tall = 0.85f + (float)rng.NextDouble() * 0.25f;
                    AddBlade(lv, lt, basePos, dir, lean, tall, 0.035f);
                }
                for (int e = 0; e < 3; e++)
                {
                    float ang = (e / 3f + (float)rng.NextDouble() * 0.2f) * Mathf.PI * 2f;
                    AddEar(ev, et, basePos, new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
                }
            }
        var leafMesh = new Mesh { name = "RiceLeaves", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        leafMesh.SetVertices(lv); leafMesh.SetTriangles(lt, 0); leafMesh.RecalculateNormals();
        var earMesh = new Mesh { name = "RiceEars", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        earMesh.SetVertices(ev); earMesh.SetTriangles(et, 0); earMesh.RecalculateNormals();
        meshCache[area] = (leafMesh, earMesh);
        return (leafMesh, earMesh);
    }

    // A leaf blade: narrow strip rising from the hill and arching outward, pointed tip (height 1 = full grown)
    private static void AddBlade(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<int> t, Vector3 basePos, Vector3 dir, float lean, float tall, float width)
    {
        var side = Vector3.Cross(Vector3.up, dir).normalized * width;
        int start = v.Count;
        for (int k = 0; k <= 3; k++)
        {
            float u = k / 3f;
            var p = basePos + dir * (lean * u * u) + Vector3.up * (tall * (u - 0.12f * u * u * u));
            float w = 1f - u * 0.85f;
            v.Add(p - side * w); v.Add(p + side * w);
        }
        v.Add(basePos + dir * (lean * 1.15f) + Vector3.up * tall * 0.9f); // tip
        for (int k = 0; k < 3; k++)
        {
            int a = start + k * 2;
            t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
        }
        int last = start + 6;
        t.AddRange(new[] { last, start + 8, last + 1 });
    }

    // A rice ear (รวงข้าว): stalk out of the clump, then a string of grains drooping down at the end
    private static void AddEar(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<int> t, Vector3 basePos, Vector3 dir)
    {
        var side = Vector3.Cross(Vector3.up, dir).normalized * 0.03f;
        for (int k = 0; k < 7; k++)
        {
            float u = k / 6f;
            var c = basePos + dir * (0.12f + u * 0.28f) + Vector3.up * (1.02f + 0.1f * Mathf.Sin(u * Mathf.PI) - u * u * 0.35f);
            float r = 0.035f * (1f - u * 0.5f);
            int s0 = v.Count;
            v.Add(c + Vector3.up * r); v.Add(c + side * 1.4f); v.Add(c - Vector3.up * r); v.Add(c - side * 1.4f);
            t.AddRange(new[] { s0, s0 + 1, s0 + 2, s0, s0 + 2, s0 + 3 });
        }
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
            rice.gameObject.SetActive(false);
            return;
        }

        // Ploughed: wet mud over the paddy; planted: rows of rice that grow and ripen
        if (stage == "plowed")
        {
            crop.localPosition = new Vector3(0f, -1f + 0.03f, 0f);
            crop.localScale = new Vector3(size.x, 0.06f, size.y);
            cropMaterial.color = Soil;
            cropRenderer.enabled = true;
            rice.gameObject.SetActive(false);
            return;
        }
        cropRenderer.enabled = false;
        rice.gameObject.SetActive(true);

        float progress = stage == "ripe" ? 1f : farm.ProgressOf(plot);
        float height = Mathf.Lerp(0.22f, 0.95f, Mathf.SmoothStep(0f, 1f, progress));
        rice.localScale = new Vector3(1f, height, 1f); // planted across the whole paddy; only the height grows
        leafMaterial.color = progress < 0.7f ? Color.Lerp(Seedling, Green, progress / 0.7f) : Color.Lerp(Green, Straw, (progress - 0.7f) / 0.3f);
        bool earsOut = progress > 0.55f;
        ears.enabled = earsOut;
        if (earsOut)
        {
            float k = Mathf.InverseLerp(0.55f, 1f, progress);
            ears.transform.localScale = new Vector3(1f, Mathf.Lerp(0.85f, 1f, k), 1f);
            earMaterial.color = Color.Lerp(EarGreen, Golden, k);
        }
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
                string timer = $"ข้าวสุกในอีก {GameClock.Countdown(farm.SecondsUntilRipe(plot))}";
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
        // Work the paddy: hoe to plough, seedlings to plant, can to water, sickle to harvest
        string motion = action == "plow" ? "chop" : action == "plant" ? "plant" : action == "water" ? "pour" : action == "harvest" ? "sweep" : null;
        if (motion != null) ActivityPose.Do(motion, 1.8f, transform.position);
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
