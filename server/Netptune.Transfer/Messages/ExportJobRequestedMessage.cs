using Netptune.Core.Events;

namespace Netptune.Transfer.Messages;

public record ExportJobRequestedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Transfer;

    public required int WorkspaceId { get; init; }

    public required int ExportJobId { get; init; }

    public required string UserId { get; init; }
}
