using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerDamageable : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerRespawnController _respawnController;

    [Header("Defence damage reduction")]
    [SerializeField, Min(0.01f)] private float _defenceCoefficient = 100f;
    [SerializeField, Min(0)] private int _minimumDamageTaken = 1;

    [Header("Damage Word Text Popup Settings")]
    [SerializeField] private DamageWordTextPopupSettings _damageWordTextPopupSettings = new();

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

    private void OnEnable()
    {
        if (_player != null && _player.Data != null)
            _player.Data.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (_player != null && _player.Data != null)
            _player.Data.OnDamageTaken -= HandleDamageTaken;
    }

    public void ReceiveDamage(int amount, object source = null)
    {
        if (!CanReceiveDamage || amount <= 0)
        {
            return;
        }

        var finalDamage = CalculateFinalDamage(amount, _player.Data.Defence);
        if (finalDamage <= 0)
            return;

        _player.Data.TakeDamage(finalDamage);
    }

    private void HandleDamageTaken(int damage)
    {
        if (damage <= 0)
            return;

        DamageWordTextPopupUtility.ShowForGameObject(gameObject, damage, 1f, _damageWordTextPopupSettings);
    }

    private int CalculateFinalDamage(int incomingDamage, int defense)
    {
        defense = Mathf.Max(0, defense);
        var damageMultiplier = _defenceCoefficient / (_defenceCoefficient + defense);
        var reducedDamage = Mathf.CeilToInt(incomingDamage * damageMultiplier);
        return Mathf.Max(_minimumDamageTaken, reducedDamage);
    }
}
