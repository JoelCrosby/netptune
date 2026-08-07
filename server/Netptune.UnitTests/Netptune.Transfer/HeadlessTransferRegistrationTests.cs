using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Export;
using Netptune.Import;
using Netptune.Services.Configuration;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Transfer;

// The job server has no HTTP request, so it takes its actor from the queued message instead. These pin
// the registration that makes that work: without a background IIdentityService the container cannot
// construct ActivityLogger, and ValidateOnBuild takes the whole job server down at startup.
public class HeadlessTransferRegistrationTests
{
    [Fact]
    public void ImportServices_ShouldConstruct_WithABackgroundActivityLogger()
    {
        using var provider = CreateJobServerProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IImportApplier>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IImportUndoService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IArchiveImporter>().Should().NotBeNull();
    }

    [Fact]
    public void ExportServices_ShouldConstruct_WithABackgroundActivityLogger()
    {
        using var provider = CreateJobServerProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IExportRunner>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IArchiveExporter>().Should().NotBeNull();
    }

    [Fact]
    public void TransferRegistrations_ShouldAllValidate_WhenTheContainerIsBuiltLikeTheJobServer()
    {
        var services = JobServerServices();
        var build = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        build.Should().NotThrow("the job server builds its container with validation turned on");
    }

    [Fact]
    public void Identity_ShouldResolveTheActorTheQueuedMessageNamed()
    {
        using var provider = CreateJobServerProvider();
        using var scope = provider.CreateScope();

        var actor = scope.ServiceProvider.GetRequiredService<IActorContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        identity.TryGetCurrentUserId().Should().BeNull("nothing has claimed the scope yet");

        using (actor.Begin(new ActorIdentity("user-7", 3, "netptune")))
        {
            identity.GetCurrentUserId().Should().Be("user-7");
            identity.GetWorkspaceKey().Should().Be("netptune");
            identity.GetWorkspaceId().GetAwaiter().GetResult().Should().Be(3);
        }

        identity.TryGetCurrentUserId().Should().BeNull("the scope was disposed");
    }

    private static ServiceProvider CreateJobServerProvider()
    {
        return JobServerServices().BuildServiceProvider();
    }

    private static ServiceCollection JobServerServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<INetptuneUnitOfWork>());
        services.AddSingleton(Substitute.For<IEventRecordWriter>());
        services.AddSingleton(Substitute.For<IEventPublisher>());
        services.AddSingleton(Substitute.For<IStorageService>());
        services.AddSingleton(Substitute.For<IUserCache>());
        services.AddSingleton(Substitute.For<IImportSessionRepository>());
        services.AddSingleton(Substitute.For<IArchiveRepository>());
        services.AddSingleton(Substitute.For<ITransferRepository>());
        services.AddSingleton(Substitute.For<IExportJobRepository>());
        services.AddSingleton(Substitute.For<IExportDefinitionRepository>());
        services.AddNetptuneEventRecording();
        services.AddNetptuneBackgroundIdentity();
        services.AddNetptuneImport();
        services.AddNetptuneExport();

        return services;
    }
}
