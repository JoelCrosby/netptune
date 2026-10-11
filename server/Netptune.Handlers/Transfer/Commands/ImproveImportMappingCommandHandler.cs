using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Ai;
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
    private readonly IAiSpendService Spend;

    public ImproveImportMappingCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportMappingAdvisor heuristics,
        IAiImportMappingAdvisor assistant,
        IAiCredentialProtector protector,
        IAiSpendService spend,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Heuristics = heuristics;
        Assistant = assistant;
        Protector = protector;
        Spend = spend;
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

        var spend = await Spend.GetStatus(workspace, cancellationToken);

        if (spend.IsOverCap)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed(AiSpendMessages.CapReached);
        }

        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        if (profile is null)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed("Inspect the file before asking the assistant.");
        }

        var userId = Identity.GetCurrentUserId();
        var credential = await AiHandlerCredentials.Resolve(UnitOfWork, userId, workspaceId, cancellationToken);

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
        var origin = new AiUsageOrigin(workspaceId, userId, AiUsagePurposes.ImportMapping);

        // Booked whatever came back: the tokens were spent either way.
        await AiUsageRecorder.Record(UnitOfWork, origin, credential, suggestion.Model, suggestion.Usage, cancellationToken);
        await AiHandlerCredentials.MarkUsed(UnitOfWork, credential, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        if (suggestion.Mapping.Bindings.Count == 0)
        {
            return ClientResponse<ImproveImportMappingResult>.Failed(
                "The assistant did not propose a usable mapping. The suggested mapping is unchanged.");
        }

        return ClientResponse<ImproveImportMappingResult>.Success(new ImproveImportMappingResult
        {
            Mapping = suggestion.Mapping,
            DiscardedBindings = suggestion.DiscardedBindings,
            DiscardReasons = suggestion.DiscardReasons,
            Notes = suggestion.Notes,
            UsedDataSampling = workspace.AllowAssistantDataSampling,
        });
    }
}
