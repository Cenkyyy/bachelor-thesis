using UnityEngine;

public sealed class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode _interactionKey = KeyCode.X;
    [SerializeField] private float _interactionRadius = 1.5f;
    [SerializeField] private LayerMask _interactionMask;

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (Input.GetKeyDown(_interactionKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _interactionRadius, _interactionMask);
        if (hits == null || hits.Length == 0)
            return;

        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            var interactable = hit.GetComponent<IInteractable>() ?? hit.GetComponentInParent<IInteractable>();

            if (interactable == null)
                continue;
            if (!interactable.CanInteract())
                continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = interactable;
            }
        }

        if (best != null)
            best.Interact();
    }
}
