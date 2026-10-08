using AppointmentScheduler.Application.Events.List;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.List;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ListEventsController(ListEventsHandler listEventsHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ListEventItemResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ListEventItemResponse>>> ListAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? search,
        [FromQuery] bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var results = await listEventsHandler.HandleAsync(
            new ListEventsQuery(from, to, search, includeCancelled),
            cancellationToken);

        return Ok(results
            .Select(result => new ListEventItemResponse(
                result.Id,
                result.Title,
                result.Description,
                result.StartTime,
                result.EndTime,
                result.IsCancelled,
                result.Version,
                result.Attendees
                    .Select(attendee => new ListAttendeeResponse(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()))
            .ToArray());
    }
}
