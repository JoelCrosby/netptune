using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Core.ViewModels.Ai;

public sealed record AiChangeFieldViewModel
{
    public required string Name { get; init; }

    public string? Before { get; init; }

    public string? After { get; init; }

    public AiChangeValueKind Kind { get; init; } = AiChangeValueKind.Text;

    public List<AiChangeValue>? BeforeValues { get; init; }

    public List<AiChangeValue>? AfterValues { get; init; }
}

public sealed record AiProposedChangeViewModel
{
    public long Id { get; init; }

    public int Sequence { get; init; }

    public required string ToolName { get; init; }

    public required string EntityType { get; init; }

    public int? EntityId { get; init; }

    public string? RefKey { get; init; }

    public required string Summary { get; init; }

    public List<AiChangeFieldViewModel> Fields { get; init; } = [];

    public AiChangeValidationStatus ValidationStatus { get; init; }

    public string? ValidationMessage { get; init; }

    public AiChangeApplyStatus ApplyStatus { get; init; }

    public string? ApplyError { get; init; }

    public int? AppliedEntityId { get; init; }

    public string? EntitySystemId { get; init; }

    public DateTime? UndoneAt { get; init; }

    public bool CanUndo { get; init; }
}

public sealed record AiChangeSetViewModel
{
    public Guid Id { get; init; }

    public Guid ConversationId { get; init; }

    public AiChangeSetStatus Status { get; init; }

    public DateTime? AppliedAt { get; init; }

    public DateTime? UndoneAt { get; init; }

    public List<AiProposedChangeViewModel> Changes { get; init; } = [];
}
