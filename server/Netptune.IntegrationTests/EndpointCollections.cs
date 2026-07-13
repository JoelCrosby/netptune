using Xunit;

namespace Netptune.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UserPreferencesCollection
{
    public const string Name = "User preferences endpoints";
}

// These endpoints all issue an UpdateUserCommand against the same seeded user. Run in parallel they
// race on EF optimistic concurrency and the loser fails with DbUpdateConcurrencyException.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UserMutationCollection
{
    public const string Name = "User mutation endpoints";
}

// Permanent deletion tears a whole workspace out from under everything referencing it, so it must
// not run alongside tests reading or writing workspace-scoped data.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WorkspaceMutationCollection
{
    public const string Name = "Workspace mutation endpoints";
}
