using UnityEngine;

/// <summary>
/// UI lookup data that maps an item status effect type to its HUD icon.
/// </summary>
[CreateAssetMenu(menuName = "Game/UI/HUD/Item Status Effect Data", fileName = "ItemStatusStatData")]
public sealed class ItemStatusEffectData : ScriptableObject
{
    [field: SerializeField] public ItemStatusEffectType StatType { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
}
