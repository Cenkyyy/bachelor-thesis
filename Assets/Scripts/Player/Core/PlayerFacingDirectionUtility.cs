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
