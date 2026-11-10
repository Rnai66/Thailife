using UnityEngine;
using UnityEngine.UI;

// Your own vehicle (bought at the bank), parked under the stilt house by HomeCar. Walk up to it to
// get in (ขับรถ) and drive it around; the "ลงจากรถ" button gets out and leaves it where you stop.
// In a car the player is hidden inside; on a bicycle or motorbike they stay visible in the saddle.
// The stick steers; the คันเร่ง (throttle) and เบรก (brake) buttons set the speed (PlayerController's
// VehicleMode). On two wheels the bike and its rider lean into the turns together.
[RequireComponent(typeof(BoxCollider))]
public class DrivableCar : MonoBehaviour
{
    private static GameObject exitCanvas;
    private static HoldButton throttleButton, brakeButton;
    private static Text speedText, limitText;
    private static GameObject limitBadge;
    private static Image speedPlate;
    private float lean;
    private static DrivableCar driving;

    private string id, carName;
    private PlayerController player;
    private Renderer[] hidden;
    private SeatedPose riding; // astride a bicycle or motorbike
    private BoxCollider zone;

    public bool Driving => driving == this;

    public void Configure(string assetId, string displayName)
    {
        id = assetId;
        carName = displayName;
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        bool two = CarModels.TwoWheels(id);
        zone.size = two ? new Vector3(1.6f, 2f, 2.4f) : new Vector3(3f, 2.4f, 5.4f);
        zone.center = new Vector3(0f, 1f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Driving || other.GetComponent<PlayerController>() == null) return;
        ContextButton.Show(this, CarModels.TwoWheels(id) ? "ขี่รถ" : "ขับรถ", $"{carName} ของคุณ · ขับเล่นรอบหมู่บ้าน", GetIn);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }

