using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Localization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;
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
            return ClientResponse<ImportSessionViewModel>.Failed(AppText.ErrorFileEmpty);
        }

        if (upload.Length > Options.UploadSizeBytes)
        {
            var error = Localized.Format(AppText.ErrorFileExceedsByteLimit, Options.UploadSizeBytes);

            return ClientResponse<ImportSessionViewModel>.Failed(error);
        }

        var sourceKind = ResolveSourceKind(upload.FileName);

        if (sourceKind is null)
        {
            return ClientResponse<ImportSessionViewModel>.Failed(
                $"'{upload.FileName}' is not a supported import file.");
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
            return ClientResponse<ImportSessionViewModel>.Failed(AppText.ErrorImportSessionCouldNotReadBack);
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
