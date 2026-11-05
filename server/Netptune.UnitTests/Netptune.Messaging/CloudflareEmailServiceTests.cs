using System.Net;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Services.Activity;
using Netptune.Messaging;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Messaging;

public class CloudflareEmailServiceTests
{
    private const string SendUrl = "https://api.cloudflare.com/client/v4/accounts/account-123/email/sending/send";

    private const string RenderedHtml = "<html><body>Rendered</body></html>";

    private static readonly SendEmailModel Model = new()
    {
        SendTo = new() { Address = "someone@netptune.co.uk", DisplayName = "Someone" },
        Name = "Someone",
        Subject = "Welcome to Netptune",
        Message = "Thanks for registering with Netptune.",
        RawTextContent = "Thanks for registering with Netptune. https://netptune.co.uk/auth/confirm",
    };

    private readonly StubHttpMessageHandler Http = new();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly ILogger<CloudflareEmailService> Logger = Substitute.For<ILogger<CloudflareEmailService>>();
    private readonly CloudflareEmailService Service;

    public CloudflareEmailServiceTests()
    {
        var options = Substitute.For<IOptionsMonitor<CloudflareEmailOptions>>();
        var renderer = Substitute.For<IEmailRenderService>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();

        options.CurrentValue.Returns(new CloudflareEmailOptions
        {
            AccountId = "account-123",
            ApiToken = "token-abc",
            DefaultFromAddress = "noreply@netptune.co.uk",
            DefaultFromDisplayName = "Netptune",
        });
        renderer.Render(Model).Returns(RenderedHtml);
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(Http));

        Service = new CloudflareEmailService(options, renderer, EventPublisher, httpClientFactory, Logger);
    }

    [Fact]
    public async Task EnqueueSend_ShouldPostTheRenderedEmailToCloudflare()
    {
        Http.On(HttpMethod.Post, SendUrl, HttpStatusCode.OK, "{\"success\":true}");

        await Service.EnqueueSend(Model);

        var request = Http.Requests.Should().ContainSingle().Subject;
        var body = JsonDocument.Parse(request.Body!).RootElement;

        request.Authorization.Should().Be("Bearer token-abc");
        body.GetProperty("to").GetString().Should().Be(Model.SendTo.Address);
        body.GetProperty("from").GetString().Should().Be("noreply@netptune.co.uk");
        body.GetProperty("subject").GetString().Should().Be(Model.Subject);
        body.GetProperty("html").GetString().Should().Be(RenderedHtml);
        body.GetProperty("text").GetString().Should().Be(Model.RawTextContent);
    }

    [Fact]
    public async Task EnqueueSend_ShouldLogTheRejection_WhenCloudflareRejectsTheEmail()
    {
        Http.On(HttpMethod.Post, SendUrl, HttpStatusCode.BadRequest, "{\"success\":false}");

        var send = () => Service.EnqueueSend(Model);

        await send.Should().NotThrowAsync();

        Logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task Send_ShouldQueueTheEmail_WithoutCallingCloudflare()
    {
        await Service.Send(Model);

        await EventPublisher.Received(1).Dispatch(Model);
        Http.Requests.Should().BeEmpty();
    }
}
