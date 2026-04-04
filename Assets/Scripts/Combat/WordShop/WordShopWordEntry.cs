using System;
using UnityEngine;

[Serializable]
public sealed class WordShopWordEntry
{
    [SerializeField] private WordCategory _category;
    [SerializeField] private int _wordValue;
    [SerializeField] private int _memoryLevelCost = 1;

    public int MemoryLevelCost => _memoryLevelCost;

    public string GetLabel()
    {
        return _category switch
        {
            WordCategory.Modifier => CombatWordDefinitions.GetLabel((ModifierWord)_wordValue),
            WordCategory.Element => CombatWordDefinitions.GetLabel((ElementWord)_wordValue),
            WordCategory.Form => CombatWordDefinitions.GetLabel((FormWord)_wordValue),
            _ => string.Empty
        };
    }

    public bool IsUnlocked(SpellWordInventory inventory)
    {
        if (inventory == null)
            return false;

        return _category switch
        {
            WordCategory.Modifier => ContainsWord(inventory.UnlockedModifiers, (ModifierWord)_wordValue),
            WordCategory.Element => ContainsWord(inventory.UnlockedElements, (ElementWord)_wordValue),
            WordCategory.Form => ContainsWord(inventory.UnlockedForms, (FormWord)_wordValue),
            _ => false
        };
    }

    public bool TryUnlock(SpellWordInventory inventory)
    {
        if (inventory == null)
            return false;

        return _category switch
        {
            WordCategory.Modifier => inventory.Unlock((ModifierWord)_wordValue),
            WordCategory.Element => inventory.Unlock((ElementWord)_wordValue),
            WordCategory.Form => inventory.Unlock((FormWord)_wordValue),
            _ => false
        };
    }

    public bool IsValid()
    {
        return _category switch
        {
            WordCategory.Modifier => Enum.IsDefined(typeof(ModifierWord), _wordValue),
            WordCategory.Element => Enum.IsDefined(typeof(ElementWord), _wordValue),
            WordCategory.Form => Enum.IsDefined(typeof(FormWord), _wordValue),
            _ => false
        };
    }

    private static bool ContainsWord<TWord>(System.Collections.Generic.IReadOnlyList<TWord> words, TWord value)
    {
        if (words == null)
            return false;

        for (int i = 0; i < words.Count; i++)
        {
            if (Equals(words[i], value))
                return true;
        }

        return false;
    }
}
