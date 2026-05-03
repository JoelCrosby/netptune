using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Netptune.App.Middleware;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Middleware;

public class ServerErrorLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_LogsUnhandledExceptionAsServerError()
    {
        var exception = new InvalidOperationException("test failure");
        var logger = new ListLogger<ServerErrorLoggingMiddleware>();
        var context = CreateHttpContext();
        var middleware = new ServerErrorLoggingMiddleware(_ => throw exception);

        await middleware.InvokeAsync(context, logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Exception == exception
            && entry.Message.Contains("responded 500"));
    }

    [Fact]
    public async Task InvokeAsync_LogsServerErrorResponseWhenNoExceptionWasThrown()
    {
        var logger = new ListLogger<ServerErrorLoggingMiddleware>();
        var context = CreateHttpContext();
        var middleware = new ServerErrorLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, logger);

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Exception == null
            && entry.Message.Contains("responded 500"));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-id"
        };

        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
