using FluentAssertions;

using Netptune.Import.Archive;
using Netptune.Transfer.Archive;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class DecompressionBudgetTests
{
    [Fact]
    public void Consume_ShouldAllow_WhenWithinTheMinimumBudget()
    {
        var budget = new DecompressionBudget(1024);

        var consume = () => budget.Consume(DecompressionBudget.MinimumBytes);

        consume.Should().NotThrow();
    }

    [Fact]
    public void Consume_ShouldThrow_WhenTheMinimumBudgetIsExceeded()
    {
        var budget = new DecompressionBudget(1024);

        budget.Consume(DecompressionBudget.MinimumBytes);

        var consume = () => budget.Consume(1);

        consume.Should().Throw<ArchiveSchemaException>();
    }

    [Fact]
    public void Consume_ShouldScaleWithCompressedSize_WhenArchiveIsLargerThanTheMinimum()
    {
        var compressedBytes = DecompressionBudget.MinimumBytes;
        var budget = new DecompressionBudget(compressedBytes);

        var consume = () => budget.Consume(compressedBytes * DecompressionBudget.MaxCompressionRatio);

        consume.Should().NotThrow();
    }

    [Fact]
    public void Consume_ShouldCountAcrossCalls_WhenABombIsSpreadOverManyEntries()
    {
        var budget = new DecompressionBudget(1024);
        var chunk = DecompressionBudget.MinimumBytes / 4;

        var consume = () =>
        {
            for (var index = 0; index < 5; index++)
            {
                budget.Consume(chunk);
            }
        };

        consume.Should().Throw<ArchiveSchemaException>();
    }
}
