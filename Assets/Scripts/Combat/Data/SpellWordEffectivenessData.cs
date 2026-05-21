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

    public float CalculateFinalMultiplier(ItemBiomeAffinity biome, EnemyRoleTag roleTags, ModifierWordData modifier, ElementWordData element, FormWordData form)
    {
        var modifierMultiplier = ResolveModifierMultiplier(roleTags, modifier);
        var biomeMultiplier = ResolveBiomeElementMultiplier(biome, element);
        var formMultiplier = ResolveFormMultiplier(roleTags, form);

        var combined = modifierMultiplier * biomeMultiplier * formMultiplier;
        return Mathf.Clamp(combined, MinFinalMultiplier, MaxFinalMultiplier);
    }

    private float ResolveBiomeElementMultiplier(ItemBiomeAffinity biome, ElementWordData element)
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

    private float ResolveFormMultiplier(EnemyRoleTag roleTags, FormWordData form)
    {
        return ResolveRoleTagWordEffectiveness(roleTags, form, rule => rule.EffectiveForms, rule => rule.IneffectiveForms);
    }

    private float ResolveModifierMultiplier(EnemyRoleTag roleTags, ModifierWordData modifier)
    {
        return ResolveRoleTagWordEffectiveness(roleTags, modifier, rule => rule.EffectiveModifiers, rule => rule.IneffectiveModifiers);
    }

    private float ResolveRoleTagWordEffectiveness<TWordData>(EnemyRoleTag roleTags, TWordData word, Func<EnemyRoleWordEffectivenessRule, IReadOnlyList<TWordData>> effectiveSelector, Func<EnemyRoleWordEffectivenessRule, IReadOnlyList<TWordData>> ineffectiveSelector)
        where TWordData : WordData
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

    private float ResolveWordEffectiveness(IReadOnlyList<ElementWordData> effectiveWords, IReadOnlyList<ElementWordData> ineffectiveWords, ElementWordData word)
    {
        if (ContainsWord(effectiveWords, word))
            return _effectiveMultiplier;

        if (ContainsWord(ineffectiveWords, word))
            return _ineffectiveMultiplier;

        return NeutralMultiplier;
    }

    private bool ContainsWord<TWordData>(IReadOnlyList<TWordData> words, TWordData word)
        where TWordData : WordData
    {
        if (words == null || word == null)
            return false;

        for (var i = 0; i < words.Count; i++)
        {
            if (words[i] == word)
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

        [SerializeField] private List<ElementWordData> _effectiveElements = new();
        [SerializeField] private List<ElementWordData> _ineffectiveElements = new();

        public IReadOnlyList<ElementWordData> EffectiveElements => _effectiveElements;
        public IReadOnlyList<ElementWordData> IneffectiveElements => _ineffectiveElements;
    }

    [Serializable]
    public sealed class EnemyRoleWordEffectivenessRule
    {
        [field: SerializeField] public EnemyRoleTag RoleTag { get; private set; } = EnemyRoleTag.Bruiser;

        [SerializeField] private List<FormWordData> _effectiveForms = new();
        [SerializeField] private List<FormWordData> _ineffectiveForms = new();
        [SerializeField] private List<ModifierWordData> _effectiveModifiers = new();
        [SerializeField] private List<ModifierWordData> _ineffectiveModifiers = new();

        public IReadOnlyList<FormWordData> EffectiveForms => _effectiveForms;
        public IReadOnlyList<FormWordData> IneffectiveForms => _ineffectiveForms;
        public IReadOnlyList<ModifierWordData> EffectiveModifiers => _effectiveModifiers;
        public IReadOnlyList<ModifierWordData> IneffectiveModifiers => _ineffectiveModifiers;
    }
}