    private void GetIn()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player == null || driving != null) return;
        driving = this;
        ContextButton.Hide(this);
        zone.enabled = false;
        // Hop on at the car's spot, facing the way it points
        player.Teleport(new Vector3(transform.position.x, player.transform.position.y, transform.position.z), transform.rotation);
        player.DriveBoost = CarModels.DriveSpeed(id);
        player.VehicleMode = true;
        lean = 0f;
        if (!CarModels.TwoWheels(id))
        {
            hidden = player.GetComponentsInChildren<Renderer>();
            foreach (var r in hidden) if (r != null) r.enabled = false;
        }
        else
        {
            riding = player.gameObject.AddComponent<SeatedPose>();
            riding.Ride = true;
        }
        ShowExitButton(true);
        GameAudio.Play("whoosh", null, 0.5f);
    }

    public void GetOut()
    {
        if (!Driving) return;
        driving = null;
        if (player != null)
        {
            player.DriveBoost = 1f;
            player.VehicleMode = false;
            player.Throttle = player.Brake = false;
            player.GetComponent<CharacterModel>()?.SetLean(0f);
            if (hidden != null) foreach (var r in hidden) if (r != null) r.enabled = true;
            if (riding != null) Destroy(riding);
            // Step out beside the vehicle
            var side = transform.right * (CarModels.TwoWheels(id) ? 0.9f : 1.6f);
            player.Teleport(player.transform.position + side, player.transform.rotation);
        }
        hidden = null;
        riding = null;
        zone.enabled = true;
        ShowExitButton(false);
    }

    private void LateUpdate()
    {
        if (!Driving || player == null) return;
        var cc = player.GetComponent<CharacterController>();
        float feet = player.transform.position.y + (cc != null ? cc.center.y - cc.height * 0.5f : 0f);
        bool two = CarModels.TwoWheels(id);
        player.Throttle = throttleButton != null && throttleButton.Held;
        player.Brake = brakeButton != null && brakeButton.Held;
        if (speedText != null)
        {
            int kmh = Mathf.RoundToInt(player.VehicleSpeed * PlayerController.KmhPerMetre);
            int limit = TrafficNet.LimitAt(player.transform.position);
            bool over = limit > 0 && kmh > limit + 2;
            speedText.text = kmh.ToString();
            speedText.color = over ? new Color(1f, 0.45f, 0.4f) : Color.white;
            speedPlate.color = over ? new Color(0.45f, 0.06f, 0.06f, 0.85f) : new Color(0.08f, 0.09f, 0.12f, 0.82f);
            limitBadge.SetActive(limit > 0);
            limitText.text = limit.ToString();
        }
        // lean into the turn: harder the faster and tighter it goes (right turn → lean right)
        float want = two ? -Mathf.Clamp(player.YawRate * player.VehicleSpeed * 0.03f, -26f, 26f) : 0f;
        lean = Mathf.Lerp(lean, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
        transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, lean);
        if (two) player.GetComponent<CharacterModel>()?.SetLean(lean);
        // On two wheels the saddle (behind the bike's centre) sits under the rider's hips
        var ahead = CarModels.TwoWheels(id) ? transform.forward * 0.28f : Vector3.zero;
        transform.position = new Vector3(player.transform.position.x, feet, player.transform.position.z) + ahead;
    }

    private void OnDestroy()
    {
        if (Driving) GetOut(); // scene change while driving: give the player back their legs
    }

    private void ShowExitButton(bool show)
    {
        if (exitCanvas == null)
        {
            if (!show) return;
            exitCanvas = new GameObject("DriveCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(exitCanvas);
            exitCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            exitCanvas.GetComponent<Canvas>().sortingOrder = 40;
            var scaler = exitCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var button = UiKit.RoundButton(exitCanvas.transform, "ลงจากรถ", Vector2.zero, new Vector2(260f, 84f), UiKit.Gold, UiKit.GoldInk,
                () => { if (driving != null) driving.GetOut(); }, 30);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 230f);
            // the pedals, right side above the action buttons: hold คันเร่ง to go, เบรก to stop
            throttleButton = Pedal("คันเร่ง", new Vector2(-190f, 640f), 200f, UiKit.PayGreen);
            brakeButton = Pedal("เบรก", new Vector2(-420f, 610f), 150f, UiKit.Danger);
            // the speedometer above the pedals, with the limit of the road you're on beside it
            var plate = UiKit.Panel("Speedometer", exitCanvas.transform, new Color(0.08f, 0.09f, 0.12f, 0.82f), Vector2.zero, new Vector2(250f, 110f));
            plate.anchorMin = plate.anchorMax = new Vector2(1f, 0f);
            plate.anchoredPosition = new Vector2(-250f, 820f);
            speedPlate = plate.GetComponent<Image>();
            speedText = UiKit.Label(plate, 54, "0", new Vector2(36f, 12f), new Vector2(150f, 64f), Color.white);
            speedText.fontStyle = FontStyle.Bold;
            UiKit.Label(plate, 18, "กม./ชม.", new Vector2(36f, -36f), new Vector2(150f, 24f), new Color(0.75f, 0.78f, 0.82f));
            var badge = UiKit.Rect("Limit", plate, new Color(0.84f, 0.12f, 0.14f));
            badge.GetComponent<Image>().sprite = UiKit.CircleSprite;
            badge.sizeDelta = new Vector2(84f, 84f);
            badge.anchoredPosition = new Vector2(-72f, 0f);
            var inner = UiKit.Rect("Face", badge, Color.white);
            inner.GetComponent<Image>().sprite = UiKit.CircleSprite;
            inner.sizeDelta = new Vector2(64f, 64f);
            limitText = UiKit.Label(inner, 30, "", Vector2.zero, new Vector2(60f, 50f), new Color(0.08f, 0.08f, 0.08f));
            limitText.fontStyle = FontStyle.Bold;
            limitBadge = badge.gameObject;
        }
        exitCanvas.SetActive(show);
    }

    private static HoldButton Pedal(string label, Vector2 at, float size, Color color)
    {
        var b = UiKit.RoundButton(exitCanvas.transform, label, Vector2.zero, new Vector2(size, size), color, Color.white, () => { }, 30, circle: true);
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = at;
        return b.gameObject.AddComponent<HoldButton>();
    }
}
