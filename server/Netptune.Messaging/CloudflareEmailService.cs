using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Services.Activity;

namespace Netptune.Messaging;

public class CloudflareEmailService : IEmailService
{
    private readonly IEmailRenderService EmailRenderer;
    private readonly IEventPublisher EventPublisher;
    private readonly CloudflareEmailOptions Options;
    private readonly IHttpClientFactory HttpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public CloudflareEmailService(
        IOptionsMonitor<CloudflareEmailOptions> options,
        IEmailRenderService emailRenderer,
        IEventPublisher eventPublisher,
        IHttpClientFactory httpClientFactory)
    {
        EmailRenderer = emailRenderer;
        EventPublisher = eventPublisher;
        Options = options.CurrentValue;
        HttpClientFactory = httpClientFactory;
    }

    public Task Send(SendEmailModel model)
    {
        EventPublisher.Dispatch(model);

        return Task.CompletedTask;
    }

    public Task Send(SendMultipleEmailModel model)
    {
        EventPublisher.Dispatch(model);

        return Task.CompletedTask;
    }

    public async Task EnqueueSend(SendEmailModel model)
    {
        var htmlContent = await EmailRenderer.Render(model);

        var payload = new CloudflareEmailPayload
        {
            From = Options.DefaultFromAddress,
            To = model.SendTo.Address,
            Subject = model.Subject,
            Html = htmlContent,
            Text = model.RawTextContent,
        };

        await SendRequest(payload);
    }

    public async Task EnqueueSend(SendMultipleEmailModel model)
    {
        var htmlContent = await EmailRenderer.Render(new SendEmailModel
        {
            Message = model.Message,
            Subject = model.Subject,
            Reason = model.Reason,
            Name = model.Name,
            Action = model.Action,
            RawTextContent = model.RawTextContent,
            PreHeader = model.PreHeader,
            Link = model.Link,
            SendTo = model.SendTo.First(),
        });

        var tasks = model.SendTo.Select(recipient =>
        {
            var payload = new CloudflareEmailPayload
            {
                From = Options.DefaultFromAddress,
                To = recipient.Address,
                Subject = model.Subject,
                Html = htmlContent,
                Text = model.RawTextContent,
            };

            return SendRequest(payload);
        });

        await Task.WhenAll(tasks);
    }

    private async Task SendRequest(CloudflareEmailPayload payload)
    {
        var client = HttpClientFactory.CreateClient();

        var url = $"https://api.cloudflare.com/client/v4/accounts/{Options.AccountId}/email/sending/send";

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiToken);

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            // TODO: handle failure
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private sealed class CloudflareEmailPayload
    {
        [JsonPropertyName("to")]
        public required string To { get; init; }

        [JsonPropertyName("from")]
        public required string From { get; init; }

        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("html")]
        public string? Html { get; init; }

        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}
