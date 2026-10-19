using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Starter quest (ภารกิจเริ่มต้น, tutorial.lua): a banner at the top of the screen says what to do
// next, a golden arrow floats over where to go, and each step pays a small reward. It walks a new
// player through the shop, food, sleep, the bus, the free cow from the อบต., fishing and selling.
[RequireComponent(typeof(NakamaConnection))]
public class TutorialGuide : MonoBehaviour
{
    private class Step
    {
        public string Scene;      // where it happens
        public string Target;     // object name the arrow points at ("" = no arrow)
        public string Text;
        public Func<TutorialGuide, bool> Done;
    }

    [Serializable] private class Progress { public int step, steps, step_reward, final_bonus, reward; public bool done; }

    private static readonly Dictionary<string, string> SceneNames = new Dictionary<string, string>
    {
        { "ThaiVillage", "หมู่บ้าน" }, { "Tambon", "ตำบล" }, { "City", "เมือง" }, { "Beach", "ทะเล" },
    };

    private static readonly Step[] Steps =
    {
        new Step { Scene = "ThaiVillage", Target = "ShopCounterZone", Text = "ซื้ออาหาร 1 อย่างที่ร้านค้าชุมชน", Done = g => g.FoodCount() > g.baseFood },
        new Step { Scene = "", Target = "", Text = "กดปุ่ม \"กิน\" ข้างแถบพลังงาน แล้วกินอาหาร", Done = g => g.FoodCount() < g.baseFood },
        new Step { Scene = "ThaiVillage", Target = "SleepZone", Text = "นอนพักบนเสื่อที่ชานบ้าน (ฟื้นพลังงาน)", Done = g => LifeClient.Instance != null && LifeClient.Instance.IsSleeping },
        new Step { Scene = "ThaiVillage", Target = "BusStop_ป้ายรถสองแถว", Text = "ยืนใต้ป้ายรถสองแถว แล้วเลือกไป \"ตำบล\"", Done = g => SceneManager.GetActiveScene().name == "Tambon" },
        new Step { Scene = "Tambon", Target = "CowProjectDesk", Text = "รับวัวฟรี 1 ตัวที่โต๊ะข้าง อบต.", Done = g => RanchClient.Instance?.Current != null && RanchClient.Instance.Current.claimed },
        new Step { Scene = "Tambon", Target = "FishingSpot_ท่าน้ำ", Text = "ตกปลาที่คลอง 1 ตัว", Done = g => g.FishCount() > g.baseFish },
        new Step { Scene = "ThaiVillage", Target = "ShopCounterZone", Text = "กลับหมู่บ้าน ขายปลาที่ร้านค้าชุมชน", Done = g => g.baseFish == 0 || g.FishCount() < g.baseFish },
    };

    private NakamaConnection connection;
    private Progress progress;
    private int baseFood, baseFish, activeStep = -1;
    private bool busy, confirmSkip;
    private float nextCheck;

    private GameObject canvas;
    private Text headText, stepText;
    private Text skipLabel;
    private Transform marker;
    private Transform target;
    private string targetScene;

    private void Awake()
    {
        connection = GetComponent<NakamaConnection>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        connection.Connected += OnConnected;
        if (connection.IsConnected) OnConnected(connection);
    }

    private void OnDisable() => connection.Connected -= OnConnected;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (canvas != null) Destroy(canvas);
        if (marker != null) Destroy(marker.gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        target = null;
        targetScene = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try { Apply(await Call("tutorial_get", "{}")); }
        catch (Exception e) { Debug.LogWarning($"[Tutorial] {e.Message}"); }
    }

