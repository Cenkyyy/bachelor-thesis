using System.Collections;
using UnityEngine;

/// <summary>
/// Connects player runtime data to the HUD stat bar views.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStatBarsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private HealthBarView _healthBar;
    [SerializeField] private ManaBarView _manaBar;
    [SerializeField] private HungerBarView _hungerBar;
    [SerializeField] private XpBarView _xpBar;
    [SerializeField] private MemoryXpBarView _memoryXpBar;

    private void Start()
    {
        StartCoroutine(InitializeBarsCoroutine());
    }

    private IEnumerator InitializeBarsCoroutine()
    {
        yield return null;

        if (_player?.Data == null)
            yield break;

        _healthBar?.Initialize(_player.Data);
        _manaBar?.Initialize(_player.Data);
        _hungerBar?.Initialize(_player.Data);
        _xpBar?.Initialize(_player.Data);
        _memoryXpBar?.Initialize(_player.Data);
    }
}
