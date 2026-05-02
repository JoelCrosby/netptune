using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Commands;

public class CreateBoardCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardRequest>().Create();
        var project = AutoFixtures.Project;

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(project);

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
        var request = Fixture.Build<AddBoardRequest>().Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        await Handler.Handle(new CreateBoardCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddBoardRequest>().Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateBoardCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
