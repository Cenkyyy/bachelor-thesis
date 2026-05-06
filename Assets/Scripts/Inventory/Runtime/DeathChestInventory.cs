using UnityEngine;

/// <summary>
/// Runtime component that creates storage for a death chest.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeathChestInventory : MonoBehaviour
{
    [Header("Storage")]
    [SerializeField] private int _size = 24;

    public IInventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = new Inventory(_size);
    }
}
