using Netptune.Core.Events.SourceControl;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmAutomationEvents
{
    private readonly List<SourceControlTaskEventMessage> Pending = [];

    public void Add(SourceControlTaskEventMessage message)
    {
        Pending.Add(message);
    }

    public void Clear()
    {
        Pending.Clear();
    }

    public IReadOnlyList<SourceControlTaskEventMessage> TakeAll()
    {
        var taken = Pending.ToList();

        Pending.Clear();

        return taken;
    }
}
