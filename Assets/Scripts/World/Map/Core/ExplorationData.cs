public sealed class ExplorationData
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool[] _explored;

    public int Width => _width;
    public int Height => _height;

    public ExplorationData(int width, int height)
    {
        _width = width;
        _height = height;
        _explored = new bool[width * height];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }

    public bool IsExplored(int x, int y)
    {
        if (!IsInside(x, y))
            return false;

        return _explored[y * _width + x];
    }

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
