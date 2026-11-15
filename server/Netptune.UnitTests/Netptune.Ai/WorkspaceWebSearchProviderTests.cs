using System.Net;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Web;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WorkspaceWebSearchProviderTests
{
    private const int WorkspaceId = 7;
    private const string SearchUrl = "https://search.test/";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IAiCredentialProtector Protector = Substitute.For<IAiCredentialProtector>();
    private readonly IWebSearchEngine Brave = Substitute.For<IWebSearchEngine>();
    private readonly StubHttpMessageHandler Http = new();

    public WorkspaceWebSearchProviderTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        UnitOfWork.WorkspaceSearchCredentials
            .GetForWorkspace(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceSearchCredential?)null);

        Brave.Provider.Returns(WebSearchProvider.Brave);
        Brave.Validate(Arg.Any<WebSearchCredential>()).Returns((string?)null);
        Brave
            .CreateRequest(Arg.Any<WebSearchCredential>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(_ => new HttpRequestMessage(HttpMethod.Get, SearchUrl));
        Brave
            .ReadHits(Arg.Any<JsonElement>())
            .Returns([new WebSearchHit { Title = "Result", Url = "https://example.test/" }]);

        Http.On(HttpMethod.Get, SearchUrl, HttpStatusCode.OK, "{}");
    }

    [Fact]
    public async Task Search_ShouldUseTheDevelopmentBraveKey_WhenTheWorkspaceHasNoSearchConnection()
    {
        var provider = CreateProvider(new AiDevelopmentKeys { BraveSearch = " brave-dev " });

        var result = await provider.Search("netptune", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Hits.Should().ContainSingle();
        Brave.Received(1).CreateRequest(
            Arg.Is<WebSearchCredential>(credential => credential.ApiKey == "brave-dev"),
            "netptune",
            5);
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_ShouldFail_WhenThereIsNoSearchConnectionOrDevelopmentKey()
    {
        var provider = CreateProvider(new AiDevelopmentKeys());

        var result = await provider.Search("netptune", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldPreferTheStoredConnection_AndStampIt()
    {
        var stored = new WorkspaceSearchCredential
        {
            WorkspaceId = WorkspaceId,
            Provider = WebSearchProvider.Brave,
            Secret = [1, 2, 3],
        };

        UnitOfWork.WorkspaceSearchCredentials.GetForWorkspace(WorkspaceId, Arg.Any<CancellationToken>()).Returns(stored);
        Protector.Unprotect(stored.Secret).Returns("brave-stored");

        var provider = CreateProvider(new AiDevelopmentKeys { BraveSearch = "brave-dev" });

        var result = await provider.Search("netptune", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        stored.LastUsedAt.Should().NotBeNull();
        Brave.Received(1).CreateRequest(
            Arg.Is<WebSearchCredential>(credential => credential.ApiKey == "brave-stored"),
            "netptune",
            5);
    }

    private WorkspaceWebSearchProvider CreateProvider(AiDevelopmentKeys developmentKeys)
    {
        var options = Options.Create(new AiOptions { DevelopmentKeys = developmentKeys });
        var client = new HttpClient(Http);

        return new WorkspaceWebSearchProvider(client, UnitOfWork, Identity, Protector, [Brave], options);
    }
}
