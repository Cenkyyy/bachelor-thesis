using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material")]
public class MaterialItem : Item, ICraftable
{
    [field: SerializeField] public int Quantity { get; private set; }
    [field: SerializeField] public string MaterialType { get; private set; }

    public MaterialItem()
    {
        Category = ItemType.Material;
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