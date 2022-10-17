using System;
using System.Collections.Generic;

using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.ProjectTasks;

public record TaskViewModel
{
    public int Id { get; set; }

    public List<AssigneeViewModel> Assignees { get; set; } = new();

    public string OwnerId { get; set; } = null!;

    public int ProjectScopeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string SystemId { get; set; } = null!;

    public ProjectTaskStatus Status { get; set; }

    public List<string> Tags { get; set; } = new();

    public bool IsFlagged { get; set; }

    public double SortOrder { get; set; }

    public int? ProjectId { get; set; }

    public int? WorkspaceId { get; set; }

    public string WorkspaceKey { get; set; }  = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string OwnerUsername { get; set; }  = null!;

    public string? OwnerPictureUrl { get; set; }

    public string? ProjectName { get; set; }
}
