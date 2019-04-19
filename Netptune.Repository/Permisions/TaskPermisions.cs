using System;
using System.Linq;
using Netptune.Models.Models;

namespace Netptune.Repository.Permisions
{
    public static class TaskPermisions
    {
        /// <summary>
        /// Check if a user has permision to edit a task.
        /// </summary>
        /// <returns></returns>
        /// <param name="task"></param>
        /// <param name="user"></param>
        public static bool CanEdit(ProjectTask task, AppUser user)
        {
            var rel = user.WorkspaceUsers.ToList();
            return rel.Any(x => x.Id == task.Workspace.Id);
        }
    }
}
