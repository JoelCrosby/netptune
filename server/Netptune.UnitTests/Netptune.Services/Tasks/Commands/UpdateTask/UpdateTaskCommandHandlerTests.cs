using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Tasks.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class UpdateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly ILogger<UpdateTaskCommandHandler> Logger = Substitute.For<ILogger<UpdateTaskCommandHandler>>();

    public UpdateTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity, Logger);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = AutoFixtures.ProjectTask;
        var viewModel = new TaskViewModel
        {
            Name = request.Name!,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        UnitOfWork.Tasks.GetAsync(request.Id).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(Arg.Any<int>()).Returns(AutoFixtures.BoardGroups);
        UnitOfWork.Boards.GetDefaultBoardInProject(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);
        UnitOfWork.InvokeTransaction();

        var result = await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldReturnFailed_WhenIdNotFound()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        UnitOfWork.Tasks.GetAsync(request.Id).ReturnsNull();

        var result = await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldCallTransaction_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = AutoFixtures.ProjectTask;
        var viewModel = new TaskViewModel
        {
            Name = request.Name!,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        UnitOfWork.Tasks.GetAsync(request.Id).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(Arg.Any<int>()).Returns(AutoFixtures.BoardGroups);
        UnitOfWork.Boards.GetDefaultBoardInProject(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);
        UnitOfWork.InvokeTransaction();

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }
}
