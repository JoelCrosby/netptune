using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;
using Netptune.JobServer.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.JobServer;

public class SearchSeedServiceTests
{
    private const string WorkspaceSlug = "acme";

    private static readonly string[] ExistingIndexes = ["tasks", "projects", "boards", "sprints"];

    private readonly IMeilisearchService SearchService = Substitute.For<IMeilisearchService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly SearchSeedService Service;

    public SearchSeedServiceTests()
    {
        var provider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        provider.GetService(typeof(INetptuneUnitOfWork)).Returns(UnitOfWork);
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        UnitOfWork.Workspaces.GetWorkspaces(Arg.Any<CancellationToken>()).Returns([new Workspace { Slug = WorkspaceSlug, Name = "Acme" }]);
        UnitOfWork.Pages.GetAllSearchSources(WorkspaceSlug, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.Pages.GetOutline(WorkspaceSlug, Arg.Any<CancellationToken>()).Returns([]);

        foreach (var index in ExistingIndexes)
        {
            SearchService.IsIndexEmptyAsync(index, Arg.Any<CancellationToken>()).Returns(false);
        }

        Service = new SearchSeedService(SearchService, scopeFactory, NullLogger<SearchSeedService>.Instance);
    }

    [Fact]
    public async Task RunAsync_ShouldSeedOnlyTheEmptyIndex_WhenTheOthersAlreadyHoldDocuments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        SearchService.IsIndexEmptyAsync(PageSearchDocuments.IndexName, cancellationToken).Returns(true);

        await Service.RunAsync(cancellationToken);

        await UnitOfWork.Pages.Received(1).GetAllSearchSources(WorkspaceSlug, cancellationToken);
        await UnitOfWork.Tasks.DidNotReceive().GetAllTaskViewModels(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Projects.DidNotReceive().GetAllProjectViewModels(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ShouldSeedNothing_WhenEveryIndexHoldsDocuments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        SearchService.IsIndexEmptyAsync(PageSearchDocuments.IndexName, cancellationToken).Returns(false);

        await Service.RunAsync(cancellationToken);

        await UnitOfWork.Pages.DidNotReceive().GetAllSearchSources(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await SearchService.Received(1).EnsureIndexSettingsAsync(cancellationToken);
    }
}
