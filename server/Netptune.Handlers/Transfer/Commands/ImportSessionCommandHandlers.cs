using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Encoding;
using Netptune.Transfer.Messages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Import;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record ImportUpload
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}

public sealed record ImportDestination
{
    public string RecordType { get; init; } = EntityRefTypes.Task;

    public string? ProjectKey { get; init; }

    public string? BoardIdentifier { get; init; }
}

public sealed record CreateImportSessionCommand(ImportUpload Upload, ImportDestination Destination)
    : IRequest<ClientResponse<ImportSessionViewModel>>;

public sealed class CreateImportSessionCommandHandler : IRequestHandler<CreateImportSessionCommand, ClientResponse<ImportSessionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IIdentityService Identity;
    private readonly IImportSourceStore Store;
    private readonly TransferOptions Options;

    public CreateImportSessionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportSourceStore store,
        IOptions<TransferOptions> options,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Store = store;
        Options = options.Value;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSessionViewModel>> Handle(CreateImportSessionCommand request, CancellationToken cancellationToken)
    {
        var upload = request.Upload;

        if (upload.Length <= 0)
        {
            return ClientResponse<ImportSessionViewModel>.Failed("The file is empty.");
        }

        if (upload.Length > Options.UploadSizeBytes)
        {
            return ClientResponse<ImportSessionViewModel>.Failed($"The file exceeds the {Options.UploadSizeBytes} byte limit.");
        }

        var sourceKind = ResolveSourceKind(upload.FileName);

        if (sourceKind is null)
        {
            return ClientResponse<ImportSessionViewModel>.Failed($"'{upload.FileName}' is not a supported import file.");
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var publicId = Guid.NewGuid();
        var storageKey = await Store.Save(workspaceKey, publicId, upload.FileName, upload.Content, upload.ContentType, cancellationToken);
        var session = await ImportSessions.AddAsync(new ImportSession
        {
            WorkspaceId = workspaceId,
            PublicId = publicId,
            Stage = ImportStage.Uploaded,
            SourceKind = sourceKind.Value,
            OriginalName = upload.FileName,
            StorageKey = storageKey,
            SizeBytes = upload.Length,
            TargetRecordType = request.Destination.RecordType,
            TargetProjectKey = request.Destination.ProjectKey,
            TargetBoardIdentifier = request.Destination.BoardIdentifier,
            CreatedBy = userId,
            CreatedByUserId = userId,
            OwnerId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(Options.SessionRetentionDays),
        }, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = await ImportSessions.GetViewModel(session.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ImportSessionViewModel>.Failed("The import session could not be read back.");
        }

        return ClientResponse<ImportSessionViewModel>.Success(viewModel);
    }

    private static ImportSourceKind? ResolveSourceKind(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" or ".txt" => ImportSourceKind.Csv,
            ".tsv" => ImportSourceKind.Tsv,
            ".xlsx" or ".xlsm" => ImportSourceKind.Xlsx,
            ".json" => ImportSourceKind.Json,
            ".ndjson" or ".jsonl" => ImportSourceKind.Ndjson,
            _ => null,
        };
    }
}

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

public sealed record CommitImportSessionCommand(Guid PublicId, bool SkipFailingRows) : IRequest<ClientResponse<ImportSessionViewModel>>;

public sealed class CommitImportSessionCommandHandler : IRequestHandler<CommitImportSessionCommand, ClientResponse<ImportSessionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;
    private readonly IImportSessionRepository ImportSessions;

    public CommitImportSessionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventPublisher eventPublisher,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventPublisher = eventPublisher;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSessionViewModel>> Handle(CommitImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        if (session.Mapping is null)
        {
            return ClientResponse<ImportSessionViewModel>.Failed("Map the file before committing it.");
        }

        var isCommittable = session.Stage is ImportStage.Mapped or ImportStage.Previewed or ImportStage.Failed;

        if (!isCommittable)
        {
            return ClientResponse<ImportSessionViewModel>.Failed($"An import that is {session.Stage} cannot be committed.");
        }

        session.Stage = ImportStage.Committing;
        session.ProgressPercent = 0;
        session.ProgressMessage = "Queued";
        session.Error = null;

        await UnitOfWork.CompleteAsync(cancellationToken);

        await EventPublisher.Dispatch(new ImportCommitRequestedMessage
        {
            WorkspaceId = workspaceId,
            ImportSessionId = session.Id,
            UserId = Identity.GetCurrentUserId(),
            SkipFailingRows = request.SkipFailingRows,
        });

        var viewModel = await ImportSessions.GetViewModel(session.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        return ClientResponse<ImportSessionViewModel>.Success(viewModel);
    }
}

public sealed record UndoImportSessionCommand(Guid PublicId) : IRequest<ClientResponse<ImportUndoResult>>;

public sealed class UndoImportSessionCommandHandler : IRequestHandler<UndoImportSessionCommand, ClientResponse<ImportUndoResult>>
{
    private readonly IIdentityService Identity;
    private readonly IImportUndoService Undo;
    private readonly IImportSessionRepository ImportSessions;

    public UndoImportSessionCommandHandler(IIdentityService identity, IImportUndoService undo, IImportSessionRepository importSessions)
    {
        Identity = identity;
        Undo = undo;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportUndoResult>> Handle(UndoImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportUndoResult>.NotFound;
        }

        if (session.Stage != ImportStage.Committed)
        {
            return ClientResponse<ImportUndoResult>.Failed($"An import that is {session.Stage} cannot be undone.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            return ClientResponse<ImportUndoResult>.Failed("This import is too old to undo.");
        }

        var result = await Undo.Undo(session, Identity.GetCurrentUserId(), cancellationToken);

        return ClientResponse<ImportUndoResult>.Success(result);
    }
}
