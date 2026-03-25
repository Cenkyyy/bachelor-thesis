using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDeathRewards : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyCore _enemyCore;
    [SerializeField] private ItemDropSpawner _dropSpawner;
    [SerializeField] private Transform _dropAnchor;

    [Header("Tuning")]
    [SerializeField] private float _destroyAfterSeconds = 1.5f;

    private bool _wasHandled;

    private void Awake()
    {
        if (_enemyCore == null)
            _enemyCore = GetComponent<EnemyCore>() ?? GetComponentInParent<EnemyCore>();

        if (_dropSpawner == null)
        {
            var spawners = FindObjectsByType<ItemDropSpawner>(FindObjectsSortMode.None);
            if (spawners != null && spawners.Length > 0)
                _dropSpawner = spawners[0];
        }
    }

    public void HandleEnemyDied(object damageSource)
    {
        if (_wasHandled)
            return;

        _wasHandled = true;

        var killer = ResolvePlayer(damageSource);
        GrantXp(killer);
        SpawnLoot();

        var destroyDelay = Mathf.Max(0f, _destroyAfterSeconds);
        if (destroyDelay <= 0f)
            Destroy(gameObject);
        else
            Destroy(gameObject, destroyDelay);
    }

    private void GrantXp(Player killer)
    {
        if (killer == null || _enemyCore == null || _enemyCore.Data == null)
            return;

        var xpReward = Mathf.Max(0, _enemyCore.Data.XpReward);
        if (xpReward <= 0)
            return;

        killer.Data.GainXP(xpReward);
    }

    private void SpawnLoot()
    {
        if (_enemyCore == null || _enemyCore.Data == null || _dropSpawner == null)
            return;

        var lootTable = _enemyCore.Data.Drops;
        if (lootTable == null || lootTable.Count == 0)
            return;

        var dropPosition = _dropAnchor != null ? _dropAnchor.position : transform.position;
        dropPosition.z = 0f;

        for (var i = 0; i < lootTable.Count; i++)
        {
            var entry = lootTable[i];
            var amount = entry.RollAmount();
            if (amount <= 0 || entry.Item == null)
                continue;

            var stack = new InventoryItem(entry.Item, amount);
            _dropSpawner.Spawn(stack, dropPosition);
        }
    }

    private static Player ResolvePlayer(object damageSource)
    {
        switch (damageSource)
        {
            case null:
                return null;
            case Player player:
                return player;
            case Component component:
                return component.GetComponent<Player>() ?? component.GetComponentInParent<Player>();
            case GameObject gameObject:
                return gameObject.GetComponent<Player>() ?? gameObject.GetComponentInParent<Player>();
            default:
                return null;
        }
    }
}
