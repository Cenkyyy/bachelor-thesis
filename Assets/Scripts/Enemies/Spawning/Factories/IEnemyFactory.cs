using UnityEngine;

public interface IEnemyFactory<out TEnemy> where TEnemy : EnemyCore
{
    TEnemy Create(EnemyData data, Vector2 spawnPoint, Transform parent = null);
}
