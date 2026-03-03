using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCombatDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerRespawnController _respawnController;

    public bool CanReceiveDamage =>
        _player != null &&
        _player.Data != null &&
        (_respawnController == null || !_respawnController.IsDefeated);

    private void Awake()
    {
        if (_player == null)
        {
            _player = GetComponentInParent<Player>();
        }

        if (_respawnController == null)
        {
            _respawnController = GetComponentInParent<PlayerRespawnController>();
        }
    }

    public void ReceiveDamage(int amount, object source = null)
    {
        if (!CanReceiveDamage || amount <= 0)
        {
            return;
        }

        _player.Data.TakeDamage(amount);
    }
}
