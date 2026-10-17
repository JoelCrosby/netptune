namespace Netptune.UnitTests.Netptune.SourceControl.Support;

public sealed class AdjustableTimeProvider : TimeProvider
{
    private DateTimeOffset Now;

    public AdjustableTimeProvider(DateTimeOffset now)
    {
        Now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }

    public void Advance(TimeSpan by)
    {
        Now += by;
    }
}
