using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Caching.Distributed;

using Netptune.Core.Cache.Common;
using Netptune.Core.Hubs;
using Netptune.Core.Services;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class UserConnectionServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly UserConnectionService Service;

    private readonly ICacheProvider CacheProvider = Substitute.For<ICacheProvider>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public UserConnectionServiceTests()
    {
        Service = new(CacheProvider, Identity);
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenConnectionExists()
    {
        var connection = Fixture.Build<UserConnection>()
            .Without(x => x.User)
            .Create();

        CacheProvider.GetValueAsync<UserConnection>(Arg.Any<string>()).Returns(connection);

        var result = await Service.Get("id");

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(connection);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenConnectionNotExists()
    {
        CacheProvider.GetValueAsync<UserConnection>(Arg.Any<string>()).ReturnsNull();

        var result = await Service.Get("id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Add_ShouldReturnCorrectly_WhenInputValid()
    {
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUser().Returns(user);
        CacheProvider.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<UserConnection>>(),
            Arg.Any<DistributedCacheEntryOptions>())
                .Returns(x => x.Arg<Func<UserConnection>>().Invoke());

        var result = await Service.Add("connection-id");

        result.Should().NotBeNull();
        result!.ConnectId.Should().Be("connection-id");
        result.User.Should().BeEquivalentTo(user);
        result.UserId.Should().BeEquivalentTo(user.Id);
    }

    [Fact]
    public async Task Remove_ShouldInvoke_CacheRemove()
    {
        await Service.Remove("connection-id");

        CacheProvider.Received(1).Remove(Arg.Any<string>());
    }
}
