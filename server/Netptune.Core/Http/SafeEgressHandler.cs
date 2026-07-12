using Microsoft.Extensions.Options;
using Microsoft.Security.AntiSSRF;

using Netptune.Core.Exceptions;

namespace Netptune.Core.Http;

public sealed class SafeEgressHandler(IOptions<SafeHttpClientOptions> options) : DelegatingHandler
{
    private readonly SafeHttpClientOptions Options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(Options.Timeout);

        HttpResponseMessage? response = null;

        try
        {
            response = await base.SendAsync(request, timeout.Token);

            await response.Content.LoadIntoBufferAsync(Options.MaxResponseBytes, timeout.Token);

            return response;
        }
        catch (AntiSSRFException exception)
        {
            response?.Dispose();

            return UrlEgressBlockedException.Throw<HttpResponseMessage>("The supplied url is not permitted.", exception);
        }
        catch (HttpRequestException exception) when (exception.InnerException is AntiSSRFException)
        {
            response?.Dispose();

            return UrlEgressBlockedException.Throw<HttpResponseMessage>("The supplied url is not permitted.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            response?.Dispose();

            return UrlEgressBlockedException.Throw<HttpResponseMessage>(
                "The supplied url timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            response?.Dispose();

            return UrlEgressBlockedException.Throw<HttpResponseMessage>("The supplied url could not be read within the configured limits.", exception);
        }
        catch
        {
            response?.Dispose();

            throw;
        }
    }
}
