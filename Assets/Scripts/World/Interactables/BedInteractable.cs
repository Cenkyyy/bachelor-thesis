using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed class BedInteractable : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ParallelWorldPanel _sleepPanel;

    [Header("Rules")]
    [SerializeField] private bool _requireNight = true;
    [SerializeField] private bool _allowDaytimeTesting = false;

    private bool _playerInside;

    private void Awake()
    {
        if (_sleepPanel == null)
            _sleepPanel = FindObjectsByType<ParallelWorldPanel>(FindObjectsSortMode.None).Single();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _playerInside = false;
        }
    }

    private void OnMouseOver()
    {
        if (!CanInteractNow())
            return;

        if (Input.GetMouseButtonDown(1))
        {
            _sleepPanel.Open();
        }
    }

    private bool CanInteractNow()
    {
        if (!_playerInside || GameStateManager.IsGamePaused)
            return false;
        if (Event.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;
        if (_requireNight && !_allowDaytimeTesting)
            return NightTimeFlag.IsNight;

        return true;
    }
}