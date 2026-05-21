using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerHeldItemVisualController _playerHeldItemVisual;
    [SerializeField] private SpellCastingPanelController _castingPanel;
    [SerializeField] private WorldTextPopupController _feedbackPopup;

    [Header("Feedback")]
    [SerializeField] private string _castOnCooldownMessage = "Spell is recharging";
    [SerializeField] private string _insufficientManaMessage = "Need to restore mana";

    [Header("Targeting")]
    [SerializeField] private LayerMask _targetMask;

    [Header("Word Effectiveness")]
    [SerializeField] private SpellWordEffectivenessData _wordEffectivenessData;
    [SerializeField] private DamagePopupFeedbackSettings _damagePopupFeedbackSettings = new();

    private readonly List<Collider2D> _targetBuffer = new(32);
    private ContactFilter2D _targetFilter;
    private float _nextCastAllowedAt;

    public event Action<ResolvedSpellCast> OnSpellCastCommitted;

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

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return;

        if (Time.time < _nextCastAllowedAt)
        {
            _feedbackPopup.ShowMessage(_castOnCooldownMessage);
            return;
        }

        if (_player == null || _player.Data == null)
            return;

        var modifierData = phrase.Modifier;
        var elementData = phrase.Element;
        var formData = phrase.Form;
        if (modifierData == null || elementData == null || formData == null)
            return;

        var manaCost = Mathf.Max(0, formData.ManaCost) + Mathf.Max(0, modifierData.AdditionalManaCost);
        var cooldown = Mathf.Max(0f, formData.CooldownSeconds) + Mathf.Max(0f, modifierData.AdditionalCooldownSeconds);

        if (_player.Data.CurrentMana < manaCost)
        {
            _feedbackPopup.ShowMessage(_insufficientManaMessage);
            return;
        }

        _player.Data.UseMana(manaCost);
        _nextCastAllowedAt = Time.time + cooldown;

        var origin = (Vector2)_playerHeldItemVisual.CurrentHandAnchor.position;
        var resolvedCast = new ResolvedSpellCast(
            modifierData,
            elementData,
            formData,
            origin,
            BuildCastDirections(origin, modifierData));

        OnSpellCastCommitted?.Invoke(resolvedCast);

        var castState = new CastState(
            resolvedCast,
            GetDamageAfterItemBonuses(formData.BaseDamage));
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
        if (castState.Modifier.Type == ModifierWordType.Splitting)
            castState.BaseDamage *= castState.Modifier.SplitDamageMultiplier;

        switch (castState.Form.Type)
        {
            case FormWordType.Shard:
                foreach (var direction in castState.Directions)
                    FireLineSpell(castState, direction, oncePerTarget: true);
                break;

            case FormWordType.Beam:
                yield return RunBeam(castState);
                break;

            case FormWordType.Wave:
                foreach (var direction in castState.Directions)
                    FireWaveSpell(castState, direction);
                break;

            case FormWordType.Barrage:
                yield return RunBarrage(castState);
                break;
        }
    }

    private IEnumerator RunBeam(CastState castState)
    {
        var elapsed = 0f;
        while (elapsed < castState.Form.BeamDuration)
        {
            foreach (var direction in castState.Directions)
                FireLineSpell(castState, direction, oncePerTarget: false);

            elapsed += castState.Form.BeamTickInterval;
            yield return new WaitForSeconds(castState.Form.BeamTickInterval);
        }
    }

    private IEnumerator RunBarrage(CastState castState)
    {
        for (var i = 0; i < castState.Form.BarrageProjectileCount; i++)
        {
            foreach (var direction in castState.Directions)
                FireLineSpell(castState, direction, oncePerTarget: true);

            yield return new WaitForSeconds(castState.Form.BarrageInterval);
        }
    }

    private void FireLineSpell(CastState castState, Vector2 direction, bool oncePerTarget)
    {
        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        QueryTargetsInRadius(origin, castState.Form.Range);
        var nearestDistance = float.MaxValue;
        var nearestTarget = (ISpellTarget)null;

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var toTarget = target.Position - origin;
            var projected = Vector2.Dot(toTarget, direction);
            if (projected <= 0f || projected > castState.Form.Range)
                continue;

            var lateralDistance = Mathf.Abs(Vector3.Cross(direction, toTarget.normalized).z) * toTarget.magnitude;
            if (lateralDistance > castState.Form.HitRadius)
                continue;

            if (castState.Modifier.Type == ModifierWordType.Piercing)
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
        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        QueryTargetsInRadius(origin, castState.Form.Range);

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var toTarget = target.Position - origin;
            if (toTarget.magnitude > castState.Form.Range)
                continue;

            var angle = Vector2.Angle(direction, toTarget);
            if (angle > castState.Form.WaveArcAngle * 0.5f)
                continue;

            ApplyHit(castState, target, target.Position);
        }
    }

    private void ApplyHit(CastState castState, ISpellTarget target, Vector2 hitPosition)
    {
        var effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
        var damage = castState.BaseDamage * effectivenessMultiplier;

        if (castState.Element.Type == ElementWordType.Lightning && target.HasAnyNegativeStatus)
            damage *= castState.Element.LightningBonusMultiplier;

        var displayedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

        DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);

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
        switch (castState.Element.Type)
        {
            case ElementWordType.Poison:
                if (castState.TryConsumePoisonCloud())
                    SpawnPoisonCloud(castState.Element, hitPosition);
                break;
            case ElementWordType.Frost:
                target.ApplyStatus(CombatStatusEffectType.Slow, castState.Element.StatusDuration);
                break;
            case ElementWordType.Ember:
                target.ApplyStatus(CombatStatusEffectType.Burn, castState.Element.StatusDuration);
                target.ApplyDamageOverTime(castState.Element.DamageOverTimePerSecond * effectivenessMultiplier, castState.Element.StatusDuration, effectivenessMultiplier, _player);
                break;
            case ElementWordType.Dark:
                target.ApplyStatus(CombatStatusEffectType.Weakened, castState.Element.StatusDuration);
                break;
        }
    }

    private void ApplyModifierSideEffects(CastState castState, ISpellTarget target, Vector2 hitPosition)
    {
        switch (castState.Modifier.Type)
        {
            case ModifierWordType.Stunning:
                target.AddStunBuildup(castState.Modifier.StunBuildupPerHit, castState.Modifier.StunThreshold, castState.Modifier.StunDuration);
                break;

            case ModifierWordType.Exploding:
                if (!castState.TryConsumeExplosion())
                    return;

                ApplyExplosion(castState, hitPosition);
                break;

            case ModifierWordType.Reclaiming:
                if (!castState.TryConsumeReclaim())
                    return;

                _player.Data.RecoverMana(castState.Modifier.ReclaimManaPerHit);
                break;
        }
    }

    private void ApplyExplosion(CastState castState, Vector2 center)
    {
        QueryTargetsInRadius(center, castState.Modifier.ExplosionRadius);
        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            var target = _targetBuffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            var effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
            var damage = castState.BaseDamage * castState.Modifier.ExplosionDamageMultiplier * effectivenessMultiplier;
            var displayedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);
            target.ReceiveSpellDamage(damage, _player);
        }
    }

    private void QueryTargetsInRadius(Vector2 center, float radius)
    {
        _targetBuffer.Clear();
        Physics2D.OverlapCircle(center, radius, _targetFilter, _targetBuffer);
    }

    private void SpawnPoisonCloud(ElementWordData element, Vector2 position)
    {
        var cloud = new GameObject("PoisonCloud");
        cloud.transform.position = position;
        var zone = cloud.AddComponent<PoisonCloudZone>();
        zone.Initialize(element.PoisonCloudRadius, element.PoisonCloudDuration, element.DamageOverTimePerSecond, _targetMask, _player);
    }

    private Vector2[] BuildCastDirections(Vector2 origin, ModifierWordData modifier)
    {
        var mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : origin + Vector2.right;

        var forward = (mouseWorld - origin).normalized;
        if (forward == Vector2.zero)
            forward = Vector2.right;

        if (modifier.Type != ModifierWordType.Splitting)
            return new[] { forward };

        return new[]
        {
            forward,
            Rotate(forward, -modifier.SplitAngleDegrees),
            Rotate(forward, modifier.SplitAngleDegrees)
        };
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

        public ModifierWordData Modifier => Spell.Modifier;
        public ElementWordData Element => Spell.Element;
        public FormWordData Form => Spell.Form;
        public IReadOnlyList<Vector2> Directions => Spell.Directions;
        public ResolvedSpellCast Spell { get; }
        public float BaseDamage { get; set; }

        public CastState(ResolvedSpellCast spell, float baseDamage)
        {
            Spell = spell;
            BaseDamage = baseDamage;
            _remainingReclaims = Mathf.Max(0, spell.Modifier.MaxReclaimsPerCast);
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
