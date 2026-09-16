namespace Netptune.Core.Requests;

public static class RequestLimits
{
    public const int MaxBulkIds = 500;

    public const int MaxBulkTags = 50;

    public const int MaxBulkAssignees = 50;

    public static string? DescribeBulkIdOverflow(int count)
    {
        return count > MaxBulkIds ? $"A maximum of {MaxBulkIds} ids can be supplied in one request." : null;
    }
}
