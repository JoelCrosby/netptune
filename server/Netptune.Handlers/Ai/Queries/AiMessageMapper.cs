using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public static class AiMessageMapper
{
    public static List<AiMessageViewModel> ToViewModels(
        List<AiMessage> messages,
        IReadOnlyDictionary<long, List<AiEntityReference>> referencesByMessage,
        List<AiChangeSet> changeSets)
    {
        var proposalMessages = changeSets
            .GroupBy(changeSet => changeSet.MessageId)
            .ToDictionary(group => group.Key, group => group.First().Id);

        var models = new List<AiMessageViewModel>(messages.Count);
        Guid? proposedChangeSetId = null;

        foreach (var message in messages)
        {
            var isProposal = proposalMessages.TryGetValue(message.Id, out var changeSetId);

            if (isProposal)
            {
                proposedChangeSetId = changeSetId;
            }

            var content = AiMessageContent.FromJsonDocument(message.Content);
            var isOutcome = message.Role == AiMessageRole.User && AiChangeSetSummary.IsOutcome(content.Text);
            var hasReferences = referencesByMessage.TryGetValue(message.Id, out var references);

            models.Add(new AiMessageViewModel
            {
                Id = message.Id,
                Sequence = message.Sequence,
                Role = message.Role,
                Text = content.Text,
                ToolNames = content.ToolCalls.Select(call => call.Name).ToList(),
                References = hasReferences ? references! : [],
                ChangeSetId = isOutcome ? proposedChangeSetId : null,
                CreatedAt = message.CreatedAt,
            });
        }

        return models;
    }
}
