using System.Text.Json;
using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record SetImportMappingCommand(Guid PublicId, ImportMappingModel Mapping) : IRequest<ClientResponse<ImportSessionViewModel>>;

public sealed class SetImportMappingCommandHandler : IRequestHandler<SetImportMappingCommand, ClientResponse<ImportSessionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IImportSessionRepository ImportSessions;

    public SetImportMappingCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSessionViewModel>> Handle(SetImportMappingCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        var isMappable = ImportStages.CanMap(session.Stage);

        if (!isMappable)
        {
            return ClientResponse<ImportSessionViewModel>.Failed(
                $"An import that is {session.Stage} cannot be mapped.");
        }

        var columnCount = ResolveColumnCount(session);
        var validation = ImportMappingValidator.Validate(request.Mapping, columnCount);

        if (!validation.IsValid)
        {
            return ClientResponse<ImportSessionViewModel>.Failed(string.Join(" ", validation.Errors));
        }

        session.Mapping = JsonSerializer.SerializeToDocument(request.Mapping, JsonOptions.Default);
        session.Stage = ImportStage.Mapped;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = await ImportSessions.GetViewModel(session.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        return ClientResponse<ImportSessionViewModel>.Success(viewModel);
    }

    private static int ResolveColumnCount(ImportSession session)
    {
        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        return profile?.Columns.Count ?? 0;
    }
}
