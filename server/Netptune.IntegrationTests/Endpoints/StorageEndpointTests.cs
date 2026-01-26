using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class StorageEndpointTests
{
    private readonly HttpClient Client;

    public StorageEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task UploadProfilePicture_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new ("api/storage/profile-picture", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(Stream.Null), "file", "picture.png" },
            },
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UploadResponse>>();

        result!.IsSuccess.Should().BeTrue();
    }
    [Fact]
    public async Task UploadMedia_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new ("api/storage/media", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(Stream.Null), "file", "media.jpg" },
            },
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UploadResponse>>();

        result!.IsSuccess.Should().BeTrue();
    }
}
