using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PageTransformEndpointTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public PageTransformEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Transform_ShouldBeForbidden_WithoutAssistantUse()
    {
        var page = await CreatePage();
        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Assistant.Use);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var request = new TransformPageTextRequest { PageId = page.Id, Action = "summarise", Text = "Some text." };
        var response = await memberClient.PostAsJsonAsync("api/ai/pages/transform", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // No key is configured in the test workspace, so an allowed caller gets as far as the handler and is
    // refused there rather than by authorization.
    [Fact]
    public async Task Transform_ShouldReachTheHandler_ForACallerWhoMayUseTheAssistant()
    {
        var page = await CreatePage();
        var request = new TransformPageTextRequest { PageId = page.Id, Action = "summarise", Text = "Some text." };
        var response = await Client.PostAsJsonAsync("api/ai/pages/transform", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<PageViewModel> CreatePage()
    {
        var create = new CreatePageRequest { Kind = PageKind.Document, Title = $"Transform target {Guid.NewGuid():N}" };
        var response = await Client.PostAsJsonAsync("api/pages", create, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }
}
