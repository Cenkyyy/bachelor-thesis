using System.Collections.Generic;

public sealed class DecorationModificationState
{
    private readonly HashSet<string> _removedDecorationIds = new HashSet<string>();

    public void MarkRemoved(string decorationInstanceId)
    {
        if (string.IsNullOrEmpty(decorationInstanceId))
            return;

        _removedDecorationIds.Add(decorationInstanceId);
    }

    public bool IsRemoved(string decorationInstanceId)
    {
        if (string.IsNullOrEmpty(decorationInstanceId))
            return false;

        return _removedDecorationIds.Contains(decorationInstanceId);
    }
}
