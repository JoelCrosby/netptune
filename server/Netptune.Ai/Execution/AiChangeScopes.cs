using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Realtime;

namespace Netptune.Ai.Execution;

// A change names the entity it was proposed against, which is not always all it touches: a comment is
// proposed on its task, but other people's comment threads have to refresh as well.
internal static class AiChangeScopes
{
    private static readonly Dictionary<string, string[]> ExtraScopesByChange = new(StringComparer.Ordinal)
    {
        [AddCommentAction.Change] = [WorkspaceEventScopes.Comment],
        [UpdateCommentAction.Change] = [WorkspaceEventScopes.Comment],
        [DeleteCommentAction.Change] = [WorkspaceEventScopes.Comment],
    };

    public static List<string> For(
        IEnumerable<AiProposedChange> changes,
        IEnumerable<AiAppliedChangeResult> results)
    {
        var appliedIds = results
            .Where(result => result.Status == AiChangeApplyStatus.Applied)
            .Select(result => result.ChangeId)
            .ToHashSet();

        return changes
            .Where(change => appliedIds.Contains(change.Id))
            .SelectMany(change => ExtraScopesByChange
                .GetValueOrDefault(change.ToolName, [])
                .Prepend(change.EntityType))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
