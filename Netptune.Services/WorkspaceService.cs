using Netptune.Core.Encoding;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;

using System.Collections.Generic;
using System.Threading.Tasks;

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

            workspace.Slug = UrlSlugger.ToUrlSlug(workspace.Name);

            workspace.WorkspaceUsers.Add(new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id
            });

            var result = await WorkspaceRepository.AddAsync(workspace);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Workspace> DeleteWorkspace(int id)
        {
            var workspace = await WorkspaceRepository.GetAsync(id);

            workspace.IsDeleted = true;

            await UnitOfWork.CompleteAsync();

            return workspace;
        }

        public Task<Workspace> GetWorkspace(int id)
        {
            return WorkspaceRepository.GetAsync(id);
        }

        public Task<Workspace> GetWorkspace(string slug)
        {
            return WorkspaceRepository.GetBySlug(slug);
        }

        public Task<List<Workspace>> GetWorkspaces(AppUser user)
        {
            return WorkspaceRepository.GetWorkspaces(user);
        }

        public async Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            var result = await WorkspaceRepository.GetAsync(workspace.Id);

            if (result is null) return null;

            result.Name = workspace.Name;
            result.Description = workspace.Description;
            result.ModifiedByUserId = user.Id;
            result.MetaInfo = workspace.MetaInfo;

            await UnitOfWork.CompleteAsync();

            await UnitOfWork.CompleteAsync();

            return result;
        }
    }
}
