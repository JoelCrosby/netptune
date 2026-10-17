using Mediator;

using Netptune.App.Configuration;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Handlers.SourceControl.Webhooks;

namespace Netptune.App.Endpoints;

public static class SourceControlWebhookEndpoints
{
    // GitHub caps payloads at 25 MB, but the events Netptune subscribes to stay far below this.
    private const int MaxBodyBytes = 5 * 1024 * 1024;

    public static RouteGroupBuilder MapSourceControlWebhookEndpoints(this RouteGroupBuilder builder)
    {
        // Anonymous because the provider authenticates with a signature over the body, which the
        // handler checks before anything is stored.
        builder.MapPost("/webhooks/source-control/{provider}", HandleWebhook)
            .WithTags("Source control webhooks")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimiterConfiguration.WebhooksPolicyName);

        return builder;
    }

    private static async Task<IResult> HandleWebhook(
        string provider,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var isKnownProvider = SourceControlProviderSlugs.TryParse(provider, out var providerKind);

        if (!isKnownProvider)
        {
            return Results.NotFound();
        }

        var body = await ReadBody(request, cancellationToken);

        if (body is null)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var webhookRequest = new ScmWebhookRequest
        {
            Body = body,
            Headers = headers,
        };

        var command = new ReceiveSourceControlWebhookCommand(providerKind, webhookRequest);
        var outcome = await mediator.Send(command, cancellationToken);

        return outcome switch
        {
            SourceControlWebhookOutcome.NotEnabled => Results.NotFound(),
            SourceControlWebhookOutcome.InvalidSignature => Results.Unauthorized(),
            SourceControlWebhookOutcome.Unroutable => Results.BadRequest(),
            _ => Results.Accepted(),
        };
    }

    private static async Task<byte[]?> ReadBody(HttpRequest request, CancellationToken cancellationToken)
    {
        var isDeclaredTooLarge = request.ContentLength > MaxBodyBytes;

        if (isDeclaredTooLarge)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];

        while (true)
        {
            var read = await request.Body.ReadAsync(chunk, cancellationToken);

            if (read == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + read > MaxBodyBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }
    }
}
