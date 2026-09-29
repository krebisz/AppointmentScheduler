using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Domain.Events;
using Microsoft.AspNetCore.Diagnostics;

namespace AppointmentScheduler.Controllers;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier };
        IResult response;
        switch (exception)
        {
            case DomainValidationException:
            case InvalidEventQueryException:
                var key = exception is InvalidEventQueryException ? "dateRange" : "event";
                response = Results.ValidationProblem(
                    new Dictionary<string, string[]> { [key] = [exception.Message] },
                    extensions: extensions);
                break;
            case EventNotFoundException:
                response = Results.Problem(statusCode: 404, title: "Event not found",
                    detail: exception.Message, extensions: extensions);
                break;
            case EventConcurrencyException:
                response = Results.Problem(statusCode: 409, title: "Event update conflict",
                    detail: exception.Message, extensions: extensions);
                break;
            default:
                logger.LogError(exception, "Unhandled API error. TraceId: {TraceId}", context.TraceIdentifier);
                response = Results.Problem(statusCode: 500, title: "An unexpected error occurred.",
                    detail: "The request could not be completed. Use the trace ID when reporting this error.",
                    extensions: extensions);
                break;
        }

        await response.ExecuteAsync(context);
        return true;
    }
}
