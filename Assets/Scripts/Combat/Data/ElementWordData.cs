using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Element Word", fileName = "ElementWordData")]
public sealed class ElementWordData : WordData
{
    [field: SerializeField] public ElementWord Element { get; private set; }
    [field: SerializeField] public Material Material { get; private set; }

    public override WordCategory Category => WordCategory.Element;
}
