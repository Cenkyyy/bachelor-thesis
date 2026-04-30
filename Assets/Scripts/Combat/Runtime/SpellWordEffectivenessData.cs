using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Effectiveness Data")]
public sealed class SpellWordEffectivenessData : ScriptableObject
{
    [field: Header("Multiplier Bounds")]
    [field: SerializeField, Min(0f)] public float NeutralMultiplier { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float MinFinalMultiplier { get; private set; } = 0.25f;
    [field: SerializeField, Min(0f)] public float MaxFinalMultiplier { get; private set; } = 1.6f;

    [Header("Multiplier Tuning")]
    [SerializeField, Min(0f)] private float _effectiveMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float _ineffectiveMultiplier = 0.63f;

    [Header("Biome -> Element Rules")]
    [SerializeField] private List<BiomeElementEffectivenessRule> _biomeRules = new();

    [Header("Enemy Tag -> Form/Modifier Rules")]
    [SerializeField] private List<EnemyRoleWordEffectivenessRule> _enemyRoleRules = new();

    public float CalculateFinalMultiplier(ItemBiomeAffinity biome, EnemyRoleTag roleTags, ModifierWord modifier, ElementWord element, FormWord form)
    {
        var modifierMultiplier = ResolveModifierMultiplier(roleTags, modifier);
        var biomeMultiplier = ResolveBiomeElementMultiplier(biome, element);
        var formMultiplier = ResolveFormMultiplier(roleTags, form);

        var combined = modifierMultiplier * biomeMultiplier * formMultiplier;
        return Mathf.Clamp(combined, MinFinalMultiplier, MaxFinalMultiplier);
    }

    private float ResolveBiomeElementMultiplier(ItemBiomeAffinity biome, ElementWord element)
    {
        for (var i = 0; i < _biomeRules.Count; i++)
        {
            var rule = _biomeRules[i];
            if (rule == null || rule.Biome != biome)
                continue;

            return ResolveWordEffectiveness(rule.EffectiveElements, rule.IneffectiveElements, element);
        }

        return NeutralMultiplier;
    }

    private float ResolveFormMultiplier(EnemyRoleTag roleTags, FormWord form)
    {
        return ResolveRoleTagWordEffectiveness(roleTags, form, rule => rule.EffectiveForms, rule => rule.IneffectiveForms);
    }

    private float ResolveModifierMultiplier(EnemyRoleTag roleTags, ModifierWord modifier)
    {
        return ResolveRoleTagWordEffectiveness(roleTags, modifier, rule => rule.EffectiveModifiers, rule => rule.IneffectiveModifiers);
    }

    private float ResolveRoleTagWordEffectiveness<TWord>(EnemyRoleTag roleTags, TWord word, Func<EnemyRoleWordEffectivenessRule, IReadOnlyList<TWord>> effectiveSelector, Func<EnemyRoleWordEffectivenessRule, IReadOnlyList<TWord>> ineffectiveSelector)
    {
        var hasEffective = false;
        var hasIneffective = false;

        for (var i = 0; i < _enemyRoleRules.Count; i++)
        {
            var rule = _enemyRoleRules[i];
            if (rule == null || !roleTags.HasFlag(rule.RoleTag))
                continue;

            if (ContainsWord(effectiveSelector(rule), word))
                hasEffective = true;

            if (ContainsWord(ineffectiveSelector(rule), word))
                hasIneffective = true;
        }

        if (hasEffective == hasIneffective)
            return NeutralMultiplier;

        return hasEffective ? _effectiveMultiplier : _ineffectiveMultiplier;
    }

    private float ResolveWordEffectiveness<TWord>(IReadOnlyList<TWord> effectiveWords, IReadOnlyList<TWord> ineffectiveWords, TWord word)
    {
        if (ContainsWord(effectiveWords, word))
            return _effectiveMultiplier;

        if (ContainsWord(ineffectiveWords, word))
            return _ineffectiveMultiplier;

        return NeutralMultiplier;
    }

    private bool ContainsWord<TWord>(IReadOnlyList<TWord> words, TWord value)
    {
        if (words == null)
            return false;

        for (var i = 0; i < words.Count; i++)
        {
            if (EqualityComparer<TWord>.Default.Equals(words[i], value))
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (_effectiveMultiplier < 0f)
            _effectiveMultiplier = 1.25f;

        if (NeutralMultiplier < 0f)
            NeutralMultiplier = 1f;

        if (_ineffectiveMultiplier < 0f)
            _ineffectiveMultiplier = 0.63f;

        if (MinFinalMultiplier < 0f)
            MinFinalMultiplier = 0.25f;

        if (MaxFinalMultiplier < MinFinalMultiplier)
            MaxFinalMultiplier = MinFinalMultiplier;
    }

    [Serializable]
    public sealed class BiomeElementEffectivenessRule
    {
        [field: SerializeField] public ItemBiomeAffinity Biome { get; private set; } = ItemBiomeAffinity.Grassland;

        [SerializeField] private List<ElementWord> _effectiveElements = new();
        [SerializeField] private List<ElementWord> _ineffectiveElements = new();

        public IReadOnlyList<ElementWord> EffectiveElements => _effectiveElements;
        public IReadOnlyList<ElementWord> IneffectiveElements => _ineffectiveElements;
    }

    [Serializable]
    public sealed class EnemyRoleWordEffectivenessRule
    {
        [field: SerializeField] public EnemyRoleTag RoleTag { get; private set; } = EnemyRoleTag.Bruiser;

        [SerializeField] private List<FormWord> _effectiveForms = new();
        [SerializeField] private List<FormWord> _ineffectiveForms = new();
        [SerializeField] private List<ModifierWord> _effectiveModifiers = new();
        [SerializeField] private List<ModifierWord> _ineffectiveModifiers = new();

        public IReadOnlyList<FormWord> EffectiveForms => _effectiveForms;
        public IReadOnlyList<FormWord> IneffectiveForms => _ineffectiveForms;
        public IReadOnlyList<ModifierWord> EffectiveModifiers => _effectiveModifiers;
        public IReadOnlyList<ModifierWord> IneffectiveModifiers => _ineffectiveModifiers;
    }
}
