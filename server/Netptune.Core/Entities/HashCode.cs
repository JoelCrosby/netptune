using System;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public class HashCode : WorkspaceEntity<int>
{
    public string Salt { get; set; }

    public string Code { get; set; }

    public DateTime Expires { get; set; }
}