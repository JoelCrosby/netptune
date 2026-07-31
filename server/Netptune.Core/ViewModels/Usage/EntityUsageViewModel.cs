using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Usage;

public sealed record EntityUsageViewModel
{
    public int Id { get; init; }

    public UsageSubjectKind Kind { get; init; }

    public string Name { get; init; } = null!;

    public int UsageCount { get; init; }

    public List<UsageReferenceGroupViewModel> References { get; init; } = [];

    public bool CanDelete { get; init; }

    public string? BlockedReason { get; init; }
}

public sealed record UsageReferenceGroupViewModel
{
    public UsageReferenceKind Kind { get; init; }

    public List<UsageReferenceViewModel> Items { get; init; } = [];
}

public sealed record UsageReferenceViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Context { get; init; }
}
