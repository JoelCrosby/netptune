namespace Netptune.Core.Models.Messaging
{
    public class SendEmailModel
    {
        public string ToAddress  { get; set; }

        public string ToDisplayName { get; set; }

        public string Subject { get; set; }

        public string RawTextContent { get; set; }
    }
}
