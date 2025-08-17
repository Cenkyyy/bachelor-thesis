using UnityEngine;

public class HotbarController : MonoBehaviour
{
    [SerializeField] GameObject hotbarPanel;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] int slotCount = 8;

    private int _selectedIndex = 0;
    private HotbarSlot[] slots;

    void Start()
    {
        slots = new HotbarSlot[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, hotbarPanel.transform);
            HotbarSlot slot = slotObject.GetComponent<HotbarSlot>();
            slots[i] = slot;

            slot.SetToDefault();
        }

        slots[_selectedIndex].HighlightSlot();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // scroll up
            ChangeSelectedSlot((_selectedIndex + 1) % slotCount);
        }
        else if (scroll < 0f)
        {
            // scroll down
            ChangeSelectedSlot((_selectedIndex - 1 + slotCount) % slotCount);
        }
    }

    private void ChangeSelectedSlot(int newIndex)
    {
        slots[_selectedIndex].SetToDefault();
        _selectedIndex = newIndex;
        slots[_selectedIndex].HighlightSlot();
    }
}
