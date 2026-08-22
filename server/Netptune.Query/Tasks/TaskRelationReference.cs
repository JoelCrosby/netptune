using System.Globalization;

namespace Netptune.Query.Tasks;

public sealed record TaskRelationReference(int RelationTypeId, TaskRelationDirection Direction)
{
    public static TaskRelationReference? Parse(string value)
    {
        var parts = value.Trim().Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length is 0 or > 2)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var relationTypeId))
        {
            return null;
        }

        var hasNoDirection = parts.Length == 1;

        if (hasNoDirection)
        {
            return new TaskRelationReference(relationTypeId, TaskRelationDirection.Any);
        }

        var direction = parts[1].ToLowerInvariant() switch
        {
            "source" => TaskRelationDirection.Source,
            "target" => TaskRelationDirection.Target,
            "any" => TaskRelationDirection.Any,
            _ => (TaskRelationDirection?)null,
        };

        if (direction is null)
        {
            return null;
        }

        return new TaskRelationReference(relationTypeId, direction.Value);
    }
}
