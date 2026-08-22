using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record ImproveImportMappingResult
{
    public required ImportMappingModel Mapping { get; init; }

    public int DiscardedBindings { get; init; }

    public IReadOnlyList<string> DiscardReasons { get; init; } = [];

    public string? Notes { get; init; }

    public bool UsedDataSampling { get; init; }
}

public sealed record ImproveImportMappingCommand(Guid PublicId) : IRequest<ClientResponse<ImproveImportMappingResult>>;

public sealed class ImproveImportMappingCommandHandler
    : IRequestHandler<ImproveImportMappingCommand, ClientResponse<ImproveImportMappingResult>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IIdentityService Identity;
    private readonly IImportMappingAdvisor Heuristics;
    private readonly IAiImportMappingAdvisor Assistant;
    private readonly IAiCredentialProtector Protector;

    public ImproveImportMappingCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportMappingAdvisor heuristics,
        IAiImportMappingAdvisor assistant,
        IAiCredentialProtector protector,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Heuristics = heuristics;
        Assistant = assistant;
        Protector = protector;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImproveImportMappingResult>> Handle(
        ImproveImportMappingCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, true, cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImproveImportMappingResult>.NotFound;
        }

        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);

        if (workspace is null || !workspace.AssistantEnabled)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed("The assistant is turned off for this workspace.");
        }

        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        if (profile is null)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed("Inspect the file before asking the assistant.");
        }

        var credential = await ResolveCredential(workspaceId, cancellationToken);

        if (credential is null)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed("No assistant key is set up for this workspace.");
        }

        var vocabulary = await ImportVocabularyReader.Read(UnitOfWork, workspaceId, workspaceKey, cancellationToken);
        var heuristic = Heuristics.Suggest(session.TargetRecordType, profile, vocabulary);
        var advisorRequest = new AiImportMappingRequest
        {
            Provider = credential.Provider,
            ApiKey = Protector.Unprotect(credential.Secret),
            RecordType = session.TargetRecordType,
            Profile = profile,
            HeuristicMapping = heuristic.Mapping,
            Vocabulary = vocabulary,
            AllowDataSampling = workspace.AllowAssistantDataSampling,
        };
        var suggestion = await Assistant.Suggest(advisorRequest, cancellationToken);

        if (suggestion.Mapping.Bindings.Count == 0)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed(
                "The assistant did not propose a usable mapping. The suggested mapping is unchanged.");
        }

        await MarkCredentialUsed(credential, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ImproveImportMappingResult>.Success(new ImproveImportMappingResult
        {
            Mapping = suggestion.Mapping,
            DiscardedBindings = suggestion.DiscardedBindings,
            DiscardReasons = suggestion.DiscardReasons,
            Notes = suggestion.Notes,
            UsedDataSampling = workspace.AllowAssistantDataSampling,
        });
    }

    private async Task<AiResolvedCredential?> ResolveCredential(int workspaceId, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var userCredentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(workspaceId, cancellationToken);

        return AiCredentialResolution.Resolve(userCredentials, workspaceCredentials).FirstOrDefault();
    }

    private async Task MarkCredentialUsed(AiResolvedCredential credential, CancellationToken cancellationToken)
    {
        if (credential.Source == AiCredentialSource.Workspace)
        {
            var workspaceCredential = await UnitOfWork.WorkspaceAiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

            if (workspaceCredential is not null)
            {
                workspaceCredential.LastUsedAt = DateTime.UtcNow;
            }

            return;
        }

        var userCredential = await UnitOfWork.AiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

        if (userCredential is not null)
        {
            userCredential.LastUsedAt = DateTime.UtcNow;
        }
    }
}
