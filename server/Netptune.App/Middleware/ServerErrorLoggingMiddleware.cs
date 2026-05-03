using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

namespace Netptune.App.Middleware;

public class ServerErrorLoggingMiddleware(RequestDelegate next)
{
    private const string ExceptionLoggedKey = "__NetptuneServerExceptionLogged";

    public async Task InvokeAsync(HttpContext context, ILogger<ServerErrorLoggingMiddleware> logger)
    {
        try
        {
            await next(context);

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                && !context.Items.ContainsKey(ExceptionLoggedKey))
            {
                LogServerError(logger, context);
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (BadHttpRequestException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = "Bad Request",
                Instance = context.Request.Path
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
        }
        catch (Exception exception)
        {
            context.Items[ExceptionLoggedKey] = true;

            LogUnhandledException(logger, context, exception);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Instance = context.Request.Path
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
        }
    }

    private static void LogServerError(ILogger logger, HttpContext context)
    {
        logger.LogError(
            "Request completed with a server error. {Method} {Path} responded {StatusCode}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            context.TraceIdentifier);
    }

    private static void LogUnhandledException(ILogger logger, HttpContext context, Exception exception)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing request. {Method} {Path} responded {StatusCode}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            StatusCodes.Status500InternalServerError,
            context.TraceIdentifier);
    }
}
