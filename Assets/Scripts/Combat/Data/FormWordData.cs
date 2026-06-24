using UnityEngine;

/// <summary>
/// Defines one form word and the shape, timing, cost, damage, targeting, and projectile VFX data for that spell form.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Words/Form Word", fileName = "FormWordData")]
public sealed class FormWordData : WordData
{
    [field: Header("Identity")]
    [field: SerializeField] public FormWordType Type { get; private set; }

    [Header("Combat")]
    [field: SerializeField] public int ManaCost { get; private set; }
    [field: SerializeField, Min(0f)] public float CooldownSeconds { get; private set; }
    [field: SerializeField, Min(0f)] public float BaseDamage { get; private set; }
    [field: SerializeField, Min(0.1f)] public float Range { get; private set; }
    [field: SerializeField, Min(0.1f)] public float HitRadius { get; private set; }

    [Header("Wave")]
    [field: SerializeField, Min(-1f)] public float WaveArcAngle { get; private set; }

    [Header("Beam")]
    [field: SerializeField, Min(-1f)] public float BeamDuration { get; private set; }
    [field: SerializeField, Min(-1f)] public float BeamTickInterval { get; private set; }

    [Header("Barrage")]
    [field: SerializeField, Min(-1)] public int BarrageProjectileCount { get; private set; }
    [field: SerializeField, Min(-1f)] public float BarrageInterval { get; private set; }

    [Header("Visuals")]
    [field: SerializeField] public GameObject ProjectilePrefab { get; private set; }

    [Header("VFX")]
    [SerializeField, Min(0f)] private float _vfxSpeed = 14f;
    [SerializeField] private Vector3 _vfxScale = new(0.35f, 0.35f, 1f);

    public override WordCategory Category => WordCategory.Form;
    public override bool IsValid => System.Enum.IsDefined(typeof(FormWordType), Type);
    public float VfxSpeed => _vfxSpeed;
    public Vector3 VfxScale => _vfxScale;

    protected override void OnValidate()
    {
        base.OnValidate();

        ManaCost = Mathf.Max(0, ManaCost);
        CooldownSeconds = Mathf.Max(0f, CooldownSeconds);
        BaseDamage = Mathf.Max(0f, BaseDamage);
        Range = Mathf.Max(0.1f, Range);
        HitRadius = Mathf.Max(0.1f, HitRadius);

        WaveArcAngle = Type == FormWordType.Wave ? Mathf.Max(0f, WaveArcAngle) : -1f;
        BeamDuration = Type == FormWordType.Beam ? Mathf.Max(0f, BeamDuration) : -1f;
        BeamTickInterval = Type == FormWordType.Beam ? Mathf.Max(0f, BeamTickInterval) : -1f;
        BarrageProjectileCount = Type == FormWordType.Barrage ? Mathf.Max(0, BarrageProjectileCount) : -1;
        BarrageInterval = Type == FormWordType.Barrage ? Mathf.Max(0f, BarrageInterval) : -1f;

        _vfxSpeed = Mathf.Max(0f, _vfxSpeed);
    }
}
