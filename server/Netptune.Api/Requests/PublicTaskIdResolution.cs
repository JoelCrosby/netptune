namespace Netptune.Api.Requests;

public sealed record PublicTaskIdResolution
{
    public List<int> Ids { get; init; } = [];

    public List<string> Missing { get; init; } = [];

    public string MissingMessage => $"These tasks were not found in the workspace: {string.Join(", ", Missing)}.";
}
