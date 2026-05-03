using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Modifier Word", fileName = "ModifierWordData")]
public sealed class ModifierWordData : WordData
{
    [field: SerializeField] public ModifierWord Modifier { get; private set; }
    [field: SerializeField] public int AdditionalManaCost { get; private set; }
    [field: SerializeField] public GameObject OptionalPrefab { get; private set; }

    public override WordCategory Category => WordCategory.Modifier;
}
