
using System.Text.Json;
using LanguageLearning.Api.Middleware;
using LanguageLearning.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task NotFoundException_ReturnsProblemDetails404()
    {
        var logger = new Mock<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new NotFoundException(
            "The requested deck does not exist or is not accessible.");

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.StartsWith(
            "application/problem+json",
            context.Response.ContentType);

        context.Response.Body.Position = 0;

        using var document = await JsonDocument.ParseAsync(
            context.Response.Body);

        var root = document.RootElement;

        Assert.Equal(
            404,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "Not Found",
            root.GetProperty("title").GetString());

        Assert.Equal(
            exception.Message,
            root.GetProperty("detail").GetString());
    }

[Fact]
public async Task ArgumentException_ReturnsProblemDetails400()
{
    var logger = new Mock<ILogger<GlobalExceptionHandler>>();
    var handler = new GlobalExceptionHandler(logger.Object);

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    var exception = new ArgumentException(
        "Deck title is required.");

    var handled = await handler.TryHandleAsync(
        context,
        exception,
        CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(400, context.Response.StatusCode);

    Assert.StartsWith(
        "application/problem+json",
        context.Response.ContentType);

    context.Response.Body.Position = 0;

    using var document = await JsonDocument.ParseAsync(
        context.Response.Body);

    var root = document.RootElement;

    Assert.Equal(
        400,
        root.GetProperty("status").GetInt32());

    Assert.Equal(
        "Bad Request",
        root.GetProperty("title").GetString());

    Assert.Equal(
        "Deck title is required.",
        root.GetProperty("detail").GetString());
}

[Fact]
public async Task UnauthorizedAccessException_ReturnsProblemDetails401()
{
    var logger = new Mock<ILogger<GlobalExceptionHandler>>();
    var handler = new GlobalExceptionHandler(logger.Object);

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    var exception = new UnauthorizedAccessException(
        "User is not authenticated.");

    var handled = await handler.TryHandleAsync(
        context,
        exception,
        CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(401, context.Response.StatusCode);

    Assert.StartsWith(
        "application/problem+json",
        context.Response.ContentType);

    context.Response.Body.Position = 0;

    using var document = await JsonDocument.ParseAsync(
        context.Response.Body);

    var root = document.RootElement;

    Assert.Equal(
        401,
        root.GetProperty("status").GetInt32());

    Assert.Equal(
        "Unauthorized",
        root.GetProperty("title").GetString());

    Assert.Equal(
        "A valid user identity is required.",
        root.GetProperty("detail").GetString());
}

[Fact]
public async Task UnexpectedException_ReturnsProblemDetails500()
{
    var logger = new Mock<ILogger<GlobalExceptionHandler>>();
    var handler = new GlobalExceptionHandler(logger.Object);

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    var exception = new Exception(
        "Sensitive database connection information.");

    var handled = await handler.TryHandleAsync(
        context,
        exception,
        CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(500, context.Response.StatusCode);

    Assert.StartsWith(
        "application/problem+json",
        context.Response.ContentType);

    context.Response.Body.Position = 0;

    using var document = await JsonDocument.ParseAsync(
        context.Response.Body);

    var root = document.RootElement;

    Assert.Equal(
        500,
        root.GetProperty("status").GetInt32());

    Assert.Equal(
        "Internal Server Error",
        root.GetProperty("title").GetString());

    Assert.Equal(
        "An unexpected error occurred.",
        root.GetProperty("detail").GetString());

    Assert.DoesNotContain(
        "Sensitive database connection information.",
        root.ToString());
}

[Fact]
public async Task ConflictException_ReturnsProblemDetails409()
{
    var logger = new Mock<ILogger<GlobalExceptionHandler>>();
    var handler = new GlobalExceptionHandler(logger.Object);

    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    var exception = new ConflictException(
        "Không thể xóa Flashcard đã có lịch sử ôn tập.");

    var handled = await handler.TryHandleAsync(
        context,
        exception,
        CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(409, context.Response.StatusCode);

    Assert.StartsWith(
        "application/problem+json",
        context.Response.ContentType);

    context.Response.Body.Position = 0;

    using var document = await JsonDocument.ParseAsync(
        context.Response.Body);

    var root = document.RootElement;

    Assert.Equal(409, root.GetProperty("status").GetInt32());
    Assert.Equal("Conflict", root.GetProperty("title").GetString());
    Assert.Equal(exception.Message, root.GetProperty("detail").GetString());
}

}
