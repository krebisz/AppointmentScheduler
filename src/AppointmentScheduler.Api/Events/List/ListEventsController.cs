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
    [ProducesResponseType<IReadOnlyCollection<EventListItemResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<EventListItemResponse>>> List(
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
            .Select(result => new EventListItemResponse(
                result.Id,
                result.Title,
                result.Description,
                result.StartTime,
                result.EndTime,
                result.IsCancelled,
                result.Version,
                result.Attendees
                    .Select(attendee => new EventAttendeeResponse(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()))
            .ToArray());
    }
}
