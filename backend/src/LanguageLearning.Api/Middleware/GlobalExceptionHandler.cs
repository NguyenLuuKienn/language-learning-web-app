
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BusinessRuleException rule when rule.Code == ErrorCodes.AuthInvalidCredentials => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                exception.Message),

            BusinessRuleException rule when rule.Code is "LESSON_CHECKPOINT_CONFLICT" or "LESSON_CONTENT_CHANGED" or "LESSON_STEP_IDS_IN_USE" or "LESSON_ACTIVITY_IN_USE" or "LESSON_STEP_POLICY_IN_USE" => (
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message),

            BusinessRuleException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message),

            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "A valid user identity is required."),

            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        problem.Extensions["code"] = exception switch
        {
            BusinessRuleException businessRule => businessRule.Code,
            ArgumentException => ErrorCodes.Validation,
            NotFoundException => "RESOURCE_NOT_FOUND",
            UnauthorizedAccessException => "UNAUTHORIZED",
            InvalidOperationException => "BUSINESS_RULE_ERROR",
            _ => "INTERNAL_ERROR"
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web),
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;

    }
}
