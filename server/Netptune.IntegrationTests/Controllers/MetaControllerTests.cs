using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.App.Utility;
using Netptune.Core.ViewModels.Web;

using Xunit;

namespace Netptune.IntegrationTests.Controllers;

[Collection(Collections.Database)]
public sealed class MetaControllerTests
{
    private readonly HttpClient Client;

    public MetaControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetBuildInfo_ShouldReturnCorrectly()
    {
        var response = await Client.GetAsync("api/meta/build-info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BuildInfoViewModel>();

        result!.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUriMetaInfo_ShouldReturnCorrectly()
    {
        var response = await Client.GetAsync($"api/meta/uri-meta-info?url={WebUtility.UrlEncode("https://www.google.com")}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MetaInfoResponse>();

        result!.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Meta.Should().NotBeNull();
    }
}
