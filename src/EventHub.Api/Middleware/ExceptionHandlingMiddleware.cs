using System.Net;
using System.Text.Json;

namespace EventHub.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found", exception.Message),
            FileNotFoundException => (HttpStatusCode.NotFound, "Resource not found", "Resource not found."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An error occurred", "An error occurred.")
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "about:blank",
            title,
            status = (int)status,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
