using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Identity.Authorization.Handlers;
using Netptune.Identity.Authorization.Requirements;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Identity.Authorization.Handlers;

public class WorkspaceAuthorizationHandlerTests
{
    private readonly WorkspaceAuthorizationHandler Handler;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    private const string WorkspaceKey = "test-workspace";
    private const string UserId = "user-123";

    public WorkspaceAuthorizationHandlerTests()
    {
        Handler = new WorkspaceAuthorizationHandler(Identity, Cache, UnitOfWork);
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, WorkspaceRequirement requirement)
    {
        return new AuthorizationHandlerContext([requirement], user, null);
    }

    private static ClaimsPrincipal AuthenticatedUserWithWorkspaceClaim() =>
        new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(NetptuneClaims.Workspace, WorkspaceKey),
        ], "Bearer"));

    private static ClaimsPrincipal AuthenticatedUserWithoutWorkspaceClaim() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "Bearer"));

    private static ClaimsPrincipal AnonymousUser() =>
        new(new ClaimsIdentity());

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_WhenAuthenticatedUserIsInWorkspace()
    {
        var user = AuthenticatedUserWithWorkspaceClaim();
        var requirement = new WorkspaceRequirement();

        Identity.GetCurrentUserId().Returns(UserId);
        Cache.IsUserInWorkspace(UserId, WorkspaceKey).Returns(true);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenAuthenticatedUserIsNotInWorkspace()
    {
        var user = AuthenticatedUserWithWorkspaceClaim();
        var requirement = new WorkspaceRequirement();

        Identity.GetCurrentUserId().Returns(UserId);
        Cache.IsUserInWorkspace(UserId, WorkspaceKey).Returns(false);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldNotQueryCache_WhenAuthenticatedUserHasNoWorkspaceClaim()
    {
        var user = AuthenticatedUserWithoutWorkspaceClaim();
        var requirement = new WorkspaceRequirement();

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        await Cache.DidNotReceive().IsUserInWorkspace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_WhenAnonymousUserRequestsPublicWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspaceRequirement();
        var workspace = AutoFixtures.Workspace with { IsPublic = true };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenAnonymousUserRequestsPrivateWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspaceRequirement();
        var workspace = AutoFixtures.Workspace with { IsPublic = false };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldNotQueryWorkspace_WhenAnonymousUserHasNoWorkspaceHeader()
    {
        var user = AnonymousUser();
        var requirement = new WorkspaceRequirement();

        Identity.TryGetWorkspaceKey().ReturnsNull();

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }
}
