using Netptune.Core.Entities;
using Netptune.Core.Statuses;

namespace Netptune.TestData.Seeders;

internal static class StatusSeeder
{
    internal static List<Status> Generate(List<Workspace> workspaces) =>
        workspaces
            .SelectMany(workspace => DefaultTaskStatuses.All.Select(definition =>
            {
                var status = DefaultTaskStatuses.Create(definition, workspace.Id, workspace.OwnerId);
                status.Workspace = workspace;
                status.Owner = workspace.Owner;
                return status;
            }))
            .ToList();
}
