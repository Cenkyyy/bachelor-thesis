using UnityEngine;

public sealed class CircularWorldShape : IWorldShape
{
    private readonly Vector2 _center;
    private readonly float _playableRadius;
    private readonly float _borderOuterRadius;
    private readonly float _playableRadiusSq;
    private readonly float _borderOuterRadiusSq;

    public Vector2 Center => _center;

    public CircularWorldShape(int width, int height, float playableRadius, float borderThickness)
    {
        _center = new Vector2(width * 0.5f, height * 0.5f);
        _playableRadius = Mathf.Max(0.1f, playableRadius);
        _borderOuterRadius = _playableRadius + Mathf.Max(0f, borderThickness);
        _playableRadiusSq = _playableRadius * _playableRadius;
        _borderOuterRadiusSq = _borderOuterRadius * _borderOuterRadius;
    }

    public bool IsInsidePlayable(Vector2 position)
    {
        return (position - _center).sqrMagnitude <= _playableRadiusSq;
    }

    public bool IsInsideBorder(Vector2 position)
    {
        return (position - _center).sqrMagnitude <= _borderOuterRadiusSq;
    }

    public Vector2 SamplePoint(System.Random rng)
    {
        float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
        float distance = Mathf.Sqrt((float)rng.NextDouble()) * _playableRadius;
        return _center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    public float GetNormalizedDistanceFromCenter(Vector2 position)
    {
        if (_playableRadius <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp01((position - _center).magnitude / _playableRadius);
    }
}
