using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Export;

public sealed record ExportDefinitionModel
{
    public const string WorkspaceRecordType = "workspace";

    public required string RecordType { get; init; }

    public required ExportFormat Format { get; init; }

    public List<string> Fields { get; init; } = [];

    public ExportFilterModel? Filter { get; init; }

    public ExportOptionsModel Options { get; init; } = new();
}
