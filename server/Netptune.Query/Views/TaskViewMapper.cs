using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;

namespace Netptune.Query.Views;

public static class TaskViewMapper
{
    public static TaskViewViewModel ToViewModel(TaskView view, string currentUserId, bool canManageShared)
    {
        var definition = view.Definition.Deserialize<TaskViewDefinition>(JsonOptions.Default);
        var isOwn = view.CreatedByUserId == currentUserId;

        return new TaskViewViewModel
        {
            Id = view.Id,
            Name = view.Name,
            Description = view.Description,
            Slug = view.Slug,
            Icon = view.Icon,
            IsShared = view.IsShared,
            Definition = definition,
            CreatedByUserId = view.CreatedByUserId,
            CreatedByDisplayName = view.CreatedByUser?.DisplayName,
            IsOwn = isOwn,
            CanEdit = isOwn || canManageShared,
            CreatedAt = view.CreatedAt,
            UpdatedAt = view.UpdatedAt,
        };
    }
}
