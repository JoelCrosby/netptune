using System.Text.Json;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiChangeSetQuery(Guid ChangeSetId) : IRequest<ClientResponse<AiChangeSetViewModel>>;

public sealed class GetAiChangeSetQueryHandler
    : IRequestHandler<GetAiChangeSetQuery, ClientResponse<AiChangeSetViewModel>>
{
    private static readonly JsonSerializerOptions FieldSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiChangeSetQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiChangeSetViewModel>> Handle(
        GetAiChangeSetQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(
            query.ChangeSetId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return ClientResponse<AiChangeSetViewModel>.NotFound;
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var model = new AiChangeSetViewModel
        {
            Id = changeSet.Id,
            ConversationId = changeSet.ConversationId,
            Status = changeSet.Status,
            AppliedAt = changeSet.AppliedAt,
            Changes = changes.Select(ToViewModel).ToList(),
        };

        return ClientResponse<AiChangeSetViewModel>.Success(model);
    }

    private static AiProposedChangeViewModel ToViewModel(AiProposedChange change)
    {
        return new AiProposedChangeViewModel
        {
            Id = change.Id,
            Sequence = change.Sequence,
            ToolName = change.ToolName,
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            RefKey = change.RefKey,
            Summary = change.Summary,
            Fields = ParseFields(change.Fields),
            ValidationStatus = change.ValidationStatus,
            ValidationMessage = change.ValidationMessage,
            ApplyStatus = change.ApplyStatus,
            ApplyError = change.ApplyError,
        };
    }

    private static List<AiChangeFieldViewModel> ParseFields(JsonDocument fields)
    {
        var parsed = fields.Deserialize<List<AiChangeFieldViewModel>>(FieldSerializerOptions);

        return parsed ?? [];
    }
}
