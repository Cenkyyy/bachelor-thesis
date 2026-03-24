using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private SpellCastingPanelController _castingPanel;
    [SerializeField] private WorldTextPopupEmitter _feedbackPopup;

    [Header("Feedback")]
    [SerializeField] private string _castOnCooldownMessage = "Spell is recharging";
    [SerializeField] private string _insufficientManaMessage = "Need to restore mana";

    [Header("Targeting")]
    [SerializeField] private LayerMask _targetMask;

    [Header("Word Effectiveness")]
    [SerializeField] private SpellWordEffectivenessData _wordEffectivenessData;
    [SerializeField] private DamageWordTextPopupSettings _damageWordTextPopupSettings = new();

    [Header("Runtime Tuning")]
    [SerializeField] private SpellRuntimeSettings _settings = new()
    {
        Range = 8f,
        HitRadius = 0.5f,
        WaveArcAngle = 80f,
        Shard = new FormRuntimeSettings { ManaCost = 8, CooldownSeconds = 0.25f, BaseDamage = 18f },
        Beam = new FormRuntimeSettings { ManaCost = 14, CooldownSeconds = 0.9f, BaseDamage = 8f },
        Wave = new FormRuntimeSettings { ManaCost = 12, CooldownSeconds = 0.6f, BaseDamage = 14f },
        Barrage = new FormRuntimeSettings { ManaCost = 15, CooldownSeconds = 0.7f, BaseDamage = 8f },
        BeamDuration = 1.0f,
        BeamTickInterval = 0.2f,
        BarrageProjectileCount = 4,
        BarrageInterval = 0.08f,
        LightningBonusMultiplier = 1.35f,
        DotDamagePerSecond = 5f,
        StatusDuration = 3f,
        Piercing = new ModifierRuntimeSettings { AdditionalManaCost = 2, AdditionalCooldownSeconds = 0.1f },
        Stunning = new ModifierRuntimeSettings { AdditionalManaCost = 3, AdditionalCooldownSeconds = 0.2f },
        Exploding = new ModifierRuntimeSettings { AdditionalManaCost = 4, AdditionalCooldownSeconds = 0.25f },
        Reclaiming = new ModifierRuntimeSettings { AdditionalManaCost = 2, AdditionalCooldownSeconds = 0.1f },
        Splitting = new ModifierRuntimeSettings { AdditionalManaCost = 3, AdditionalCooldownSeconds = 0.2f },
        StunBuildupPerHit = 1f,
        StunThreshold = 3f,
        StunDuration = 1.25f,
        ExplosionRadius = 1.8f,
        ExplosionDamageMultiplier = 0.7f,
        ReclaimManaPerHit = 2,
        MaxReclaimsPerCast = 2,
        PoisonCloudRadius = 1.9f,
        PoisonCloudDuration = 4f
    };

    private readonly List<Collider2D> _targetBuffer = new(32);
    private ContactFilter2D _targetFilter;
    private float _nextCastAllowedAt;

    public event Action<SpellPhrase> OnSpellCastCommitted;

    private void Awake()
    {
        _targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _targetMask,
            useTriggers = true
        };
    }

    private void OnEnable()
    {
        if (_castingPanel != null)
            _castingPanel.OnPhraseCompleted += HandlePhraseCompleted;
    }

    private void OnDisable()
    {
        if (_castingPanel != null)
            _castingPanel.OnPhraseCompleted -= HandlePhraseCompleted;
    }

    private void HandlePhraseCompleted(SpellPhrase phrase)
    {
        if (!phrase.IsComplete)
            return;

        if (Time.time < _nextCastAllowedAt)
        {
            _feedbackPopup.ShowMessage(_castOnCooldownMessage);
            return;
        }

        var formSettings = _settings.GetFormSettings(phrase.Form.Value);
        var modifierSettings = _settings.GetModifierSettings(phrase.Modifier.Value);

        var manaCost = formSettings.ManaCost + modifierSettings.AdditionalManaCost;
        var cooldown = formSettings.CooldownSeconds + modifierSettings.AdditionalCooldownSeconds;

        if (_player == null || _player.Data == null)
            return;

        if (_player.Data.CurrentMana < manaCost)
        {
            _feedbackPopup.ShowMessage(_insufficientManaMessage);
            return;
        }

        _player.Data.UseMana(manaCost);
        _nextCastAllowedAt = Time.time + cooldown;
        OnSpellCastCommitted?.Invoke(phrase);

        var castState = new CastState(
            phrase.Modifier.Value,
            phrase.Element.Value,
            phrase.Form.Value,
            GetDamageAfterItemBonuses(formSettings.BaseDamage),
            _settings.MaxReclaimsPerCast);
        StartCoroutine(ExecuteCast(castState));
    }

    private float GetDamageAfterItemBonuses(float baseDamage)
    {
        if (_player == null || _player.Data == null)
            return Mathf.Max(0f, baseDamage);

        return Mathf.Max(0f, baseDamage + _player.Data.SpellDamageBonus);
    }

    private IEnumerator ExecuteCast(CastState castState)
    {
        var directions = GetCastDirections();
        if (castState.Modifier == ModifierWord.Splitting)
        {
            for (var i = 0; i < directions.Length; i++)
                directions[i] = Rotate(directions[0], i == 0 ? 0f : (i == 1 ? -45f : 45f));

            castState.BaseDamage /= 3f;
        }
        else
        {
            directions = new[] { directions[0] };
        }

        switch (castState.Form)
        {
            case FormWord.Shard:
                foreach (var direction in directions)
                    FireLineSpell(castState, direction, oncePerTarget: true);
                break;

            case FormWord.Beam:
                yield return RunBeam(castState, directions);
                break;

            case FormWord.Wave:
                foreach (var direction in directions)
                    FireWaveSpell(castState, direction);
                break;

            case FormWord.Barrage:
                yield return RunBarrage(castState, directions);
                break;
        }
    }

    private IEnumerator RunBeam(CastState castState, IReadOnlyList<Vector2> directions)
    {
        var elapsed = 0f;
        while (elapsed < _settings.BeamDuration)
        {
            foreach (var direction in directions)
                FireLineSpell(castState, direction, oncePerTarget: false);

            elapsed += _settings.BeamTickInterval;
            yield return new WaitForSeconds(_settings.BeamTickInterval);
        }
    }

    private IEnumerator RunBarrage(CastState castState, IReadOnlyList<Vector2> directions)
    {
        for (var i = 0; i < _settings.BarrageProjectileCount; i++)
        {
            foreach (var direction in directions)
                FireLineSpell(castState, direction, oncePerTarget: true);

            yield return new WaitForSeconds(_settings.BarrageInterval);
        }
    }

    private void FireLineSpell(CastState castState, Vector2 direction, bool oncePerTarget)
    {
        var origin = (Vector2)transform.position;
        QueryTargetsInRadius(origin, _settings.Range);
        var nearestDistance = float.MaxValue;
        var nearestTarget = (ISpellTarget)null;

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var toTarget = target.Position - origin;
            var projected = Vector2.Dot(toTarget, direction);
            if (projected <= 0f || projected > _settings.Range)
                continue;

            var lateralDistance = Mathf.Abs(Vector3.Cross(direction, toTarget.normalized).z) * toTarget.magnitude;
            if (lateralDistance > _settings.HitRadius)
                continue;

            if (castState.Modifier == ModifierWord.Piercing)
            {
                ApplyHit(castState, target, target.Position);
                if (oncePerTarget)
                    castState.MarkTargetAsHit(target);
                continue;
            }

            if (oncePerTarget && castState.WasTargetHit(target))
                continue;

            if (projected < nearestDistance)
            {
                nearestDistance = projected;
                nearestTarget = target;
            }
        }

        if (nearestTarget != null)
        {
            ApplyHit(castState, nearestTarget, nearestTarget.Position);
            if (oncePerTarget)
                castState.MarkTargetAsHit(nearestTarget);
        }
    }

    private void FireWaveSpell(CastState castState, Vector2 direction)
    {
        var origin = (Vector2)transform.position;
        QueryTargetsInRadius(origin, _settings.Range);

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var toTarget = target.Position - origin;
            if (toTarget.magnitude > _settings.Range)
                continue;

            var angle = Vector2.Angle(direction, toTarget);
            if (angle > _settings.WaveArcAngle * 0.5f)
                continue;

            ApplyHit(castState, target, target.Position);
        }
    }

    private void ApplyHit(CastState castState, ISpellTarget target, Vector2 hitPosition)
    {
        var effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
        var damage = castState.BaseDamage * effectivenessMultiplier;

        if (castState.Element == ElementWord.Lightning && target.HasAnyNegativeStatus)
            damage *= _settings.LightningBonusMultiplier;
        
        var displayedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

        DamageWordTextPopupUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damageWordTextPopupSettings);

        target.ReceiveSpellDamage(damage, _player);
        ApplyElementStatus(castState, target, hitPosition, effectivenessMultiplier);
        ApplyModifierSideEffects(castState, target, hitPosition);
    }

    private float ResolveWordEffectivenessMultiplier(CastState castState, ISpellTarget target)
    {
        if (target == null)
            return _wordEffectivenessData.NeutralMultiplier;

        if (target is not Component targetComponent)
            return _wordEffectivenessData.NeutralMultiplier;

        var effectivenessReceiver = targetComponent.GetComponentInParent<ISpellWordEffectivenessTarget>();
        if (effectivenessReceiver == null || !effectivenessReceiver.TryGetSpellWordEffectivenessData(out var biome, out var roleTags))
            return _wordEffectivenessData.NeutralMultiplier;

        return _wordEffectivenessData.CalculateFinalMultiplier(biome, roleTags, castState.Modifier, castState.Element, castState.Form);
    }

    private void ApplyElementStatus(CastState castState, ISpellTarget target, Vector2 hitPosition, float effectivenessMultiplier)
    {
        switch (castState.Element)
        {
            case ElementWord.Poison:
                if (castState.TryConsumePoisonCloud())
                    SpawnPoisonCloud(hitPosition);
                break;
            case ElementWord.Frost:
                target.ApplyStatus(CombatStatusEffect.Slow, _settings.StatusDuration);
                break;
            case ElementWord.Ember:
                target.ApplyStatus(CombatStatusEffect.Burn, _settings.StatusDuration);
                target.ApplyDamageOverTime(_settings.DotDamagePerSecond * effectivenessMultiplier, _settings.StatusDuration, effectivenessMultiplier, _player);
                break;
            case ElementWord.Dark:
                target.ApplyStatus(CombatStatusEffect.Weakened, _settings.StatusDuration);
                break;
        }
    }

    private void ApplyModifierSideEffects(CastState castState, ISpellTarget target, Vector2 hitPosition)
    {
        switch (castState.Modifier)
        {
            case ModifierWord.Stunning:
                target.AddStunBuildup(_settings.StunBuildupPerHit, _settings.StunThreshold, _settings.StunDuration);
                break;

            case ModifierWord.Exploding:
                if (!castState.TryConsumeExplosion())
                    return;

                ApplyExplosion(castState, hitPosition);
                break;

            case ModifierWord.Reclaiming:
                if (!castState.TryConsumeReclaim())
                    return;

                _player.Data.RecoverMana(_settings.ReclaimManaPerHit);
                break;
        }
    }

    private void ApplyExplosion(CastState castState, Vector2 center)
    {
        QueryTargetsInRadius(center, _settings.ExplosionRadius);
        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
            var damage = castState.BaseDamage * _settings.ExplosionDamageMultiplier * effectivenessMultiplier;
            var displayedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            DamageWordTextPopupUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damageWordTextPopupSettings);
            target.ReceiveSpellDamage(damage, _player);
        }
    }

    private void QueryTargetsInRadius(Vector2 center, float radius)
    {
        _targetBuffer.Clear();
        Physics2D.OverlapCircle(center, radius, _targetFilter, _targetBuffer);
    }

    private void SpawnPoisonCloud(Vector2 position)
    {
        var cloud = new GameObject("PoisonCloud");
        cloud.transform.position = position;
        var zone = cloud.AddComponent<PoisonCloudZone>();
        zone.Initialize(_settings.PoisonCloudRadius, _settings.PoisonCloudDuration, _settings.DotDamagePerSecond, _targetMask, _player);
    }

    private Vector2[] GetCastDirections()
    {
        var mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : (Vector2)transform.position + Vector2.right;

        var forward = (mouseWorld - (Vector2)transform.position).normalized;
        if (forward == Vector2.zero)
            forward = Vector2.right;

        return new[] { forward, forward, forward };
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y).normalized;
    }

    private sealed class CastState
    {
        private readonly HashSet<ISpellTarget> _hitTargets = new();
        private int _remainingExplosions = 1;
        private int _remainingPoisonClouds = 1;
        private int _remainingReclaims;

        public ModifierWord Modifier { get; }
        public ElementWord Element { get; }
        public FormWord Form { get; }
        public float BaseDamage { get; set; }

        public CastState(ModifierWord modifier, ElementWord element, FormWord form, float baseDamage, int maxReclaimsPerCast)
        {
            Modifier = modifier;
            Element = element;
            Form = form;
            BaseDamage = baseDamage;
            _remainingReclaims = Mathf.Max(0, maxReclaimsPerCast);
        }

        public bool WasTargetHit(ISpellTarget target) => _hitTargets.Contains(target);

        public void MarkTargetAsHit(ISpellTarget target)
        {
            _hitTargets.Add(target);
        }

        public bool TryConsumeExplosion()
        {
            if (_remainingExplosions <= 0)
                return false;

            _remainingExplosions--;
            return true;
        }

        public bool TryConsumeReclaim()
        {
            if (_remainingReclaims <= 0)
                return false;

            _remainingReclaims--;
            return true;
        }

        public bool TryConsumePoisonCloud()
        {
            if (_remainingPoisonClouds <= 0)
                return false;

            _remainingPoisonClouds--;
            return true;
        }
    }
}
