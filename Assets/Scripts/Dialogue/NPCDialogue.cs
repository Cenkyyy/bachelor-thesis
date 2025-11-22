using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC Dialogue", menuName = "Dialogue/NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    [Header("General Settings")]
    [field: SerializeField] public string NpcName { get; private set; }
    [field: SerializeField] public Sprite NpcPortrait { get; private set; }
    [field: SerializeField] public List<string> DialogueLines { get; private set; }

    [Header("Typing Settings")]
    [field: SerializeField] public bool[] AutoProgressLines { get; private set; }
    [field: SerializeField] public float AutoProgressDelay { get; private set; } = 1.5f;
    [field: SerializeField] public float TypingSpeed { get; private set; } = 0.05f;

    [Header("Voice Settings")]
    [field: SerializeField] public AudioClip VoiceSound { get; private set; }
    [field: SerializeField] public float VoicePitch { get; private set; } = 1f;

    public string GetDialogueLine(int index)
    {
        if (DialogueLines == null || index < 0 || index >= DialogueLines.Count)
        {
            return string.Empty;
        }
        return DialogueLines[index];
    }

    public bool IsAutoProgressLine(int index)
    {
        if (AutoProgressLines == null || index < 0 || index >= AutoProgressLines.Length)
        {
            return false;
        }
        return AutoProgressLines[index];
    }
}