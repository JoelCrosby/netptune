using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record InspectImportSessionRequest
{
    public string? Delimiter { get; init; }

    public bool? HasHeaderRow { get; init; }

    public string? SelectedSheet { get; init; }
}

public sealed record InspectImportSessionCommand(Guid PublicId, InspectImportSessionRequest? Request)
    : IRequest<ClientResponse<ImportSourceProfile>>;

public sealed class InspectImportSessionCommandHandler : IRequestHandler<InspectImportSessionCommand, ClientResponse<ImportSourceProfile>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IImportSourceStore Store;
    private readonly IEnumerable<IImportSourceReader> Readers;
    private readonly IImportSessionRepository ImportSessions;

    public InspectImportSessionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportSourceStore store,
        IEnumerable<IImportSourceReader> readers,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Store = store;
        Readers = readers;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSourceProfile>> Handle(InspectImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportSourceProfile>.NotFound;
        }

        var isInspectable = ImportStages.CanInspect(session.Stage);

        if (!isInspectable)
        {
            return ClientResponse<ImportSourceProfile>.Failed(
                $"An import that is {session.Stage} cannot be inspected.");
        }

        var reader = Readers.FirstOrDefault(candidate => candidate.CanRead(session.OriginalName));

        if (reader is null)
        {
            return ClientResponse<ImportSourceProfile>.Failed($"'{session.OriginalName}' cannot be read yet.");
        }

        await using var source = await Store.Open(session, cancellationToken);

        var options = new ImportReadOptions
        {
            Delimiter = ParseDelimiter(request.Request?.Delimiter),
            HasHeaderRow = request.Request?.HasHeaderRow ?? true,
            SelectedSheet = request.Request?.SelectedSheet,
        };
        var profile = await reader.Profile(source, options, cancellationToken);

        session.SourceProfile = JsonSerializer.SerializeToDocument(profile, JsonOptions.Default);
        session.Stage = ImportStage.Inspected;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ImportSourceProfile>.Success(profile);
    }

    private static char? ParseDelimiter(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value switch
        {
            "\\t" or "tab" => '\t',
            _ => value[0],
        };
    }
}
