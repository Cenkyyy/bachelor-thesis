using System;
using UnityEngine;

/// <summary>
/// Centralizes player gameplay input and publishes player action requests to runtime controllers.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private PlayerMovementController _movement;
    [SerializeField] private PlayerAnimationController _animation;

    [Header("Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;

    public Vector2 MoveInput { get; private set; }

    public event Action ConsumePressed;
    public event Action<bool> DropPressed;

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
        {
            MoveInput = Vector2.zero;
            ApplyMoveInput();
            return;
        }

        var gameplayInputBlocked = IsGameplayInputBlocked();
        if (gameplayInputBlocked)
        {
            MoveInput = Vector2.zero;
        }
        else
        {
            MoveInput = ReadMoveInput();

            if (Input.GetKeyDown(_inputBindings.ConsumeKey))
                ConsumePressed?.Invoke();
        }

        if (CanReadDropInput(gameplayInputBlocked) && Input.GetKeyDown(_inputBindings.DropKey))
            DropPressed?.Invoke(Input.GetKey(_inputBindings.DropAllModifierKey));

        ApplyMoveInput();
    }

    private void OnDisable()
    {
        MoveInput = Vector2.zero;
        ApplyMoveInput();
    }

    private bool IsGameplayInputBlocked()
    {
        return PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput;
    }

    private bool CanReadDropInput(bool gameplayInputBlocked)
    {
        return !gameplayInputBlocked || _backpackPanel.IsOpen;
    }

    private Vector2 ReadMoveInput()
    {
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");
        var input = new Vector2(x, y);

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private void ApplyMoveInput()
    {
        _movement.SetMovementInput(MoveInput);
        _animation.SetWalkingState(MoveInput != Vector2.zero);
    }
}
