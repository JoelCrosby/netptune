using System.Text;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

using Netptune.App.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Services;

public class EventStreamResponseTests
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task OpenAsync_ShouldWriteHeartbeats_WhenNoFramesAreWritten()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new MemoryStream();
        var context = CreateContext(body);

        await using (await EventStreamResponse.OpenAsync(context.Response, HeartbeatInterval, cancellationToken))
        {
            await Task.Delay(HeartbeatInterval * 20, cancellationToken);
        }

        var written = Encoding.UTF8.GetString(body.ToArray());

        written.Should().Contain(": heartbeat\n\n");
        context.Response.ContentType.Should().Be("text/event-stream");
    }

    [Fact]
    public async Task OpenAsync_ShouldAbortTheRequest_WhenAHeartbeatCannotBeWritten()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = CreateContext(new UnwritableStream());
        var lifetime = Substitute.For<IHttpRequestLifetimeFeature>();
        var aborted = new TaskCompletionSource();

        lifetime.When(feature => feature.Abort()).Do(_ => aborted.TrySetResult());
        context.Features.Set(lifetime);

        await using (await EventStreamResponse.OpenAsync(context.Response, HeartbeatInterval, cancellationToken))
        {
            await aborted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }

        aborted.Task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_ShouldWriteTheFrame_WhenTheStreamIsOpen()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new MemoryStream();
        var context = CreateContext(body);

        await using (var stream = await EventStreamResponse.OpenAsync(context.Response, cancellationToken))
        {
            await stream.WriteAsync("data: {}\n\n", cancellationToken);
        }

        var written = Encoding.UTF8.GetString(body.ToArray());

        written.Should().Be("data: {}\n\n");
    }

    private static DefaultHttpContext CreateContext(Stream body)
    {
        return new DefaultHttpContext
        {
            Response = { Body = body },
        };
    }

    private sealed class UnwritableStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("Connection reset");

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new IOException("Connection reset");
        }
    }
}
