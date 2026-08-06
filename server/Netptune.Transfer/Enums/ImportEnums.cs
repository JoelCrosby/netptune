namespace Netptune.Transfer.Enums;

public enum ImportSourceKind
{
    Csv = 0,
    Tsv = 1,
    Xlsx = 2,
    Json = 3,
    Ndjson = 4,
    Archive = 5,
}

public enum ImportVendorProfile
{
    None = 0,
    Jira = 1,
    Trello = 2,
    Asana = 3,
    Netptune = 4,
}

public enum ImportEntryOperation
{
    Created = 0,
    Updated = 1,
}

public enum ImportDedupeAction
{
    CreateAlways = 0,
    SkipExisting = 1,
    UpdateExisting = 2,
}

public enum ImportUnknownPolicy
{
    Create = 0,
    UseDefault = 1,
    Skip = 2,
    Fail = 3,
}

public enum ImportTransformKind
{
    Trim = 0,
    Lowercase = 1,
    Uppercase = 2,
    SplitOn = 3,
    ParseDate = 4,
    ParseNumber = 5,
    Coalesce = 6,
    StripHtml = 7,
    Truncate = 8,
    Prefix = 9,
    Suffix = 10,
}

public enum ImportBindingOrigin
{
    Heuristic = 0,
    Vendor = 1,
    Assistant = 2,
    User = 3,
}

public enum ImportDiagnosticSeverity
{
    Error = 0,
    Warning = 1,
    Info = 2,
}

public enum ImportRowAction
{
    Create = 0,
    Update = 1,
    Skip = 2,
    Error = 3,
}
