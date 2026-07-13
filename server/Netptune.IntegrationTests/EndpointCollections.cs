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
