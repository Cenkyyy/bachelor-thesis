using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies poison damage and poison status to combat targets inside a temporary area.
/// </summary>
public sealed class PoisonCloudZone : MonoBehaviour
{
    private const float TickIntervalSeconds = 1f;

    [Header("References")]
    [SerializeField] private CapsuleCollider2D _hitbox;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;

    [Header("Animation")]
    [SerializeField] private string _fadeOutTrigger = "FadeOut";
    [SerializeField, Min(0f)] private float _fadeOutDurationSeconds = 0.75f;

    private readonly List<Collider2D> _buffer = new(32);

    private LayerMask _targetMask;
    private ContactFilter2D _targetFilter;
    private float _duration;
    private float _dps;
    private float _elapsed;
    private float _tickAccumulator;
    private object _damageSource;
    private DamagePopupFeedbackSettings _damagePopupFeedbackSettings;
    private bool _isFadingOut;

    private void Update()
    {
        if (_isFadingOut)
            return;

        float delta = Time.deltaTime;
        _elapsed += delta;

        if (_elapsed >= _duration)
        {
            BeginFadeOut();
            return;
        }

        _tickAccumulator += delta;

        while (_tickAccumulator >= TickIntervalSeconds)
        {
            _tickAccumulator -= TickIntervalSeconds;

            float remainingDuration = Mathf.Max(0f, _duration - (_elapsed - _tickAccumulator));
            float tickDuration = Mathf.Min(TickIntervalSeconds, remainingDuration);
            ApplyPoisonTick(tickDuration);
        }

    }

    public void Initialize(
        float durationSeconds,
        float damagePerSecond,
        LayerMask targetMask,
        object damageSource,
        Material elementMaterial,
        DamagePopupFeedbackSettings damagePopupFeedbackSettings)
    {
        _duration = durationSeconds;
        _dps = damagePerSecond;
        _targetMask = targetMask;
        _damageSource = damageSource;
        _damagePopupFeedbackSettings = damagePopupFeedbackSettings;
        _targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _targetMask,
            useTriggers = true
        };

        if (_hitbox != null)
            _hitbox.isTrigger = true;

        if (_spriteRenderer != null && elementMaterial != null)
            _spriteRenderer.material = elementMaterial;
    }

    private void ApplyPoisonTick(float tickDuration)
    {
        if (tickDuration <= 0f || _hitbox == null)
            return;

        _buffer.Clear();
        _hitbox.Overlap(_targetFilter, _buffer);

        for (var i = 0; i < _buffer.Count; i++)
        {
            if (!SpellCombatTargetUtility.TryGetCombatTarget(_buffer[i], out ICombatTarget target))
                continue;

            if (!SpellCombatTargetUtility.TryApplyDamage(target, _dps * tickDuration, _damageSource, out int displayedDamage))
                continue;

            DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, 1f, _damagePopupFeedbackSettings);

            IStatusEffectTarget statusTarget = SpellCombatTargetUtility.GetStatusEffectTarget(target);
            statusTarget?.ApplyStatus(CombatStatusEffectType.Poison, TickIntervalSeconds);
        }
    }

    private void BeginFadeOut()
    {
        _isFadingOut = true;

        if (_hitbox != null)
            _hitbox.enabled = false;

        if (TryTriggerFadeOut())
            Destroy(gameObject, _fadeOutDurationSeconds);
        else
            Destroy(gameObject);
    }

    private bool TryTriggerFadeOut()
    {
        if (_animator == null || string.IsNullOrWhiteSpace(_fadeOutTrigger))
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger && parameters[i].name == _fadeOutTrigger)
            {
                _animator.SetTrigger(_fadeOutTrigger);
                return true;
            }
        }

        return false;
    }
}
