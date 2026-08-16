using FluentAssertions;

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
        Reason = "You received this because someone invited you to a workspace.",
    };

    [Fact]
    public async Task Render_ShouldProduceHtml_ContainingTheMessageAndAction()
    {
        var renderer = CreateRenderService();

        var html = await renderer.Render(Model);

        html.Should().Contain(Model.Message);
        html.Should().Contain(Model.Action);
        html.Should().Contain(Model.Link);
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
            Reason = Model.Reason,
        };

        var html = await renderer.Render(minimal);

        html.Should().Contain(minimal.Message);
    }

    private static IEmailRenderService CreateRenderService()
    {
        var services = new ServiceCollection();

        services.AddCloudflareEmailService(options =>
        {
            options.ApiToken = "token";
            options.AccountId = "account";
            options.DefaultFromAddress = "no-reply@netptune.co.uk";
            options.DefaultFromDisplayName = "Netptune";
        });

        return services.BuildServiceProvider().GetRequiredService<IEmailRenderService>();
    }
}
