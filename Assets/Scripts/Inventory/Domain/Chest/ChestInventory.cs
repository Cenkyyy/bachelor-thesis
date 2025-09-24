using UnityEngine;

[DisallowMultipleComponent]
public sealed class ChestInventory : MonoBehaviour
{
    [SerializeField] private int _size = 24;

    public IInventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = new Inventory(_size);
    }
}