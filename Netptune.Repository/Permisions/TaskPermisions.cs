using System;
using System.Linq;
using Netptune.Models.Models;

namespace Netptune.Repository.Permisions
{
    public static class TaskPermisions
    {
        public static bool CanEdit(ProjectTask task, AppUser user)
        {
            // Check if the task is in workspace user belongs in.
            return user.WorkspaceUsers.Any(x => x.Id == task.Workspace.Id);
        }
    }
}
