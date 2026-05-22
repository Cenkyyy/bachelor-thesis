using System;
using UnityEngine;

/// <summary>
/// Defines damage popup colors, multiplier thresholds, cooldown, and debug display options.
/// </summary>
[Serializable]
public sealed class DamagePopupFeedbackSettings
{
    [Header("Colors")]
    [SerializeField] private Color _effectiveColor = new(0.1f, 1f, 0.85f, 1f);
    [SerializeField] private Color _resistedColor = new(1f, 0.4f, 0.15f, 1f);
    [SerializeField] private Color _neutralColor = Color.white;

    [Header("Multiplier Thresholds")]
    [SerializeField, Min(0f)] private float _ineffectiveThreshold = 0.9f;
    [SerializeField, Min(0f)] private float _effectiveThreshold = 1.1f;

    [Header("Display")]
    [SerializeField, Min(0f)] private float _cooldownSeconds = 0f;
    [SerializeField] private bool _showMultiplierInDebugBuilds = true;

    public float CooldownSeconds => Mathf.Max(0f, _cooldownSeconds);

    public Color ResolveColor(float multiplier)
    {
        var ineffectiveThreshold = Mathf.Min(_ineffectiveThreshold, _effectiveThreshold);
        var effectiveThreshold = Mathf.Max(_ineffectiveThreshold, _effectiveThreshold);

        if (multiplier < ineffectiveThreshold)
            return _resistedColor;

        if (multiplier > effectiveThreshold)
            return _effectiveColor;

        return _neutralColor;
    }

    public string BuildMessage(int damage, float multiplier)
    {
        var clampedDamage = Mathf.Max(1, damage);
        if (_showMultiplierInDebugBuilds && Debug.isDebugBuild)
            return $"{clampedDamage} (x{multiplier:0.00})";

        return clampedDamage.ToString();
    }
}
