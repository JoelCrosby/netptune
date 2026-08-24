using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
using Netptune.Core.Meta;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Boards.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Boards.Commands;

public class UpdateBoardCommandHandlerTests
{
    private const int WorkspaceId = 7;

    private readonly Fixture Fixture = new();
    private readonly UpdateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateBoardCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>()
            .With(x => x.Meta, new BoardMeta { Color = "blue" })
            .Create();
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);

        var result = await Handler.Handle(new UpdateBoardCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier!.ToUrlSlug());
        result.Payload.MetaInfo!.Color.Should().Be("blue");
    }

    [Fact]
    public async Task Update_ShouldKeepBrandingImages_WhenTheRequestMetaOmitsThem()
    {
        var request = Fixture.Build<UpdateBoardRequest>()
            .With(x => x.Meta, new BoardMeta { Color = "blue" })
            .Create();
        var board = AutoFixtures.Board;

        board.MetaInfo = new BoardMeta
        {
            Color = "red",
            LogoFileId = "logo-content-id",
            BackgroundFileId = "background-content-id",
        };

        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);

        var result = await Handler.Handle(new UpdateBoardCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.MetaInfo!.Color.Should().Be("blue");
        result.Payload.MetaInfo.LogoFileId.Should().Be("logo-content-id");
        result.Payload.MetaInfo.BackgroundFileId.Should().Be("background-content-id");
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);

        await Handler.Handle(new UpdateBoardCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateBoardCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
