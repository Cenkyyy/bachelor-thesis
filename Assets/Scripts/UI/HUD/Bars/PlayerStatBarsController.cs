using UnityEngine;

public class PlayerStatBarsController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private HealthBarUI _healthBar;
    [SerializeField] private ManaBarUI _manaBar;
    [SerializeField] private HungerBarUI _hungerBar;
    [SerializeField] private XpBarUI _xpBar;
    [SerializeField] private MemoryXpBarUI _memoryXpBar;

    private void Start()
    {
        _healthBar.Initialize(_player.Data);
        _manaBar.Initialize(_player.Data);
        _hungerBar.Initialize(_player.Data);
        _xpBar.Initialize(_player.Data);
        _memoryXpBar.Initialize(_player.Data);
    }
}
