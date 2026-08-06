using Netptune.Core.Enums;

namespace Netptune.Transfer.Definitions;

public sealed record ExportFilterModel
{
    public List<string> ProjectKeys { get; init; } = [];

    public List<string> BoardIdentifiers { get; init; } = [];

    public List<string> StatusKeys { get; init; } = [];

    public List<int> StatusCategories { get; init; } = [];

    public List<string> Tags { get; init; } = [];

    public List<string> AssigneeEmails { get; init; } = [];

    public List<TaskPriority> Priorities { get; init; } = [];

    public string? SprintRef { get; init; }

    public string? Term { get; init; }

    public bool IncludeDeleted { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }

    public DateTime? UpdatedSince { get; init; }
}
