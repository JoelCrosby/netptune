using Xunit;

namespace Netptune.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UserPreferencesCollection
{
    public const string Name = "User preferences endpoints";
}
