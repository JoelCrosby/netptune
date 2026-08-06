using Netptune.Transfer.Enums;
using System.Text.Json;

using Netptune.Core.BaseEntities;

namespace Netptune.Transfer.Entities;

public record ImportDefinition : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string RecordType { get; set; } = null!;

    public ImportVendorProfile VendorProfile { get; set; } = ImportVendorProfile.None;

    public JsonDocument Mapping { get; set; } = null!;
}
