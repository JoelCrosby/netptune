using System;
using System.Collections.Generic;
using System.Globalization;

using CsvHelper.Configuration;

namespace Netptune.Core.ViewModels.ProjectTasks;

public class ExportTaskViewModel
{
    public string SystemId { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string Status { get; init; } = null!;

    public bool IsFlagged { get; init; }

    public double SortOrder { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public List<string> Assignees { get; init; } = new();

    public string Owner { get; init; }= null!;

    public string? Group { get; init; }

    public string? Tags { get; set; }

    public string? Project { get; init; }

    public string? Board { get; init; }
}

public sealed class ExportTaskViewModelMap : ClassMap<ExportTaskViewModel>
{
    public ExportTaskViewModelMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        Map(m => m.Assignees).Convert(m => string.Join(" | ", m.Value.Assignees));
    }
}
