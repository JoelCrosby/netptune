using System;
using System.Collections.Generic;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Comments;

public record CommentViewModel
{
    public int Id { get; init; }

    public string UserDisplayName { get; init; } = null!;

    public string? UserDisplayImage { get; init; }

    public string UserId { get; init; } = null!;

    public string Body { get; init; } = null!;

    public int EntityId { get; init; }

    public EntityType EntityType { get; init; }

    public List<ReactionViewModel> Reactions { get; init; } = new();

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
