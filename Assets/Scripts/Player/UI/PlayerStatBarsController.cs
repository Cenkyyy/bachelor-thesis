using UnityEngine;

public class PlayerStatBarsController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private HealthBarUI _healthBar;
    [SerializeField] private ManaBarUI _manaBar;
    [SerializeField] private HungerBarUI _hungerBar;
    [SerializeField] private XpBarUI _xpBar;

    private void Start()
    {
        if (_player == null || _player.Data == null ||
            _healthBar == null || _manaBar == null ||
            _hungerBar == null || _xpBar == null)
        {
            return;
        }

        _healthBar.Initialize(_player.Data);
        _manaBar.Initialize(_player.Data);
        _hungerBar.Initialize(_player.Data);
        _xpBar.Initialize(_player.Data);
    }
}
