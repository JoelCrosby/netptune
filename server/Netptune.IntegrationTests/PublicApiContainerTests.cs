using FluentAssertions;

using Mediator;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Netptune.IntegrationTests;

// Mediator registers every handler in Netptune.Handlers into whichever host references it, so the
// public API host carries registrations for app-only handlers it never maps. That is why its
// Program.cs turns ValidateOnBuild off. This test keeps the resulting hole measured: the public
// API must be able to construct every handler except the ones needing a subsystem it deliberately
// does not wire up. A handler breaking for any other reason shows up here as a new entry.
public sealed class PublicApiContainerTests
{
    private static readonly string[] SubsystemsNotHostedByThePublicApi =
    [
        "Netptune.Core.Messaging.IEmailService",
        "Netptune.Core.Preferences.IPreferenceDefinitionRegistry",
        "Netptune.Core.Services.Ai.IAiTurnRegistry",
        "Netptune.Core.Services.Ai.IAiUndoCatalog",
        "Netptune.Core.Services.Automations.IAutomationTriggerEvaluator",
        "Netptune.Core.Services.IStorageService",
        "Netptune.Transfer.Services.IArchiveImporter",
        "Netptune.Transfer.Services.IExportRunner",
        "Netptune.Transfer.Services.IImportMappingAdvisor",
        "Netptune.Transfer.Services.IImportSourceStore",
        "Netptune.Transfer.Services.IImportUndoService",
    ];

    private readonly NetptuneFixture Fixture;

    public PublicApiContainerTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public void PublicApiHandlers_ShouldOnlyFailToResolve_ForSubsystemsItDoesNotHost()
    {
        var handlerTypes = typeof(Netptune.Handlers.ServiceCollectionExtensions).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(ImplementsRequestHandler)
            .ToList();

        handlerTypes.Should().NotBeEmpty();

        using var scope = Fixture.PublicApiServices.CreateScope();

        var unresolvable = handlerTypes
            .Select(type => MissingDependency(scope.ServiceProvider, type))
            .Where(missing => missing is not null)
            .Distinct()
            .Order()
            .ToList();

        unresolvable.Should().BeEquivalentTo(SubsystemsNotHostedByThePublicApi);
    }

    private static bool ImplementsRequestHandler(Type type)
    {
        return type.GetInterfaces().Any(contract =>
            contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    }

    private static string? MissingDependency(IServiceProvider services, Type handlerType)
    {
        try
        {
            ActivatorUtilities.CreateInstance(services, handlerType);

            return null;
        }
        catch (InvalidOperationException exception)
        {
            return TypeNameFromResolutionFailure(exception.Message);
        }
    }

    private static string? TypeNameFromResolutionFailure(string message)
    {
        const string prefix = "Unable to resolve service for type '";

        var start = message.IndexOf(prefix, StringComparison.Ordinal);

        if (start < 0)
        {
            return message;
        }

        var nameStart = start + prefix.Length;
        var nameEnd = message.IndexOf('\'', nameStart);

        return message[nameStart..nameEnd];
    }
}
