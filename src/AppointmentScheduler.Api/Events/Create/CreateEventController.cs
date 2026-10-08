using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Api.Events.Get;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.Create;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CreateEventController(CreateEventHandler createEventHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEventResponse>> CreateAsync(
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
                    .Select(attendee => new CreateAttendeeInput(
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
                .Select(attendee => new CreateAttendeeResponse(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());

        // Give callers a resolvable Location for discovering the newly created resource.
        return CreatedAtRoute(GetEventController.RouteName, new { id = result.Id }, response);
    }
}
