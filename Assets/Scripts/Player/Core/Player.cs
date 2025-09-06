using UnityEngine;

[RequireComponent(typeof(PlayerInventoryWrapper))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerDataSO playerDataSO;

    public PlayerData PlayerData { get; private set; } = new PlayerData();

    private PlayerInventoryWrapper _playerInventoryWrapper;

    private void Awake()
    {
        // initialize player stats
        PlayerData.InitializeFrom(playerDataSO);

        // initialzie inventory wrapper
        _playerInventoryWrapper = GetComponent<PlayerInventoryWrapper>();
        if (_playerInventoryWrapper != null)
        {
            _playerInventoryWrapper.InitializeFromPlayer(playerDataSO);
        }
    }
}