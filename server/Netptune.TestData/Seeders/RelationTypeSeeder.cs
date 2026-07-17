using Netptune.Core.Entities;
using Netptune.Core.Relations;

namespace Netptune.TestData.Seeders;

internal static class RelationTypeSeeder
{
    internal static List<RelationType> Generate(List<Workspace> workspaces) =>
        workspaces
            .SelectMany(workspace => DefaultRelationTypes.All.Select(definition =>
            {
                var relationType = DefaultRelationTypes.Create(
                    definition,
                    workspace.Id,
                    workspace.OwnerId);

                relationType.Workspace = workspace;
                relationType.Owner = workspace.Owner;

                return relationType;
            }))
            .ToList();
}
