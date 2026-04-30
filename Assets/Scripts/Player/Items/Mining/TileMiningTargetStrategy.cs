using UnityEngine;

public sealed class TileMiningTargetStrategy : IMiningTargetStrategy
{
    private readonly WallChunkGenerator _wallChunkGenerator;

    public TileMiningTargetStrategy(WallChunkGenerator wallChunkGenerator)
    {
        _wallChunkGenerator = wallChunkGenerator;
    }

    public bool TryResolveTarget(Vector3 worldPosition, out IMineableTarget target)
    {
        target = null;
        return _wallChunkGenerator != null && _wallChunkGenerator.TryCreateMiningTarget(worldPosition, out target);
    }
}
