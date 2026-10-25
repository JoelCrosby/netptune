namespace Netptune.Core.Requests.Ai;

public sealed record UpdateAiCredentialModelsRequest
{
    public string? Model { get; init; }

    public string? ReviewModel { get; init; }
}
