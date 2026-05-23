using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates player spell casting, resource payment, target checks, damage, and word side effects.
/// </summary>
public sealed class PlayerSpellCombatController : MonoBehaviour
{
    private const float DamageOverTimeTickIntervalSeconds = 0.5f;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerHeldItemVisualController _playerHeldItemVisual;
    [SerializeField] private SpellCastingPanel _castingPanel;
    [SerializeField] private WorldTextPopupController _feedbackPopup;
    [SerializeField] private Transform _spellContainer;

    [Header("Feedback")]
    [SerializeField] private string _castOnCooldownMessage = "Spell is recharging";
    [SerializeField] private string _insufficientManaMessage = "Need to restore mana";

    [Header("Targeting")]
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _spellObstructionMask;

    [Header("Word Effectiveness")]
    [SerializeField] private SpellWordEffectivenessData _wordEffectivenessData;
    [SerializeField] private DamagePopupFeedbackSettings _damagePopupFeedbackSettings = new();

    private readonly List<Collider2D> _targetBuffer = new(32);
    private ContactFilter2D _targetFilter;
    private float _nextCastAllowedAt;

    public event Action<SpellVisualRequest> OnSpellVisualRequested;

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

        ModifierWordData modifierData = phrase.Modifier;
        ElementWordData elementData = phrase.Element;
        FormWordData formData = phrase.Form;
        if (modifierData == null || elementData == null || formData == null)
            return;

        int manaCost = Mathf.Max(0, formData.ManaCost) + Mathf.Max(0, modifierData.AdditionalManaCost);
        float cooldown = Mathf.Max(0f, formData.CooldownSeconds) + Mathf.Max(0f, modifierData.AdditionalCooldownSeconds);

        if (_player.Data.CurrentMana < manaCost)
        {
            _feedbackPopup.ShowMessage(_insufficientManaMessage);
            return;
        }

        _player.Data.UseMana(manaCost);
        _nextCastAllowedAt = Time.time + cooldown;

        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        phrase.SetCastContext(origin, BuildCastDirections(origin, modifierData));

