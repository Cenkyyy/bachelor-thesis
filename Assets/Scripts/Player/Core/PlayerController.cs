using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(PlayerAnimation))]
public class PlayerController : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    private PlayerAnimation _playerAnimation;

    private Vector2 _input;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAnimation = GetComponent<PlayerAnimation>();
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
        {
            // zero the input so the player doesn't move while the game is paused
            _input = Vector2.zero;
        }
        else
        {
            CheckInput();
        }

        _playerMovement.SetMovementInput(_input);
        _playerAnimation.SetWalkingState(_input != Vector2.zero);
    }

    private void CheckInput()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");

        _input.Normalize();
    }
}
