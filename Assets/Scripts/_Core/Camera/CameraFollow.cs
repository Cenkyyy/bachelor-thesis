using UnityEngine;

/// <summary>
/// Smoothly follows the assigned target while preserving the camera z position.
/// </summary>
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;

    [Header("Follow")]
    [SerializeField] private float _followSpeed = 10f;

    [Header("Pause Settings")]
    [SerializeField] private bool _useUnscaledTimeWhenPaused = true;

    private void LateUpdate()
    {
        if (!_target)
            return;

        // use unscaled delta time if the game is paused allowing the camera to follow the target during transitions or pre-game states, otherwise use regular delta time
        float deltaTime = Time.deltaTime;
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused && _useUnscaledTimeWhenPaused)
            deltaTime = Time.unscaledDeltaTime;

        var pos = _target.position;
        pos.z = transform.position.z;

        transform.position = Vector3.Lerp(transform.position, pos, _followSpeed * deltaTime);
    }
}
