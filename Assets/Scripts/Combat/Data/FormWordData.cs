using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Words/Form Word", fileName = "FormWordData")]
public sealed class FormWordData : WordData
{
    [field: SerializeField] public FormWord Form { get; private set; }
    [field: SerializeField] public int ManaCost { get; private set; }
    [field: SerializeField] public GameObject ProjectilePrefab { get; private set; }

    public override WordCategory Category => WordCategory.Form;
}
