using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Audit;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AuditEndpointTests
{
    private readonly HttpClient Client;

    public AuditEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnAuditLog_WhenWorkspaceHasEvents()
    {
        await RecordExportEvent();

        var response = await Client.GetAsync("api/audit?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<AuditLogViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_ShouldFilterByActivityType_WhenActivityTypeSupplied()
    {
        await RecordExportEvent();

        var response = await Client.GetAsync(
            $"api/audit?page=1&pageSize=50&activityType={(int)ActivityType.ExportRequested}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<AuditLogViewModel>>>();

        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().OnlyContain(item => item.Type == ActivityType.ExportRequested);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnActivityPoints_WhenWorkspaceHasEvents()
    {
        await RecordExportEvent();

        var response = await Client.GetAsync("api/audit/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<AuditActivityPoint>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Sum(point => point.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDetail_ShouldReturnCorrectly_WhenEntryExists()
    {
        var entry = await RecordExportEvent();

        var response = await Client.GetAsync($"api/audit/{entry.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AuditLogDetailViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Id.Should().Be(entry.Id);
        result.Payload.EventKey.Should().NotBeNullOrWhiteSpace();
        result.Payload.RetentionClass.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDetail_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        var response = await Client.GetAsync("api/audit/9223372036854775807");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Export_ShouldReturnCsv_WhenInputValid()
    {
        await RecordExportEvent();

        var response = await Client.GetAsync("api/audit/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("OccurredAt");
    }

    private async Task<AuditLogViewModel> RecordExportEvent()
    {
        var export = await Client.PostAsJsonAsync("api/export/run", new
        {
            definition = new { recordType = "task", format = 0 },
        });

        export.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<AuditLogViewModel>>>(
            $"api/audit?page=1&pageSize=1&activityType={(int)ActivityType.ExportRequested}");

        return page.Payload!.Items.First();
    }
}
