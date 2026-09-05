using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Ai;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class AiSpendEndpointTests
{
    private readonly NetptuneFixture Fixture;

    public AiSpendEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Spend_ShouldReportTheCurrentMonth()
    {
        var client = Fixture.CreateNetptuneClient();
        var summary = await GetSummary(client);
        var today = DateTime.UtcNow.Date;

        summary.PeriodStart.Date.Should().Be(new DateTime(today.Year, today.Month, 1));
        summary.PeriodEnd.Should().Be(summary.PeriodStart.AddMonths(1));
        summary.Daily.Should().HaveCount(today.Day);
        summary.MonthToDate.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public async Task SpendCap_ShouldRoundTrip()
    {
        var client = Fixture.CreateNetptuneClient();

        try
        {
            var saved = await SetCap(client, 25m);

            saved.Cap.Should().Be(25m);

            var summary = await GetSummary(client);

            summary.Cap.Should().Be(25m);
        }
        finally
        {
            await SetCap(client, null);
        }
    }

    [Fact]
    public async Task SpendCap_ShouldClear_WhenSetToNull()
    {
        var client = Fixture.CreateNetptuneClient();

        await SetCap(client, 25m);

        var cleared = await SetCap(client, null);

        cleared.Cap.Should().BeNull();
    }

    [Fact]
    public async Task SpendCap_ShouldRefuse_WhenTheCapIsNotPositive()
    {
        var client = Fixture.CreateNetptuneClient();
        var response = await client.PutAsJsonAsync("api/ai/admin/spend-cap", new { cap = 0m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<AiSpendViewModel> GetSummary(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<ClientResponse<AiSpendViewModel>>("api/ai/admin/spend");

        response!.IsSuccess.Should().BeTrue();

        return response.Payload!;
    }

    private static async Task<AiSpendViewModel> SetCap(HttpClient client, decimal? cap)
    {
        var response = await client.PutAsJsonAsync("api/ai/admin/spend-cap", new { cap });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await response.Content.ReadFromJsonAsync<ClientResponse<AiSpendViewModel>>();

        saved!.IsSuccess.Should().BeTrue();

        return saved.Payload!;
    }
}
