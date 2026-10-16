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

    // Set by on-screen controls (virtual joystick) in the mobile phase.
    // When non-zero it takes priority over keyboard/gamepad.
    public Vector2 ExternalMoveInput { get; set; }

    // Lowered while blocking (PlayerActions)
    public float MoveSpeedMultiplier { get; set; } = 1f;

    public bool IsGrounded => controller != null && controller.isGrounded;

    public bool Jump()
    {
        if (!controller.isGrounded) return false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        return true;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
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

        Vector3 velocity = move * (moveSpeed * MoveSpeedMultiplier);
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
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
