using FluentAssertions;

using Netptune.Core.Localization;
using Netptune.Core.Models.Messaging;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Models.Messaging;

public class EmailMessagesTests
{
    private static readonly SendTo Recipient = new() { Address = "ada@netptune.co.uk", DisplayName = "Ada Lovelace" };

    [Fact]
    public void PasswordReset_ShouldBeWrittenInTheCurrentLanguage()
    {
        using var culture = CultureScope.Use("fr");

        var email = EmailMessages.PasswordReset(Recipient, "Ada", "https://netptune.co.uk/reset");

        email.Language.Should().Be("fr");
        email.Subject.Should().Be("Réinitialiser votre mot de passe");
        email.Greeting.Should().Be("Bonjour Ada,");
        email.RawTextContent.Should().EndWith("https://netptune.co.uk/reset");
    }

    [Fact]
    public void Welcome_ShouldKeepTheMessageParagraphs()
    {
        var email = EmailMessages.Welcome(Recipient, "Ada", "https://netptune.co.uk/confirm");

        email.Language.Should().Be("en-GB");
        email.Message.Should().Contain("\n\n", "the template renders each paragraph separately");
    }

    [Fact]
    public void WorkspaceInvite_ShouldNameTheWorkspace_AndGreetWithoutAName()
    {
        using var culture = CultureScope.Use("es");

        var email = EmailMessages.WorkspaceInvite("new@netptune.co.uk", "Acme", "https://netptune.co.uk/invite");

        email.Greeting.Should().Be("Hola:");
        email.Message.Should().Contain("Acme");
        email.RawTextContent.Should().EndWith("https://netptune.co.uk/invite");
        email.SendTo.Address.Should().Be("new@netptune.co.uk");
    }
}
