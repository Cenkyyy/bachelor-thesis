using System.Collections.Generic;

public enum WordCategory
{
    Modifier = 0,
    Element = 1,
    Form = 2
}

public enum ModifierWord
{
    Piercing = 0,
    Stunning = 1,
    Exploding = 2,
    Reclaiming = 3,
    Splitting = 4
}

public enum ElementWord
{
    Lightning = 0,
    Poison = 1,
    Frost = 2,
    Ember = 3,
    Dark = 4
}

public enum FormWord
{
    Shard = 0,
    Beam = 1,
    Wave = 2,
    Barrage = 3
}

public static class CombatWordDefinitions
{
    private static readonly Dictionary<ModifierWord, string> ModifierLabels = new()
    {
        { ModifierWord.Piercing, "Piercing" },
        { ModifierWord.Stunning, "Stunning" },
        { ModifierWord.Exploding, "Exploding" },
        { ModifierWord.Reclaiming, "Reclaiming" },
        { ModifierWord.Splitting, "Splitting" }
    };

    private static readonly Dictionary<ElementWord, string> ElementLabels = new()
    {
        { ElementWord.Lightning, "Lightning" },
        { ElementWord.Poison, "Poison" },
        { ElementWord.Frost, "Frost" },
        { ElementWord.Ember, "Ember" },
        { ElementWord.Dark, "Dark" }
    };

    private static readonly Dictionary<FormWord, string> FormLabels = new()
    {
        { FormWord.Shard, "Shard" },
        { FormWord.Beam, "Beam" },
        { FormWord.Wave, "Wave" },
        { FormWord.Barrage, "Barrage" }
    };

    public static string GetLabel(ModifierWord word) => ModifierLabels[word];

    public static string GetLabel(ElementWord word) => ElementLabels[word];

    public static string GetLabel(FormWord word) => FormLabels[word];
}