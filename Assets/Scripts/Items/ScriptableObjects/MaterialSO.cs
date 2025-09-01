using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material")]
public class MaterialSO : ItemBaseSO, ICraftable
{
    [Header(UIStrings.ItemSO_MaterialInfo__Title)]
    [field: SerializeField] public int Quantity { get; private set; }
    [field: SerializeField] public string MaterialType { get; private set; }

    public MaterialSO()
    {
        Category = ItemCategory.Material;
    }

    public void Craft(GameObject crafter)
    {
        Debug.Log($"{ItemName} crafted by {crafter.name}. Quantity: {Quantity}, Type: {MaterialType}.");
    }
    
    public void Deconstruct(GameObject crafter)
    {
        Debug.Log($"{ItemName} deconstructed by {crafter.name}. Quantity: {Quantity}, Type: {MaterialType}.");
    }
}