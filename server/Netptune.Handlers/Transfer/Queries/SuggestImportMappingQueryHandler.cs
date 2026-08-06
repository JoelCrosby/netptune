using Netptune.Transfer.Repositories;
using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Transfer.Mapping;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record SuggestImportMappingQuery(Guid PublicId) : IRequest<ClientResponse<ImportMappingSuggestion>>;

public sealed class SuggestImportMappingQueryHandler : IRequestHandler<SuggestImportMappingQuery, ClientResponse<ImportMappingSuggestion>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IIdentityService Identity;
    private readonly IImportMappingAdvisor Advisor;

    public SuggestImportMappingQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportMappingAdvisor advisor,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Advisor = advisor;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportMappingSuggestion>> Handle(SuggestImportMappingQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, true, cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportMappingSuggestion>.NotFound;
        }

        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        if (profile is null)
        {
            return ClientResponse<ImportMappingSuggestion>.Failed("Inspect the file before asking for a mapping.");
        }

        var vocabulary = await ImportVocabularyReader.Read(UnitOfWork, workspaceId, workspaceKey, cancellationToken);
        var suggestion = Advisor.Suggest(session.TargetRecordType, profile, vocabulary);

        return ClientResponse<ImportMappingSuggestion>.Success(suggestion);
    }
}
