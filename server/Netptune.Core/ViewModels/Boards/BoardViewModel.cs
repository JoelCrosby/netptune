using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Boards;

public class BoardViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public int ProjectId { get; set; }

    public BoardType BoardType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string OwnerUsername { get; set; } = null!;

    public BoardMeta? MetaInfo { get; set; }

    public int TaskCount { get; set; }

    public DateTime LastUpdated { get; set; }

    public List<AssigneeViewModel> Assignees { get; set; } = new();
}

public class BoardsViewModel
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public List<BoardViewModel> Boards { get; set; } = null!;
}
