using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] PlayerStatsSO playerStats;
    private IStatBar[] _statBars;

    private void Awake()
    {
        _statBars = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IStatBar>().ToArray();
        foreach (var statBar in _statBars)
        {
            statBar.Initialize(playerStats);
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
