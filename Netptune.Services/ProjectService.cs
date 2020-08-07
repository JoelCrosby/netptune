using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository ProjectRepository;
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;

        public ProjectService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
        {
            ProjectRepository = unitOfWork.Projects;
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
        }

        public Task<ProjectViewModel> AddProject(AddProjectRequest request)
        {
            return UnitOfWork.Transaction(async () =>
            {
                var user = await IdentityService.GetCurrentUser();

                var project = new Project
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatedByUserId = user.Id,
                    OwnerId = user.Id,
                    RepositoryUrl = request.RepositoryUrl,
                };

                project.ProjectUsers.Add(new ProjectUser
                {
                    ProjectId = project.Id,
                    UserId = user.Id
                });

                project.ProjectBoards.Add(GenerateDefaultBoard(project));

                project.Key = await GetProjectKey(project) ??
                              throw new Exception("Failed to generate unique project Key for project.");

                var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Workspace);

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await ProjectRepository.GetProjectViewModel(project.Id, true);
            });
        }

        private static Board GenerateDefaultBoard(Project project)
        {
            return new Board
            {
                Identifier = GenerateDefaultBoardId(project.Name),
                Name = project.Name,
                OwnerId = project.OwnerId,
                BoardType = BoardType.Default,
                BoardGroups = new[]
                {
                    new BoardGroup
                    {
                        Name = "Backlog",
                        Type = BoardGroupType.Backlog,
                        SortOrder = 1D
                    },
                    new BoardGroup
                    {
                        Name = "Todo",
                        Type = BoardGroupType.Todo,
                        SortOrder = 1.1D
                    },
                    new BoardGroup
                    {
                        Name = "Done",
                        Type = BoardGroupType.Done,
                        SortOrder = 1.3D
                    }
                }
            };
        }

        private static string GenerateDefaultBoardId(string projectName)
        {
            return $"{projectName.ToLowerInvariant().ToUrlSlug()}-default-board";
        }

        public async Task<Project> DeleteProject(int id)
        {
            var project = await ProjectRepository.GetAsync(id);
            var user = await IdentityService.GetCurrentUser();

            if (project is null) return null;

            project.IsDeleted = true;
            project.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return project;
        }

        public Task<ProjectViewModel> GetProject(int id)
        {
            return ProjectRepository.GetProjectViewModel(id, true);
        }

        public Task<List<ProjectViewModel>> GetProjects(string workspaceSlug)
        {
            return ProjectRepository.GetProjects(workspaceSlug, true);
        }

        public async Task<ProjectViewModel> UpdateProject(Project project)
        {
            var result = await ProjectRepository.GetAsync(project.Id);
            var user = await IdentityService.GetCurrentUser();

            if (result is null) return null;

            result.Name = project.Name;
            result.Description = project.Description;
            result.ModifiedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return await ProjectRepository.GetProjectViewModel(result.Id, true);
        }

        private Task<string> GetProjectKey(Project project)
        {
            var key = project.Name.Substring(0, 3).ToLowerInvariant();

            async Task<string> TryGetKey(string currentKey, int keyLength)
            {
                while (true)
                {
                    var isAvailable = await ProjectRepository.IsProjectKeyAvailable(currentKey, project.WorkspaceId);

                    if (isAvailable) return key;

                    var nextKey = project.Name.Substring(0, keyLength).ToLowerInvariant();

                    currentKey = nextKey;

                    keyLength += 1;
                }
            }

            return TryGetKey(key, 3);
        }
    }
}
