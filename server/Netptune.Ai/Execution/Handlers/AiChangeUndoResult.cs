using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Handlers;

/// <summary>
/// An undo reports itself with the same statuses as an apply: applied means the
/// change was taken back, failed means it is still in place.
/// </summary>
public static class AiChangeUndoResult
{
    public static AiAppliedChangeResult Undone(AiProposedChange change, int? entityId)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Applied,
            AppliedEntityId = entityId,
        };
    }

    public static AiAppliedChangeResult Failure(AiProposedChange change, string message)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Failed,
            Error = message,
        };
    }
}
