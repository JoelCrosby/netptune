using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Extensions;
using Netptune.Core.Models.Import;
using Netptune.Core.Responses.Common;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ImportEndpointTests
{
    private readonly HttpClient Client;

    public ImportEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task ImportWorkspaceTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var userEmail = TestData.Users.ElementAt(0).Email;

        var import = $"""
            Name,assignees,owner,group
            task name,"{userEmail}","{userEmail}",done
            """;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new ("api/import/tasks/neovim", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(import.Trim().ToStream()), "file", "import.csv" },
            },
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskImportResult>>();

        result!.IsSuccess.Should().BeTrue();
    }
}
