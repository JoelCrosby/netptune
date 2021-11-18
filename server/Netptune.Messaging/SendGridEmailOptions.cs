using Netptune.Core.Models.Messaging;

namespace Netptune.Messaging;

public class SendGridEmailOptions : EmailOptions
{
    public string SendGridApiKey { get; set; }
}