using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Transfer.Entities;
using Netptune.Transfer.ViewModels;

namespace Netptune.Transfer.Definitions;

public static class ExportDefinitionMapper
{
    public static ExportDefinitionViewModel ToViewModel(ExportDefinition definition)
    {
        var model = definition.Definition.Deserialize<ExportDefinitionModel>(JsonOptions.Default);

        return new ExportDefinitionViewModel
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            RecordType = definition.RecordType,
            Format = definition.Format,
            IsShared = definition.IsShared,
            Definition = model,
            CreatedByUserId = definition.CreatedByUserId,
            CreatedByDisplayName = definition.CreatedByUser?.DisplayName,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
        };
    }
}
