using UnityEngine;

public sealed class WallTileMiningTarget : IMineableTarget
{
    private readonly WallChunkGenerator _wallGenerator;
    private readonly Vector2Int _tile;

    public WallTileMiningTarget(WallChunkGenerator wallGenerator, Vector2Int tile)
    {
        _wallGenerator = wallGenerator;
        _tile = tile;
    }

    public Vector3 WorldPosition => _wallGenerator != null ? _wallGenerator.GetTileCenterWorld(_tile) : Vector3.zero;

    public bool CanBeMinedWith(MiningToolContext tool)
    {
        return _wallGenerator != null && _wallGenerator.CanMineTile(_tile, tool);
    }

    public void ShowHigherToolRequiredFeedback()
    {
        _wallGenerator?.ShowHigherToolRequiredFeedback(_tile);
    }

    public void NotifyMiningStarted()
    {
        _wallGenerator?.NotifyMiningStarted(_tile);
    }

    public void ApplyMiningDamage(float basePower, Player miner, ItemDropSpawner dropSpawner)
    {
        _wallGenerator?.ApplyMiningDamage(_tile, basePower, miner, dropSpawner);
    }

    public void NotifyMiningStopped()
    {
        _wallGenerator?.NotifyMiningStopped(_tile);
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var wallTarget = other as WallTileMiningTarget;
        if (wallTarget == null)
            return false;

        return wallTarget._wallGenerator == _wallGenerator && wallTarget._tile == _tile;
    }
}
