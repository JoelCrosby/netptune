using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class BoardGroupServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly BoardGroupService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public BoardGroupServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Service.GetBoardGroup(1);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(boardGroup);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateBoardGroupRequest>()
            .Create();

        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Service.Update(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateBoardGroupRequest>()
            .Create();

        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        await Service.Update(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture
            .Build<UpdateBoardGroupRequest>()
            .Create();

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddBoardGroupRequest>()
            .Create();

        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).Returns(board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

        var result = await Service.Create(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<AddBoardGroupRequest>()
            .Create();

        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).Returns(board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

        await Service.Create(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenBoardNotFound()
    {
        var request = Fixture
            .Build<AddBoardGroupRequest>()
            .Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).ReturnsNull();
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).Returns(boardGroup);

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).Returns(boardGroup);

        await Service.Delete(1);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).ReturnsNull();

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
