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
    [SerializeField] private string _castInProgressMessage = "Already casting";
    [SerializeField] private string _insufficientManaMessage = "Need to restore mana";

    [Header("Targeting")]
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _spellObstructionMask;

    [Header("Word Effectiveness")]
    [SerializeField] private SpellWordEffectivenessData _wordEffectivenessData;
    [SerializeField] private DamagePopupFeedbackSettings _damagePopupFeedbackSettings = new();

    private ContactFilter2D _targetFilter;
    private float _nextCastAllowedAt;
    private float _castLockedUntil;

    public LayerMask TargetMask => _targetMask;

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

        if (Time.time < _castLockedUntil)
        {
            _feedbackPopup.ShowMessage(_castInProgressMessage);
            return;
        }

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

        int manaCost = Mathf.Max(0, formData.ManaCost) + Mathf.Max(0, modifierData.AdditionalManaCost) + GetCurrentWeaponManaCost();
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

        return Mathf.Max(0f, baseDamage + _player.Data.SpellDamageBonus + GetCurrentWeaponDamageBonus());
    }

    private float GetCurrentWeaponDamageBonus()
    {
        if (_player == null || _player.Inventory == null)
            return 0f;

        var selectedItem = _player.Inventory.GetItemAt(_player.Inventory.SelectedHotbarIndex);
        return selectedItem.Item is WeaponItemData weapon ? Mathf.Max(0, weapon.Damage) : 0f;
    }

    private int GetCurrentWeaponManaCost()
    {
        if (_player == null || _player.Inventory == null)
            return 0;

        var selectedItem = _player.Inventory.GetItemAt(_player.Inventory.SelectedHotbarIndex);
        return selectedItem.Item is WeaponItemData weapon ? Mathf.Max(0, weapon.ManaCost) : 0;
    }

    private IEnumerator ExecuteCast(CastState castState)
    {
        SpellCastRuntimeData runtimeData = new(castState.Spell, castState.BaseDamage);

        switch (castState.Form.Type)
        {
            case FormWordType.Shard:
                SpawnProjectiles(castState, CreateObstructedTravelDistances(castState), runtimeData);
                break;

            case FormWordType.Beam:
                SpawnBeams(castState, runtimeData);
                break;

            case FormWordType.Wave:
                SpawnProjectiles(castState, CreateObstructedTravelDistances(castState), runtimeData);
                break;

            case FormWordType.Barrage:
                yield return RunBarrage(castState, runtimeData);
                break;
        }
    }

    private IEnumerator RunBarrage(CastState castState, SpellCastRuntimeData runtimeData)
    {
        for (int i = 0; i < castState.Form.BarrageProjectileCount; i++)
        {
            SpawnProjectiles(castState, CreateObstructedTravelDistances(castState), runtimeData);

            yield return new WaitForSeconds(castState.Form.BarrageInterval);
        }
    }

    public bool TryApplyProjectileHit(SpellCastRuntimeData runtimeData, Collider2D collider, ISet<ICombatTarget> alreadyHitTargets)
    {
        if (runtimeData == null || collider == null || !IsInLayerMask(collider.gameObject.layer, _targetMask))
            return false;

        if (!SpellCombatTargetUtility.TryGetCombatTarget(collider, out ICombatTarget target))
            return false;

        if (alreadyHitTargets != null && alreadyHitTargets.Contains(target))
            return false;

        ApplyHit(runtimeData, target, target.Position);
        alreadyHitTargets?.Add(target);
        return true;
    }

    private void ApplyHit(SpellCastRuntimeData runtimeData, ICombatTarget target, Vector2 hitPosition)
    {
        float effectivenessMultiplier = ResolveWordEffectivenessMultiplier(runtimeData, target);
        float damage = runtimeData.BaseDamage * effectivenessMultiplier;
        IStatusEffectTarget statusTarget = SpellCombatTargetUtility.GetStatusEffectTarget(target);

        if (runtimeData.Element.Type == ElementWordType.Lightning && statusTarget != null && statusTarget.HasAnyNegativeStatus)
            damage *= runtimeData.Element.LightningBonusMultiplier;

        if (SpellCombatTargetUtility.TryApplyDamage(target, damage, _player, out int displayedDamage))
            DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);

        ApplyElementStatus(runtimeData, target, statusTarget, hitPosition, effectivenessMultiplier);
        ApplyModifierSideEffects(runtimeData, target, statusTarget, hitPosition);
    }

    private float ResolveWordEffectivenessMultiplier(SpellCastRuntimeData runtimeData, ICombatTarget target)
    {
        if (_wordEffectivenessData == null)
            return 1f;

        if (target == null)
            return _wordEffectivenessData.NeutralMultiplier;

        if (target is not Component targetComponent)
            return _wordEffectivenessData.NeutralMultiplier;

        ISpellWordEffectivenessTarget effectivenessReceiver = targetComponent.GetComponentInParent<ISpellWordEffectivenessTarget>();
        if (effectivenessReceiver == null || !effectivenessReceiver.TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags))
            return _wordEffectivenessData.NeutralMultiplier;

        return _wordEffectivenessData.CalculateFinalMultiplier(biome, roleTags, runtimeData.Modifier, runtimeData.Element, runtimeData.Form);
    }

    private void ApplyElementStatus(SpellCastRuntimeData runtimeData, ICombatTarget target, IStatusEffectTarget statusTarget, Vector2 hitPosition, float effectivenessMultiplier)
    {
        switch (runtimeData.Element.Type)
        {
            case ElementWordType.Poison:
                if (runtimeData.TryConsumePoisonCloud(target))
                    SpawnPoisonCloud(runtimeData.Element, hitPosition);
                break;
            case ElementWordType.Frost:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Slow, runtimeData.Element.StatusDuration);
                break;
            case ElementWordType.Ember:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Burn, runtimeData.Element.StatusDuration);
                StartCoroutine(ApplyBurnDamageTicks(target, runtimeData.Element.DamageOverTimePerSecond * effectivenessMultiplier, runtimeData.Element.StatusDuration, effectivenessMultiplier, _player));
                break;
            case ElementWordType.Dark:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Weakened, runtimeData.Element.StatusDuration);
                break;
        }
    }

    private void ApplyModifierSideEffects(SpellCastRuntimeData runtimeData, ICombatTarget directHitTarget, IStatusEffectTarget statusTarget, Vector2 hitPosition)
    {
        switch (runtimeData.Modifier.Type)
        {
            case ModifierWordType.Stunning:
                statusTarget?.ApplyStatus(CombatStatusEffectType.Stunned, ResolveStunDuration(runtimeData.Modifier));
                break;

            case ModifierWordType.Exploding:
                if (!runtimeData.TryConsumeExplosion())
                    return;

                ApplyExplosion(runtimeData, hitPosition, directHitTarget);
                break;

            case ModifierWordType.Reclaiming:
                if (!runtimeData.TryConsumeReclaim())
                    return;

                _player.Data.RecoverMana(runtimeData.Modifier.ReclaimManaPerHit);
                break;
        }
    }

    private void ApplyExplosion(SpellCastRuntimeData runtimeData, Vector2 center, ICombatTarget directHitTarget)
    {
        if (runtimeData.Modifier.ExplosionPrefab == null)
            return;

        StartCoroutine(RunExplosion(runtimeData, center, directHitTarget));
    }

    private IEnumerator RunExplosion(SpellCastRuntimeData runtimeData, Vector2 center, ICombatTarget directHitTarget)
    {
        GameObject explosion = Instantiate(runtimeData.Modifier.ExplosionPrefab, center, Quaternion.identity, _spellContainer);
        ApplyExplosionVisuals(explosion, runtimeData.Element);

        CircleCollider2D hitbox = explosion.GetComponent<CircleCollider2D>();
        if (hitbox == null)
        {
            Destroy(explosion);
            yield break;
        }

        hitbox.isTrigger = true;

        float lifetime = ResolveExplosionLifetime(explosion);
        float elapsed = 0f;
        HashSet<ICombatTarget> damagedTargets = new();
        List<Collider2D> overlappingTargets = new(32);

        while (elapsed < lifetime && explosion != null)
        {
            overlappingTargets.Clear();
            hitbox.Overlap(_targetFilter, overlappingTargets);

            for (var i = 0; i < overlappingTargets.Count; i++)
            {
                ApplyExplosionHit(runtimeData, overlappingTargets[i], directHitTarget, damagedTargets);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (explosion != null)
            Destroy(explosion);
    }

    private static void ApplyExplosionVisuals(GameObject explosion, ElementWordData element)
    {
        if (explosion == null || element == null)
            return;

        SpriteRenderer spriteRenderer = explosion.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && element.Material != null)
            spriteRenderer.material = element.Material;
    }

    private void ApplyExplosionHit(
        SpellCastRuntimeData runtimeData,
        Collider2D collider,
        ICombatTarget directHitTarget,
        ISet<ICombatTarget> damagedTargets)
    {
        if (!SpellCombatTargetUtility.TryGetCombatTarget(collider, out ICombatTarget target))
            return;

        if (target == null || !target.IsAlive || ReferenceEquals(target, directHitTarget) || damagedTargets.Contains(target))
            return;

        float effectivenessMultiplier = ResolveWordEffectivenessMultiplier(runtimeData, target);
        float damage = runtimeData.BaseDamage * runtimeData.Modifier.ExplosionDamageMultiplier * effectivenessMultiplier;
        if (SpellCombatTargetUtility.TryApplyDamage(target, damage, _player, out int displayedDamage))
            DamagePopupFeedbackUtility.ShowForTarget(target, displayedDamage, effectivenessMultiplier, _damagePopupFeedbackSettings);

        damagedTargets.Add(target);
    }

    private static float ResolveExplosionLifetime(GameObject explosion)
    {
        const float FallbackLifetime = 0.75f;

        if (explosion == null || !explosion.TryGetComponent(out Animator animator) || animator.layerCount == 0)
            return FallbackLifetime;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.length > 0f)
            return Mathf.Max(0.01f, stateInfo.length);

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length == 0 || clipInfo[0].clip == null)
            return FallbackLifetime;

        return Mathf.Max(0.01f, clipInfo[0].clip.length);
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

    private void SpawnPoisonCloud(ElementWordData element, Vector2 position)
    {
        if (element == null || element.PoisonCloudPrefab == null)
            return;

        GameObject cloud = Instantiate(element.PoisonCloudPrefab, position, Quaternion.identity, _spellContainer);

        PoisonCloudZone zone = cloud.GetComponent<PoisonCloudZone>();
        if (zone == null)
        {
            Destroy(cloud);
            return;
        }

        zone.Initialize(element.PoisonCloudDuration, element.DamageOverTimePerSecond, _targetMask, _player, element.Material, _damagePopupFeedbackSettings);
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

    private void SpawnProjectiles(CastState castState, IReadOnlyList<float> travelDistances, SpellCastRuntimeData runtimeData)
    {
        SpellPhrase spell = castState.Spell;
        spell.SetCastContext(_playerHeldItemVisual.CurrentHandAnchor.position, CopyDirections(castState.Directions));

        if (!spell.IsCommitted || spell.Form.ProjectilePrefab == null)
            return;

        for (int i = 0; i < spell.Directions.Count; i++)
        {
            GameObject spellObject = Instantiate(spell.Form.ProjectilePrefab, spell.Origin, Quaternion.identity, _spellContainer);
            spellObject.transform.localScale = spell.Form.VfxScale;

            SpellProjectile projectile = spellObject.GetComponent<SpellProjectile>();
            if (projectile == null)
                projectile = spellObject.AddComponent<SpellProjectile>();

            float travelDistance = travelDistances != null && i < travelDistances.Count ? travelDistances[i] : spell.Form.Range;
            projectile.Initialize(
                spell.Directions[i],
                ResolveProjectileSpeed(spell.Form),
                travelDistance,
                _spellObstructionMask,
                spell.Element.Material,
                this,
                runtimeData,
                ResolveHitMode(spell));
        }
    }

    private void SpawnBeams(CastState castState, SpellCastRuntimeData runtimeData)
    {
        SpellPhrase spell = castState.Spell;
        spell.SetCastContext(_playerHeldItemVisual.CurrentHandAnchor.position, CopyDirections(castState.Directions));

        if (!spell.IsCommitted || spell.Form.ProjectilePrefab == null || spell.Directions.Count == 0)
            return;

        _castLockedUntil = Mathf.Max(_castLockedUntil, Time.time + spell.Form.BeamDuration);

        Vector2 baseDirection = spell.Directions[0];
        for (int i = 0; i < spell.Directions.Count; i++)
        {
            GameObject spellObject = Instantiate(spell.Form.ProjectilePrefab, spell.Origin, Quaternion.identity, _spellContainer);
            spellObject.transform.localScale = spell.Form.VfxScale;

            SpellBeam beam = spellObject.GetComponent<SpellBeam>();
            if (beam == null)
                beam = spellObject.AddComponent<SpellBeam>();

            beam.Initialize(
                _playerHeldItemVisual,
                spell.Origin,
                Vector2.SignedAngle(baseDirection, spell.Directions[i]),
                _targetMask,
                _spellObstructionMask,
                spell.Element.Material,
                this,
                runtimeData);
        }
    }

    private static float ResolveProjectileSpeed(FormWordData form)
    {
        return form.Type == FormWordType.Beam ? 0f : form.VfxSpeed;
    }

    private static SpellProjectile.HitMode ResolveHitMode(SpellPhrase spell)
    {
        if (spell.Form.Type == FormWordType.Beam)
            return SpellProjectile.HitMode.BeamTick;

        if (spell.Form.Type == FormWordType.Wave)
            return spell.Modifier.Type == ModifierWordType.Piercing
                ? SpellProjectile.HitMode.PiercingWave
                : SpellProjectile.HitMode.WaveWall;

        return spell.Modifier.Type == ModifierWordType.Piercing
            ? SpellProjectile.HitMode.Piercing
            : SpellProjectile.HitMode.SingleImpact;
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

    private static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    /// <summary>
    /// Stores per-cast word data while a spell executes.
    /// </summary>
    private sealed class CastState
    {
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
        }
    }
}
