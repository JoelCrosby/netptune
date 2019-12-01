using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Extensions;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;

namespace Netptune.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        protected readonly INetptuneUnitOfWork UnitOfWork;
        protected readonly IWorkspaceRepository WorkspaceRepository;

        public WorkspaceService(INetptuneUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
            WorkspaceRepository = unitOfWork.Workspaces;
        }

        public async Task<Workspace> AddWorkspace(Workspace workspace, AppUser user)
        {
            workspace.CreatedByUserId = user.Id;
            workspace.OwnerId = user.Id;

            workspace.Slug = workspace.Name.ToUrlSlug();

            workspace.WorkspaceUsers.Add(new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id
            });

            var result = await WorkspaceRepository.AddWorkspace(workspace);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Workspace> DeleteWorkspace(int id)
        {
            var result = await WorkspaceRepository.DeleteWorkspace(id);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public Task<Workspace> GetWorkspace(int id)
        {
            return WorkspaceRepository.GetAsync(id);
        }


        public Task<List<Workspace>> GetWorkspaces(AppUser user)
        {
            return WorkspaceRepository.GetWorkspaces(user);
        }

        public async Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            var result = await WorkspaceRepository.UpdateWorkspace(workspace, user);

            await UnitOfWork.CompleteAsync();

            return result;
        }
    }
}
