using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Webhooks;

namespace Netptune.Core.SourceControl;

public interface ISourceControlWebhookReader
{
    ScmWebhookVerification Verify(ScmWebhookRequest request, ScmConnection? routedConnection);

    ScmWebhookEnvelope? ReadEnvelope(ScmWebhookRequest request);

    IReadOnlyList<ScmEvent> Normalise(ScmWebhookEnvelope envelope, ScmConnection connection);
}
