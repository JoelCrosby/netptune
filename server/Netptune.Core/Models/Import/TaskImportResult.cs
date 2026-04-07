namespace Netptune.Core.Models.Import;

public class TaskImportResult : ImportResult
{
    public IEnumerable<string> MissingEmails { get; init; } = null!;
}
