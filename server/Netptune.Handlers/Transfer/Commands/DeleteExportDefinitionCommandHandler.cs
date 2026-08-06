using Netptune.Transfer.Repositories;
using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record DeleteExportDefinitionCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteExportDefinitionCommandHandler : IRequestHandler<DeleteExportDefinitionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportDefinitionRepository ExportDefinitions;
    private readonly IIdentityService Identity;

    public DeleteExportDefinitionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity,
        IExportDefinitionRepository exportDefinitions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ExportDefinitions = exportDefinitions;
    }

    public async ValueTask<ClientResponse> Handle(DeleteExportDefinitionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var definition = await ExportDefinitions.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (definition is null)
        {
            return ClientResponse.NotFound;
        }

        definition.Delete(Identity.GetCurrentUserId());

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
