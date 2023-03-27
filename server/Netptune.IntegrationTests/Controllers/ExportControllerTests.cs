using System.Net;

using FluentAssertions;

using Xunit;

namespace Netptune.IntegrationTests.Controllers;

[Collection(Collections.Database)]
public sealed class ExportControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public ExportControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task ExportWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/export/tasks/export-workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentLength!.HasValue.Should().BeTrue();
        response.Content.Headers.ContentLength!.Value.Should().BeGreaterThan(0);

        response.Content.Headers.ContentDisposition!.FileName
            .Should()
            .MatchRegex(@"Netptune-Task-Export_.*-.{0,16}\.csv");
    }

    [Fact]
    public async Task ExportBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/export/tasks/export-board/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentLength!.HasValue.Should().BeTrue();
        response.Content.Headers.ContentLength!.Value.Should().BeGreaterThan(0);

        response.Content.Headers.ContentDisposition!.FileName
            .Should()
            .MatchRegex(@"Netptune-Task-Export_.*-.{0,16}\.csv");
    }
}
