using System.Collections.Generic;

namespace Netptune.Core.Models.Messaging;

public class SendEmailModel
{
    public SendTo SendTo { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Subject { get; init; } = null!;

    public string? PreHeader { get; init; }

    public string Message { get; init; } = null!;

    public string? Link { get; init; }

    public string? Action { get; init; }

    public string RawTextContent { get; init; } = null!;

    public string Reason { get; init; } = null!;
}

public class SendEmailModelMultiple
{
    public List<string> ToAddress { get; init; } = null!;

    public string ToDisplayName { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Subject { get; init; } = null!;

    public string PreHeader { get; init; } = null!;

    public string Message { get; init; } = null!;

    public string? Link { get; init; }

    public string? Action { get; init; }

    public string? RawTextContent { get; init; }
}

public class SendTo
{
    public string Address { get; init; } = null!;

    public string DisplayName { get; init; }  = null!;
}
