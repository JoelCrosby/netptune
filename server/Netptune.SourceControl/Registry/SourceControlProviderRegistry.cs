using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Registry;

public sealed class SourceControlProviderRegistry : ISourceControlProviderRegistry
{
    private readonly Dictionary<SourceControlProvider, ISourceControlProvider> ProvidersByKind;

    public IReadOnlyList<ISourceControlProvider> All { get; }

    public SourceControlProviderRegistry(IEnumerable<ISourceControlProvider> providers)
    {
        var registered = providers.ToList();

        var duplicateKinds = registered
            .GroupBy(provider => provider.Provider)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateKinds.Count > 0)
        {
            var duplicates = string.Join(", ", duplicateKinds);

            throw new InvalidOperationException($"Source control providers registered more than once: {duplicates}.");
        }

        ProvidersByKind = registered.ToDictionary(provider => provider.Provider);
        All = registered.OrderBy(provider => provider.Provider).ToList();
    }

    public bool IsEnabled(SourceControlProvider provider)
    {
        return ProvidersByKind.ContainsKey(provider);
    }

    public ISourceControlProvider Resolve(SourceControlProvider provider)
    {
        var found = ProvidersByKind.TryGetValue(provider, out var resolved);

        if (!found)
        {
            throw new InvalidOperationException($"No source control provider is enabled for {provider}.");
        }

        return resolved!;
    }

    public SourceControlCapabilities GetEffectiveCapabilities(ScmConnection connection)
    {
        var isActive = connection.Status == ScmConnectionStatus.Active;
        var isEnabled = IsEnabled(connection.Provider);

        if (!isActive || !isEnabled)
        {
            return SourceControlCapabilities.None;
        }

        var supported = Resolve(connection.Provider).Capabilities;

        return supported & connection.GrantedCapabilities;
    }

    public ClientResponse Require(ScmConnection connection, SourceControlCapabilities capability)
    {
        var isEnabled = IsEnabled(connection.Provider);

        if (!isEnabled)
        {
            return ClientResponse.Failed($"{connection.Provider} is not enabled on this Netptune instance.");
        }

        var isActive = connection.Status == ScmConnectionStatus.Active;

        if (!isActive)
        {
            var status = connection.Status.ToString().ToLowerInvariant();

            return ClientResponse.Failed($"This {connection.Provider} connection is {status}.");
        }

        var effective = GetEffectiveCapabilities(connection);
        var isAllowed = effective.HasFlag(capability);

        if (!isAllowed)
        {
            return ClientResponse.Failed($"This {connection.Provider} connection doesn't allow {capability}.");
        }

        return ClientResponse.Success;
    }

    public ScmConnectionContext CreateContext(ScmConnection connection)
    {
        var provider = Resolve(connection.Provider);
        var capabilities = GetEffectiveCapabilities(connection);

        return new ScmConnectionContext(connection, capabilities, provider.Connector);
    }
}
