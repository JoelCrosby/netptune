using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Actions;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Automations.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Automations.Queries;

public class ValidateAutomationRuleQueryHandlerTests
{
    private const int WorkspaceId = 123;
    private const int StatusId = 9;
    private const string ServiceUserId = "service-user";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IServiceAccountRepository ServiceAccounts = Substitute.For<IServiceAccountRepository>();
    private readonly IStatusRepository Statuses = Substitute.For<IStatusRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly ValidateAutomationRuleQueryHandler Handler;

    public ValidateAutomationRuleQueryHandlerTests()
    {
        UnitOfWork.ServiceAccounts.Returns(ServiceAccounts);
        UnitOfWork.Statuses.Returns(Statuses);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.GetWorkspaceKey().Returns("workspace");
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = WorkspaceRole.Owner,
            Permissions = [],
        });
        ServiceAccounts.GetAutomationPrincipal(ServiceUserId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new AutomationExecutionPrincipal
            {
                UserId = ServiceUserId,
                IsEnabled = true,
                Permissions = NetptunePermissions.All,
            });
        Statuses.GetInWorkspace(StatusId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Status { Id = StatusId, Name = "Done" });

        var services = new ServiceCollection();
        services.AddNetptuneAutomationActions();
        var actionRegistry = services.BuildServiceProvider().GetRequiredService<IAutomationActionRegistry>();

        Handler = new ValidateAutomationRuleQueryHandler(UnitOfWork, Identity, actionRegistry, PermissionCache);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenTheRuleIsValid()
    {
        var result = await Validate(BuildRequest());

        result.IsSuccess.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheRuleShapeIsInvalid()
    {
        var request = BuildRequest() with { Actions = [] };

        var result = await Validate(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("At least one automation action is required.");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheServiceAccountIsNotInTheWorkspace()
    {
        var request = BuildRequest() with { ExecutionUserId = "someone-else" };

        var result = await Validate(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("does not belong to this workspace");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAnActionReferencesAStatusOutsideTheWorkspace()
    {
        Statuses.GetInWorkspace(StatusId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns((Status?)null);

        var result = await Validate(BuildRequest());

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be($"Status with id {StatusId} was not found in the workspace.");
    }

    private async Task<ClientResponse> Validate(AutomationRuleRequest request)
    {
        return await Handler.Handle(new ValidateAutomationRuleQuery(request), TestContext.Current.CancellationToken);
    }

    private static AutomationRuleRequest BuildRequest()
    {
        return new AutomationRuleRequest
        {
            Name = "Close finished tasks",
            ExecutionUserId = ServiceUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskCreated,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.UpdateTask,
                    StatusId = StatusId,
                },
            ],
        };
    }
}
