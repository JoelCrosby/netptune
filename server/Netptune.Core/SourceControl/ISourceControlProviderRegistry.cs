using Netptune.Core.Responses.Common;

namespace Netptune.Core.SourceControl;

public interface ISourceControlProviderRegistry
{
    IReadOnlyList<ISourceControlProvider> All { get; }

    bool IsEnabled(SourceControlProvider provider);

    ISourceControlProvider Resolve(SourceControlProvider provider);

    SourceControlCapabilities GetEffectiveCapabilities(ScmConnection connection);

    ClientResponse Require(ScmConnection connection, SourceControlCapabilities capability);

    ScmConnectionContext CreateContext(ScmConnection connection);
}
