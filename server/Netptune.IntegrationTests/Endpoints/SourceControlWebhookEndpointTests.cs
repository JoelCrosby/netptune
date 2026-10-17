using System.Net;
using System.Net.Http.Json;
using System.Text;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SourceControlWebhookEndpointTests
{
    private const string WebhookUrl = "api/webhooks/source-control/gitlab";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;
    private readonly HttpClient Anonymous;

    public SourceControlWebhookEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
        Anonymous = fixture.CreateAnonymousNetptuneClient("netptune");
    }

    [Fact]
    public async Task Webhook_ShouldRejectAnUnsignedDelivery()
    {
        var account = await ConnectAccount();

        var response = await PostWebhook(account, Guid.NewGuid().ToString(), signature: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredDeliveries(account)).Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_ShouldRejectABadSignature()
    {
        var account = await ConnectAccount();

        var response = await PostWebhook(account, Guid.NewGuid().ToString(), signature: "forged");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredDeliveries(account)).Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_ShouldReturnNotFoundForAnUnknownOrDisabledProvider()
    {
        var unknown = await Anonymous.PostAsync("api/webhooks/source-control/svn", null, TestContext.Current.CancellationToken);
        var disabled = await Anonymous.PostAsync("api/webhooks/source-control/github", null, TestContext.Current.CancellationToken);

        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        disabled.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Webhook_ShouldAcknowledgeButNotStoreDeliveriesForAnAccountNobodyConnected()
    {
        var account = UniqueAccount();

        var response = await PostWebhook(account, Guid.NewGuid().ToString());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await StoredDeliveries(account)).Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_ShouldStoreARedeliveredWebhookOnce()
    {
        var account = await ConnectAccount();
        var deliveryId = Guid.NewGuid().ToString();

        var first = await PostWebhook(account, deliveryId);
        var redelivered = await PostWebhook(account, deliveryId);

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        redelivered.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await StoredDeliveries(account)).Should().ContainSingle();
    }

    [Fact]
    public async Task Processing_ShouldApplyAnUninstallToTheConnection()
    {
        var account = await ConnectAccount();

        await PostWebhook(account, Guid.NewGuid().ToString(), eventName: TestSourceControlProvider.UninstallEvent);

        var delivery = (await StoredDeliveries(account)).Single();

        using (var scope = Fixture.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IScmWebhookProcessor>();

            await processor.Process(delivery.Id, TestContext.Current.CancellationToken);
        }

        using var verifyScope = Fixture.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<DataContext>();

        var connection = await context.SourceControlConnections
            .SingleAsync(stored => stored.ExternalAccountId == account, TestContext.Current.CancellationToken);

        var processed = await context.ScmWebhookDeliveries
            .SingleAsync(stored => stored.Id == delivery.Id, TestContext.Current.CancellationToken);

        connection.Status.Should().Be(ScmConnectionStatus.Revoked);
        processed.ProcessedAt.Should().NotBeNull();
        processed.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task Processing_ShouldIgnoreADeliveryAlreadyProcessed()
    {
        var account = await ConnectAccount();

        await PostWebhook(account, Guid.NewGuid().ToString(), eventName: TestSourceControlProvider.UninstallEvent);

        var delivery = (await StoredDeliveries(account)).Single();

        for (var run = 0; run < 2; run++)
        {
            using var scope = Fixture.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IScmWebhookProcessor>();

            await processor.Process(delivery.Id, TestContext.Current.CancellationToken);
        }

        var stored = (await StoredDeliveries(account)).Single();

        stored.Attempts.Should().Be(0);
        stored.ProcessedAt.Should().NotBeNull();
    }

    private static string UniqueAccount()
    {
        return $"hook-{Guid.NewGuid():N}"[..20];
    }

    private async Task<string> ConnectAccount()
    {
        var account = UniqueAccount();

        var request = new CompleteSourceControlConnectRequest
        {
            Provider = SourceControlProvider.GitLab,
            Parameters = new Dictionary<string, string> { ["account"] = account },
        };

        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            request,
            TestContext.Current.CancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);

        return account;
    }

    private async Task<HttpResponseMessage> PostWebhook(
        string account,
        string deliveryId,
        string? signature = TestSourceControlProvider.ValidSignature,
        string eventName = "push")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);

        request.Content = new StringContent($$"""{ "account": "{{account}}" }""", Encoding.UTF8, "application/json");
        request.Headers.Add(TestSourceControlProvider.DeliveryHeader, deliveryId);
        request.Headers.Add(TestSourceControlProvider.EventHeader, eventName);

        if (signature is not null)
        {
            request.Headers.Add(TestSourceControlProvider.SignatureHeader, signature);
        }

        var response = await Anonymous.SendAsync(request, TestContext.Current.CancellationToken);

        return response;
    }

    private async Task<List<ScmWebhookDelivery>> StoredDeliveries(string account)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var deliveries = await context.ScmWebhookDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.ExternalAccountId == account)
            .ToListAsync(TestContext.Current.CancellationToken);

        return deliveries;
    }
}
