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

    public HashSet<string> Assignees { get; init; } = new();

    public string Owner { get; init; }= null!;

    public string? Group { get; init; }

    public HashSet<string> Tags { get; set; } = new();

    public string? Project { get; init; }

    public string? Board { get; init; }
}

public sealed class ExportTaskViewModelMap : ClassMap<ExportTaskViewModel>
{
    public ExportTaskViewModelMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        Map(m => m.Assignees).Convert(m => string.Join(" | ", m.Value.Assignees));
        Map(m => m.Tags).Convert(m => string.Join(" | ", m.Value.Tags));
    }
}
