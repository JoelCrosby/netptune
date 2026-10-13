using Netptune.Core.Entities;

namespace Netptune.Core.Services.Ai;

public enum AiChangeEditorKind
{
    Text = 0,
    Markdown = 1,
    Priority = 2,
    Estimate = 3,
    Date = 4,
    Status = 5,
    User = 6,
    Users = 7,
    Tags = 8,
    Sprint = 9,
    BoardGroup = 10,
}

public sealed record AiChangeFieldEditor
{
    public required AiChangeEditorKind Kind { get; init; }

    public bool IsClearable { get; init; }

    public int? ProjectId { get; init; }
}

public sealed record AiChangeFieldEdit
{
    public required string Name { get; init; }

    public string? Value { get; init; }

    public List<string>? Values { get; init; }

    public decimal? Number { get; init; }
}

public interface IAiChangeEditCatalog
{
    AiChangeFieldEditor? FindEditor(AiProposedChange change, string fieldName);

    Task<AiChangeEditResult> Apply(AiProposedChange change, IReadOnlyList<AiProposedChange> changeSet, IReadOnlyList<AiChangeFieldEdit> edits, CancellationToken cancellationToken);
}
