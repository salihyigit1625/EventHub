using System.Text.Json;
using EventHub.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventHub.Tests.Api;

[TestFixture]
public class ExceptionHandlingMiddlewareTests
{
    [TestCase(typeof(KeyNotFoundException), 404, "Resource not found", "missing")]
    [TestCase(typeof(UnauthorizedAccessException), 401, "Unauthorized", "nope")]
    [TestCase(typeof(InvalidOperationException), 400, "Invalid operation", "sold out")]
    [TestCase(typeof(Exception), 500, "An error occurred", "boom")]
    public async Task MapsExceptionToProblemDetailsAndIncludesMessage(
        Type exceptionType,
        int status,
        string title,
        string message)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.That(context.Response.StatusCode, Is.EqualTo(status));
        Assert.That(context.Response.ContentType, Does.Contain("application/problem+json"));
        Assert.That(doc.RootElement.GetProperty("title").GetString(), Is.EqualTo(title));
        Assert.That(doc.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(status));
        Assert.That(doc.RootElement.GetProperty("detail").GetString(), Is.EqualTo(message));
    }

    [Test]
    public async Task DoesNotInterceptSuccessfulRequests()
    {
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = 204;
                return Task.CompletedTask;
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(204));
    }
}
