using AppointmentScheduler.Application.Events.Attendance;
using AppointmentScheduler.Application.Events.Cancel;
using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Application.Events.Update;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Controllers;

[ApiController]
[Route("api/events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    ListEventsHandler listEventsHandler,
    UpdateEventHandler updateEventHandler,
    CancelEventHandler cancelEventHandler,
    SetAttendanceHandler setAttendanceHandler) : ControllerBase
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UpdateEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateEventResponse>> Update(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Attendees.Any(attendee => attendee is null))
        {
            ModelState.AddModelError("attendees", "Attendees must not contain null entries.");
            return ValidationProblem(ModelState);
        }

        var result = await updateEventHandler.HandleAsync(
            new UpdateEventCommand(
                id,
                request.Version,
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.Attendees
                    .Select(attendee => new UpdateAttendeeCommand(
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()),
            cancellationToken);

        return Ok(new UpdateEventResponse(
            result.Id,
            result.Title,
            result.Description,
            result.StartTime,
            result.EndTime,
            result.Version,
            result.Attendees
                .Select(attendee => new UpdatedAttendeeResponse(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray()));
    }

    [HttpPatch("{eventId:guid}/attendees/{attendeeId:guid}/attendance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetAttendance(
        Guid eventId,
        Guid attendeeId,
        SetAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        await setAttendanceHandler.HandleAsync(
            new SetAttendanceCommand(
                eventId,
                attendeeId,
                request.IsAttending!.Value,
                request.Version),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        await cancelEventHandler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType<CreateEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEventResponse>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Attendees.Any(attendee => attendee is null))
        {
            ModelState.AddModelError("attendees", "Attendees must not contain null entries.");
            return ValidationProblem(ModelState);
        }

        var result = await createEventHandler.HandleAsync(
            new CreateEventCommand(
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.Attendees
                    .Select(attendee => new CreateAttendeeCommand(
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()),
            cancellationToken);

        var response = new CreateEventResponse(
            result.Id,
            result.Title,
            result.Description,
            result.StartTime,
            result.EndTime,
            result.Version,
            result.Attendees
                .Select(attendee => new CreatedAttendeeResponse(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());

        return StatusCode(StatusCodes.Status201Created, response);
    }

}
