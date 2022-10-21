using System;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record HashCode : WorkspaceEntity<int>
{
    public string Salt { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTime Expires { get; set; }
}
