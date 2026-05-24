/// <summary>
/// Contract for scene systems that must finish setup before a transition can reveal the scene.
/// </summary>
public interface ISceneTransitionReadinessBlocker
{
    bool IsReadyForSceneReveal { get; }
}
