using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Configuration;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiToolRegistrationTests
{
    [Fact]
    public void EveryWriteTool_ShouldHaveAChangeHandler()
    {
        using var provider = CreateProvider();

        var tools = provider.GetServices<IAiTool>().Where(tool => tool.Kind == AiToolKind.Write).ToList();
        var handlerNames = provider.GetServices<IAiChangeHandler>().Select(handler => handler.ToolName).ToHashSet();
        var orphaned = tools.Where(tool => !handlerNames.Contains(tool.Name)).Select(tool => tool.Name).ToList();

        tools.Should().NotBeEmpty("this guard is worthless if no write tools resolve");
        orphaned.Should().BeEmpty("a write tool without a change handler proposes changes that can never be applied");
    }

    [Fact]
    public void EveryChangeHandler_ShouldHaveAWriteTool()
    {
        using var provider = CreateProvider();

        var toolNames = provider.GetServices<IAiTool>().Select(tool => tool.Name).ToHashSet();
        var handlers = provider.GetServices<IAiChangeHandler>().Select(handler => handler.ToolName).ToList();
        var orphaned = handlers.Where(name => !toolNames.Contains(name)).ToList();

        orphaned.Should().BeEmpty("a change handler with no tool is dead code");
    }

    [Fact]
    public void EveryTool_ShouldDeclareAtLeastOnePermission()
    {
        using var provider = CreateProvider();

        var tools = provider.GetServices<IAiTool>().ToList();
        var unguarded = tools.Where(tool => tool.RequiredPermissions.Count == 0).Select(tool => tool.Name).ToList();

        tools.Should().NotBeEmpty("this guard is worthless if no tools resolve");
        unguarded.Should().BeEmpty("a tool with no required permission is offered to every workspace member");
    }

    [Fact]
    public void TheQuestionTool_ShouldBeTheOnlyOneThatEndsATurn()
    {
        using var provider = CreateProvider();

        var asking = provider.GetServices<IAiTool>()
            .Where(tool => tool.Kind == AiToolKind.Question)
            .Select(tool => tool.Name)
            .ToList();

        asking.Should().Equal(
            ["ask_question"],
            "the runner stops the turn on a pending question, so a second asking tool would be unreachable");
    }

    [Fact]
    public void TheQuestionTool_ShouldBeOfferedToEveryMember()
    {
        using var provider = CreateProvider();

        var tool = provider.GetServices<IAiTool>().Single(item => item.Kind == AiToolKind.Question);
        var viewerPermissions = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer);

        tool.RequiredPermissions.Should().BeSubsetOf(
            viewerPermissions,
            "a viewer can chat with the assistant, so it must be able to ask them something");
    }

    [Fact]
    public void EveryToolName_ShouldBeUnique()
    {
        using var provider = CreateProvider();

        var names = provider.GetServices<IAiTool>().Select(tool => tool.Name).ToList();

        names.Should().OnlyHaveUniqueItems();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Substitute.For<IMediator>());
        services.AddSingleton(Substitute.For<INetptuneUnitOfWork>());
        services.AddSingleton(Substitute.For<IIdentityService>());
        services.AddSingleton(Substitute.For<IAiCredentialProtector>());
        services.AddSingleton(Substitute.For<IAiExecutionContext>());
        services.AddNetptuneAi(configuration);

        return services.BuildServiceProvider();
    }
}
