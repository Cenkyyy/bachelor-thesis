using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows the player's equipped items and forwards equipment slot interactions.
/// </summary>
public sealed class EquipmentPanel : MonoBehaviour, IPanel
{
    [Header("View")]
    [SerializeField] private EquipmentSlotView[] _slots;
    [SerializeField] private Transform _slotsParent;

    [Header("Model")]
    [SerializeField] private Player _player;

    [Header("Input")]
    [SerializeField] private InventoryItemInteractionController _itemInteractionController;

    [Header("Tooltip")]
    [SerializeField] private ItemTooltipController _tooltipController;

    public bool IsOpen => _slotsParent != null && _slotsParent.gameObject.activeSelf;

    private void Start()
    {
        StartCoroutine(InitializeEquipmentPanelCoroutine());
    }

    private IEnumerator InitializeEquipmentPanelCoroutine()
    {
        yield return null;
        BindSlots();
        if (_player?.Equipment != null)
            _player.Equipment.OnItemChanged += RefreshSlot;
    }

    private void OnDestroy()
    {
        if (_player?.Equipment != null)
            _player.Equipment.OnItemChanged -= RefreshSlot;

        UnbindSlots();
    }

    private void BindSlots()
    {
        if (_player?.Equipment == null || _slots == null || _slotsParent == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i])
                continue;

            if (!_player.Equipment.TryGetIndexForSlotType(_slots[i].SlotType, out int inventoryIndex))
                continue;

            _slots[i].Bind(_player.Equipment, inventoryIndex, _player.Equipment.GetItemAt(inventoryIndex));
            _slots[i].OnPointerClicked += HandleSlotClicked;
            _slots[i].OnPointerEntered += HandleSlotEnter;
            _slots[i].OnPointerExited += HandleSlotExit;
            _slots[i].OnSlotDisabled += HandleSlotDisabled;
        }
    }

    private void UnbindSlots()
    {
        if (_slots == null)
            return;

        foreach (var slot in _slots)
        {
            if (!slot)
                continue;
            
            slot.OnPointerClicked -= HandleSlotClicked;
            slot.OnPointerEntered -= HandleSlotEnter;
            slot.OnPointerExited -= HandleSlotExit;
            slot.OnSlotDisabled -= HandleSlotDisabled;
        }
    }

    public void Open()
    {
        _slotsParent.gameObject.SetActive(true);
    }

    public void Close()
    {
        _slotsParent.gameObject.SetActive(false);
    }

    public void RefreshSlot(int index)
    {
        if (_player?.Equipment == null || _slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (!slot) 
                continue;

            if (!_player.Equipment.TryGetIndexForSlotType(slot.SlotType, out int inventoryIndex))
                continue;

            if (inventoryIndex != index)
                continue;

            slot.Refresh(_player.Equipment.GetItemAt(index));
            return;
        }
    }

    private void HandleSlotClicked(InventorySlotView slot, PointerEventData evt) 
    {
        _itemInteractionController?.OnSlotPointerClicked(slot, evt);
    }

    private void HandleSlotEnter(InventorySlotView slot, PointerEventData _)
    {
        _itemInteractionController?.OnSlotPointerEnter(slot);
        _tooltipController?.OnSlotPointerEnter(slot);
    }

    private void HandleSlotExit(InventorySlotView slot, PointerEventData _)
    {
        _tooltipController?.OnSlotPointerExit(slot);
    }

    private void HandleSlotDisabled(InventorySlotView slot)
    {
        _tooltipController?.OnSlotDisabled(slot);
    }
}
