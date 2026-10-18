using Mediator;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class ActivityCanonicalEventConsumer : ICanonicalEventConsumer
{
    private readonly IMediator Mediator;

    public ActivityCanonicalEventConsumer(IMediator mediator)
    {
        Mediator = mediator;
    }

    public static string DurableName => "netptune-activity-canonical-v1";

    public static bool ReplaysHistory => true;

    public async ValueTask Handle(CanonicalEventEnvelope envelope, CancellationToken cancellationToken)
    {
        await Mediator.Send(envelope, cancellationToken);
    }
}
