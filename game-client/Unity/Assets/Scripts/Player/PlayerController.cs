using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private Vector3 impulseVelocity; // knockback, decays over time

    // Set by on-screen controls (virtual joystick) in the mobile phase.
    // When non-zero it takes priority over keyboard/gamepad.
    public Vector2 ExternalMoveInput { get; set; }

    // Walking by itself (AutoQuest): a world-space direction, used while the player isn't steering
    public Vector3 AutoMove { get; set; }

    // The player is steering with the joystick or keys this frame
    public bool ManualInput { get; private set; }

    // Lowered while blocking (PlayerActions)
    public float MoveSpeedMultiplier { get; set; } = 1f;

    // Injured (บาดเจ็บ) after a knockout: slower until treated
    public float InjurySlow { get; set; } = 1f;

    // Out of energy (Exertion): exhausted, walks slowly
    public float TiredSlow { get; set; } = 1f;

    // Owning a motorbike or car (bank.lua) makes getting around faster
    public float VehicleBoost { get; set; } = 1f;

    // Extra speed while driving your own car around (DrivableCar)
    public float DriveBoost { get; set; } = 1f;

    // Driving (DrivableCar): the stick only steers; the speed comes from the throttle (held: speeds up
    // to the vehicle's top speed, let go: rolls to a stop) and the brake. YawRate is how fast it's
    // turning (degrees a second, + = right), for leaning into the turn.
    public bool VehicleMode { get; set; }
    public bool Throttle { get; set; }
    public bool Brake { get; set; }
    public float VehicleSpeed { get; private set; }
    public float YawRate { get; private set; }
    private const float Accel = 0.55f, Coast = 0.3f, Braking = 1.4f; // fractions of top speed a second
    private const float StickCruise = 0.6f; // the stick alone drives along at this much of top speed
    public const float KmhPerMetre = 5.76f; // the speedometer's km/h for 1 m/s (the world is built small)

    public bool IsGrounded => controller != null && controller.isGrounded;

    // False while knocked out
    public bool InputEnabled { get; set; } = true;

    public void AddImpulse(Vector3 velocity)
    {
        velocity.y = 0f;
        impulseVelocity += velocity;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false; // CharacterController overrides direct position changes
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        verticalVelocity = 0f;
        impulseVelocity = Vector3.zero;
    }

    public bool Jump()
    {
        if (!InputEnabled || !controller.isGrounded) return false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        Exertion.Jumped();
        return true;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        CharacterModel.Attach(gameObject);
        if (GetComponent<Exertion>() == null) gameObject.AddComponent<Exertion>(); // walking and fighting cost energy

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = InputEnabled ? ReadMoveInput() : Vector2.zero;
        ManualInput = input.sqrMagnitude > 0.01f;

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
        if (VehicleMode)
        {
            Drive(Vector3.ClampMagnitude(move, 1f));
            return;
        }
        VehicleSpeed = 0f;
        YawRate = 0f;
        if (!ManualInput && InputEnabled && AutoMove.sqrMagnitude > 0.0001f) move = new Vector3(AutoMove.x, 0f, AutoMove.z);
        move = Vector3.ClampMagnitude(move, 1f);

        if (move.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                turnSpeed * Time.deltaTime
            );
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * (moveSpeed * MoveSpeedMultiplier * InjurySlow * TiredSlow * VehicleBoost * DriveBoost) + impulseVelocity;
        velocity.y = verticalVelocity;
        impulseVelocity = Vector3.Lerp(impulseVelocity, Vector3.zero, 1f - Mathf.Exp(-8f * Time.deltaTime));

        controller.Move(velocity * Time.deltaTime);
    }

    // The vehicle: steer towards the stick, speed from the throttle and brake
    private void Drive(Vector3 steer)
    {
        float dt = Time.deltaTime;
        float top = moveSpeed * DriveBoost;
        bool throttle = Throttle, brake = Brake;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) { throttle |= kb.leftShiftKey.isPressed; brake |= kb.spaceKey.isPressed; }
#endif
        if (!InputEnabled) { throttle = false; brake = true; }
        // คันเร่ง: full speed; just the stick: along at a steady pace (one thumb is enough); เบรก: stop
        float target = brake ? 0f : throttle ? top : top * StickCruise * Mathf.Clamp01((steer.magnitude - 0.2f) / 0.6f);
        float change = brake ? Braking : target > VehicleSpeed ? Accel : Coast;
        VehicleSpeed = Mathf.MoveTowards(VehicleSpeed, target, top * change * dt);

        float yaw = transform.eulerAngles.y, before = yaw;
        if (steer.sqrMagnitude > 0.04f)
        {
            float want = Quaternion.LookRotation(steer).eulerAngles.y;
            float k = Mathf.Clamp01(VehicleSpeed / Mathf.Max(top, 0.01f));
            float rate = Mathf.Lerp(70f, 110f, Mathf.Clamp01(VehicleSpeed / 2f)) * Mathf.Lerp(1f, 0.6f, k); // tighter turns slowly
            yaw = Mathf.MoveTowardsAngle(yaw, want, rate * dt);
        }
        YawRate = dt > 0f ? Mathf.DeltaAngle(before, yaw) / dt : 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * dt;
        Vector3 velocity = transform.forward * VehicleSpeed + impulseVelocity;
        velocity.y = verticalVelocity;
        impulseVelocity = Vector3.Lerp(impulseVelocity, Vector3.zero, 1f - Mathf.Exp(-8f * dt));
        CollisionFlags hit = controller.Move(velocity * dt);
        if ((hit & CollisionFlags.Sides) != 0) VehicleSpeed *= 0.5f; // ran into something
    }

    private Vector2 ReadMoveInput()
    {
        if (ExternalMoveInput.sqrMagnitude > 0.0001f)
            return Vector2.ClampMagnitude(ExternalMoveInput, 1f);

#if ENABLE_INPUT_SYSTEM
        Vector2 value = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) value.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) value.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) value.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) value.x -= 1f;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null && value == Vector2.zero)
            value = pad.leftStick.ReadValue();

        return Vector2.ClampMagnitude(value, 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }
}
