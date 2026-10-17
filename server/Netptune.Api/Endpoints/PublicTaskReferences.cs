using Mediator;

using Netptune.Api.Requests;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Api.Endpoints;

public static class PublicTaskReferences
{
    public static async Task<int?> Resolve(IMediator mediator, string systemId, CancellationToken cancellationToken)
    {
        var resolution = await Resolve(mediator, [systemId], cancellationToken);

        return FirstId(resolution);
    }

    public static Task<PublicTaskIdResolution> Resolve(
        IMediator mediator,
        IReadOnlyCollection<string> systemIds,
        CancellationToken cancellationToken)
    {
        var query = new GetTaskIdsBySystemIdQuery(systemIds, IncludeArchived: false);

        return Resolve(mediator, query, cancellationToken);
    }

    public static Task<PublicTaskIdResolution> ResolveIncludingArchived(
        IMediator mediator,
        IReadOnlyCollection<string> systemIds,
        CancellationToken cancellationToken)
    {
        var query = new GetTaskIdsBySystemIdQuery(systemIds, IncludeArchived: true);

        return Resolve(mediator, query, cancellationToken);
    }

    private static async Task<PublicTaskIdResolution> Resolve(
        IMediator mediator,
        GetTaskIdsBySystemIdQuery query,
        CancellationToken cancellationToken)
    {
        var idsBySystemId = await mediator.Send(query, cancellationToken);
        var requested = query.SystemIds.Distinct(StringComparer.Ordinal).ToList();
        var missing = requested.Where(systemId => !idsBySystemId.ContainsKey(systemId)).ToList();
        var ids = requested
            .Where(idsBySystemId.ContainsKey)
            .Select(systemId => idsBySystemId[systemId])
            .Distinct()
            .ToList();

        return new PublicTaskIdResolution { Ids = ids, Missing = missing };
    }

    private static int? FirstId(PublicTaskIdResolution resolution)
    {
        var isFound = resolution.Missing.Count == 0 && resolution.Ids.Count > 0;

        return isFound ? resolution.Ids[0] : null;
    }
}
