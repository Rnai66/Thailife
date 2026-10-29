using UnityEngine;
using UnityEngine.UI;

// Your own vehicle (bought at the bank), parked under the stilt house by HomeCar. Walk up to it to
// get in (ขับรถ) and drive it around; the "ลงจากรถ" button gets out and leaves it where you stop.
// In a car the player is hidden inside; on a bicycle or motorbike they stay visible in the saddle.
[RequireComponent(typeof(BoxCollider))]
public class DrivableCar : MonoBehaviour
{
    private static GameObject exitCanvas;
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
        transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
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
        }
        exitCanvas.SetActive(show);
    }
}
