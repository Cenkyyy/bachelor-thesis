using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CharacterPanel : MonoBehaviour, IPanel
{
    [Header("View")]
    [SerializeField] private EquipmentSlot _slotPrefab;
    [SerializeField] private Transform _slotParent;

    [Header("Model")]
    [SerializeField] private Player _player;

    public bool IsOpen => _slotParent != null && _slotParent.gameObject.activeSelf;

    private EquipmentSlot[] _slots;

    private void Start()
    {
        BuildSlots();
        if (_player?.Equipment != null)
            _player.Equipment.OnItemChanged += RefreshSlot;
    }

    private void OnDestroy()
    {
        if (_player?.Equipment != null)
            _player.Equipment.OnItemChanged -= RefreshSlot;

        if (_slots != null)
        {
            foreach (var s in _slots)
            {
                if (!s) continue;
                s.OnPointerClicked -= HandleSlotClicked;
                s.OnPointerEntered -= HandleSlotEnter;
            }
        }
    }

    private void BuildSlots()
    {
        if (_player?.Equipment == null || _slotPrefab == null || _slotParent == null)
            return;

        _slots = new EquipmentSlot[_player.Equipment.Capacity];
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = Instantiate(_slotPrefab, _slotParent);
            _slots[i].Bind(_player.Equipment, i, _player.Equipment.GetItemAt(i));

            _slots[i].OnPointerClicked += HandleSlotClicked;
            _slots[i].OnPointerEntered += HandleSlotEnter;
        }
    }

    public void Open()
    {
        _slotParent.gameObject.SetActive(true);
    }

    public void Close()
    {
        _slotParent.gameObject.SetActive(false);
    }

    public void RefreshSlot(int index)
    {
        if (_slots == null || index < 0 || index >= _slots.Length) return;
        _slots[index].Refresh(_player.Equipment.GetItemAt(index));
    }

    private void HandleSlotClicked(Slot slot, PointerEventData evt) =>
        ItemInteractionController.Instance?.OnSlotPointerClicked(slot, evt);

    private void HandleSlotEnter(Slot slot, PointerEventData evt) =>
        ItemInteractionController.Instance?.OnSlotPointerEnter(slot, evt);
}
