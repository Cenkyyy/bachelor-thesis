using System;
using UnityEngine;

[Serializable]
public struct FormRuntimeSettings
{
    [Min(0)] public int ManaCost;
    [Min(0f)] public float CooldownSeconds;
    [Min(0f)] public float BaseDamage;
}

[Serializable]
public struct ModifierRuntimeSettings
{
    [Min(0)] public int AdditionalManaCost;
    [Min(0f)] public float AdditionalCooldownSeconds;
}

[Serializable]
public struct SpellRuntimeSettings
{
    [Header("General")]
    [Min(0.1f)] public float Range;
    [Min(0.1f)] public float HitRadius;
    [Min(0.1f)] public float WaveArcAngle;

    [Header("Form Numbers")]
    public FormRuntimeSettings Shard;
    public FormRuntimeSettings Beam;
    public FormRuntimeSettings Wave;
    public FormRuntimeSettings Barrage;

    [Header("Beam")]
    [Min(0f)] public float BeamDuration;
    [Min(0.01f)] public float BeamTickInterval;

    [Header("Barrage")]
    [Min(1)] public int BarrageProjectileCount;
    [Min(0.01f)] public float BarrageInterval;

    [Header("Element")]
    [Min(0f)] public float LightningBonusMultiplier;
    [Min(0f)] public float DotDamagePerSecond;
    [Min(0f)] public float StatusDuration;

    [Header("Modifier")]
    public ModifierRuntimeSettings Piercing;
    public ModifierRuntimeSettings Stunning;
    public ModifierRuntimeSettings Exploding;
    public ModifierRuntimeSettings Reclaiming;
    public ModifierRuntimeSettings Splitting;

    [Min(0f)] public float StunBuildupPerHit;
    [Min(0.01f)] public float StunThreshold;
    [Min(0f)] public float StunDuration;

    [Min(0f)] public float ExplosionRadius;
    [Min(0f)] public float ExplosionDamageMultiplier;

    [Min(0)] public int ReclaimManaPerHit;
    [Min(0)] public int MaxReclaimsPerCast;

    [Min(0f)] public float PoisonCloudRadius;
    [Min(0f)] public float PoisonCloudDuration;

    public FormRuntimeSettings GetFormSettings(FormWordType form)
    {
        return form switch
        {
            FormWordType.Shard => Shard,
            FormWordType.Beam => Beam,
            FormWordType.Wave => Wave,
            FormWordType.Barrage => Barrage,
            _ => Shard
        };
    }

    public ModifierRuntimeSettings GetModifierSettings(ModifierWordType modifier)
    {
        return modifier switch
        {
            ModifierWordType.Piercing => Piercing,
            ModifierWordType.Stunning => Stunning,
            ModifierWordType.Exploding => Exploding,
            ModifierWordType.Reclaiming => Reclaiming,
            ModifierWordType.Splitting => Splitting,
            _ => Piercing
        };
    }
}
