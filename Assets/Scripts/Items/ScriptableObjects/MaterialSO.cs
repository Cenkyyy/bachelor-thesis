using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material")]
public class MaterialSO : ItemBaseSO, ICraftable
{
    [Header(UIStrings.ItemSO_MaterialInfo__Title)]
    public int quantity;
    public string materialType;

    public MaterialSO()
    {
        category = ItemCategory.Material;
    }

    public void Craft(GameObject crafter)
    {
        Debug.Log($"{itemName} crafted by {crafter.name}. Quantity: {quantity}, Type: {materialType}.");
    }
    
    public void Deconstruct(GameObject crafter)
    {
        Debug.Log($"{itemName} deconstructed by {crafter.name}. Quantity: {quantity}, Type: {materialType}.");
    }
}