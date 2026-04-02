using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/HUD/Item Status Effect Data", fileName = "ItemStatusStatData")]
public sealed class ItemStatusEffectData : ScriptableObject
{
    [field: SerializeField] public ItemStatusEffectType StatType { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
}
