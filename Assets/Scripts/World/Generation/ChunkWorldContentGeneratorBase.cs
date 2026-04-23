using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ChunkWorldContentGeneratorBase : MonoBehaviour, ISceneTransitionReadinessBlocker
{
    [Header("Dependencies")]
    [SerializeField] protected WorldGenerationController _worldGenerator;
    [SerializeField] protected Transform _playerTransform;

    [Header("Chunk Settings")]
    [SerializeField, Min(8)] protected int _chunkSize = 32;
    [SerializeField, Min(0)] protected int _initialGenerationRadiusInChunks = 4;
    [SerializeField, Min(0)] protected int _generationRadiusInChunks = 4;
    [SerializeField, Min(0)] protected int _unloadRadiusInChunks = 6;
    [SerializeField, Min(0.02f)] protected float _refreshIntervalSeconds = 0.1f;
    [SerializeField, Min(0f)] protected float _initialRefreshOffsetSeconds = 0.03f;
    [SerializeField, Min(1)] protected int _chunksGeneratedPerFrame = 1;
    [SerializeField, Min(1)] protected int _chunksUnloadedPerFrame = 1;

    private Coroutine _streamingCoroutine;

    public bool IsReadyForSceneReveal { get; private set; }

    protected virtual bool EnableChunkUnloading => true;

    protected virtual void OnEnable()
    {
        IsReadyForSceneReveal = false;

        if (_streamingCoroutine != null)
            StopCoroutine(_streamingCoroutine);

        _streamingCoroutine = StartCoroutine(StreamChunksCoroutine());
    }

    protected virtual void OnDisable()
    {
        if (_streamingCoroutine != null)
        {
            StopCoroutine(_streamingCoroutine);
            _streamingCoroutine = null;
        }

        RunImmediate(ClearGeneratedChunksCoroutine());
        IsReadyForSceneReveal = false;
    }

    public int SpawnMissingChunksAround(Vector3 worldPosition, int radiusInChunks, int maxChunksToSpawn)
    {
        if (!TryGetWorldData(out var data))
            return 0;

        var focusTile = _worldGenerator.RuntimeState.ResolveTileFromWorld(worldPosition);
        var focusChunk = WorldChunkUtility.GetChunkCoordFromTile(focusTile, _chunkSize);
        var desiredChunks = WorldChunkUtility.BuildChunkSetInRadius(focusChunk, radiusInChunks);

        int spawnedCount = 0;
        for (int i = 0; i < desiredChunks.Count; i++)
        {
            var chunkCoord = desiredChunks[i];
            if (IsChunkLoaded(chunkCoord))
                continue;

            GenerateChunk(data, chunkCoord);
            spawnedCount++;

            if (spawnedCount >= maxChunksToSpawn)
                break;
        }

        return spawnedCount;
    }

    public bool AreChunksSpawnedAround(Vector3 worldPosition, int radiusInChunks)
    {
        if (!TryGetWorldData(out _))
            return false;

        var focusTile = _worldGenerator.RuntimeState.ResolveTileFromWorld(worldPosition);
        var focusChunk = WorldChunkUtility.GetChunkCoordFromTile(focusTile, _chunkSize);
        var desiredChunks = WorldChunkUtility.BuildChunkSetInRadius(focusChunk, radiusInChunks);

        for (int i = 0; i < desiredChunks.Count; i++)
        {
            if (!IsChunkLoaded(desiredChunks[i]))
                return false;
        }

        return true;
    }

    protected virtual bool CanStartStreaming()
    {
        return true;
    }

    protected abstract bool IsChunkLoaded(Vector2Int chunkCoord);
    protected abstract void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord);
    protected abstract void UnloadChunk(Vector2Int chunkCoord);
    protected abstract IEnumerable<Vector2Int> GetLoadedChunks();
    protected abstract void ClearGeneratedChunks();

    protected virtual IEnumerator ClearGeneratedChunksCoroutine()
    {
        ClearGeneratedChunks();
        yield break;
    }

    protected virtual IEnumerator GenerateChunkCoroutine(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        GenerateChunk(data, chunkCoord);
        yield break;
    }

    protected virtual IEnumerator UnloadChunkCoroutine(Vector2Int chunkCoord)
    {
        UnloadChunk(chunkCoord);
        yield break;
    }

    private IEnumerator StreamChunksCoroutine()
    {
        // get data, do initial fill, set the scene to be ready for reveal
        WorldRuntimeData initialData;
        while (!TryGetWorldData(out initialData) || !CanStartStreaming())
            yield return null;

        yield return StreamInitialChunksCoroutine(initialData);
        IsReadyForSceneReveal = true;

        float initialOffset = _initialRefreshOffsetSeconds;
        if (initialOffset > 0f)
            yield return new WaitForSecondsRealtime(initialOffset);

        // generate missing desired chunks, unload far chunks
        while (enabled && gameObject.activeInHierarchy)
        {
            if (!TryGetWorldData(out var data))
            {
                yield return null;
                continue;
            }

            var focusTile = WorldChunkUtility.ResolveFocusTile(data, _worldGenerator.GroundTilemap, _playerTransform);
            var focusChunk = WorldChunkUtility.GetChunkCoordFromTile(focusTile, _chunkSize);
            var desiredChunks = WorldChunkUtility.BuildChunkSetInRadius(focusChunk, _generationRadiusInChunks);

            int chunkBudget = 0;
            for (int i = 0; i < desiredChunks.Count; i++)
            {
                var chunk = desiredChunks[i];
                if (IsChunkLoaded(chunk))
                    continue;

                yield return GenerateChunkCoroutine(data, chunk);
                chunkBudget++;

                if (chunkBudget >= _chunksGeneratedPerFrame)
                {
                    chunkBudget = 0;
                    yield return null;
                }
            }

            yield return UnloadFarChunksCoroutine(focusChunk);
            yield return new WaitForSecondsRealtime(_refreshIntervalSeconds);
        }
    }

    private IEnumerator StreamInitialChunksCoroutine(WorldRuntimeData data)
    {
        var focusTile = WorldChunkUtility.ResolveFocusTile(data, _worldGenerator.GroundTilemap, _playerTransform);
        var focusChunk = WorldChunkUtility.GetChunkCoordFromTile(focusTile, _chunkSize);
        var initialChunks = WorldChunkUtility.BuildChunkSetInRadius(focusChunk, _initialGenerationRadiusInChunks);

        int chunkBudget = 0;
        int perFrameBudget = _chunksGeneratedPerFrame;

        for (int i = 0; i < initialChunks.Count; i++)
        {
            var chunk = initialChunks[i];
            if (IsChunkLoaded(chunk))
                continue;

            yield return GenerateChunkCoroutine(data, chunk);
            chunkBudget++;

            if (chunkBudget >= perFrameBudget)
            {
                chunkBudget = 0;
                yield return null;
            }
        }
    }

    private IEnumerator UnloadFarChunksCoroutine(Vector2Int focusChunk)
    {
        if (!EnableChunkUnloading)
            yield break;

        int unloadRadius = Mathf.Max(_generationRadiusInChunks, _unloadRadiusInChunks);
        int sqrRadius = unloadRadius * unloadRadius;

        var chunksToUnload = new List<Vector2Int>();
        foreach (var chunkCoord in GetLoadedChunks())
        {
            int dx = chunkCoord.x - focusChunk.x;
            int dy = chunkCoord.y - focusChunk.y;
            if ((dx * dx) + (dy * dy) > sqrRadius)
                chunksToUnload.Add(chunkCoord);
        }

        int unloadBudget = _chunksUnloadedPerFrame;
        int unloadedThisFrame = 0;

        for (int i = 0; i < chunksToUnload.Count; i++)
        {
            yield return UnloadChunkCoroutine(chunksToUnload[i]);
            unloadedThisFrame++;

            if (unloadedThisFrame >= unloadBudget)
            {
                unloadedThisFrame = 0;
                yield return null;
            }
        }
    }

    private bool TryGetWorldData(out WorldRuntimeData data)
    {
        data = null;
        if (_worldGenerator == null || !_worldGenerator.IsReadyForSceneReveal || _worldGenerator.CurrentWorldData == null)
            return false;

        data = _worldGenerator.CurrentWorldData;
        return true;
    }

    private void RunImmediate(IEnumerator routine)
    {
        if (routine == null)
            return;

        while (routine.MoveNext()) { }
    }
}
