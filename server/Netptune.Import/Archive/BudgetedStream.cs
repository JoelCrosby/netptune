namespace Netptune.Import.Archive;

public sealed class BudgetedStream : Stream
{
    private readonly Stream Inner;
    private readonly DecompressionBudget Budget;

    public BudgetedStream(Stream inner, DecompressionBudget budget)
    {
        Inner = inner;
        Budget = budget;
    }

    public override bool CanRead => Inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => Inner.Length;

    public override long Position
    {
        get => Inner.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = Inner.Read(buffer, offset, count);

        Budget.Consume(read);

        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await Inner.ReadAsync(buffer, cancellationToken);

        Budget.Consume(read);

        return read;
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
