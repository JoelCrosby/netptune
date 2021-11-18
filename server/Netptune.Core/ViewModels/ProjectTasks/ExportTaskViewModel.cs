using System;

namespace Netptune.Core.ViewModels.ProjectTasks;

public class ExportTaskViewModel
{
    public string SystemId { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public bool IsFlagged { get; set; }

    public double SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Assignee { get; set; }

    public string Owner { get; set; }

    public string Group { get; set; }

    public string Tags { get; set; }

    public string Project { get; set; }

    public string Board { get; set; }
}