using UnityEngine;

/// <summary>
/// Consumes hotbar consumables and applies their instant and timed player effects.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerConsumableController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerInputController _inputController;
    [SerializeField] private PlayerStatusEffectController _statusEffectController;
    [SerializeField] private PlayerItemCooldownController _cooldownController;

    private void OnEnable()
    {
        if (_inputController != null)
            _inputController.ConsumePressed += TryConsumeSelectedHotbarItem;
    }

    private void OnDisable()
    {
        if (_inputController != null)
            _inputController.ConsumePressed -= TryConsumeSelectedHotbarItem;
    }

    private void TryConsumeSelectedHotbarItem()
    {
        var slotIndex = _player.Inventory.SelectedHotbarIndex;
        var slotItem = _player.Inventory.GetItemAt(slotIndex);
        if (slotItem.IsEmpty)
            return;

        if (slotItem.Item is not ConsumableItemData consumable)
            return;

        if (_cooldownController.IsOnCooldown(consumable))
            return;

        ApplyConsumable(consumable);
        _cooldownController.TryStartCooldown(consumable);
        ConsumeOne(slotIndex, slotItem);
    }

    private void ApplyConsumable(ConsumableItemData consumable)
    {
        _player.Data.ApplyHealthDelta(consumable.RestoreHealth);
        _player.Data.RecoverMana(consumable.RestoreMana);
        _player.Data.EatFood(consumable.RestoreHunger);
        _statusEffectController.ApplyTimedConsumableStatusEffect(consumable);
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
