using Mediator;

using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

internal static class AiSprintLookup
{
    public const string DateFormat = "yyyy-MM-dd";

    public static async Task<SprintDetailViewModel?> Find(
        IMediator mediator,
        int sprintId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSprintQuery(sprintId), cancellationToken);

        return response.IsSuccess ? response.Payload : null;
    }
}
