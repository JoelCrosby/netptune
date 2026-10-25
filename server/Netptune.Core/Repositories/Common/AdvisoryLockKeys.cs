namespace Netptune.Core.Repositories.Common;

public static class AdvisoryLockKeys
{
    public const long WorkspaceFileReconciliation = 528_401_001;

    // Advisory lock keys are one bigint space; the high word keeps per-run review locks clear of the fixed keys.
    private const long AiReviewRunNamespace = 651L << 32;

    public static long AiReviewRun(int runId)
    {
        return AiReviewRunNamespace | (uint)runId;
    }
}
