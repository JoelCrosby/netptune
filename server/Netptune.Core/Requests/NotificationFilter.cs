namespace Netptune.Core.Requests;

public class NotificationFilter : PageRequest
{
    public string? Search { get; init; }

    public string? UserId { get; init; }
}
