using UnityEngine;

/// <summary>
/// Consumes consumables from the selected hotbar slot and applies their instant effects.
/// </summary>
public sealed class PlayerConsumableController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerItemStatController _itemStatController;
    [SerializeField] private GameplayInputBindingsData _inputBindings;

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>();

        if (_itemStatController == null)
            _itemStatController = GetComponent<PlayerItemStatController>();
    }

    private void Update()
    {
        if (_player == null || GameStateManager.IsGamePaused)
            return;

        if (!Input.GetKeyDown(_inputBindings.ConsumeKey))
            return;

        TryConsumeSelectedHotbarItem();
    }

    private void TryConsumeSelectedHotbarItem()
    {
        var slotIndex = _player.Inventory.SelectedHotbarIndex;
        var slotItem = _player.Inventory.GetItemAt(slotIndex);
        if (slotItem.IsEmpty)
            return;

        if (slotItem.Item is not ConsumableItemData consumable)
            return;

        ApplyConsumable(consumable);
        ConsumeOne(slotIndex, slotItem);
    }

    private void ApplyConsumable(ConsumableItemData consumable)
    {
        _player.Data.Heal(consumable.RestoreHealth);
        _player.Data.RecoverMana(consumable.RestoreMana);
        _player.Data.EatFood(consumable.RestoreHunger);

        _itemStatController?.ApplyTimedConsumableModifiers(consumable);
    }

    private void ConsumeOne(int slotIndex, InventoryItem slotItem)
    {
        if (slotItem.Amount <= 1)
        {
            _player.Inventory.ClearItemAt(slotIndex);
            return;
        }

        _player.Inventory.SetItemAt(slotIndex, slotItem.WithAmount(slotItem.Amount - 1));
    }
}
