using UnityEngine;

public enum PlayerFacingDirection
{
    Down,
    Up,
    Left,
    Right
}

public static class PlayerFacingDirectionUtility
{
    public static PlayerFacingDirection FromVector(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? PlayerFacingDirection.Right : PlayerFacingDirection.Left;
        }

        return direction.y >= 0f ? PlayerFacingDirection.Up : PlayerFacingDirection.Down;
    }
}
