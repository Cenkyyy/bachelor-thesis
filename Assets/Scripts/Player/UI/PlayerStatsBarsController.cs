using System.Linq;
using UnityEngine;

public class PlayerStatsBarsController : MonoBehaviour
{
    [SerializeField] PlayerDataSO playerData;

    private IStatBar[] _statBars;

    private void Awake()
    {
        _statBars = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IStatBar>().ToArray();
        foreach (var statBar in _statBars)
        {
            statBar.Initialize(playerData);
        }
    }

    void Update()
    {
        foreach (var statBar in _statBars)
        {
            statBar.UpdateBar();
        }
    }
}
