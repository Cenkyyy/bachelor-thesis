using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Form Word", fileName = "FormWordData")]
public sealed class FormWordData : WordData
{
    [field: SerializeField] public FormWordType Form { get; private set; }
    [field: SerializeField] public int ManaCost { get; private set; }
    [field: SerializeField] public GameObject ProjectilePrefab { get; private set; }

    [Header("VFX")]
    [SerializeField, Min(0.01f)] private float _vfxLifetime = 0.35f;
    [SerializeField, Min(0f)] private float _vfxSpeed = 14f;
    [SerializeField] private Vector3 _vfxScale = new(0.35f, 0.35f, 1f);

    public override WordCategory Category => WordCategory.Form;
    public override bool IsValid => System.Enum.IsDefined(typeof(FormWordType), Form);
    public float VfxLifetime => _vfxLifetime;
    public float VfxSpeed => _vfxSpeed;
    public Vector3 VfxScale => _vfxScale;
}
