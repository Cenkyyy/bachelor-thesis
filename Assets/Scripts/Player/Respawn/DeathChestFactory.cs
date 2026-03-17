using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathChestFactory : MonoBehaviour
{
    [SerializeField] private GameObject _deathChestPrefab;
    [SerializeField] private Transform _deathChestParent;

    public DeathChestHandle Create(string chestId, Vector3 worldPosition)
    {
        if (_deathChestPrefab == null)
        {
            Debug.LogWarning("Death chest prefab is missing.");
            return null;
        }

        var chestObject = Instantiate(_deathChestPrefab, worldPosition, Quaternion.identity, _deathChestParent);
        var chestInventory = chestObject.GetComponent<ChestInventory>();

        if (chestInventory == null)
        {
            Debug.LogWarning("Death chest prefab must have a ChestInventory component.");
            Destroy(chestObject);
            return null;
        }

        var chestController = chestObject.GetComponent<TemporaryDeathChestController>();
        if (chestController == null)
            chestController = chestObject.AddComponent<TemporaryDeathChestController>();

        return new DeathChestHandle(chestId, worldPosition, chestInventory, chestController);
    }
}
