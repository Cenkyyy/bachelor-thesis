using UnityEngine;

public interface IWorldShape
{
    Vector2 Center { get; }

    bool IsInsidePlayable(Vector2 position);
    bool IsInsideBorder(Vector2 position);

    Vector2 SamplePoint(System.Random rng);
    float GetNormalizedDistanceFromCenter(Vector2 position);
}
