using System.Collections.Generic;

namespace Netptune.Core.Models.Import;

public class TaskImportResult : ImportResult
{
    public IEnumerable<string> MissingEmails { get; set; }
}