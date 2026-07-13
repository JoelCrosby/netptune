namespace Netptune.Core.Events;

public static class MessageKeys
{
    public const string Queue = "netptune-events";

    public static class Subjects
    {
        public const string Activity = "netptune.activity";

        public const string Search = "netptune.search";

        public const string Email = "netptune.email";

        public const string Automation = "netptune.automation";

        public const string Typed = "netptune.>";
    }

    public static class Consumers
    {
        public const string Activity = "netptune-activity-consumer";

        public const string Jobs = "netptune-jobs-consumer";
    }
}
