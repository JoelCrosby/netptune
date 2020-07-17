namespace Netptune.Core.Models.Messaging
{
    public class SendEmailModel
    {
        public string ToAddress  { get; set; }

        public string ToDisplayName { get; set; }

        public string Name { get; set; }

        public string Subject { get; set; }

        public string PreHeader { get; set; }

        public string Message { get; set; }

        public string Link { get; set; }

        public string Action { get; set; }

        public string RawTextContent { get; set; }
    }
}
