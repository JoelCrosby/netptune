using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Entities
{
    public class Project : WorkspaceEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }

        public ProjectMeta MetaInfo { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

        [JsonIgnore]
        public ICollection<Post> ProjectPosts { get; set; } = new HashSet<Post>();

        [JsonIgnore]
        public ICollection<Board> ProjectBoards { get; set; } = new HashSet<Board>();

        #endregion

        #region Methods

        public ProjectViewModel ToViewModel()
        {
            var defaultBoard = ProjectBoards?.FirstOrDefault(board => board.BoardType == BoardType.Default);
            var identifier = defaultBoard?.Identifier;

            return new ProjectViewModel
            {
                Id = Id,
                Key = Key,
                Name = Name,
                Description = Description,
                RepositoryUrl = RepositoryUrl,
                WorkspaceId = WorkspaceId,
                OwnerDisplayName = Owner.DisplayName,
                UpdatedAt = UpdatedAt,
                CreatedAt = CreatedAt,
                DefaultBoardIdentifier = identifier,
                Color = MetaInfo.Color,
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
            };

            project.ProjectUsers.Add(new ProjectUser
            {
                ProjectId = project.Id,
                UserId = options.UserId
            });

            project.ProjectBoards.Add(GenerateDefaultBoard(project, options.WorkspaceId));

            return project;
        }

        private static Board GenerateDefaultBoard(Project project, int workspaceId)
        {
            return new()
            {
                Identifier = GenerateDefaultBoardId(project.Key),
                Name = project.Name,
                OwnerId = project.OwnerId,
                MetaInfo = new BoardMeta
                {
                    Color = project.MetaInfo.Color,
                },
                BoardType = BoardType.Default,
                WorkspaceId = workspaceId,
                BoardGroups = new[]
                {
                    new BoardGroup
                    {
                        Name = "Backlog",
                        Type = BoardGroupType.Backlog,
                        SortOrder = 1D,
                        WorkspaceId = workspaceId,
                    },
                    new BoardGroup
                    {
                        Name = "Todo",
                        Type = BoardGroupType.Todo,
                        SortOrder = 1.1D,
                        WorkspaceId = workspaceId,
                    },
                    new BoardGroup
                    {
                        Name = "Done",
                        Type = BoardGroupType.Done,
                        SortOrder = 1.3D,
                        WorkspaceId = workspaceId,
                    }
                }
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
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }

        public string UserId { get; set; }

        public int WorkspaceId { get; set; }

        public ProjectMeta MetaInfo { get; set; }
    }
}