    private async Task<Progress> Call(string rpc, string payload)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        return JsonUtility.FromJson<Progress>(result.Payload);
    }

    private void Apply(Progress p)
    {
        progress = p;
        bool show = p != null && !p.done && p.step >= 1 && p.step <= Steps.Length;
        if (!show)
        {
            if (canvas != null) canvas.SetActive(false);
            if (marker != null) marker.gameObject.SetActive(false);
            return;
        }
        if (canvas == null) BuildUi();
        canvas.SetActive(true);
        if (activeStep != p.step)
        {
            activeStep = p.step;
            baseFood = FoodCount();
            baseFish = FishCount();
            target = null;
            targetScene = null;
            confirmSkip = false;
            skipLabel.text = "ข้าม";
        }
    }

    private int FoodCount()
    {
        var foods = LifeClient.Instance?.Current?.foods;
        int n = 0;
        if (foods != null) foreach (var f in foods) n += f.count;
        return n;
    }

    private int FishCount() => FishingClient.Instance?.Current?.total ?? 0;

    private void Update()
    {
        if (progress == null || progress.done || canvas == null || !canvas.activeSelf) return;
        var step = Steps[progress.step - 1];
        string scene = SceneManager.GetActiveScene().name;
        var player = GameAudio.Player;

        // Where the arrow goes: the step's target here, or the bus stop when it's in another place
        bool elsewhere = step.Scene != "" && step.Scene != scene && !(progress.step == 4 && scene == "Tambon");
        string want = elsewhere ? "BusStop_ป้ายรถสองแถว" : step.Target;
        if (targetScene != scene + want)
        {
            targetScene = scene + want;
            target = want == "" ? null : Nearest(want, player);
        }
        else if (want == "FishingSpot_ท่าน้ำ" && Time.frameCount % 60 == 0) target = Nearest(want, player);

        string distance = target != null && player != null ? $" · อีก {Vector3.Distance(player.position, target.position):0} ม." : "";
        headText.text = $"ภารกิจเริ่มต้น {progress.step}/{progress.steps} · รางวัล {progress.step_reward} บาท"
                        + (progress.step == progress.steps ? $" + โบนัส {progress.final_bonus}" : "");
        stepText.text = (elsewhere ? $"นั่งรถสองแถวไป{SceneNames.GetValueOrDefault(step.Scene, step.Scene)} แล้ว: {step.Text}" : step.Text) + distance;
        UpdateMarker();

        if (busy || Time.time < nextCheck) return;
        nextCheck = Time.time + 0.5f;
        if (step.Done(this)) Complete(progress.step);
    }

    private static Transform Nearest(string name, Transform from)
    {
        Transform best = null;
        float bestD = float.MaxValue;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name != name) continue;
                float d = from != null ? (t.position - from.position).sqrMagnitude : 0f;
                if (d < bestD) { best = t; bestD = d; }
            }
        return best;
    }

    private async void Complete(int step)
    {
        busy = true;
        try
        {
            var p = await Call("tutorial_done", $"{{\"step\":{step}}}");
            UiKit.FloatAbovePlayer($"ภารกิจเริ่มต้นสำเร็จ! +{p.reward} ฿", new Color(1f, 0.85f, 0.3f));
            GameAudio.Play("coins");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            Apply(p);
            if (p.done) UiKit.FloatAbovePlayer("จบภารกิจเริ่มต้น! ลองเปิด \"ภารกิจวันนี้\" และ เมนู → ปฏิทินงาน", new Color(0.5f, 0.9f, 1f));
        }
        catch (ApiResponseException e)
        {
            Debug.LogWarning($"[Tutorial] {e.Message}");
            try { Apply(await Call("tutorial_get", "{}")); } catch (Exception) { }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tutorial] {e.Message}");
        }
        busy = false;
    }

    private async void Skip()
    {
        if (!confirmSkip) { confirmSkip = true; skipLabel.text = "ยืนยัน?"; return; }
        try { Apply(await Call("tutorial_skip", "{}")); }
        catch (Exception e) { Debug.LogWarning($"[Tutorial] {e.Message}"); }
    }

    // ── Banner (top centre) ─────────────────────────────────────────────────────
    private void BuildUi()
    {
        canvas = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var plate = UiKit.WoodPlate(canvas.transform, Vector2.zero, new Vector2(640f, 104f));
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 1f);
        plate.pivot = new Vector2(0.5f, 1f);
        plate.anchoredPosition = new Vector2(0f, -8f);
        headText = UiKit.Label(plate, 20, "", new Vector2(-40f, 26f), new Vector2(520f, 30f), UiKit.GoldText);
        headText.horizontalOverflow = HorizontalWrapMode.Overflow;
        stepText = UiKit.Cartoon(UiKit.Label(plate, 24, "", new Vector2(-40f, -14f), new Vector2(540f, 52f), UiKit.WoodInk), 2f);
        var skip = UiKit.RoundButton(plate, "ข้าม", new Vector2(270f, 0f), new Vector2(80f, 50f), new Color32(0x5A, 0x3A, 0x26, 0xFF), UiKit.HeaderText, Skip, 20);
        skipLabel = skip.GetComponentInChildren<Text>();
    }

    // ── Golden arrow over the target ───────────────────────────────────────────
    private void UpdateMarker()
    {
        if (target == null)
        {
            if (marker != null) marker.gameObject.SetActive(false);
            return;
        }
        if (marker == null) marker = BuildMarker();
        marker.gameObject.SetActive(true);
        marker.position = target.position + Vector3.up * (3.2f + Mathf.Sin(Time.time * 3f) * 0.3f);
        marker.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
    }

    private static Transform BuildMarker()
    {
        var go = new GameObject("TutorialArrow");
        var mesh = new Mesh { name = "Arrow" };
        // Downward pyramid: square top, tip at the bottom
        var v = new[]
        {
            new Vector3(-0.45f, 0.6f, -0.45f), new Vector3(0.45f, 0.6f, -0.45f), new Vector3(0.45f, 0.6f, 0.45f), new Vector3(-0.45f, 0.6f, 0.45f),
            new Vector3(0f, -0.6f, 0f),
        };
        mesh.vertices = v;
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4 };
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        // Borrow the project's lit shader from a primitive (always included in the build)
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var material = new Material(temp.GetComponent<Renderer>().sharedMaterial) { color = new Color(1f, 0.8f, 0.2f) };
        Destroy(temp);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.15f) * 2.5f);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }
}
