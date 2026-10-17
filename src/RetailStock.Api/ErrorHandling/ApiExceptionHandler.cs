using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common;
using RetailStock.Domain.Common;

namespace RetailStock.Api.ErrorHandling;

/// <summary>Translates application and domain exceptions into RFC 7807 problem details.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "not_found"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrent modification", "concurrency_conflict"),
            DomainException domain => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", domain.Code),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Invalid request", "bad_request"),
            _ => (0, string.Empty, string.Empty),
        };

        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Extensions = { ["code"] = code },
            },
        });
    }
}
