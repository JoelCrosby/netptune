namespace Netptune.Automation.Execution;

internal static class AutomationChainPolicy
{
    internal const int MaxDepth = 5;

    internal static bool HasReachedLimit(int chainDepth)
    {
        return chainDepth >= MaxDepth;
    }
}
