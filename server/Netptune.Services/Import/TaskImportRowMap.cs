
using CsvHelper.Configuration;

using Netptune.Core.Models.Import;

namespace Netptune.Services.Import;

public sealed class TaskImportRowMap : ClassMap<TaskImportRow>
{
    public TaskImportRowMap()
    {
        Map(m => m.Name);
        Map(m => m.Description).Optional();
        Map(m => m.Status).Optional();
        Map(m => m.IsFlagged).Optional();
        Map(m => m.CreatedAt).Optional();
        Map(m => m.UpdatedAt).Optional();
        Map(m => m.Assignees);
        Map(m => m.Owner);
        Map(m => m.Group);
        Map(m => m.Tags).Optional();
    }
}
