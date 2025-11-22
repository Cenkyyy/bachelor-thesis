using UnityEngine;

[RequireComponent(typeof(HumanNpcCore))]
public sealed class HumanNpcInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCDialogue _dialogue;

    private HumanNpcCore _core;

    private void Awake()
    {
        _core = GetComponent<HumanNpcCore>();
    }

    public bool CanInteract()
    {
        if (_dialogue == null)
            return false;
        if (DialogueController.Instance == null)
            return false;

        return !DialogueController.Instance.IsDialogueActive;
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        DialogueController.Instance.StartDialogue(_dialogue, _core);
    }
}
