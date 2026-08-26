using Netptune.Core.Services.Ai;

namespace Netptune.Core.Requests.Ai;

public sealed record UpdateAiProposedChangeRequest
{
    public List<AiChangeFieldEdit> Fields { get; init; } = [];
}
