using Netptune.Core.Entities;
using Netptune.Core.SourceControl.Events;

namespace Netptune.SourceControl.Webhooks;

internal interface IScmEventHandler
{
    bool CanHandle(ScmEvent scmEvent);

    // Returns whether anything the workspace can see changed, so open views are told to refresh.
    Task<bool> Handle(ScmEvent scmEvent, SourceControlConnection connection, CancellationToken cancellationToken);
}
