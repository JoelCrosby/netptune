namespace Netptune.Core.SourceControl;

public interface ISourceControlProvider
{
    SourceControlProvider Provider { get; }

    SourceControlCapabilities Capabilities { get; }

    ScmTerminology Terminology { get; }

    ISourceControlConnector Connector { get; }

    ISourceControlClient Client { get; }

    ISourceControlWebhookReader? WebhookReader { get; }
}
