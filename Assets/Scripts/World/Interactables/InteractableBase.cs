using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    protected bool IsPlayerInside { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        IsPlayerInside = true;
        OnPlayerEnterTrigger(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        IsPlayerInside = false;
        OnPlayerExitTrigger(other);
    }

    private void OnMouseOver()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        Interact();
    }

    protected virtual void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
    }

    protected virtual void OnPlayerExitTrigger(Collider2D playerCollider)
    {
    }

    public abstract bool CanInteract();
    public abstract void Interact();
}
