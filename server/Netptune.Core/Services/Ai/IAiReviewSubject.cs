using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services.Ai;

public interface IAiReviewSubject
{
    EntityType Type { get; }

    IReadOnlyList<string> RunPermissions { get; }

    IReadOnlyList<string> DecidePermissions { get; }

    IReadOnlySet<AiReviewFindingCategory> Categories { get; }

    string MissingMessage { get; }

    Task<AiReviewContext?> Load(AiReviewLoadRequest request, CancellationToken cancellationToken);

    Task<string?> GetFingerprint(int workspaceId, int subjectId, CancellationToken cancellationToken);

    Task<string?> ReadField(int workspaceId, int targetId, string field, CancellationToken cancellationToken);

    Task<ClientResponse> WriteField(int targetId, string field, string value, CancellationToken cancellationToken);

    IReadOnlyList<string> PermissionsFor(AiReviewApplyRequest request);

    Task<AiReviewApplication> ApplySuggestion(AiReviewApplyRequest request, CancellationToken cancellationToken);
}
