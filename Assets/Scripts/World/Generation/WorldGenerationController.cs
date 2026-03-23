using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WorldGenerationController : MonoBehaviour, ISceneTransitionReadinessBlocker
{
    private const int DefaultWorldWidth = 2048;
    private const int DefaultWorldHeight = DefaultWorldWidth;
    private const int DefaultBorderThickness = 32;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _borderTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase _voidTile;
    [SerializeField] private TileBase _grasslandBaseTile;
    [SerializeField] private TileBase _iceTundraBaseTile;
    [SerializeField] private TileBase _desertBaseTile;
    [SerializeField] private TileBase _amethystRiftBaseTile;
    [SerializeField] private TileBase _borderTile;

    [Header("World Settings")]
    [SerializeField] private int _worldWidth = DefaultWorldWidth;
    [SerializeField] private int _worldHeight = DefaultWorldHeight;
    [SerializeField] private int _borderThickness = DefaultBorderThickness;
    [SerializeField] private int _defaultSeed = 12345;
    [SerializeField] private bool _randomizeSeedOnPlay = false; // should be true when creating new game
    [SerializeField, Min(1)] private int _biomeCenterCount = 48;
    [SerializeField, Min(1)] private int _centerSamplingCandidates = 128;
    [SerializeField, Min(0f)] private float _centerJitter = 6f;
    [SerializeField] private BiomeType[] _centerBiomes = { BiomeType.Grassland, BiomeType.IceTundra, BiomeType.Desert, BiomeType.AmethystRift };

    [Header("Radial Biome Distribution")]
    [SerializeField, Range(0f, 1f)] private float _innerRingMaxNormalizedRadius = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _middleRingMaxNormalizedRadius = 0.70f;
    [SerializeField] private RadialBiomeWeights _middleRingWeights = RadialBiomeWeights.Create(25f, 37.5f, 37.5f, 0f);
    [SerializeField] private RadialBiomeWeights _outerRingWeights = RadialBiomeWeights.Create(10f, 17.5f, 17.5f, 55f);

    [Header("Biome Transition Band")]
    [SerializeField, Min(0f)] private float _biomeTransitionBandWidthTiles = 6f;
    [SerializeField, Min(0.001f)] private float _biomeTransitionNoiseScale = 0.6f;
    [SerializeField, Min(0f)] private float _biomeTransitionDisplacementTiles = 4f;

    [Header("Player")]
    [SerializeField] private Transform _playerTransform;

    [Header("Minimap")]
    [SerializeField] private MinimapController _minimap;

    [Header("Performance")]
    [SerializeField, Min(4)] private int _chunkSize = 32;
    [SerializeField, Min(0)] private int _initialRenderRadiusInChunks = 4;
    [SerializeField, Min(0)] private int _streamingRenderRadiusInChunks = 6;
    [SerializeField, Min(0.02f)] private float _streamingRefreshIntervalSeconds = 0.1f;
    [SerializeField, Min(1)] private int _chunksPerFrame = 2;

    public int CurrentSeed => _runtimeState.Seed;
    public WorldRuntimeData CurrentWorldData => _runtimeState.Data;
    public Tilemap GroundTilemap => _runtimeState.GroundTilemap;
    public WorldRuntimeState RuntimeState => _runtimeState;
    public bool IsReadyForSceneReveal { get; private set; }

    [System.Serializable]
    private struct RadialBiomeWeights
    {
        [Min(0f)] public float Grassland;
        [Min(0f)] public float IceTundra;
        [Min(0f)] public float Desert;
        [Min(0f)] public float AmethystRift;

        public static RadialBiomeWeights Create(float grassland, float iceTundra, float desert, float amethystRift)
        {
            return new RadialBiomeWeights
            {
                Grassland = grassland,
                IceTundra = iceTundra,
                Desert = desert,
                AmethystRift = amethystRift
            };
        }
    }

    private int _playableRadius => (_worldWidth - (2 * _borderThickness)) / 2;
    private Coroutine _generationCoroutine;
    private Coroutine _streamingCoroutine;
    private readonly HashSet<Vector2Int> _renderedChunks = new HashSet<Vector2Int>();
    private readonly WorldRuntimeState _runtimeState = new WorldRuntimeState();

    private void Start()
    {
        GenerateAndRender(ResolveSeed());
    }

    public void GenerateAndRender(int seedUsed)
    {
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
        }

        if (_streamingCoroutine != null)
        {
            StopCoroutine(_streamingCoroutine);
            _streamingCoroutine = null;
        }

        _runtimeState.Clear();

        _generationCoroutine = StartCoroutine(GenerateAndRenderCoroutine(seedUsed));
    }

    private void OnDisable()
    {
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
        }

        if (_streamingCoroutine != null)
        {
            StopCoroutine(_streamingCoroutine);
            _streamingCoroutine = null;
        }

        _runtimeState.Clear();
    }

    private IEnumerator GenerateAndRenderCoroutine(int seedUsed)
    {
        IsReadyForSceneReveal = false;

        var worldShape = CreateWorldShape(_worldWidth, _worldHeight, _borderThickness, _playableRadius);

        var settings = new WorldGenerator.Settings
        {
            Width = _worldWidth,
            Height = _worldHeight,
            WorldShape = worldShape,
            Seed = seedUsed,
            TransitionBandWidthTiles = Mathf.Max(0f, _biomeTransitionBandWidthTiles),
            TransitionNoiseScale = Mathf.Max(0.001f, _biomeTransitionNoiseScale),
            TransitionDisplacementTiles = Mathf.Max(0f, _biomeTransitionDisplacementTiles),
            BiomeCenters = CreateBiomeCenters(seedUsed, worldShape)
        };

        var generateTask = Task.Run(() =>
        {
            var generator = new WorldGenerator(settings);
            return generator.Generate();
        });

        while (!generateTask.IsCompleted)
        {
            yield return null;
        }

        if (generateTask.IsFaulted)
        {
            Debug.LogException(generateTask.Exception);
            IsReadyForSceneReveal = true;
            _generationCoroutine = null;
            yield break;
        }

        var data = generateTask.Result;
        _runtimeState.Update(seedUsed, data, _groundTilemap);

        yield return RenderInitialChunksCoroutine(data);
        PositionPlayer(data);
        StartChunkStreaming(data);

        if (_minimap != null)
        {
            _minimap.Initialize(data);
        }
        
        IsReadyForSceneReveal = true;
        _generationCoroutine = null;
    }

    private int ResolveSeed()
    {
        if (_randomizeSeedOnPlay)
        {
            return WorldSeedUtils.CreateRandomSeed();
        }

        return _defaultSeed;
    }

    private List<WorldGenerator.BiomeCenter> CreateBiomeCenters(int seedUsed, IWorldShape worldShape)
    {
        var rng = new System.Random(seedUsed);
        var centers = new List<WorldGenerator.BiomeCenter>(Mathf.Max(1, _biomeCenterCount));
        var centerPositions = new List<Vector2>(Mathf.Max(1, _biomeCenterCount));
        var allowedBiomes = GetAllowedBiomes();

        Vector2 firstCenter = worldShape.SamplePoint(rng);
        firstCenter = JitterInsideWorldShape(rng, firstCenter, worldShape);
        centerPositions.Add(firstCenter);
        centers.Add(new WorldGenerator.BiomeCenter(firstCenter, PickBiomeByDistance(rng, firstCenter, worldShape, allowedBiomes)));

        int targetCount = Mathf.Max(1, _biomeCenterCount);
        int candidateCount = Mathf.Max(4, _centerSamplingCandidates);

        while (centers.Count < targetCount)
        {
            var sampledPosition = SampleD2Center(rng, worldShape, centerPositions, candidateCount);
            sampledPosition = JitterInsideWorldShape(rng, sampledPosition, worldShape);
            centerPositions.Add(sampledPosition);
            centers.Add(new WorldGenerator.BiomeCenter(sampledPosition, PickBiomeByDistance(rng, sampledPosition, worldShape, allowedBiomes)));
        }

        return centers;
    }

    private Vector2 SampleD2Center(System.Random rng, IWorldShape worldShape, List<Vector2> existingCenters, int candidateCount)
    {
        if (existingCenters == null || existingCenters.Count == 0)
            return worldShape.SamplePoint(rng);

        var candidates = new Vector2[candidateCount];
        var distances = new float[candidateCount];
        float totalWeight = 0f;

        // For each center candidate, which is chosen randomly from the world, find the distance to the nearest existing center
        // and store it inside the distances array. The total weight is the sum of all distances, which is used to pick a candidate randomly with a probability proportional to its distance.
        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = worldShape.SamplePoint(rng);
            candidates[i] = candidate;

            float minDistSq = float.MaxValue;
            for (int c = 0; c < existingCenters.Count; c++)
            {
                float distSq = (candidate - existingCenters[c]).sqrMagnitude;
                if (distSq < minDistSq)
                    minDistSq = distSq;
            }

            distances[i] = minDistSq;
            totalWeight += minDistSq;
        }

        if (totalWeight <= Mathf.Epsilon)
            return candidates[rng.Next(0, candidates.Length)];

        float pick = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < candidates.Length; i++)
        {
            cumulative += distances[i];
            if (pick <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Length - 1];
    }

    private Vector2 JitterInsideWorldShape(System.Random rng, Vector2 point, IWorldShape worldShape)
    {
        if (_centerJitter <= 0f)
            return point;

        int maxJitterAttempts = 8;
        for (int attempt = 0; attempt < maxJitterAttempts; attempt++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float distance = (float)rng.NextDouble() * _centerJitter;
            var jitter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            var jittered = point + jitter;
            if (worldShape.IsInsidePlayable(jittered))
                return jittered;
        }

        return point;
    }

    private BiomeType PickBiomeByDistance(System.Random rng, Vector2 position, IWorldShape worldShape, BiomeType[] allowedBiomes)
    {
        var normalizedRadius = worldShape.GetNormalizedDistanceFromCenter(position);

        if (normalizedRadius <= _innerRingMaxNormalizedRadius)
            return BiomeType.Grassland;

        if (normalizedRadius <= _middleRingMaxNormalizedRadius)
            return PickWeightedBiome(rng, _middleRingWeights, allowedBiomes);

        return PickWeightedBiome(rng, _outerRingWeights, allowedBiomes);
    }

    private IWorldShape CreateWorldShape(int worldWidth, int worldHeight, int borderThickness, int playableRadius)
    {
        return new CircularWorldShape(worldWidth, worldHeight, playableRadius, borderThickness);
    }

    private BiomeType PickWeightedBiome(System.Random rng, RadialBiomeWeights weights, BiomeType[] allowedBiomes)
    {
        float grasslandWeight = IsBiomeAllowed(BiomeType.Grassland, allowedBiomes) ? Mathf.Max(0f, weights.Grassland) : 0f;
        float iceTundraWeight = IsBiomeAllowed(BiomeType.IceTundra, allowedBiomes) ? Mathf.Max(0f, weights.IceTundra) : 0f;
        float desertWeight = IsBiomeAllowed(BiomeType.Desert, allowedBiomes) ? Mathf.Max(0f, weights.Desert) : 0f;
        float amethystRiftWeight = IsBiomeAllowed(BiomeType.AmethystRift, allowedBiomes) ? Mathf.Max(0f, weights.AmethystRift) : 0f;

        float total = grasslandWeight + iceTundraWeight + desertWeight + amethystRiftWeight;
        if (total <= Mathf.Epsilon)
            return PickBiome(rng, allowedBiomes);

        float roll = (float)rng.NextDouble() * total;
        if (roll < grasslandWeight)
            return BiomeType.Grassland;

        roll -= grasslandWeight;
        if (roll < iceTundraWeight)
            return BiomeType.IceTundra;

        roll -= iceTundraWeight;
        if (roll < desertWeight)
            return BiomeType.Desert;

        return BiomeType.AmethystRift;
    }

    private bool IsBiomeAllowed(BiomeType biome, BiomeType[] allowedBiomes)
    {
        if (allowedBiomes == null || allowedBiomes.Length == 0)
            return biome != BiomeType.None;

        for (int i = 0; i < allowedBiomes.Length; i++)
        {
            if (allowedBiomes[i] == biome)
                return true;
        }

        return false;
    }

    private BiomeType[] GetAllowedBiomes()
    {
        if (_centerBiomes == null || _centerBiomes.Length == 0)
            return new[] { BiomeType.Grassland, BiomeType.IceTundra, BiomeType.Desert, BiomeType.AmethystRift };

        var result = new List<BiomeType>(_centerBiomes.Length);
        for (int i = 0; i < _centerBiomes.Length; i++)
        {
            if (_centerBiomes[i] == BiomeType.None)
                continue;

            if (!result.Contains(_centerBiomes[i]))
                result.Add(_centerBiomes[i]);
        }

        if (result.Count == 0)
            result.Add(BiomeType.Grassland);

        return result.ToArray();
    }

    private BiomeType PickBiome(System.Random rng, BiomeType[] allowedBiomes)
    {
        if (allowedBiomes == null || allowedBiomes.Length == 0)
            return BiomeType.Grassland;

        int idx = rng.Next(0, allowedBiomes.Length);
        return allowedBiomes[idx];
    }

    private IEnumerator RenderInitialChunksCoroutine(WorldRuntimeData data)
    {
        yield return ClearWorldCoroutine(data);

        var centerChunk = WorldChunkUtility.GetChunkCoordFromTile(data.SpawnTile, _chunkSize);
        int chunkBudget = 0;
        foreach (var chunkCoord in WorldChunkUtility.BuildChunkSetInRadius(centerChunk, _initialRenderRadiusInChunks))
        {
            RenderChunk(data, chunkCoord);
            chunkBudget++;

            if (chunkBudget >= Mathf.Max(1, _chunksPerFrame))
            {
                chunkBudget = 0;
                yield return null;
            }
        }
    }

    private IEnumerator ClearWorldCoroutine(WorldRuntimeData data)
    {
        _renderedChunks.Clear();
        _groundTilemap.ClearAllTiles();
        _borderTilemap.ClearAllTiles();

        yield return null;
    }

    private void StartChunkStreaming(WorldRuntimeData data)
    {
        if (_streamingCoroutine != null)
        {
            StopCoroutine(_streamingCoroutine);
        }
        _streamingCoroutine = StartCoroutine(StreamChunksAroundPlayerCoroutine(data));
    }

    private IEnumerator StreamChunksAroundPlayerCoroutine(WorldRuntimeData data)
    {
        while (CurrentWorldData == data)
        {
            var focusTile = WorldChunkUtility.ResolveFocusTile(data, _groundTilemap, _playerTransform);
            var focusChunk = WorldChunkUtility.GetChunkCoordFromTile(focusTile, _chunkSize);

            int chunkBudget = 0;
            foreach (var chunkCoord in WorldChunkUtility.BuildChunkSetInRadius(focusChunk, _streamingRenderRadiusInChunks))
            {
                if (_renderedChunks.Contains(chunkCoord))
                    continue;

                RenderChunk(data, chunkCoord);
                chunkBudget++;

                if (chunkBudget >= Mathf.Max(1, _chunksPerFrame))
                {
                    chunkBudget = 0;
                    yield return null;
                }
            }
            yield return new WaitForSeconds(Mathf.Max(0.02f, _streamingRefreshIntervalSeconds));
        }
    }

    private void RenderChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        if (_renderedChunks.Contains(chunkCoord))
            return;

        int chunkSize = Mathf.Max(1, _chunkSize);
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return;

        var groundTiles = new TileBase[width * height];
        var borderTiles = new TileBase[width * height];
        var borderTileAsset = GetBorderTileAsset();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dataX = startX + x;
                int dataY = startY + y;
                int index = x + (y * width);
                var tile = data.GetTile(dataX, dataY);

                if (tile.TileType == TileType.Void)
                {
                    borderTiles[index] = borderTileAsset;
                    continue;
                }

                groundTiles[index] = GetTileAsset(tile.TileType);
            }
        }

        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);
        _groundTilemap.SetTilesBlock(chunkBounds, groundTiles);
        _borderTilemap.SetTilesBlock(chunkBounds, borderTiles);
        _renderedChunks.Add(chunkCoord);
    }

    private TileBase GetBorderTileAsset()
    {
        if (_borderTile != null)
            return _borderTile;

        return _voidTile;
    }

    private TileBase GetTileAsset(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Void:
                return _voidTile;
            case TileType.GrasslandBase:
                return _grasslandBaseTile;
            case TileType.IceTundraBase:
                return _iceTundraBaseTile;
            case TileType.DesertBase:
                return _desertBaseTile;
            case TileType.AmethystRift:
                return _amethystRiftBaseTile;
            default:
                return null;
        }
    }

    private void PositionPlayer(WorldRuntimeData data)
    {
        if (_playerTransform == null || _groundTilemap == null)
            return;

        var cellPos = data.DataToCell(data.SpawnTile.x, data.SpawnTile.y);

        var cellWorldPos = _groundTilemap.CellToWorld(cellPos);
        var worldPos = cellWorldPos + new Vector3(0.5f, 0.5f, 0f); // move to center of tile

        _playerTransform.position = worldPos;
    }
}
