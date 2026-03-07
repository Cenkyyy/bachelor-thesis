using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldGeneratorBehaviour : MonoBehaviour
{
    private const int DefaultWorldWidth = 1024;
    private const int DefaultWorldHeight = 1024;
    private const int DefaultPlayableRadius = 480;
    private const int DefaultBorderThickness = 32;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _walkableDecorationTilemap;
    [SerializeField] private Tilemap _nonWalkableDecorationTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase _voidTile;
    [SerializeField] private TileBase _grasslandBaseTile;
    [SerializeField] private TileBase _iceTundraBaseTile;
    [SerializeField] private TileBase _desertBaseTile;
    [SerializeField] private TileBase _amethystRiftBaseTile;

    [Header("World Settings")]
    [SerializeField] private int _worldWidth = DefaultWorldWidth;
    [SerializeField] private int _worldHeight = DefaultWorldHeight;
    [SerializeField] private int _playableRadius = DefaultPlayableRadius;
    [SerializeField] private int _borderThickness = DefaultBorderThickness;
    [SerializeField] private int _defaultSeed = 12345;
    [SerializeField] private bool _randomizeSeedOnPlay = false; // should be true when creating new game
    [SerializeField, Min(1)] private int _biomeCenterCount = 32;
    [SerializeField, Min(1)] private int _centerSamplingCandidates = 128;
    [SerializeField, Min(0f)] private float _centerJitter = 3f;
    [SerializeField] private BiomeType[] _centerBiomes = { BiomeType.Grassland, BiomeType.IceTundra, BiomeType.Desert, BiomeType.AmethystRift };

    [Header("Player")]
    [SerializeField] private Transform _playerTransform;

    [Header("Minimap")]
    [SerializeField] private MinimapController _minimap;

    public int CurrentSeed { get; private set; }
    public WorldData CurrentWorldData { get; private set; }
    public Tilemap GroundTilemap => _groundTilemap;

    private void Start()
    {
        CurrentSeed = ResolveSeed();
        GenerateAndRender(CurrentSeed);
    }

    public void GenerateAndRender(int seedUsed)
    {
        var settings = new WorldGenerator.Settings
        {
            Width = Mathf.Max(1, _worldWidth),
            Height = Mathf.Max(1, _worldHeight),
            PlayableRadius = Mathf.Max(1, _playableRadius),
            BorderThickness = Mathf.Max(0, _borderThickness),
            Seed = seedUsed,
            BiomeCenters = CreateBiomeCenters(seedUsed)
        };

        var generator = new WorldGenerator(settings);
        var data = generator.Generate();
        CurrentWorldData = data;

        RenderWorld(data);
        PositionPlayer(data);
        _minimap.Initialize(data);
    }

    private int ResolveSeed()
    {
        if (_randomizeSeedOnPlay)
        {
            return WorldSeedUtils.CreateRandomSeed();
        }

        return _defaultSeed;
    }

    private List<WorldGenerator.BiomeCenter> CreateBiomeCenters(int seedUsed)
    {
        var rng = new System.Random(seedUsed);
        var centers = new List<WorldGenerator.BiomeCenter>(Mathf.Max(1, _biomeCenterCount));
        var centerPositions = new List<Vector2>(Mathf.Max(1, _biomeCenterCount));
        var allowedBiomes = GetAllowedBiomes();
        var worldCenter = new Vector2(_worldWidth * 0.5f, _worldHeight * 0.5f);

        Vector2 firstCenter = RandomPointInCircle(rng, worldCenter, _playableRadius);
        firstCenter = JitterInsideCircle(rng, firstCenter, worldCenter, _playableRadius);
        centerPositions.Add(firstCenter);
        centers.Add(new WorldGenerator.BiomeCenter(firstCenter, PickBiome(rng, allowedBiomes)));

        int targetCount = Mathf.Max(1, _biomeCenterCount);
        int candidateCount = Mathf.Max(4, _centerSamplingCandidates);

        while (centers.Count < targetCount)
        {
            var sampledPosition = SampleD2Center(rng, worldCenter, _playableRadius, centerPositions, candidateCount);
            sampledPosition = JitterInsideCircle(rng, sampledPosition, worldCenter, _playableRadius);
            centerPositions.Add(sampledPosition);
            centers.Add(new WorldGenerator.BiomeCenter(sampledPosition, PickBiome(rng, allowedBiomes)));
        }

        return centers;
    }

    private Vector2 SampleD2Center(System.Random rng, Vector2 worldCenter, float playableRadius, List<Vector2> existingCenters, int candidateCount)
    {
        if (existingCenters == null || existingCenters.Count == 0)
            return RandomPointInCircle(rng, worldCenter, playableRadius);

        var candidates = new Vector2[candidateCount];
        var distances = new float[candidateCount];
        float totalWeight = 0f;

        // For each center candidate, which is chosen randomly from the world, find the distance to the nearest existing center
        // and store it inside the distances array. The total weight is the sum of all distances, which is used to pick a candidate randomly with a probability proportional to its distance.
        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = RandomPointInCircle(rng, worldCenter, playableRadius);
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

    private Vector2 JitterInsideCircle(System.Random rng, Vector2 point, Vector2 circleCenter, float radius)
    {
        if (_centerJitter <= 0f)
            return point;

        float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
        float distance = (float)rng.NextDouble() * _centerJitter;
        var jitter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        var jittered = point + jitter;

        var offset = jittered - circleCenter;
        float offsetMagnitude = offset.magnitude;
        if (offsetMagnitude <= radius)
            return jittered;

        if (offsetMagnitude <= Mathf.Epsilon)
            return circleCenter;

        return circleCenter + offset / offsetMagnitude * radius;
    }

    private Vector2 RandomPointInCircle(System.Random rng, Vector2 center, float radius)
    {
        float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
        float distance = Mathf.Sqrt((float)rng.NextDouble()) * radius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
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

    private void RenderWorld(WorldData data)
    {
        _groundTilemap.ClearAllTiles();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var tile = data.Tiles[x, y];

                var tileAsset = GetTileAsset(tile.TileType);
                if (tileAsset == null)
                    continue;

                var tilePos = data.DataToCell(x, y);
                _groundTilemap.SetTile(tilePos, tileAsset);
            }
        }
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

    private void PositionPlayer(WorldData data)
    {
        if (_playerTransform == null || _groundTilemap == null)
            return;

        var cellPos = data.DataToCell(data.SpawnTile.x, data.SpawnTile.y);

        var cellWorldPos = _groundTilemap.CellToWorld(cellPos);
        var worldPos = cellWorldPos + new Vector3(0.5f, 0.5f, 0f);

        _playerTransform.position = worldPos;
    }
}
