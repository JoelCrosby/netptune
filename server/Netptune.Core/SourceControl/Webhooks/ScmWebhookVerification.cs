namespace Netptune.Core.SourceControl.Webhooks;

public enum ScmWebhookVerification
{
    Verified = 0,
    MissingSignature = 1,
    InvalidSignature = 2,
    NotConfigured = 3,
}
