using FluentAssertions;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Commands;

public class ResendWorkspaceInviteCommandHandlerTests
{
    private readonly ResendWorkspaceInviteCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHostingService Hosting = Substitute.For<IHostingService>();

    public ResendWorkspaceInviteCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Email, Hosting);
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnSuccess_WhenPendingInviteExists()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var invite = new WorkspaceInvite { Email = "user@email.com", WorkspaceId = workspace.Id, Code = "existingcode", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        Hosting.ClientOrigin.Returns("https://test.example.com");
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), workspace.Id, Arg.Any<CancellationToken>()).Returns(invite);

        var result = await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnFailure_WhenNoPendingInviteFound()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResendInvite_ShouldRotateInviteCode()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var invite = new WorkspaceInvite { Email = "user@email.com", WorkspaceId = workspace.Id, Code = "oldcode", ExpiresAt = DateTime.UtcNow.AddDays(1) };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        Hosting.ClientOrigin.Returns("https://test.example.com");
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), workspace.Id, Arg.Any<CancellationToken>()).Returns(invite);

        await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        invite.Code.Should().NotBe("oldcode");
    }

    [Fact]
    public async Task ResendInvite_ShouldExtendExpiry()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var originalExpiry = DateTime.UtcNow.AddDays(1);
        var invite = new WorkspaceInvite { Email = "user@email.com", WorkspaceId = workspace.Id, Code = "code", ExpiresAt = originalExpiry };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        Hosting.ClientOrigin.Returns("https://test.example.com");
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), workspace.Id, Arg.Any<CancellationToken>()).Returns(invite);

        await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        invite.ExpiresAt.Should().BeAfter(originalExpiry);
    }

    [Fact]
    public async Task ResendInvite_ShouldSendEmail()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var invite = new WorkspaceInvite { Email = "user@email.com", WorkspaceId = workspace.Id, Code = "code", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        Hosting.ClientOrigin.Returns("https://test.example.com");
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), workspace.Id, Arg.Any<CancellationToken>()).Returns(invite);

        await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        await Email.Received(1).Send(Arg.Any<SendEmailModel>());
    }

    [Fact]
    public async Task ResendInvite_ShouldCallCompleteAsync()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var invite = new WorkspaceInvite { Email = "user@email.com", WorkspaceId = workspace.Id, Code = "code", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        Hosting.ClientOrigin.Returns("https://test.example.com");
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.WorkspaceInvites.GetPendingByEmail(Arg.Any<string>(), workspace.Id, Arg.Any<CancellationToken>()).Returns(invite);

        await Handler.Handle(
            new ResendWorkspaceInviteCommand("user@email.com"),
            TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
