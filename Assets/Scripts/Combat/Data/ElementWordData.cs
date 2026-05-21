using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Element Word", fileName = "ElementWordData")]
public sealed class ElementWordData : WordData
{
    [field: SerializeField] public ElementWordType Type { get; private set; }
    [field: SerializeField] public Material Material { get; private set; }

    [Header("Status")]
    [field: SerializeField, Min(-1f)] public float StatusDuration { get; private set; }
    [field: SerializeField, Min(-1f)] public float DamageOverTimePerSecond { get; private set; }

    [Header("Lightning")]
    [field: SerializeField, Min(-1f)] public float LightningBonusMultiplier { get; private set; }

    [Header("Poison")]
    [field: SerializeField, Min(-1f)] public float PoisonCloudRadius { get; private set; }
    [field: SerializeField, Min(-1f)] public float PoisonCloudDuration { get; private set; }

    public override WordCategory Category => WordCategory.Element;
    public override bool IsValid => System.Enum.IsDefined(typeof(ElementWordType), Type);

    protected override void OnValidate()
    {
        base.OnValidate();

        StatusDuration = UsesStatusDuration() ? GetPositiveOrDefault(StatusDuration, 3f) : -1f;
        DamageOverTimePerSecond = UsesDamageOverTime() ? GetNonNegativeOrDefault(DamageOverTimePerSecond, 5f) : -1f;
        LightningBonusMultiplier = Type == ElementWordType.Lightning ? GetPositiveOrDefault(LightningBonusMultiplier, 1.35f) : -1f;
        PoisonCloudRadius = Type == ElementWordType.Poison ? GetPositiveOrDefault(PoisonCloudRadius, 1.9f) : -1f;
        PoisonCloudDuration = Type == ElementWordType.Poison ? GetPositiveOrDefault(PoisonCloudDuration, 4f) : -1f;
    }

    private bool UsesStatusDuration()
    {
        return Type == ElementWordType.Frost ||
               Type == ElementWordType.Ember ||
               Type == ElementWordType.Dark;
    }

    private bool UsesDamageOverTime()
    {
        return Type == ElementWordType.Ember ||
               Type == ElementWordType.Poison;
    }

    private static float GetPositiveOrDefault(float value, float defaultValue)
    {
        return value > 0f ? value : defaultValue;
    }

    private static float GetNonNegativeOrDefault(float value, float defaultValue)
    {
        return value >= 0f ? value : defaultValue;
    }
}
