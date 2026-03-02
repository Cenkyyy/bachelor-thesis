using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DeathPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRespawnController _respawnController;
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _respawnButton;
    [SerializeField] private Button _returnToMenuButton;

    private void Awake()
    {
        _panelRoot.SetActive(false);

        _respawnButton.onClick.AddListener(HandleRespawnClicked);
        _returnToMenuButton.onClick.AddListener(HandleReturnToMenuClicked);
    }

    private void OnEnable()
    {
        if (_respawnController == null)
            return;

        _respawnController.OnDefeated += ShowPanel;
        _respawnController.OnRespawned += HidePanel;
    }

    private void OnDisable()
    {
        if (_respawnController == null)
            return;

        _respawnController.OnDefeated -= ShowPanel;
        _respawnController.OnRespawned -= HidePanel;
    }

    private void ShowPanel()
    {
        _panelRoot.SetActive(true);
        GameStateManager.SetPause(true);
    }

    private void HidePanel()
    {
        _panelRoot.SetActive(false);
        GameStateManager.SetPause(false);
    }

    private void HandleRespawnClicked()
    {
        _respawnController.Respawn();
    }

    private void HandleReturnToMenuClicked()
    {
        _respawnController.ReturnToMenu();
    }
}
