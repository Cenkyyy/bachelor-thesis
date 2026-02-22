using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// One line of item pickup text with delayed fade out.
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemPickupFeedEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Coroutine _lifetimeRoutine;

    public void Initialize(string message, float visibleDuration, float fadeDuration)
    {
        if (_label == null)
            return;

        _label.text = message;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);

        _lifetimeRoutine = StartCoroutine(RunLifetime(visibleDuration, fadeDuration));
    }

    private IEnumerator RunLifetime(float visibleDuration, float fadeDuration)
    {
        if (visibleDuration > 0f)
            yield return new WaitForSeconds(visibleDuration);

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - progress;

            yield return null;
        }

        Destroy(gameObject);
    }
}