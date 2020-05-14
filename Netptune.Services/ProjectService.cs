﻿using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

using System.Collections.Generic;
using System.Threading.Tasks;

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
                    RepositoryUrl = request.RepositoryUrl
                };

                project.ProjectUsers.Add(new ProjectUser
                {
                    ProjectId = project.Id,
                    UserId = user.Id
                });

                project.ProjectBoards.Add(new Board
                {
                    Identifier = GenerateDefaultBoardId(project),
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

                });

                var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Workspace);

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await GetProjectViewModel(project);
            });
        }

        private static string GenerateDefaultBoardId(Project project)
        {
            return $"{project.Name.ToLowerInvariant()}-default-board";
        }

        public async Task<ProjectViewModel> DeleteProject(int id)
        {
            var project = await ProjectRepository.GetAsync(id);
            var user = await IdentityService.GetCurrentUser();

            if (project is null) return null;

            project.IsDeleted = true;
            project.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return await GetProjectViewModel(project);
        }

        public async Task<ProjectViewModel> GetProject(int id)
        {
            var result = await ProjectRepository.GetAsync(id);

            return await GetProjectViewModel(result);
        }

        public Task<List<ProjectViewModel>> GetProjects(string workspaceSlug)
        {
            return ProjectRepository.GetProjects(workspaceSlug);
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

            return await GetProjectViewModel(result);
        }

        private Task<ProjectViewModel> GetProjectViewModel(Project project)
        {
            return ProjectRepository.GetProjectViewModel(project.Id);
        }
    }
}
