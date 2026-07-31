using FluentAssertions;

using Microsoft.Extensions.Caching.Distributed;

using Netptune.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Cache;

public class WorkspaceUserCacheTests
{
    private readonly WorkspaceUserCache Cache;
    private readonly ICacheProvider CacheProvider = Substitute.For<ICacheProvider>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public WorkspaceUserCacheTests()
    {
        Cache = new(CacheProvider, UnitOfWork);

        CacheProvider.TryGetValueAsync<bool>(Arg.Any<string>()).Returns((false, false));
    }

    [Fact]
    public async Task IsUserInWorkspace_ShouldCacheMembership_WhenTheUserIsAMember()
    {
        UnitOfWork.Users.IsUserInWorkspace("user", "workspace").Returns(true);

        var result = await Cache.IsUserInWorkspace("user", "workspace");

        result.Should().BeTrue();

        await CacheProvider.Received(1).SetAsync(
            "workspace:workspace:user",
            true,
            Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task IsUserInWorkspace_ShouldNotCacheTheAnswer_WhenTheUserIsNotAMember()
    {
        UnitOfWork.Users.IsUserInWorkspace("user", "workspace").Returns(false);

        var result = await Cache.IsUserInWorkspace("user", "workspace");

        result.Should().BeFalse();

        await CacheProvider.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<DistributedCacheEntryOptions>());
    }
}
