using UnityEngine;

public static class MovementPushResistanceUtils
{
    private const float DirectionEpsilonSq = 1e-4f;
    public const int DefaultCastBufferSize = 8;

    public static RaycastHit2D[] CreateCastBuffer(int size = DefaultCastBufferSize)
    {
        return new RaycastHit2D[Mathf.Max(1, size)];
    }

    public static bool ShouldReducePush(Rigidbody2D body, Vector2 direction, float castDistance, RaycastHit2D[] castHits, LayerMask pushTargetMask)
    {
        if (body == null || castHits == null || castHits.Length == 0)
        {
            return false;
        }

        if (direction.sqrMagnitude <= DirectionEpsilonSq || castDistance <= 0f)
        {
            return false;
        }

        var hitCount = body.Cast(direction, castHits, castDistance);
        for (var i = 0; i < hitCount; i++)
        {
            var hitCollider = castHits[i].collider;
            if (hitCollider == null || hitCollider.attachedRigidbody == body)
            {
                continue;
            }

            if (IsLayerInMask(hitCollider.gameObject.layer, pushTargetMask))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