        CastState castState = new(
            phrase,
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
                float[] shardTravelDistances = new float[castState.Directions.Count];
                for (int directionIndex = 0; directionIndex < castState.Directions.Count; directionIndex++)
                    shardTravelDistances[directionIndex] = FireLineSpell(castState, castState.Directions[directionIndex], oncePerTarget: true);

                RequestSpellVisual(castState, shardTravelDistances);
                break;

            case FormWordType.Beam:
                RequestSpellVisual(castState, CreateObstructedTravelDistances(castState));
                yield return RunBeam(castState);
                break;

            case FormWordType.Wave:
                RequestSpellVisual(castState, CreateFullRangeTravelDistances(castState));
                foreach (Vector2 direction in castState.Directions)
                    FireWaveSpell(castState, direction);
                break;

            case FormWordType.Barrage:
                yield return RunBarrage(castState);
                break;
        }
    }

    private IEnumerator RunBeam(CastState castState)
    {
        float elapsed = 0f;
        while (elapsed < castState.Form.BeamDuration)
        {
            foreach (Vector2 direction in castState.Directions)
                FireLineSpell(castState, direction, oncePerTarget: false);

            elapsed += castState.Form.BeamTickInterval;
            yield return new WaitForSeconds(castState.Form.BeamTickInterval);
        }
    }

    private IEnumerator RunBarrage(CastState castState)
    {
        for (int i = 0; i < castState.Form.BarrageProjectileCount; i++)
        {
            castState.ClearHitTargets();
            float[] travelDistances = new float[castState.Directions.Count];

            for (int directionIndex = 0; directionIndex < castState.Directions.Count; directionIndex++)
                travelDistances[directionIndex] = FireLineSpell(castState, castState.Directions[directionIndex], oncePerTarget: true);

            RequestSpellVisual(castState, travelDistances);

            yield return new WaitForSeconds(castState.Form.BarrageInterval);
        }
    }

    private float FireLineSpell(CastState castState, Vector2 direction, bool oncePerTarget)
    {
        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        float maxTravelDistance = ResolveLineSpellTravelDistance(origin, direction, castState.Form.Range);
        QueryTargetsInRadius(origin, maxTravelDistance);
        float nearestDistance = float.MaxValue;
        ICombatTarget nearestTarget = null;

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            ICombatTarget target = _targetBuffer[i].GetComponentInParent<ICombatTarget>();
            if (target == null || !target.IsAlive)
                continue;

            Vector2 toTarget = target.Position - origin;
            float projected = Vector2.Dot(toTarget, direction);
            if (projected <= 0f || projected > maxTravelDistance)
                continue;

            float lateralDistance = Mathf.Abs(Vector3.Cross(direction, toTarget.normalized).z) * toTarget.magnitude;
            if (lateralDistance > castState.Form.HitRadius)
                continue;

            if (castState.Modifier.Type == ModifierWordType.Piercing)
            {
                if (oncePerTarget && castState.WasTargetHit(target))
                    continue;

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

            return nearestDistance;
        }

        return maxTravelDistance;
    }

    private void FireWaveSpell(CastState castState, Vector2 direction)
    {
        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        QueryTargetsInRadius(origin, castState.Form.Range);

        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            ICombatTarget target = _targetBuffer[i].GetComponentInParent<ICombatTarget>();
            if (target == null || !target.IsAlive)
                continue;

            Vector2 toTarget = target.Position - origin;
            if (toTarget.magnitude > castState.Form.Range)
                continue;

            float angle = Vector2.Angle(direction, toTarget);
            if (angle > castState.Form.WaveArcAngle * 0.5f)
                continue;

            ApplyHit(castState, target, target.Position);
        }
    }

    private void ApplyHit(CastState castState, ICombatTarget target, Vector2 hitPosition)
    {
        float effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
        float damage = castState.BaseDamage * effectivenessMultiplier;
        IStatusEffectTarget statusTarget = SpellCombatTargetUtility.GetStatusEffectTarget(target);

        if (castState.Element.Type == ElementWordType.Lightning && statusTarget != null && statusTarget.HasAnyNegativeStatus)
            damage *= castState.Element.LightningBonusMultiplier;

        if (SpellCombatTargetUtility.TryApplyDamage(target, damage, _player, out int displayedDamage))
            DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);

        ApplyElementStatus(castState, target, statusTarget, hitPosition, effectivenessMultiplier);
        ApplyModifierSideEffects(castState, target, statusTarget, hitPosition);
    }

    private float ResolveWordEffectivenessMultiplier(CastState castState, ICombatTarget target)
    {
        if (target == null)
            return _wordEffectivenessData.NeutralMultiplier;

        if (target is not Component targetComponent)
            return _wordEffectivenessData.NeutralMultiplier;

        ISpellWordEffectivenessTarget effectivenessReceiver = targetComponent.GetComponentInParent<ISpellWordEffectivenessTarget>();
        if (effectivenessReceiver == null || !effectivenessReceiver.TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags))
            return _wordEffectivenessData.NeutralMultiplier;

        return _wordEffectivenessData.CalculateFinalMultiplier(biome, roleTags, castState.Modifier, castState.Element, castState.Form);
    }

    private void ApplyElementStatus(CastState castState, ICombatTarget target, IStatusEffectTarget statusTarget, Vector2 hitPosition, float effectivenessMultiplier)
    {
        switch (castState.Element.Type)
        {
            case ElementWordType.Poison:
                if (castState.TryConsumePoisonCloud())
                    SpawnPoisonCloud(castState.Element, hitPosition);
                break;
            case ElementWordType.Frost:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Slow, castState.Element.StatusDuration);
                break;
            case ElementWordType.Ember:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Burn, castState.Element.StatusDuration);
                StartCoroutine(ApplyBurnDamageTicks(target, castState.Element.DamageOverTimePerSecond * effectivenessMultiplier, castState.Element.StatusDuration, effectivenessMultiplier, _player));
                break;
            case ElementWordType.Dark:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Weakened, castState.Element.StatusDuration);
                break;
        }
    }

    private void ApplyModifierSideEffects(CastState castState, ICombatTarget directHitTarget, IStatusEffectTarget statusTarget, Vector2 hitPosition)
    {
        switch (castState.Modifier.Type)
        {
            case ModifierWordType.Stunning:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Stunned, ResolveStunDuration(castState.Modifier));
                break;

            case ModifierWordType.Exploding:
                if (!castState.TryConsumeExplosion())
                    return;

                ApplyExplosion(castState, hitPosition, directHitTarget);
                break;

            case ModifierWordType.Reclaiming:
                if (!castState.TryConsumeReclaim())
                    return;

                _player.Data.RecoverMana(castState.Modifier.ReclaimManaPerHit);
                break;
        }
    }

    private void ApplyExplosion(CastState castState, Vector2 center, ICombatTarget directHitTarget)
    {
        QueryTargetsInRadius(center, castState.Modifier.ExplosionRadius);
        for (var i = 0; i < _targetBuffer.Count; i++)
        {
            ICombatTarget target = _targetBuffer[i].GetComponentInParent<ICombatTarget>();
            if (target == null || !target.IsAlive)
                continue;

            if (ReferenceEquals(target, directHitTarget))
                continue;

            float effectivenessMultiplier = ResolveWordEffectivenessMultiplier(castState, target);
            float damage = castState.BaseDamage * castState.Modifier.ExplosionDamageMultiplier * effectivenessMultiplier;
            if (SpellCombatTargetUtility.TryApplyDamage(target, damage, _player, out int displayedDamage))
                DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);
        }
    }

    private IEnumerator ApplyBurnDamageTicks(ICombatTarget target, float damagePerSecond, float durationSeconds, float effectivenessMultiplier, object source)
    {
        if (damagePerSecond <= 0f || durationSeconds <= 0f)
            yield break;

        float elapsed = 0f;
        float initialDelay = Mathf.Min(DamageOverTimeTickIntervalSeconds, durationSeconds);
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
            elapsed += initialDelay;
        }

        while (elapsed < durationSeconds)
        {
            if (target is not Component targetComponent || targetComponent == null || !target.IsAlive)
                yield break;

            float delta = Mathf.Min(DamageOverTimeTickIntervalSeconds, durationSeconds - elapsed);
            int damage = Mathf.RoundToInt(damagePerSecond * delta);
            if (damage > 0)
            {
                if (SpellCombatTargetUtility.TryApplyDamage(target, damage, source, out int displayedDamage))
                    DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);
            }

            elapsed += delta;
            if (elapsed < durationSeconds)
                yield return new WaitForSeconds(Mathf.Min(DamageOverTimeTickIntervalSeconds, durationSeconds - elapsed));
        }
    }

    private void QueryTargetsInRadius(Vector2 center, float radius)
    {
        _targetBuffer.Clear();
        Physics2D.OverlapCircle(center, radius, _targetFilter, _targetBuffer);
    }

    private float ResolveLineSpellTravelDistance(Vector2 origin, Vector2 direction, float maxRange)
    {
        if (_spellObstructionMask.value == 0)
            return maxRange;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxRange, _spellObstructionMask);
        if (hit.collider == null)
            return maxRange;

        return Mathf.Max(0.01f, hit.distance);
    }

    private void SpawnPoisonCloud(ElementWordData element, Vector2 position)
    {
        GameObject cloud = new("PoisonCloud");
        cloud.transform.position = position;
        if (_spellContainer != null)
            cloud.transform.SetParent(_spellContainer);

        PoisonCloudZone zone = cloud.AddComponent<PoisonCloudZone>();
        zone.Initialize(element.PoisonCloudRadius, element.PoisonCloudDuration, element.DamageOverTimePerSecond, _targetMask, _player);
    }

    private void RequestSpellVisual(CastState castState, IReadOnlyList<float> travelDistances)
    {
        SpellPhrase visualSpell = castState.Spell;
        visualSpell.SetCastContext(_playerHeldItemVisual.CurrentHandAnchor.position, CopyDirections(castState.Directions));
        OnSpellVisualRequested?.Invoke(new SpellVisualRequest(visualSpell, travelDistances, _spellObstructionMask));
    }

    private static float ResolveStunDuration(ModifierWordData modifier)
    {
        if (modifier == null)
            return 0f;

        if (modifier.StunDurationMax <= modifier.StunDurationMin)
            return modifier.StunDurationMin;

        return UnityEngine.Random.Range(modifier.StunDurationMin, modifier.StunDurationMax);
    }

    private static Vector2[] CopyDirections(IReadOnlyList<Vector2> directions)
    {
        Vector2[] copiedDirections = new Vector2[directions.Count];
        for (int i = 0; i < directions.Count; i++)
            copiedDirections[i] = directions[i];

        return copiedDirections;
    }

    private static float[] CreateFullRangeTravelDistances(CastState castState)
    {
        float[] travelDistances = new float[castState.Directions.Count];
        for (int i = 0; i < travelDistances.Length; i++)
            travelDistances[i] = castState.Form.Range;

        return travelDistances;
    }

    private float[] CreateObstructedTravelDistances(CastState castState)
    {
        Vector2 origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        float[] travelDistances = new float[castState.Directions.Count];
        for (int i = 0; i < travelDistances.Length; i++)
            travelDistances[i] = ResolveLineSpellTravelDistance(origin, castState.Directions[i], castState.Form.Range);

        return travelDistances;
    }

    private Vector2[] BuildCastDirections(Vector2 origin, ModifierWordData modifier)
    {
        Vector2 mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : origin + Vector2.right;

        Vector2 forward = (mouseWorld - origin).normalized;
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
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y).normalized;
    }

    /// <summary>
    /// Stores per-cast word data and one-time modifier consumption state while a spell executes.
    /// </summary>
    private sealed class CastState
    {
        private readonly HashSet<ICombatTarget> _hitTargets = new();
        private int _remainingExplosions = 1;
        private int _remainingPoisonClouds = 1;
        private int _remainingReclaims;

        public ModifierWordData Modifier => Spell.Modifier;
        public ElementWordData Element => Spell.Element;
        public FormWordData Form => Spell.Form;
        public IReadOnlyList<Vector2> Directions => Spell.Directions;
        public SpellPhrase Spell { get; }
        public float BaseDamage { get; set; }

        public CastState(SpellPhrase spell, float baseDamage)
        {
            Spell = spell;
            BaseDamage = baseDamage;
            _remainingReclaims = Mathf.Max(0, spell.Modifier.MaxReclaimsPerCast);
        }

        public bool WasTargetHit(ICombatTarget target) => _hitTargets.Contains(target);

        public void ClearHitTargets()
        {
            _hitTargets.Clear();
        }

        public void MarkTargetAsHit(ICombatTarget target)
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
