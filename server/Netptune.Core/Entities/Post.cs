using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record Post : WorkspaceEntity<int>
{
    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public PostType Type { get; set; }

    #region ForeignKeys

    public int ProjectId { get; set; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public virtual Project? Project { get; set; }

    #endregion

}
