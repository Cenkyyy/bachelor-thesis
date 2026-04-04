using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Shop Catalog", fileName = "WordShopCatalog")]
public sealed class WordShopCatalogData : ScriptableObject
{
    [SerializeField] private List<WordShopWordEntry> _entries = new();

    public IReadOnlyList<WordShopWordEntry> Entries => _entries;

    public void GetAvailableEntries(SpellWordInventory inventory, List<WordShopWordEntry> resultBuffer)
    {
        resultBuffer.Clear();

        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || !entry.IsValid() || entry.IsUnlocked(inventory))
                continue;

            resultBuffer.Add(entry);
        }
    }
}
