namespace Netptune.Transfer.Definitions;

public sealed record ExportOptionsModel
{
    public char Delimiter { get; init; } = ',';

    public string DateFormat { get; init; } = "yyyy-MM-dd";

    public string TimeZoneId { get; init; } = "UTC";

    public string CollectionSeparator { get; init; } = "|";

    public bool IncludeHeaderRow { get; init; } = true;

    public bool ExpandCollectionsToRows { get; init; }

    public bool IncludeHistory { get; init; }

    public bool IncludeFiles { get; init; }

    public bool IncludeMembers { get; init; }
}
