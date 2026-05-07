using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Data", fileName = "DialogueData")]
public sealed class NarrativeIntroductionData : ScriptableObject
{
    [field: SerializeField, TextArea(2, 6)] public List<string> Lines { get; private set; } = new();

    [field: Header("Visual")]
    [field: SerializeField] public Sprite BackgroundSprite { get; private set; }
    [field: SerializeField] public Color BackgroundColorWhenSpriteAssigned { get; private set; } = Color.white;
    [field: SerializeField] public Color BackgroundFallbackColor { get; private set; } = Color.black;

    [field: Header("Timing")]
    [field: SerializeField, Min(1f)] public float CharactersPerSecond { get; private set; } = 45f;
    [field: SerializeField, Min(0f)] public float AutoAdvanceDelay { get; private set; } = 3.5f;
    [field: SerializeField, Min(0.001f)] public float MaxDeltaTime { get; private set; } = 0.05f;

    [field: Header("Input")]
    [field: SerializeField] public KeyCode AdvanceKey { get; private set; } = KeyCode.Space;

    private void OnValidate()
    {
        if (Lines.Count == 0)
            Debug.LogWarning($"[{nameof(NarrativeIntroductionData)}] '{name}' has no dialogue lines assigned.", this);
    }
}
