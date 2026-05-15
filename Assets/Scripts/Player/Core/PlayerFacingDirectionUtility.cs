using UnityEngine;

/// <summary>
/// Converts directional vectors into the cardinal facing directions used by player animation and item visuals.
/// </summary>
public static class PlayerFacingDirectionUtility
{
    public static PlayerFacingDirection FromVector(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? PlayerFacingDirection.Right : PlayerFacingDirection.Left;

        return direction.y >= 0f ? PlayerFacingDirection.Up : PlayerFacingDirection.Down;
    }

    public static Vector2 ToVector(PlayerFacingDirection direction)
    {
        return direction switch
        {
            PlayerFacingDirection.Up => Vector2.up,
            PlayerFacingDirection.Left => Vector2.left,
            PlayerFacingDirection.Right => Vector2.right,
            _ => Vector2.down
        };
    }
}

/// <summary>
/// Cardinal direction currently faced by the player.
/// </summary>
public enum PlayerFacingDirection
{
    Down,
    Up,
    Left,
    Right
}
