using UnityEngine;

/// <summary>
/// Marks a scene object as persistent so it survives scene loads.
/// </summary>
public class PersistentGameObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
