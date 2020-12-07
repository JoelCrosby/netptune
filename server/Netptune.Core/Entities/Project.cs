using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;

namespace Netptune.Core.Entities
{
    public class Project : WorkspaceEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

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

        public static Project Create(CreateProjectOptions options)
        {
            var project = new Project
            {
                Name = options.Name,
                Description = options.Description,
                CreatedByUserId = options.User.Id,
                OwnerId = options.User.Id,
                RepositoryUrl = options.RepositoryUrl,
                Key = options.Key,
            };

            project.ProjectUsers.Add(new ProjectUser
            {
                ProjectId = project.Id,
                UserId = options.User.Id
            });

            project.ProjectBoards.Add(GenerateDefaultBoard(project, options.WorkspaceId));

            return project;
        }

        private static Board GenerateDefaultBoard(Project project, int workspaceId)
        {
            return new Board
            {
                Identifier = GenerateDefaultBoardId(project.Key),
                Name = project.Name,
                OwnerId = project.OwnerId,
                MetaInfo = new BoardMeta(),
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

        public AppUser User { get; set; }

        public int WorkspaceId { get; set; }
    }
}
