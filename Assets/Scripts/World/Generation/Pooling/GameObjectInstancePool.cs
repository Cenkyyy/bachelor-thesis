using System.Collections.Generic;
using UnityEngine;

public sealed class GameObjectInstancePool
{
    private readonly Dictionary<GameObject, Stack<GameObject>> _availableByPrefab = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> _prefabByInstance = new Dictionary<GameObject, GameObject>();

    public GameObject Acquire(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out bool wasReused)
    {
        wasReused = false;
        if (prefab == null)
            return null;

        if (!_availableByPrefab.TryGetValue(prefab, out var availableInstances))
        {
            availableInstances = new Stack<GameObject>();
            _availableByPrefab[prefab] = availableInstances;
        }

        GameObject instance = null;
        while (availableInstances.Count > 0)
        {
            instance = availableInstances.Pop();
            if (instance != null)
            {
                wasReused = true;
                break;
            }
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, position, rotation, parent);
            _prefabByInstance[instance] = prefab;
            return instance;
        }

        var instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, false);
        instanceTransform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public bool Release(GameObject instance, Transform parent)
    {
        if (instance == null)
            return false;

        if (!_prefabByInstance.TryGetValue(instance, out var prefab) || prefab == null)
            return false;

        if (!_availableByPrefab.TryGetValue(prefab, out var availableInstances))
        {
            availableInstances = new Stack<GameObject>();
            _availableByPrefab[prefab] = availableInstances;
        }

        instance.SetActive(false);
        instance.transform.SetParent(parent, false);
        availableInstances.Push(instance);
        return true;
    }

    public void Clear()
    {
        foreach (var pair in _availableByPrefab)
        {
            var stack = pair.Value;
            while (stack.Count > 0)
            {
                var instance = stack.Pop();
                if (instance != null)
                    Object.Destroy(instance);
            }
        }

        _availableByPrefab.Clear();
        _prefabByInstance.Clear();
    }
}
