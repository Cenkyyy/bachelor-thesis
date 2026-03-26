using System.Collections.Generic;
using System.Text;

public static class ItemTooltipBodyBuilder
{
    public static string BuildBody(List<ItemTooltipLineRuntimeData> lines)
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;

        var builder = new StringBuilder(128);
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            builder.Append(line.Label);
            builder.Append(": ");
            builder.Append(line.Value);

            if (i < lines.Count - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }
}
