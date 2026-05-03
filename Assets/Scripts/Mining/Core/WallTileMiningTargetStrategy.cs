using UnityEngine;

public sealed class WallTileMiningTargetStrategy : IMiningTargetStrategy
{
    private readonly WallChunkGenerator _wallChunkGenerator;

    public WallTileMiningTargetStrategy(WallChunkGenerator wallChunkGenerator)
    {
        _wallChunkGenerator = wallChunkGenerator;
    }

    public bool TryResolveTarget(Vector3 worldPosition, out IMineableTarget target)
    {
        target = null;
        return _wallChunkGenerator != null && _wallChunkGenerator.TryCreateMiningTarget(worldPosition, out target);
    }
}
