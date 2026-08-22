using Netptune.Core.Requests;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks;

// TaskFilter is declared in Netptune.Core, which does not reference this project, so a query travels
// on a subclass that TaskRepository resolves by type.
public sealed class TaskQueryFilter : TaskFilter
{
    public QueryGroup? Query { get; init; }
}
