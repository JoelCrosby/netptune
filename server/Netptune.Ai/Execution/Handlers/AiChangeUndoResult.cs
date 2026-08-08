using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Handlers;

public static class AiChangeUndoResult
{
    public static AiAppliedChangeResult Undone(AiProposedChange change, int? entityId)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            EntityType = change.EntityType,
            Status = AiChangeApplyStatus.Applied,
            AppliedEntityId = entityId,
        };
    }

    public static AiAppliedChangeResult Failure(AiProposedChange change, string message)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            EntityType = change.EntityType,
            Status = AiChangeApplyStatus.Failed,
            Error = message,
        };
    }
}
