using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Connect;

internal sealed record ScmConnectState(SourceControlProvider Provider, int WorkspaceId, string UserId);
