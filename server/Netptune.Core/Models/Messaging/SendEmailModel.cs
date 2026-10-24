using Netptune.Core.Events;
using Netptune.Core.Localization;

namespace Netptune.Core.Models.Messaging;

public class SendEmailModel : IEventMessage
{
    static string IEventMessage.Subject => MessageKeys.Subjects.Email;

    public SendTo SendTo { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Subject { get; init; } = null!;

    public string? PreHeader { get; init; }

    public string Message { get; init; } = null!;

    public string? Link { get; init; }

    public string? Action { get; init; }

    public string RawTextContent { get; init; } = null!;

    public string? Greeting { get; init; }

    public string? Footer { get; init; }

    public string Tagline { get; init; } = "Netptune";

    public string Language { get; init; } = AppLocales.Default;
}

public class SendTo
{
    public string Address { get; init; } = null!;

    public string DisplayName { get; init; } = null!;
}
