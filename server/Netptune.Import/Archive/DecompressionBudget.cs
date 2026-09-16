using Netptune.Transfer.Archive;

namespace Netptune.Import.Archive;

public sealed class DecompressionBudget
{
    public const long MinimumBytes = 256L * 1024 * 1024;

    public const int MaxCompressionRatio = 50;

    private long Remaining;

    public DecompressionBudget(long compressedBytes)
    {
        Remaining = Math.Max(MinimumBytes, compressedBytes * MaxCompressionRatio);
    }

    public void Consume(long bytes)
    {
        Remaining -= bytes;

        if (Remaining < 0)
        {
            throw new ArchiveSchemaException("The archive expands to more data than its size allows.");
        }
    }
}
