using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Domain.Events;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    ListEventsHandler listEventsHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<EventListItemResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<EventListItemResponse>>> List(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await listEventsHandler.HandleAsync(
                new ListEventsQuery(from, to, search),
                cancellationToken);

            return Ok(results
                .Select(result => new EventListItemResponse(
                    result.Id,
                    result.Title,
                    result.Description,
                    result.StartTime,
                    result.EndTime,
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
