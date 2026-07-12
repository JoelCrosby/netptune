using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Entities;

public record Project : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? RepositoryUrl { get; set; }

    public string Key { get; set; }= null!;

    public ProjectMeta? MetaInfo { get; set; }

    public int? DefaultStatusId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new HashSet<ProjectUser>();

    [JsonIgnore]
    public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

    [JsonIgnore]
    public Status? DefaultStatus { get; set; }

    [JsonIgnore]
    public ICollection<Sprint> Sprints { get; set; } = new HashSet<Sprint>();

    [JsonIgnore]
    public ICollection<Board> ProjectBoards { get; set; } = new HashSet<Board>();

    #endregion

    #region Methods

    public ProjectViewModel ToViewModel()
    {
        var defaultBoard = ProjectBoards.FirstOrDefault(board => board.BoardType == BoardType.Default);
        var identifier = defaultBoard?.Identifier;

        return new ()
        {
            Id = Id,
            Key = Key,
            Name = Name,
            Description = Description,
            RepositoryUrl = RepositoryUrl,
            WorkspaceId = WorkspaceId,
            OwnerDisplayName = Owner!.DisplayName,
            UpdatedAt = UpdatedAt,
            CreatedAt = CreatedAt,
            DefaultBoardIdentifier = identifier,
            Color = MetaInfo?.Color,
            DefaultStatusId = DefaultStatusId,
            DefaultStatusName = DefaultStatus?.Name,
        };
    }

    public static Project Create(CreateProjectOptions options)
    {
        var project = new Project
        {
            Name = options.Name,
            Description = options.Description,
            CreatedByUserId = options.UserId,
            OwnerId = options.UserId,
            RepositoryUrl = options.RepositoryUrl,
            Key = options.Key,
            MetaInfo = options.MetaInfo,
            DefaultStatusId = options.DefaultStatusId,
        };

        project.ProjectUsers.Add(new ProjectUser
        {
            ProjectId = project.Id,
            UserId = options.UserId,
        });

        project.ProjectBoards.Add(GenerateDefaultBoard(project, options));

        return project;
    }

    private static Board GenerateDefaultBoard(Project project, CreateProjectOptions options)
    {
        var workspaceId = options.WorkspaceId;

        return new()
        {
            Identifier = GenerateDefaultBoardId(project.Key),
            Name = project.Name,
            OwnerId = project.OwnerId,
            MetaInfo = new BoardMeta
            {
                Color = project.MetaInfo?.Color,
            },
            BoardType = BoardType.Default,
            WorkspaceId = workspaceId,
            BoardGroups = new[]
            {
                new BoardGroup
                {
                    Name = "Backlog",
                    SortOrder = 1D,
                    WorkspaceId = workspaceId,
                    StatusId = options.BacklogStatusId,
                },
                new BoardGroup
                {
                    Name = "Todo",
                    SortOrder = 1.1D,
                    WorkspaceId = workspaceId,
                    StatusId = options.ActiveStatusId,
                },
                new BoardGroup
                {
                    Name = "Done",
                    SortOrder = 1.3D,
                    WorkspaceId = workspaceId,
                    StatusId = options.DoneStatusId,
                },
            },
        };
    }

    private static string GenerateDefaultBoardId(string projectKey)
    {
        return $"{projectKey.ToLowerInvariant().ToUrlSlug()}-default-board";
    }

    #endregion
}

public class CreateProjectOptions
{
    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? RepositoryUrl { get; init; }

    public string Key { get; init; } = null!;

    public string UserId { get; init; } = null!;

    public int WorkspaceId { get; init; }

    public ProjectMeta? MetaInfo { get; init; }

    public int? DefaultStatusId { get; init; }

    // Optional pre-assigned statuses for the auto-created Backlog/Todo/Done groups.
    public int? BacklogStatusId { get; init; }

    public int? ActiveStatusId { get; init; }

    public int? DoneStatusId { get; init; }
}
