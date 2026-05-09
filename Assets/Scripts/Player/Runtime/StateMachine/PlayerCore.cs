using UnityEngine;

public class PlayerCore : StateMachineCore
{
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerAnimationController _animation;

    private Vector2 _input;

    public Vector2 ReadMoveInput()
    {
        if (GameStateManager.IsGamePaused)
            return Vector2.zero;

        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");
        var v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    public void ApplyMovement(Vector2 input)
    {
        _input = input;
        _movement.SetMovementInput(_input);
        _animation.SetWalkingState(_input != Vector2.zero);
    }

    public void StopMovement()
    {
        ApplyMovement(Vector2.zero);
    }
}
