using UnityEngine;

public sealed class DeathChestRuntimeData
{
    public string DeathChestId { get; }
    public Vector3 WorldPosition { get; }
    public DeathChestInventory Inventory { get; }
    public DeathChestLifecycleController LifecycleController { get; }

    public DeathChestRuntimeData(string deathChestId, Vector3 worldPosition, DeathChestInventory inventory, DeathChestLifecycleController controller)
    {
        DeathChestId = deathChestId;
        WorldPosition = worldPosition;
        Inventory = inventory;
        LifecycleController = controller;
    }
}
