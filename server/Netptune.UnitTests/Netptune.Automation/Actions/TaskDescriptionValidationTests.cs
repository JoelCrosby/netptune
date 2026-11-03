using FluentAssertions;

using Netptune.Automation.Actions;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Requests;

using Xunit;

namespace Netptune.UnitTests.Netptune.Automation.Actions;

public sealed class TaskDescriptionValidationTests
{
    private static readonly string TooLong = new('a', ProjectTaskDescription.MaxLength + 1);
    private static readonly string FitsOnceNormalized = new string('a', ProjectTaskDescription.MaxLength - 1) + "\r\n\r\n";

    [Fact]
    public void CreateTaskAction_ShouldRejectADescriptionOverTheLimit()
    {
        var error = new CreateTaskAction().Validate(CreateTaskRequest(TooLong));

        error.Should().Be(ProjectTaskDescription.TooLongMessage);
    }

    [Fact]
    public void CreateTaskAction_ShouldAcceptADescriptionThatFitsOnceNormalized()
    {
        var error = new CreateTaskAction().Validate(CreateTaskRequest(FitsOnceNormalized));

        error.Should().BeNull();
    }

    [Fact]
    public void UpdateTaskAction_ShouldRejectADescriptionOverTheLimit()
    {
        var error = new UpdateTaskAction().Validate(new AutomationActionRequest { TaskDescription = TooLong });

        error.Should().Be(ProjectTaskDescription.TooLongMessage);
    }

    [Fact]
    public void UpdateTaskAction_ShouldAcceptADescriptionThatFitsOnceNormalized()
    {
        var error = new UpdateTaskAction().Validate(new AutomationActionRequest { TaskDescription = FitsOnceNormalized });

        error.Should().BeNull();
    }

    private static AutomationActionRequest CreateTaskRequest(string description)
    {
        return new AutomationActionRequest { TaskName = "Follow up", TaskDescription = description };
    }
}
