using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores mutable per-cast data shared by spell projectile instances from the same cast.
/// </summary>
public sealed class SpellCastRuntimeData
{
    private int _remainingExplosions = 1;
    private int _remainingPoisonClouds = 1;
    private int _remainingReclaims;
    private readonly HashSet<ICombatTarget> _poisonCloudTargets = new();

    public ModifierWordData Modifier => Spell.Modifier;
    public ElementWordData Element => Spell.Element;
    public FormWordData Form => Spell.Form;
    public SpellPhrase Spell { get; }
    public float BaseDamage { get; }

    public SpellCastRuntimeData(SpellPhrase spell, float baseDamage)
    {
        Spell = spell;
        BaseDamage = spell.Modifier.Type == ModifierWordType.Splitting
            ? Mathf.Max(0f, baseDamage) * spell.Modifier.SplitDamageMultiplier
            : Mathf.Max(0f, baseDamage);
        _remainingReclaims = Mathf.Max(0, spell.Modifier.MaxReclaimsPerCast);
        _remainingPoisonClouds = spell.Modifier.Type == ModifierWordType.Piercing ? int.MaxValue : 1;
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

    public bool TryConsumePoisonCloud(ICombatTarget target)
    {
        if (Modifier.Type == ModifierWordType.Piercing)
            return target != null && _poisonCloudTargets.Add(target);

        if (_remainingPoisonClouds <= 0)
            return false;

        _remainingPoisonClouds--;
        return true;
    }
}
