using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Modifier Word", fileName = "ModifierWordData")]
public sealed class ModifierWordData : WordData
{
    [field: SerializeField] public ModifierWordType Type { get; private set; }

    [Header("Combat")]
    [field: SerializeField] public int AdditionalManaCost { get; private set; }
    [field: SerializeField, Min(0f)] public float AdditionalCooldownSeconds { get; private set; }

    [Header("Stunning")]
    [field: SerializeField, Min(-1f)] public float StunBuildupPerHit { get; private set; }
    [field: SerializeField, Min(-1f)] public float StunThreshold { get; private set; }
    [field: SerializeField, Min(-1f)] public float StunDuration { get; private set; }

    [Header("Exploding")]
    [field: SerializeField, Min(-1f)] public float ExplosionRadius { get; private set; }
    [field: SerializeField, Min(-1f)] public float ExplosionDamageMultiplier { get; private set; }

    [Header("Reclaiming")]
    [field: SerializeField, Min(-1)] public int ReclaimManaPerHit { get; private set; }
    [field: SerializeField, Min(-1)] public int MaxReclaimsPerCast { get; private set; }

    [Header("Splitting")]
    [field: SerializeField, Min(-1f)] public float SplitAngleDegrees { get; private set; }
    [field: SerializeField, Min(-1f)] public float SplitDamageMultiplier { get; private set; }

    [Header("Visuals")]
    [field: SerializeField] public GameObject OptionalPrefab { get; private set; }

    public override WordCategory Category => WordCategory.Modifier;
    public override bool IsValid => System.Enum.IsDefined(typeof(ModifierWordType), Type);

    protected override void OnValidate()
    {
        base.OnValidate();

        AdditionalManaCost = Mathf.Max(0, AdditionalManaCost);
        AdditionalCooldownSeconds = Mathf.Max(0f, AdditionalCooldownSeconds);

        StunBuildupPerHit = Type == ModifierWordType.Stunning ? GetPositiveOrDefault(StunBuildupPerHit, 1f) : -1f;
        StunThreshold = Type == ModifierWordType.Stunning ? GetPositiveOrDefault(StunThreshold, 3f) : -1f;
        StunDuration = Type == ModifierWordType.Stunning ? GetPositiveOrDefault(StunDuration, 1.25f) : -1f;

        ExplosionRadius = Type == ModifierWordType.Exploding ? GetPositiveOrDefault(ExplosionRadius, 1.8f) : -1f;
        ExplosionDamageMultiplier = Type == ModifierWordType.Exploding ? GetPositiveOrDefault(ExplosionDamageMultiplier, 0.7f) : -1f;

        ReclaimManaPerHit = Type == ModifierWordType.Reclaiming ? GetNonNegativeOrDefault(ReclaimManaPerHit, 2) : -1;
        MaxReclaimsPerCast = Type == ModifierWordType.Reclaiming ? GetNonNegativeOrDefault(MaxReclaimsPerCast, 2) : -1;

        SplitAngleDegrees = Type == ModifierWordType.Splitting ? GetPositiveOrDefault(SplitAngleDegrees, 45f) : -1f;
        SplitDamageMultiplier = Type == ModifierWordType.Splitting ? GetPositiveOrDefault(SplitDamageMultiplier, 1f / 3f) : -1f;
    }

    private static float GetPositiveOrDefault(float value, float defaultValue)
    {
        return value > 0f ? value : defaultValue;
    }

    private static int GetNonNegativeOrDefault(int value, int defaultValue)
    {
        return value >= 0 ? value : defaultValue;
    }
}
