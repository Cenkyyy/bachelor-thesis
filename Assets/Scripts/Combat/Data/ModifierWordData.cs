using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Modifier Word", fileName = "ModifierWordData")]
public sealed class ModifierWordData : WordData
{
    [field: SerializeField] public ModifierWordType Modifier { get; private set; }
    [field: SerializeField] public int AdditionalManaCost { get; private set; }
    [field: SerializeField] public GameObject OptionalPrefab { get; private set; }

    public override WordCategory Category => WordCategory.Modifier;
    public override bool IsValid => System.Enum.IsDefined(typeof(ModifierWordType), Modifier);
}
