using System;
using System.Collections.Generic;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Comments;

public class CommentViewModel
{
    public int Id { get; set; }

    public string UserDisplayName { get; set; } = null!;

    public string UserDisplayImage { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Body { get; set; } = null!;

    public int EntityId { get; set; }

    public EntityType EntityType { get; set; }

    public List<Reaction> Reactions { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
