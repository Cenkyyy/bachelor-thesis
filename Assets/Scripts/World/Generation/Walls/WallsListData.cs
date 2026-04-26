using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Walls/Walls List Data")]
public class WallsListData : ScriptableObject
{
    [SerializeField] private List<BiomeWallsListData> _biomeWalls = new();

    public IReadOnlyList<BiomeWallsListData> BiomeWalls => _biomeWalls;
}
