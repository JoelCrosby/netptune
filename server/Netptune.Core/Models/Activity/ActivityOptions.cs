using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity;

public class ActivityOptions
{
    [Required]
    public int? EntityId { get; set; }

    [Required]
    public int? WorkspaceId { get; set; }

    [Required]
    public EntityType EntityType { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public ActivityType Type { get; set; }
}

public class ActivityOptions<TMeta> : ActivityOptions where TMeta : class
{
    public TMeta Meta { get; set; } = null!;
}

public class ActivityMultipleOptions
{
    [Required]
    public IEnumerable<int> EntityIds { get; set; } = null!;

    [Required]
    public int? WorkspaceId { get; set; }

    [Required]
    public EntityType EntityType { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public ActivityType Type { get; set; }
}

public class ActivityMultipleOptions<TMeta> : ActivityMultipleOptions where TMeta : class
{
    public TMeta Meta { get; set; } = null!;
}
