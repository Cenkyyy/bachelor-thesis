using UnityEngine;

/// <summary>
/// Defines one modifier word and the additional cost, side effects, and optional VFX for that modifier.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Words/Modifier Word", fileName = "ModifierWordData")]
public sealed class ModifierWordData : WordData
{
    [field: Header("Identity")]
    [field: SerializeField] public ModifierWordType Type { get; private set; }

    [Header("Combat")]
    [field: SerializeField] public int AdditionalManaCost { get; private set; }
    [field: SerializeField, Min(0f)] public float AdditionalCooldownSeconds { get; private set; }

    [Header("Stunning")]
    [field: SerializeField, Min(-1f)] public float StunDurationMin { get; private set; }
    [field: SerializeField, Min(-1f)] public float StunDurationMax { get; private set; }

    [Header("Exploding")]
    [field: SerializeField, Min(-1f)] public float ExplosionDamageMultiplier { get; private set; }
    [field: SerializeField] public GameObject ExplosionPrefab { get; private set; }

    [Header("Reclaiming")]
    [field: SerializeField, Min(-1)] public int ReclaimManaPerHit { get; private set; }
    [field: SerializeField, Min(-1)] public int MaxReclaimsPerCast { get; private set; }

    [Header("Splitting")]
    [field: SerializeField, Min(-1f)] public float SplitAngleDegrees { get; private set; }
    [field: SerializeField, Min(-1f)] public float SplitDamageMultiplier { get; private set; }

    public override WordCategory Category => WordCategory.Modifier;
    public override bool IsValid => System.Enum.IsDefined(typeof(ModifierWordType), Type);

    protected override void OnValidate()
    {
        base.OnValidate();

        AdditionalManaCost = Mathf.Max(0, AdditionalManaCost);
        AdditionalCooldownSeconds = Mathf.Max(0f, AdditionalCooldownSeconds);

        if (Type == ModifierWordType.Stunning)
        {
            StunDurationMin = Mathf.Max(0f, StunDurationMin);
            StunDurationMax = Mathf.Max(0f, StunDurationMax);

            if (StunDurationMax < StunDurationMin)
                StunDurationMax = StunDurationMin;
        }
        else
        {
            StunDurationMin = -1f;
            StunDurationMax = -1f;
        }

        ExplosionDamageMultiplier = Type == ModifierWordType.Exploding ? Mathf.Max(0f, ExplosionDamageMultiplier) : -1f;
        ExplosionPrefab = Type == ModifierWordType.Exploding ? ExplosionPrefab : null;

        ReclaimManaPerHit = Type == ModifierWordType.Reclaiming ? Mathf.Max(0, ReclaimManaPerHit) : -1;
        MaxReclaimsPerCast = Type == ModifierWordType.Reclaiming ? Mathf.Max(0, MaxReclaimsPerCast) : -1;

        SplitAngleDegrees = Type == ModifierWordType.Splitting ? Mathf.Max(0f, SplitAngleDegrees) : -1f;
        SplitDamageMultiplier = Type == ModifierWordType.Splitting ? Mathf.Max(0f, SplitDamageMultiplier) : -1f;
    }
}
