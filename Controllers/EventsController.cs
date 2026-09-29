using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Application.Events.Cancel;
using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Application.Events.Update;
using AppointmentScheduler.Domain.Events;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    ListEventsHandler listEventsHandler,
    UpdateEventHandler updateEventHandler,
    CancelEventHandler cancelEventHandler) : ControllerBase
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
        try
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
                    result.Attendees
                        .Select(attendee => new EventAttendeeResponse(
                            attendee.Id,
                            attendee.Name,
                            attendee.EmailAddress,
                            attendee.IsAttending))
                        .ToArray()))
                .ToArray());
        }
        catch (InvalidEventQueryException exception)
        {
            ModelState.AddModelError("dateRange", exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UpdateEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateEventResponse>> Update(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await updateEventHandler.HandleAsync(
                new UpdateEventCommand(
                    id,
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
                result.Attendees
                    .Select(attendee => new UpdatedAttendeeResponse(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()));
        }
        catch (EventNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Event not found",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddModelError("event", exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await cancelEventHandler.HandleAsync(id, cancellationToken);
            return NoContent();
        }
        catch (EventNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Event not found",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPost]
    [ProducesResponseType<CreateEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEventResponse>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
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
                result.Attendees
                    .Select(attendee => new CreatedAttendeeResponse(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray());

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddModelError("event", exception.Message);
            return ValidationProblem(ModelState);
        }
    }
}
