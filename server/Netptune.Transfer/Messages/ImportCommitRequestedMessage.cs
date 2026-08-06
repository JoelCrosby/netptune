using Netptune.Core.Events;

namespace Netptune.Transfer.Messages;

public record ImportCommitRequestedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Transfer;

    public required int WorkspaceId { get; init; }

    public required int ImportSessionId { get; init; }

    public required string UserId { get; init; }

    public bool SkipFailingRows { get; init; }
}
