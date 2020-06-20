﻿using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<Workspace> GetWorkspace(string slug);

        ValueTask<Workspace> GetWorkspace(int id);

        Task<List<Workspace>> GetWorkspaces();

        Task<Workspace> UpdateWorkspace(Workspace workspace);

        Task<Workspace> AddWorkspace(Workspace workspace);

        Task<Workspace> DeleteWorkspace(int id);
    }
}
