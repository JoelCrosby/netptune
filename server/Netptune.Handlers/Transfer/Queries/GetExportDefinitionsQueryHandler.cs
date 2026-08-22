using Mediator;

using Netptune.Core.Services;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetExportDefinitionsQuery : IRequest<List<ExportDefinitionViewModel>>;

public sealed class GetExportDefinitionsQueryHandler : IRequestHandler<GetExportDefinitionsQuery, List<ExportDefinitionViewModel>>
{
    private readonly IExportDefinitionRepository ExportDefinitions;
    private readonly IIdentityService Identity;

    public GetExportDefinitionsQueryHandler(IIdentityService identity, IExportDefinitionRepository exportDefinitions)
    {
        Identity = identity;
        ExportDefinitions = exportDefinitions;
    }

    public async ValueTask<List<ExportDefinitionViewModel>> Handle(GetExportDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var definitions = await ExportDefinitions.GetVisibleInWorkspace(workspaceId, userId, cancellationToken);

        return definitions.Select(ExportDefinitionMapper.ToViewModel).ToList();
    }
}
