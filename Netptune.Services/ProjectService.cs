using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
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
                var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Workspace);

                if (workspace is null) return null;

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

                var projectKey = await GetProjectKey(project, workspace.Id);

                if (projectKey is null)
                {
                    throw new Exception("Failed to generate unique project Key for project.");
                }

                project.Key = projectKey;
                project.ProjectBoards.Add(GenerateDefaultBoard(project));

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await ProjectRepository.GetProjectViewModel(project.Id, true);
            });
        }

        private static Board GenerateDefaultBoard(Project project)
        {
            return new Board
            {
                Identifier = GenerateDefaultBoardId(project.Key),
                Name = project.Name,
                OwnerId = project.OwnerId,
                MetaInfo = new BoardMeta(),
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

        private static string GenerateDefaultBoardId(string projectKey)
        {
            return $"{projectKey.ToLowerInvariant().ToUrlSlug()}-default-board";
        }

        public async Task<ClientResponse> Delete(int id)
        {
            var project = await ProjectRepository.GetAsync(id);
            var userId = await IdentityService.GetCurrentUserId();

            if (project is null || userId is null) return null;

            project.Delete(userId);

            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
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

        private Task<string> GetProjectKey(Project project, int workspaceId)
        {
            const int keyLength = 4;

            var key = project.Name.Substring(0, keyLength).ToLowerInvariant();

            async Task<string> TryGetKey(string currentKey, int currentKeyLength)
            {
                while (true)
                {
                    var isAvailable = await ProjectRepository.IsProjectKeyAvailable(currentKey, workspaceId);

                    if (isAvailable) return currentKey;

                    var nextKey = project.Name.Substring(0, currentKeyLength + 1).ToLowerInvariant();

                    currentKey = nextKey;

                    currentKeyLength += 1;
                }
            }

            return TryGetKey(key, keyLength);
        }
    }
}
