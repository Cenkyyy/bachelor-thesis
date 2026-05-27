using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDeathRewards : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityCore _entityCore;
    [SerializeField] private WorldItemSpawner _dropSpawner;
    [SerializeField] private Transform _dropAnchor;

    [Header("Tuning")]
    [SerializeField] private float _destroyAfterSeconds = 1.5f;

    private bool _wasHandled;

    private void Awake()
    {
        if (_entityCore == null)
            _entityCore = GetComponent<EntityCore>() ?? GetComponentInParent<EntityCore>();

        if (_dropSpawner == null)
        {
            var spawners = FindObjectsByType<WorldItemSpawner>(FindObjectsSortMode.None);
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
        if (killer == null || _entityCore == null || _entityCore.Data is not EnemyData enemyData)
            return;

        killer.Data.GainXP(enemyData.XpReward);
    }

    private void SpawnLoot()
    {
        if (_entityCore == null || _entityCore.Data is not EnemyData enemyData || _dropSpawner == null)
            return;

        var dropPosition = _dropAnchor != null ? _dropAnchor.position : transform.position;
        LootDropUtility.SpawnInWorld(enemyData.Drops, _dropSpawner, dropPosition);
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
