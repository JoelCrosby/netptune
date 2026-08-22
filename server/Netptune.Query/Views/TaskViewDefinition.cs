using Netptune.Query.Model;

namespace Netptune.Query.Views;

public sealed record TaskViewDefinition
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public QueryGroup Query { get; init; } = new();

    public TaskViewDisplay Display { get; init; } = new();
}
