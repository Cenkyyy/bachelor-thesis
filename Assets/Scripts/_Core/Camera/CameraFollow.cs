using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _followSpeed = 10f;

    [Header("Pause Settings")]
    [SerializeField] private bool _useUnscaledTimeWhenPaused = true; // When true, camera follow uses unscaled time while the game is paused, allowing it to follow the target even during transitions/pre-game states

    private void LateUpdate()
    {
        if (!_target)
            return;

        // Use unscaled delta time if the game is paused allowing the camera to follow the target during transitions or pre-game states, otherwise use regular delta time
        float deltaTime = Time.deltaTime;
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused && _useUnscaledTimeWhenPaused)
            deltaTime = Time.unscaledDeltaTime;

        var pos = _target.position;
        pos.z = transform.position.z;

        transform.position = Vector3.Lerp(transform.position, pos, _followSpeed * deltaTime);
    }
}
