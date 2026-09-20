using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.SharedKernel;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Middleware;

/// <summary>
/// Centralized translation of exceptions to RFC 7807 ProblemDetails. Known, expected exception types
/// (validation, not-found, forbidden, conflict, domain rule violations) surface their own message —
/// those messages are always business-facing text, never internal detail. Anything else is logged as an
/// error and returned as a generic 500 with no internal detail, keyed by correlation id.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.TraceIdentifier;

        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred.", (string?)null),
            ForbiddenAccessException e => (StatusCodes.Status403Forbidden, "Access to this resource is forbidden.", e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "The requested resource was not found.", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "The request conflicts with the current state.", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by someone else. Reload and try again.", null),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "A business rule was violated.", e.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access to this resource is forbidden.", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception for request {Method} {Path} (correlation {CorrelationId})",
                httpContext.Request.Method, httpContext.Request.Path, correlationId);
        }
        else
        {
            logger.LogWarning(
                "{ExceptionType} for request {Method} {Path} (correlation {CorrelationId}): {Message}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, correlationId, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Extensions = { ["correlationId"] = correlationId },
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
