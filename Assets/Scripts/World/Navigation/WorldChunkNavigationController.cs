using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared chunk-aware navigation grid for entity path and clearance queries.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldChunkNavigationController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WorldGenerationController _worldGenerator;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;

    [Header("Grid")]
    [SerializeField] private float _cellSize = 0.25f;
    [SerializeField] private int _chunkSizeTiles = 16;

    private readonly Dictionary<Vector2Int, int> _blockedCellCounts = new();
    private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _blockedCellsByChunk = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> _wallCellsByTile = new();
    private readonly Dictionary<WorldNavigationObstacle, List<Vector2Int>> _obstacleCellsByObstacle = new();

    public static WorldChunkNavigationController Instance { get; private set; }

    private void OnEnable()
    {
        Instance = this;
        RegisterWallEvents();
    }

    private void OnDisable()
    {
        UnregisterWallEvents();

        if (Instance == this)
            Instance = null;
    }

    public bool TryBuildPath(
        Vector2 startWorld,
        Vector2 targetWorld,
        float nodeStep,
        float probeRadius,
        int maxIterations,
        List<Vector2> output)
    {
        return GridAStarPathfinder.TryBuildPath(
            startWorld: startWorld,
            goalWorld: targetWorld,
            nodeStep: nodeStep,
            probeRadius: probeRadius,
            maxIterations: maxIterations,
            output: output,
            canUseWorldPosition: IsWorldAreaWalkable,
            allowPartialPath: true
        );
    }

    public bool CanMoveDirectly(Vector2 startWorld, Vector2 targetWorld, float probeRadius)
    {
        var offset = targetWorld - startWorld;
        if (offset.sqrMagnitude <= Mathf.Epsilon)
            return IsWorldAreaWalkable(targetWorld, probeRadius);

        var distance = offset.magnitude;
        var stepDistance = _cellSize * 0.5f;
        var steps = Mathf.CeilToInt(distance / stepDistance);
        var direction = offset / steps;

        for (var i = 0; i <= steps; i++)
        {
            var sample = startWorld + direction * i;
            if (!IsWorldAreaWalkable(sample, probeRadius))
                return false;
        }

        return true;
    }

    public void RegisterObstacle(WorldNavigationObstacle obstacle)
    {
        if (obstacle == null)
            return;

        UnregisterObstacle(obstacle);

        var cells = new List<Vector2Int>();
        var colliders = obstacle.BlockingColliders;
        for (var i = 0; i < colliders.Count; i++)
        {
            var collider = colliders[i];
            if (collider == null || collider.isTrigger || !collider.enabled || !collider.gameObject.activeInHierarchy)
                continue;

            AppendBoundsCells(collider.bounds, cells);
        }

        if (cells.Count == 0)
            return;

        AddBlockedCells(cells);
        _obstacleCellsByObstacle[obstacle] = cells;
    }

    public void UnregisterObstacle(WorldNavigationObstacle obstacle)
    {
        if (obstacle == null || !_obstacleCellsByObstacle.TryGetValue(obstacle, out var cells))
            return;

        RemoveBlockedCells(cells);
        _obstacleCellsByObstacle.Remove(obstacle);
    }

    public void RefreshObstacle(WorldNavigationObstacle obstacle)
    {
        if (obstacle == null)
            return;

        UnregisterObstacle(obstacle);
        RegisterObstacle(obstacle);
    }

    public bool IsWorldAreaWalkable(Vector2 worldPoint, float probeRadius)
    {
        if (!IsWorldPointWalkable(worldPoint))
            return false;

        var centerCell = ToNavigationCell(worldPoint);
        var radiusCells = Mathf.CeilToInt(probeRadius / _cellSize);
        for (var y = -radiusCells; y <= radiusCells; y++)
        {
            for (var x = -radiusCells; x <= radiusCells; x++)
            {
                var cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                if (!IsBlockedCellWithinProbe(cell, worldPoint, probeRadius))
                    continue;

                if (IsCellBlocked(cell))
                    return false;
            }
        }

        return true;
    }

    public bool IsWorldPointWalkable(Vector2 worldPoint)
    {
        if (_worldGenerator == null || _worldGenerator.CurrentWorldData == null || _worldGenerator.GroundTilemap == null)
            return true;

        var cell = _worldGenerator.GroundTilemap.WorldToCell(worldPoint);
        var dataTile = _worldGenerator.CurrentWorldData.CellToData(cell);
        if (!_worldGenerator.CurrentWorldData.IsInside(dataTile.x, dataTile.y))
            return false;

        return _worldGenerator.CurrentWorldData.GetTile(dataTile.x, dataTile.y).TileType != WorldTileType.BorderBase;
    }

    private void RegisterWallEvents()
    {
        if (_wallChunkGenerator == null)
            return;

        _wallChunkGenerator.OnWallTileChanged -= HandleWallTileChanged;
        _wallChunkGenerator.OnWallTileChanged += HandleWallTileChanged;
        SyncLoadedWallTiles();
    }

    private void UnregisterWallEvents()
    {
        if (_wallChunkGenerator == null)
            return;

        _wallChunkGenerator.OnWallTileChanged -= HandleWallTileChanged;
    }

    private void HandleWallTileChanged(Vector2Int dataTile)
    {
        if (_wallChunkGenerator == null)
            return;

        if (_wallChunkGenerator.HasWallAtDataTile(dataTile))
            RegisterWallTile(dataTile);
        else
            UnregisterWallTile(dataTile);
    }

    private void SyncLoadedWallTiles()
    {
        var registeredTiles = new List<Vector2Int>(_wallCellsByTile.Keys);
        for (var i = 0; i < registeredTiles.Count; i++)
            UnregisterWallTile(registeredTiles[i]);

        foreach (var dataTile in _wallChunkGenerator.GetLoadedWallTiles())
            RegisterWallTile(dataTile);
    }

    private void RegisterWallTile(Vector2Int dataTile)
    {
        UnregisterWallTile(dataTile);

        if (_worldGenerator == null || _worldGenerator.CurrentWorldData == null || _worldGenerator.GroundTilemap == null)
            return;

        var worldCenter = _worldGenerator.GroundTilemap.GetCellCenterWorld(_worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y));
        var bounds = new Bounds(worldCenter, Vector3.one);
        var cells = new List<Vector2Int>();
        AppendBoundsCells(bounds, cells);

        AddBlockedCells(cells);
        _wallCellsByTile[dataTile] = cells;
    }

    private void UnregisterWallTile(Vector2Int dataTile)
    {
        if (!_wallCellsByTile.TryGetValue(dataTile, out var cells))
            return;

        RemoveBlockedCells(cells);
        _wallCellsByTile.Remove(dataTile);
    }

    private void AppendBoundsCells(Bounds bounds, List<Vector2Int> cells)
    {
        var min = ToNavigationCell(bounds.min);
        var max = ToNavigationCell(bounds.max);

        for (var y = min.y; y <= max.y; y++)
        {
            for (var x = min.x; x <= max.x; x++)
                cells.Add(new Vector2Int(x, y));
        }
    }

    private void AddBlockedCells(List<Vector2Int> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            _blockedCellCounts.TryGetValue(cell, out var count);
            _blockedCellCounts[cell] = count + 1;
            if (count > 0)
                continue;

            var chunk = ToNavigationChunk(cell);
            if (!_blockedCellsByChunk.TryGetValue(chunk, out var chunkCells))
            {
                chunkCells = new HashSet<Vector2Int>();
                _blockedCellsByChunk[chunk] = chunkCells;
            }

            chunkCells.Add(cell);
        }
    }

    private void RemoveBlockedCells(List<Vector2Int> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (!_blockedCellCounts.TryGetValue(cell, out var count))
                continue;

            if (count <= 1)
            {
                _blockedCellCounts.Remove(cell);
                RemoveBlockedCellFromChunk(cell);
            }
            else
                _blockedCellCounts[cell] = count - 1;
        }
    }

    private void RemoveBlockedCellFromChunk(Vector2Int cell)
    {
        var chunk = ToNavigationChunk(cell);
        if (!_blockedCellsByChunk.TryGetValue(chunk, out var chunkCells))
            return;

        chunkCells.Remove(cell);
        if (chunkCells.Count == 0)
            _blockedCellsByChunk.Remove(chunk);
    }

    private bool IsCellBlocked(Vector2Int cell)
    {
        return _blockedCellCounts.ContainsKey(cell);
    }

    private bool IsBlockedCellWithinProbe(Vector2Int cell, Vector2 worldPoint, float probeRadius)
    {
        var cellCenter = ToWorldPosition(cell);
        var maxDistance = probeRadius + _cellSize * 0.7072f;
        return (cellCenter - worldPoint).sqrMagnitude <= maxDistance * maxDistance;
    }

    private Vector2Int ToNavigationCell(Vector2 worldPoint)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPoint.x / _cellSize), Mathf.FloorToInt(worldPoint.y / _cellSize));
    }

    private Vector2 ToWorldPosition(Vector2Int navigationCell)
    {
        return new Vector2((navigationCell.x + 0.5f) * _cellSize, (navigationCell.y + 0.5f) * _cellSize);
    }

    private Vector2Int ToNavigationChunk(Vector2Int navigationCell)
    {
        var cellsPerChunk = Mathf.RoundToInt(_chunkSizeTiles / _cellSize);
        return new Vector2Int(
            Mathf.FloorToInt((float)navigationCell.x / cellsPerChunk),
            Mathf.FloorToInt((float)navigationCell.y / cellsPerChunk)
        );
    }
}
