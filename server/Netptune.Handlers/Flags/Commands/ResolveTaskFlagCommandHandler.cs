using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Flags.Commands;

public sealed record ResolveTaskFlagCommand(int TaskId, int FlagId, ResolveTaskFlagRequest Request)
    : IRequest<ClientResponse>;

public sealed class ResolveTaskFlagCommandHandler : IRequestHandler<ResolveTaskFlagCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public ResolveTaskFlagCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse> Handle(ResolveTaskFlagCommand request, CancellationToken cancellationToken)
    {
        var hasValidResolution = Enum.IsDefined(request.Request.Resolution);

        if (!hasValidResolution)
        {
            return ClientResponse.Failed("The flag resolution is not supported.");
        }

        var workspaceId = await Identity.GetWorkspaceId();

        var flag = await UnitOfWork.Flags.GetTaskFlagForUpdate(
            request.FlagId,
            request.TaskId,
            workspaceId,
            cancellationToken);

        if (flag is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var resolvedAt = DateTime.UtcNow;

        flag.Resolution = request.Request.Resolution;
        flag.ResolvedAt = resolvedAt;
        flag.ResolvedByUserId = userId;
        flag.IsDeleted = true;
        flag.DeletedByUserId = userId;
        flag.UpdatedAt = resolvedAt;

        var flagEvent = new EventWriteRequest<FlagResolutionPayload>
        {
            WorkspaceId = workspaceId,
            EventKey = EventKeys.FlagResolutionRecorded,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = request.TaskId.ToString(),
            Payload = new FlagResolutionPayload
            {
                FlagId = flag.Id,
                FlagName = flag.Name,
                Resolution = request.Request.Resolution.ToString(),
                AutomationRuleId = flag.AutomationRuleId,
            },
        };

        await EventRecords.Append(flagEvent, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
