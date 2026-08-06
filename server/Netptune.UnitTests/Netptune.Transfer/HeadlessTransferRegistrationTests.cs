using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Transfer.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Export;
using Netptune.Transfer.Repositories;
using Netptune.Import;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Transfer;

// The job server has no HTTP request, so it cannot supply IIdentityService and therefore cannot build
// ActivityLogger. Everything transfer registers has to construct without one — registering the real
// logger there once took the whole job server down at startup, because ValidateOnBuild rejects a
// descriptor it cannot construct even when nothing resolves it.
public class HeadlessTransferRegistrationTests
{
    [Fact]
    public void ImportServices_ShouldConstruct_WithoutAnActivityLogger()
    {
        using var provider = CreateHeadlessProvider();

        provider.GetRequiredService<IImportApplier>().Should().NotBeNull();
        provider.GetRequiredService<IImportUndoService>().Should().NotBeNull();
        provider.GetRequiredService<IArchiveImporter>().Should().NotBeNull();
    }

    [Fact]
    public void ExportServices_ShouldConstruct_WithoutAnActivityLogger()
    {
        using var provider = CreateHeadlessProvider();

        provider.GetRequiredService<IExportRunner>().Should().NotBeNull();
        provider.GetRequiredService<IArchiveExporter>().Should().NotBeNull();
    }

    [Fact]
    public void TransferRegistrations_ShouldAllValidate_WhenTheContainerIsBuiltLikeTheJobServer()
    {
        var services = HeadlessServices();
        var build = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        build.Should().NotThrow("the job server builds its container with validation turned on");
    }

    private static ServiceProvider CreateHeadlessProvider()
    {
        return HeadlessServices().BuildServiceProvider();
    }

    private static ServiceCollection HeadlessServices()
    {
        var services = new ServiceCollection();

        // Deliberately no IIdentityService and no IActivityLogger — this is the job server's container.
        services.AddSingleton(Substitute.For<INetptuneUnitOfWork>());
        services.AddSingleton(Substitute.For<IEventRecordWriter>());
        services.AddSingleton(Substitute.For<IEventPublisher>());
        services.AddSingleton(Substitute.For<IStorageService>());
        services.AddSingleton(Substitute.For<IImportSessionRepository>());
        services.AddSingleton(Substitute.For<IArchiveRepository>());
        services.AddSingleton(Substitute.For<ITransferRepository>());
        services.AddSingleton(Substitute.For<IExportJobRepository>());
        services.AddSingleton(Substitute.For<IExportDefinitionRepository>());
        services.AddNetptuneImport();
        services.AddNetptuneExport();

        return services;
    }
}
