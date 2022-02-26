using System;

namespace Netptune.Core.Models.Import;

public class TaskImportRow
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public string IsFlagged { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Assignee { get; set; }

    public string Owner { get; set; }

    public string Group { get; set; }

    public string Tags { get; set; }
}
