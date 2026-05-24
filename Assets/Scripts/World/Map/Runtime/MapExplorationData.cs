/// <summary>
/// Tracks which generated world tiles have been revealed on the map.
/// </summary>
public sealed class MapExplorationData
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool[] _explored;

    /// <summary>
    /// Creates an unexplored map grid for the given world dimensions.
    /// </summary>
    public MapExplorationData(int width, int height)
    {
        _width = width;
        _height = height;
        _explored = new bool[width * height];
    }

    /// <summary>
    /// Returns whether the tile coordinate is inside the exploration grid.
    /// </summary>
    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }

    /// <summary>
    /// Returns whether the tile coordinate has already been explored.
    /// </summary>
    public bool IsExplored(int x, int y)
    {
        if (!IsInside(x, y))
            return false;

        return _explored[y * _width + x];
    }

    /// <summary>
    /// Marks a tile as explored and reports whether it changed state.
    /// </summary>
    public bool TrySetExplored(int x, int y)
    {
        if (!IsInside(x, y))
            return false;

        int idx = y * _width + x;
        if (_explored[idx])
            return false;

        _explored[idx] = true;
        return true;
    }
}
