using Netptune.Core.Events;

namespace Netptune.Core.SourceControl.History;

public sealed record ScmHistorySyncMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.SourceControl;

    public int RepoId { get; init; }
}
