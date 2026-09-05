using System.Net;

using Anthropic.Exceptions;

using FluentAssertions;

using Netptune.Ai.Providers;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiProviderErrorsTests
{
    private const string CreditBody =
        """
        {"type":"error","error":{"type":"invalid_request_error","message":"Your credit balance is too low to access the Anthropic API. Please go to Plans & Billing to upgrade or purchase credits."}}
        """;

    private const string QuotaBody =
        """
        {"error":{"message":"You exceeded your current quota, please check your plan and billing details.","type":"insufficient_quota"}}
        """;

    [Fact]
    public void Describe_ShouldReportABadRequestSpentAccountAsOutOfCredit()
    {
        var exception = CreateBadRequest(CreditBody);

        AiProviderErrors.Describe(exception).Should().Contain("out of credit");
    }

    [Fact]
    public void Describe_ShouldReportARateLimitedSpentAccountAsOutOfCredit()
    {
        var inner = new HttpRequestException(QuotaBody, null, HttpStatusCode.TooManyRequests);
        var exception = new AnthropicRateLimitException(inner)
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            ResponseBody = QuotaBody,
        };

        AiProviderErrors.Describe(exception).Should().Contain("out of credit");
    }

    [Fact]
    public void Describe_ShouldSeparateRateLimitingFromASpentAccount()
    {
        var body = """{"type":"error","error":{"type":"rate_limit_error","message":"Number of requests has exceeded your rate limit."}}""";
        var inner = new HttpRequestException(body, null, HttpStatusCode.TooManyRequests);
        var exception = new AnthropicRateLimitException(inner)
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            ResponseBody = body,
        };

        var described = AiProviderErrors.Describe(exception);

        described.Should().Contain("rate limiting");
        described.Should().NotContain("out of credit");
    }

    [Fact]
    public void Describe_ShouldNotReportABadRequestAsAnOutage()
    {
        var body = """{"type":"error","error":{"type":"invalid_request_error","message":"max_tokens is too large"}}""";
        var exception = CreateBadRequest(body);

        var described = AiProviderErrors.Describe(exception);

        described.Should().NotContain("unavailable");
        described.Should().Contain("max_tokens is too large");
    }

    [Fact]
    public void Describe_ShouldFallBackToRejectionWhenTheBodyCarriesNoReason()
    {
        var exception = CreateBadRequest("not json");

        AiProviderErrors.Describe(exception).Should().Be("The provider rejected the request.");
    }

    [Fact]
    public void Describe_ShouldKeepMappingKnownStatuses()
    {
        var inner = new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized);
        var exception = new AnthropicUnauthorizedException(inner)
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ResponseBody = "unauthorized",
        };

        AiProviderErrors.Describe(exception).Should().Contain("API key");
    }

    [Fact]
    public void Describe_ShouldIgnoreUnrelatedFailures()
    {
        AiProviderErrors.Describe(new InvalidOperationException("nope")).Should().BeNull();
    }

    private static AnthropicBadRequestException CreateBadRequest(string body)
    {
        var inner = new HttpRequestException(body, null, HttpStatusCode.BadRequest);

        return new AnthropicBadRequestException(inner)
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = body,
        };
    }
}
