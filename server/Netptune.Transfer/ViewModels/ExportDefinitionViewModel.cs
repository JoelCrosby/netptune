using Netptune.Transfer.Enums;
using Netptune.Transfer.Definitions;

namespace Netptune.Transfer.ViewModels;

public sealed record ExportDefinitionViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string RecordType { get; init; } = null!;

    public ExportFormat Format { get; init; }

    public bool IsShared { get; init; }

    public ExportDefinitionModel? Definition { get; init; }

    public string? CreatedByUserId { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
