using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpsTrack.Business.Common;

namespace OpsTrack.Api.Errors;

/// <summary>
/// Maps business exceptions to problem details (400 / 404 / 409).
/// Anything else is left to the default handler, which logs it and returns a 500 problem details.
/// </summary>
public sealed class OpsTrackExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public OpsTrackExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationFailedException => (StatusCodes.Status400BadRequest, "ข้อมูลไม่ถูกต้อง"),
            NotFoundException => (StatusCodes.Status404NotFound, "ไม่พบข้อมูล"),
            ConflictException => (StatusCodes.Status409Conflict, "สถานะงานไม่อนุญาตให้ทำรายการนี้"),
            _ => (0, null),
        };

        if (status == 0)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
        };

        if (exception is ValidationFailedException validation)
        {
            problem.Extensions["field"] = validation.Field;
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
