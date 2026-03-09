using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DeathPanelController : MonoBehaviour, IMajorPanel
{
    [Header("References")]
    [SerializeField] private PlayerRespawnController _respawnController;
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _respawnButton;
    [SerializeField] private Button _returnToMenuButton;

    public PanelId Id => PanelId.Death;
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private void Awake()
    {
        Close();

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

    public void Open()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void ShowPanel()
    {
        PanelManager.Instance.OpenMajorPanel(PanelId.Death);
        return;
    }

    private void HidePanel()
    {
        PanelManager.Instance.CloseCurrentMajorPanel(force: true);
        return;
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
