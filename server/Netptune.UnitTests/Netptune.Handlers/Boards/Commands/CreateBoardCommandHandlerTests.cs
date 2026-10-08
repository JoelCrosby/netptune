using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Boards.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Boards.Commands;

public class CreateBoardCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public CreateBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity, Identity);
        UnitOfWork.Statuses
            .GetAllInWorkspace(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardRequest>()
            .Without(item => item.TemplateKey)
            .Create();
        var project = AutoFixtures.Project;

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(project);

        var result = await Handler.Handle(new CreateBoardCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier.ToUrlSlug());
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardRequest>()
            .Without(item => item.TemplateKey)
            .Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        await Handler.Handle(new CreateBoardCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldSeedTheTemplateGroups_ByDefault()
    {
        var board = await CreateCapturingBoard(seedDefaultGroups: true);

        board.BoardGroups.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_ShouldLeaveTheBoardWithoutGroups_WhenDefaultsAreNotWanted()
    {
        var board = await CreateCapturingBoard(seedDefaultGroups: false);

        board.BoardGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddBoardRequest>()
            .Without(item => item.TemplateKey)
            .Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateBoardCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    private async Task<Board> CreateCapturingBoard(bool seedDefaultGroups)
    {
        var request = Fixture.Build<AddBoardRequest>()
            .Without(item => item.TemplateKey)
            .Create();
        Board? added = null;

        UnitOfWork.Boards.AddAsync(Arg.Do<Board>(board => added = board), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        await Handler.Handle(new CreateBoardCommand(request, seedDefaultGroups), TestContext.Current.CancellationToken);

        return added!;
    }
}
