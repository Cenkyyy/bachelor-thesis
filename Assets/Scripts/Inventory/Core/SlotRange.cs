public readonly struct SlotRange
{
    public int StartInclusive { get; }
    public int EndExclusive { get; }

    public SlotRange(int startInclusive, int endExclusive)
    {
        if (startInclusive < 0)
            throw new System.ArgumentOutOfRangeException(nameof(startInclusive), "Start index must be non-negative.");
        
        if (endExclusive <= startInclusive)
            throw new System.ArgumentOutOfRangeException(nameof(endExclusive), "End index must be greater than start index.");
        
        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
    }
}
