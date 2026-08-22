using Netptune.Query.Schema;

namespace Netptune.Query.Tasks;

internal sealed class TaskRelationReferenceParser : IQueryValueParser
{
    public bool TryParse(string value, out object? parsed)
    {
        parsed = TaskRelationReference.Parse(value);

        return parsed is not null;
    }
}
