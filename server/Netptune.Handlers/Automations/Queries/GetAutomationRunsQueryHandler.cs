using Mediator;

using Netptune.Core.Models.Automations;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRunsQuery(int RuleId, AutomationRunFilter Filter)
    : IRequest<ClientResponse<PagedResponse<AutomationRunViewModel>>>;

public sealed class GetAutomationRunsQueryHandler
    : IRequestHandler<GetAutomationRunsQuery, ClientResponse<PagedResponse<AutomationRunViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationRunsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<AutomationRunViewModel>>> Handle(
        GetAutomationRunsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.RuleId, workspaceId, true, cancellationToken);

        if (rule is null) return ClientResponse<PagedResponse<AutomationRunViewModel>>.NotFound;

        var runs = await UnitOfWork.Automations.GetRunsPaged(
            request.RuleId,
            workspaceId,
            request.Filter,
            cancellationToken);

        return ClientResponse<PagedResponse<AutomationRunViewModel>>.Success(runs);
    }
}
