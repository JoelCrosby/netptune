using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Messaging;

using Xunit;

namespace Netptune.UnitTests.Netptune.Messaging;

// RazorLight compiles the embedded template at runtime rather than at build time, so nothing
// fails until an email is actually sent. These render it through the real registration.
public class EmailRenderServiceTests
{
    private static readonly SendEmailModel Model = new()
    {
        SendTo = new() { Address = "someone@netptune.co.uk", DisplayName = "Someone" },
        Name = "Someone",
        Subject = "You have been invited",
        PreHeader = "An invitation is waiting",
        Message = "Joel invited you to the netptune workspace.",
        Link = "https://netptune.co.uk/invite/abc123",
        Action = "Accept invite",
        RawTextContent = "Joel invited you to the netptune workspace.",
        Greeting = "Hi Someone,",
        Footer = "You received this because someone invited you to a workspace.",
        Tagline = "Netptune task management",
        Language = "fr",
    };

    [Fact]
    public async Task Render_ShouldProduceHtml_ContainingTheMessageAndAction()
    {
        var renderer = CreateRenderService();

        var html = await renderer.Render(Model);

        html.Should().Contain(Model.Message);
        html.Should().Contain(Model.Action);
        html.Should().Contain(Model.Link);
        html.Should().Contain(Model.Greeting);
        html.Should().Contain(Model.Footer);
        html.Should().Contain(Model.Tagline);
        html.Should().Contain("lang=\"fr\"");
    }

    [Fact]
    public async Task Render_ShouldSucceed_WhenTheOptionalFieldsAreAbsent()
    {
        var renderer = CreateRenderService();
        var minimal = new SendEmailModel
        {
            SendTo = Model.SendTo,
            Name = Model.Name,
            Subject = Model.Subject,
            Message = Model.Message,
            RawTextContent = Model.RawTextContent,
        };

        var html = await renderer.Render(minimal);

        html.Should().Contain(minimal.Message);
    }

    private static IEmailRenderService CreateRenderService()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:DefaultFromAddress"] = "no-reply@netptune.co.uk",
                ["Email:DefaultFromDisplayName"] = "Netptune",
                ["Email:Cloudflare:ApiToken"] = "token",
                ["Email:Cloudflare:AccountId"] = "account",
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddCloudflareEmailService();

        return services.BuildServiceProvider().GetRequiredService<IEmailRenderService>();
    }
}
