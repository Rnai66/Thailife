using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Network-synced action events. Values are sent as one byte — don't renumber.
public enum PlayerAction : byte
{
    Punch = 1,
    Kick = 2,
    BlockStart = 3,
    BlockEnd = 4,
    Jump = 5,
}

// Buttons the player can press (on-screen, keyboard or gamepad).
public enum ActionInput
{
    Jump,   // A — bottom  (PlayStation ✕) · Space · Gamepad South
    Kick,   // B — right   (PlayStation ○) · K     · Gamepad East
    Punch,  // C — left    (PlayStation □) · J     · Gamepad West
    Block,  // D — top     (PlayStation △) · L     · Gamepad North (hold)
}

// Combat/movement actions for the boxer career later on. Attacks are placeholders until
// animations and hit detection exist; ActionFeedback shows where the hit would land.
[RequireComponent(typeof(PlayerController))]
public class PlayerActions : MonoBehaviour
{
    [SerializeField] private float punchCooldown = 0.35f;
    [SerializeField] private float kickCooldown = 0.6f;
    [SerializeField] private float blockMoveMultiplier = 0.4f;

    // Raised for every action this player performs (MatchSync sends them to others).
    public event Action<PlayerAction> Performed;

    public bool IsBlocking { get; private set; }

    private PlayerController controller;
    private ActionFeedback feedback;
    private PlayerCombat combat;
    private float nextAttackTime;
    private bool blockHeldByButton;
    private bool blockHeldByKey;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        feedback = GetComponent<ActionFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<ActionFeedback>();
        combat = PlayerCombat.GetOrAdd(gameObject);
    }

    public void Press(ActionInput input)
    {
        if (combat.IsKnockedOut) return;

        switch (input)
        {
            case ActionInput.Jump:
                if (!IsBlocking && controller.Jump()) Perform(PlayerAction.Jump);
                break;
            case ActionInput.Punch:
                TryAttack(PlayerAction.Punch, punchCooldown);
                break;
            case ActionInput.Kick:
                TryAttack(PlayerAction.Kick, kickCooldown);
                break;
            case ActionInput.Block:
                blockHeldByButton = true;
                RefreshBlock();
                break;
        }
    }

    public void Release(ActionInput input)
    {
        if (input != ActionInput.Block) return;
        blockHeldByButton = false;
        RefreshBlock();
    }

    private void Update()
    {
        ReadKeyboardAndGamepad();
    }

    private void TryAttack(PlayerAction attack, float cooldown)
    {
        if (IsBlocking || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + cooldown;
        Perform(attack);
        combat.PerformAttack(attack);
    }

    // Re-evaluates blocking from held buttons/keys (also called by PlayerCombat on knockout/respawn).
    public void RefreshBlock()
    {
        bool blocking = (blockHeldByButton || blockHeldByKey) && !combat.IsKnockedOut;
        if (blocking == IsBlocking) return;

        IsBlocking = blocking;
        controller.MoveSpeedMultiplier = blocking ? blockMoveMultiplier : 1f;
        Perform(blocking ? PlayerAction.BlockStart : PlayerAction.BlockEnd);
    }

    private void Perform(PlayerAction action)
    {
        feedback.Play(action);
        Performed?.Invoke(action);
    }

    private void OnDisable()
    {
        blockHeldByButton = blockHeldByKey = false;
        RefreshBlock();
    }

    private void ReadKeyboardAndGamepad()
    {
        if (!controller.InputEnabled) return; // typing a name, sleeping, playing music, knocked out

        bool jump = false, punch = false, kick = false, blockHeld = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            jump |= kb.spaceKey.wasPressedThisFrame;
            punch |= kb.jKey.wasPressedThisFrame;
            kick |= kb.kKey.wasPressedThisFrame;
            blockHeld |= kb.lKey.isPressed;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            jump |= pad.buttonSouth.wasPressedThisFrame;
            kick |= pad.buttonEast.wasPressedThisFrame;
            punch |= pad.buttonWest.wasPressedThisFrame;
            blockHeld |= pad.buttonNorth.isPressed;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        jump = Input.GetKeyDown(KeyCode.Space);
        punch = Input.GetKeyDown(KeyCode.J);
        kick = Input.GetKeyDown(KeyCode.K);
        blockHeld = Input.GetKey(KeyCode.L);
#endif

        if (blockHeld != blockHeldByKey)
        {
            blockHeldByKey = blockHeld;
            RefreshBlock();
        }

        if (jump) Press(ActionInput.Jump);
        if (punch) Press(ActionInput.Punch);
        if (kick) Press(ActionInput.Kick);
    }
}
