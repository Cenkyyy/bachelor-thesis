using UnityEngine;

/// <summary>
/// Shows short-lived text rows for item pickups (e.g. "+ 3 Copper Ore") at the HUD.
/// </summary>
public sealed class ItemPickupFeed : MonoBehaviour
{
    public static ItemPickupFeed Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform _entriesRoot;
    [SerializeField] private ItemPickupFeedEntry _entryPrefab;

    [Header("Timing")]
    [SerializeField] private float _visibleDuration = 2f;
    [SerializeField] private float _fadeDuration = 1.5f;

    [Header("Limits")]
    [SerializeField] private int _maxVisibleEntries = 6;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowPickup(ItemData itemDefinition, int pickedAmount)
    {
        if (_entriesRoot == null || _entryPrefab == null)
            return;

        if (itemDefinition == null || pickedAmount <= 0)
            return;

        var text = $"+ {pickedAmount} {itemDefinition.ItemName}";

        var instance = Instantiate(_entryPrefab, _entriesRoot);
        instance.transform.SetAsLastSibling();
        instance.Initialize(text, _visibleDuration, _fadeDuration);

        TrimOverflowEntries();
    }

    private void TrimOverflowEntries()
    {
        if (_maxVisibleEntries <= 0)
            return;

        var overflow = _entriesRoot.childCount - _maxVisibleEntries;
        if (overflow <= 0)
            return;

        for (var i = 0; i < overflow; i++)
            Destroy(_entriesRoot.GetChild(i).gameObject);
    }
}
