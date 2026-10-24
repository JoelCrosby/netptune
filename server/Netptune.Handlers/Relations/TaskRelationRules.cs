using Netptune.Core.Entities;
using Netptune.Core.Localization;
using Netptune.Core.Relations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Relations;

internal static class TaskRelationRules
{
    public static async Task<string?> FindViolation(
        INetptuneUnitOfWork unitOfWork,
        RelationType relationType,
        int sourceTaskId,
        int targetTaskId,
        CancellationToken cancellationToken)
    {
        var isLinked = await unitOfWork.ProjectTaskRelations.Exists(relationType.Id, sourceTaskId, targetTaskId, cancellationToken);

        if (isLinked)
        {
            return AppText.ErrorTasksAlreadyLinkedRelation;
        }

        var hasSingleSource = RelationTypeRules.HasSingleSource(relationType.Category);

        if (hasSingleSource)
        {
            var hasSource = await unitOfWork.ProjectTaskRelations.HasExistingSource(relationType.Id, targetTaskId, cancellationToken);

            if (hasSource)
            {
                return Localized.Format(AppText.ErrorTaskAlreadyLinkTaskCanOnlyOne, relationType.Name);
            }
        }

        var isAcyclic = RelationTypeRules.IsAcyclic(relationType.Category);

        if (isAcyclic)
        {
            var wouldCycle = await unitOfWork.ProjectTaskRelations.WouldCreateCycle(relationType.Id, sourceTaskId, targetTaskId, cancellationToken);

            if (wouldCycle)
            {
                return Localized.Format(AppText.ErrorWouldCreateCircularChain, relationType.Name);
            }
        }

        return null;
    }
}
