[System.Serializable]
public class Item
{
    public ItemBaseSO item;
    public int amount;

    public Item(ItemBaseSO item, int amount = 1)
    {
        this.item = item;
        this.amount = amount;
    }
}