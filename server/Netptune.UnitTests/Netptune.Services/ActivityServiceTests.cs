using AutoFixture;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Activity;
using Netptune.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class ActivityServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly ActivityService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public ActivityServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetActivities_ShouldReturnCorrectly_WhenValidId()
    {
        UnitOfWork.ActivityLogs.GetActivities(Arg.Any<EntityType>(), Arg.Any<int>())
            .Returns(new List<ActivityViewModel>
            {
                Fixture.Build<ActivityViewModel>().Without(x => x.Meta).Create(),
            });

        Identity.GetWorkspaceId().Returns(1);
        UnitOfWork.Users.GetUserAvatars(Arg.Any<IEnumerable<string>>(), Arg.Any<int>())
            .Returns(new List<UserAvatar>
            {
                Fixture.Create<UserAvatar>(),
            });

        var result = await Service.GetActivities(EntityType.Task, 1);

        result.IsSuccess.Should().BeTrue();
    }
}
