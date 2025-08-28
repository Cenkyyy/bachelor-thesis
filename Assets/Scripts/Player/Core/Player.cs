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

    public void TakeDamage(int amount) => PlayerData.TakeDamage(amount);
    public void UseMana(int amount) => PlayerData.UseMana(amount);
    public void EatFood(int amount) => PlayerData.EatFood(amount);
    public void GainXP(int amount) => PlayerData.GainXP(amount);
}